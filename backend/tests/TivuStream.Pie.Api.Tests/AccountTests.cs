using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Routing;
using TivuStream.Pie.Storage;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies the management of accounts and the change of one's own password.
/// </summary>
/// <remarks>
/// Authentication Specification, API Contract and V7: an administrator able
/// to sign in always exists; a password set by someone else must be changed
/// before anything else, and cannot be changed into itself; a session ends
/// when the password changes or the account is disabled or removed.
/// </remarks>
public sealed class AccountTests : IDisposable
{
    private const string NewPassword = "a-new-password-for-the-tests";

    private readonly PieApplication _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    // ------------------------------------------------------------------
    // V7: an administrator always exists
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("PATCH", """{"enabled":false}""")]
    [InlineData("PATCH", """{"role":"Viewer"}""")]
    [InlineData("DELETE", null)]
    public async Task The_last_administrator_cannot_be_disabled_demoted_or_removed(string method, string? body)
    {
        // The administrator acts on the only administrator there is: its own
        // account. A disabled administrator does not count as a way in.
        HttpClient administrator = await _app.AdministratorAsync();
        _app.AddAccount("spare", AccountRole.Administrator);
        await SendAsync(administrator, HttpMethod.Patch, "/api/v1/accounts/spare", new { enabled = false });

        Answer answer = await SendAsync(administrator, new HttpMethod(method), "/api/v1/accounts/tester", Json(body));

        Assert.Equal(HttpStatusCode.Conflict, answer.Status);
        Assert.Equal("LastAdministrator", Code(answer));

        StoredAccount tester = _app.Account("tester")!;
        Assert.Equal(AccountRole.Administrator, tester.Role);
        Assert.True(tester.Enabled);

        // And the session it was asked from still works.
        Assert.Equal(HttpStatusCode.OK, (await _app.GetAsync("/api/v1/auth/session")).Status);
    }

    [Fact]
    public async Task An_administrator_can_step_down_while_another_can_sign_in()
    {
        HttpClient administrator = await _app.AdministratorAsync();
        _app.AddAccount("root", AccountRole.Administrator);

        Answer answer = await SendAsync(administrator, HttpMethod.Patch, "/api/v1/accounts/tester", new { role = "Viewer" });

        Assert.Equal(HttpStatusCode.OK, answer.Status);
        Assert.Equal("Viewer", answer.Body.GetProperty("data").GetProperty("role").GetString());

        // Effective at once, on the same session.
        Assert.Equal(HttpStatusCode.Forbidden, (await _app.GetAsync("/api/v1/accounts")).Status);
    }

    // ------------------------------------------------------------------
    // Listing and creating
    // ------------------------------------------------------------------

    [Fact]
    public async Task The_list_describes_every_account_and_never_carries_a_password_hash()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        Answer answer = await _app.GetAsync("/api/v1/accounts");

        JsonElement[] accounts = answer.Body.GetProperty("data").EnumerateArray().ToArray();

        Assert.Equal(["maria", "tester"], accounts.Select(account => account.GetProperty("username").GetString()));

        JsonElement maria = accounts[0];
        Assert.Equal("Viewer", maria.GetProperty("role").GetString());
        Assert.True(maria.GetProperty("enabled").GetBoolean());
        Assert.False(maria.GetProperty("passwordChangeRequired").GetBoolean());
        Assert.Equal(PieApplication.Now, maria.GetProperty("createdAt").GetDateTimeOffset());

        Assert.DoesNotContain("pbkdf2", answer.Raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hash", answer.Raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_created_account_can_sign_in_and_must_change_its_password_first()
    {
        HttpClient administrator = await _app.AdministratorAsync();

        Answer created = await SendAsync(
            administrator,
            HttpMethod.Post,
            "/api/v1/accounts",
            new { username = " Maria ", role = "Viewer", password = PieApplication.Password });

        Assert.Equal(HttpStatusCode.Created, created.Status);

        JsonElement data = created.Body.GetProperty("data");
        Assert.Equal("maria", data.GetProperty("username").GetString());
        Assert.True(data.GetProperty("passwordChangeRequired").GetBoolean());

        using HttpClient maria = await SignInAsync("maria");

        Answer health = await _app.GetAsync("/api/v1/health", maria);

        Assert.Equal(HttpStatusCode.Forbidden, health.Status);
        Assert.Equal("PasswordChangeRequired", Code(health));
    }

    [Fact]
    public async Task A_name_already_taken_is_refused_whatever_its_case()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        Answer answer = await SendAsync(
            await _app.AdministratorAsync(),
            HttpMethod.Post,
            "/api/v1/accounts",
            new { username = "MARIA", role = "Administrator", password = PieApplication.Password });

        Assert.Equal(HttpStatusCode.Conflict, answer.Status);
        Assert.Equal("AccountExists", Code(answer));
        Assert.Equal(AccountRole.Viewer, _app.Account("maria")!.Role);
    }

    [Theory]
    [InlineData("ab", "Viewer", PieApplication.Password, "UsernameRejected", null)]
    [InlineData("maria rossi", "Viewer", PieApplication.Password, "UsernameRejected", null)]
    [InlineData("maria", null, PieApplication.Password, "RoleRejected", null)]
    [InlineData("maria", "Root", PieApplication.Password, "RoleRejected", null)]
    [InlineData("maria", "viewer", PieApplication.Password, "RoleRejected", null)]
    [InlineData("maria", "0", PieApplication.Password, "RoleRejected", null)]
    [InlineData("maria", "Viewer", "short", "PasswordRejected", "TooShort")]
    [InlineData("maria", "Viewer", null, "PasswordRejected", "TooShort")]
    [InlineData("maria.rossi.12", "Viewer", "maria.rossi.12", "PasswordRejected", "EqualsUsername")]
    public async Task An_unacceptable_account_is_refused_and_nothing_is_created(
        string username,
        string? role,
        string? password,
        string code,
        string? reason)
    {
        Answer answer = await SendAsync(
            await _app.AdministratorAsync(),
            HttpMethod.Post,
            "/api/v1/accounts",
            new { username, role, password });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, answer.Status);
        Assert.Equal(code, Code(answer));
        Assert.Equal(reason, Reason(answer));

        // Only the administrator the test signed in with.
        Assert.Single((await _app.GetAsync("/api/v1/accounts")).Body.GetProperty("data").EnumerateArray());
    }

    // ------------------------------------------------------------------
    // Changing and removing
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task An_account_that_does_not_exist_is_reported_as_such(string method)
    {
        Answer answer = await SendAsync(
            await _app.AdministratorAsync(),
            new HttpMethod(method),
            "/api/v1/accounts/nobody",
            method == "PATCH" ? new { enabled = false } : null);

        Assert.Equal(HttpStatusCode.NotFound, answer.Status);
        Assert.Equal("AccountNotFound", Code(answer));
    }

    [Fact]
    public async Task A_change_with_nothing_in_it_changes_nothing_and_returns_the_account()
    {
        StoredAccount before = _app.AddAccount("maria", AccountRole.Viewer);

        Answer answer = await SendAsync(await _app.AdministratorAsync(), HttpMethod.Patch, "/api/v1/accounts/maria", new { });

        Assert.Equal(HttpStatusCode.OK, answer.Status);
        Assert.Equal("maria", answer.Body.GetProperty("data").GetProperty("username").GetString());
        Assert.Equal(before, _app.Account("maria"));
    }

    [Theory]
    [InlineData("""{"role":"Administrator","password":"short"}""", "PasswordRejected")]
    [InlineData("""{"enabled":false,"role":"Root"}""", "RoleRejected")]
    public async Task A_change_is_applied_whole_or_not_at_all(string body, string code)
    {
        StoredAccount before = _app.AddAccount("maria", AccountRole.Viewer);

        Answer answer = await SendAsync(await _app.AdministratorAsync(), HttpMethod.Patch, "/api/v1/accounts/maria", Json(body));

        Assert.Equal(code, Code(answer));
        Assert.Equal(before, _app.Account("maria"));
    }

    [Fact]
    public async Task A_refusal_for_the_last_administrator_keeps_the_password_carried_with_it()
    {
        HttpClient administrator = await _app.AdministratorAsync();
        string hash = _app.Account("tester")!.PasswordHash;

        Answer answer = await SendAsync(
            administrator,
            HttpMethod.Patch,
            "/api/v1/accounts/tester",
            new { role = "Viewer", password = NewPassword });

        Assert.Equal("LastAdministrator", Code(answer));
        Assert.Equal(hash, _app.Account("tester")!.PasswordHash);
    }

    [Fact]
    public async Task A_reset_by_an_administrator_ends_every_session_of_the_account_and_must_be_followed_by_a_change()
    {
        using HttpClient maria = await _app.SignedInAsync(AccountRole.Viewer, "maria");

        Answer reset = await SendAsync(
            await _app.AdministratorAsync(),
            HttpMethod.Patch,
            "/api/v1/accounts/maria",
            new { password = NewPassword });

        Assert.Equal(HttpStatusCode.OK, reset.Status);
        Assert.True(reset.Body.GetProperty("data").GetProperty("passwordChangeRequired").GetBoolean());

        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.GetAsync("/api/v1/auth/session", maria)).Status);

        // The old password no longer opens it, the new one does.
        Assert.Equal(HttpStatusCode.Unauthorized, (await LogInAsync("maria", PieApplication.Password)).Status);
        Assert.Equal(HttpStatusCode.OK, (await LogInAsync("maria", NewPassword)).Status);
    }

    [Fact]
    public async Task A_reset_of_ones_own_account_ends_ones_own_session_too()
    {
        HttpClient administrator = await _app.AdministratorAsync();

        Answer reset = await SendAsync(administrator, HttpMethod.Patch, "/api/v1/accounts/tester", new { password = NewPassword });

        Assert.Equal(HttpStatusCode.OK, reset.Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.GetAsync("/api/v1/auth/session")).Status);
    }

    [Fact]
    public async Task Disabling_an_account_ends_its_sessions_and_enabling_it_again_does_not_bring_them_back()
    {
        using HttpClient maria = await _app.SignedInAsync(AccountRole.Viewer, "maria");
        HttpClient administrator = await _app.AdministratorAsync();

        Answer disabled = await SendAsync(administrator, HttpMethod.Patch, "/api/v1/accounts/maria", new { enabled = false });

        Assert.False(disabled.Body.GetProperty("data").GetProperty("enabled").GetBoolean());

        await SendAsync(administrator, HttpMethod.Patch, "/api/v1/accounts/maria", new { enabled = true });

        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.GetAsync("/api/v1/auth/session", maria)).Status);
    }

    [Fact]
    public async Task A_removed_account_can_no_longer_do_anything()
    {
        using HttpClient maria = await _app.SignedInAsync(AccountRole.Viewer, "maria");

        Answer removed = await SendAsync(await _app.AdministratorAsync(), HttpMethod.Delete, "/api/v1/accounts/MARIA");

        Assert.Equal(HttpStatusCode.OK, removed.Status);
        Assert.Null(_app.Account("maria"));
        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.GetAsync("/api/v1/auth/session", maria)).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LogInAsync("maria", PieApplication.Password)).Status);
    }

    // ------------------------------------------------------------------
    // A password that must be changed comes before anything else
    // ------------------------------------------------------------------

    [Fact]
    public async Task While_the_password_must_be_changed_only_the_session_the_change_and_leaving_are_reachable()
    {
        // An administrator: the refusal must be about the password even where
        // the role would allow the request.
        _app.AddAccount("root", AccountRole.Administrator, passwordChangeRequired: true);

        using HttpClient root = await SignInAsync("root");

        string[] reachable = ["/api/v1/auth/logout", "/api/v1/auth/password", "/api/v1/auth/session"];

        List<RouteEndpoint> endpoints = _app.Endpoints()
            .Where(endpoint => !PieApplication.IsAnonymous(endpoint) && !reachable.Contains(endpoint.RoutePattern.RawText))
            .ToList();

        Assert.True(endpoints.Count >= 10, "the host should expose its endpoints");

        foreach (RouteEndpoint endpoint in endpoints)
        {
            HttpMethod method = PieApplication.MethodOf(endpoint);

            Answer answer = await SendAsync(root, method, PieApplication.ConcretePath(endpoint), method == HttpMethod.Post || method == HttpMethod.Patch ? new { } : null);

            Assert.True(answer.Status == HttpStatusCode.Forbidden, $"{endpoint.RoutePattern.RawText} answered {answer.Status}");
            Assert.Equal("PasswordChangeRequired", Code(answer));
        }

        Answer session = await _app.GetAsync("/api/v1/auth/session", root);

        Assert.Equal(HttpStatusCode.OK, session.Status);
        Assert.True(session.Body.GetProperty("data").GetProperty("passwordChangeRequired").GetBoolean());
    }

    [Fact]
    public async Task The_password_comes_before_what_the_role_would_not_allow()
    {
        _app.AddAccount("maria", AccountRole.Viewer, passwordChangeRequired: true);

        using HttpClient maria = await SignInAsync("maria");

        Assert.Equal("PasswordChangeRequired", Code(await _app.GetAsync("/api/v1/devices", maria)));
    }

    // ------------------------------------------------------------------
    // Changing one's own password
    // ------------------------------------------------------------------

    [Fact]
    public async Task Changing_the_password_lifts_the_obligation_keeps_this_session_and_ends_the_others()
    {
        _app.AddAccount("maria", AccountRole.Viewer, passwordChangeRequired: true);

        using HttpClient here = await SignInAsync("maria");
        using HttpClient elsewhere = await SignInAsync("maria");

        Answer changed = await SendAsync(
            here,
            HttpMethod.Post,
            "/api/v1/auth/password",
            new { currentPassword = PieApplication.Password, newPassword = NewPassword });

        Assert.Equal(HttpStatusCode.OK, changed.Status);
        Assert.False(changed.Body.GetProperty("data").GetProperty("passwordChangeRequired").GetBoolean());

        Assert.Equal(HttpStatusCode.OK, (await _app.GetAsync("/api/v1/health", here)).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _app.GetAsync("/api/v1/auth/session", elsewhere)).Status);

        Assert.Equal(HttpStatusCode.Unauthorized, (await LogInAsync("maria", PieApplication.Password)).Status);
        Assert.Equal(HttpStatusCode.OK, (await LogInAsync("maria", NewPassword)).Status);
    }

    [Theory]
    [InlineData("not the current password", NewPassword, HttpStatusCode.Forbidden, "CurrentPasswordRejected", null)]
    [InlineData(null, NewPassword, HttpStatusCode.Forbidden, "CurrentPasswordRejected", null)]
    [InlineData(PieApplication.Password, PieApplication.Password, HttpStatusCode.UnprocessableEntity, "PasswordRejected", "Unchanged")]
    [InlineData(PieApplication.Password, "short", HttpStatusCode.UnprocessableEntity, "PasswordRejected", "TooShort")]
    [InlineData(PieApplication.Password, null, HttpStatusCode.UnprocessableEntity, "PasswordRejected", "TooShort")]
    public async Task A_refused_change_of_password_changes_nothing_and_keeps_the_session(
        string? currentPassword,
        string? newPassword,
        HttpStatusCode status,
        string code,
        string? reason)
    {
        StoredAccount before = _app.AddAccount("maria", AccountRole.Viewer, passwordChangeRequired: true);

        using HttpClient maria = await SignInAsync("maria");

        Answer answer = await SendAsync(maria, HttpMethod.Post, "/api/v1/auth/password", new { currentPassword, newPassword });

        Assert.Equal(status, answer.Status);
        Assert.Equal(code, Code(answer));
        Assert.Equal(reason, Reason(answer));
        Assert.Equal(before, _app.Account("maria"));

        // Not a 401: the session is valid, and the interface would read a 401
        // as the session having ended.
        Assert.Equal(HttpStatusCode.OK, (await _app.GetAsync("/api/v1/auth/session", maria)).Status);
    }

    [Fact]
    public async Task The_same_password_is_recognised_however_it_is_written()
    {
        // "ﬁ" is one character that NFKC turns into "fi". Written either way,
        // it is the same password, and so no change.
        _app.AddAccount("maria", AccountRole.Viewer, "a-password-fine-for-tests", passwordChangeRequired: true);

        using HttpClient maria = await SignInAsync("maria", "a-password-fine-for-tests");

        Answer answer = await SendAsync(
            maria,
            HttpMethod.Post,
            "/api/v1/auth/password",
            new { currentPassword = "a-password-fine-for-tests", newPassword = "a-password-ﬁne-for-tests" });

        Assert.Equal("Unchanged", Reason(answer));
    }

    // ------------------------------------------------------------------

    private async Task<HttpClient> SignInAsync(string username, string password = PieApplication.Password)
    {
        HttpClient client = _app.NewClient();

        Answer answer = await SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", new { username, password });

        Assert.Equal(HttpStatusCode.OK, answer.Status);

        return client;
    }

    private async Task<Answer> LogInAsync(string username, string password)
    {
        using HttpClient client = _app.NewClient();

        return await SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", new { username, password });
    }

    private static Task<Answer> SendAsync(HttpClient client, HttpMethod method, string path, object? body = null)
    {
        return PieApplication.SendAsync(client, method, path, body);
    }

    private static JsonElement? Json(string? body)
    {
        return body is null ? null : JsonDocument.Parse(body).RootElement.Clone();
    }

    private static string? Code(Answer answer)
    {
        return answer.Body.GetProperty("error").GetProperty("code").GetString();
    }

    private static string? Reason(Answer answer)
    {
        return answer.Body.GetProperty("error").TryGetProperty("reason", out JsonElement reason) ? reason.GetString() : null;
    }
}
