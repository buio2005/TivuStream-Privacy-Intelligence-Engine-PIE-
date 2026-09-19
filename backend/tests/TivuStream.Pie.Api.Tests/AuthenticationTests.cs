using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using TivuStream.Pie.Api.Authentication;
using TivuStream.Pie.Storage;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies that nothing answers without an identity, and what an identity
/// is allowed.
/// </summary>
/// <remarks>
/// Authentication Specification: access is refused by default (V1, V2), a
/// refusal teaches nothing (V3), a session ends when it should (V5), and the
/// answers are not kept (V12).
/// </remarks>
public sealed class AuthenticationTests : IDisposable
{
    private readonly PieApplication _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    // ------------------------------------------------------------------
    // V1, V2: refused by default
    // ------------------------------------------------------------------

    [Fact]
    public async Task Every_endpoint_of_the_host_refuses_a_caller_who_has_not_signed_in()
    {
        List<RouteEndpoint> endpoints = Endpoints();

        // A host that reports no endpoints would make every check below pass
        // by having nothing to check.
        Assert.True(endpoints.Count >= 8, "the host should expose its endpoints");

        foreach (RouteEndpoint endpoint in endpoints.Where(endpoint => !IsAnonymous(endpoint)))
        {
            using HttpClient client = _app.NewClient();

            Answer answer = await PieApplication.SendAsync(client, MethodOf(endpoint), ConcretePath(endpoint));

            Assert.True(answer.Status == HttpStatusCode.Unauthorized, $"{endpoint.RoutePattern.RawText} answered {answer.Status}");
            Assert.Equal("AuthenticationRequired", answer.Body.GetProperty("error").GetProperty("code").GetString());
        }
    }

    [Fact]
    public void The_only_endpoint_reachable_without_credentials_is_the_one_that_obtains_them()
    {
        string[] open = Endpoints()
            .Where(IsAnonymous)
            .Select(endpoint => endpoint.RoutePattern.RawText!)
            .Order()
            .ToArray();

        // Adding an endpoint that opens itself to everyone must fail here, and
        // ask whoever added it to say why.
        Assert.Equal(["/api/v1/auth/login"], open);
    }

    [Fact]
    public void Every_endpoint_declares_the_role_it_requires()
    {
        foreach (RouteEndpoint endpoint in Endpoints())
        {
            bool declared = IsAnonymous(endpoint) || endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0;

            Assert.True(declared, $"{endpoint.RoutePattern.RawText} declares neither a role nor that it is open");
        }
    }

    [Fact]
    public async Task An_endpoint_that_declares_nothing_requires_an_administrator()
    {
        IAuthorizationPolicyProvider provider = _app.Services.GetRequiredService<IAuthorizationPolicyProvider>();
        IAuthorizationService authorization = _app.Services.GetRequiredService<IAuthorizationService>();

        AuthorizationPolicy fallback = (await provider.GetFallbackPolicyAsync())!;

        Assert.NotNull(fallback);

        // Forgetting a declaration must produce a refusal and never an opening.
        Assert.False((await authorization.AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, fallback)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal("Viewer"), null, fallback)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal("Administrator"), null, fallback)).Succeeded);
    }

    // ------------------------------------------------------------------
    // V3: a refusal teaches nothing
    // ------------------------------------------------------------------

    [Fact]
    public async Task A_refused_sign_in_says_the_same_thing_whatever_the_reason()
    {
        _app.AddAccount("maria", AccountRole.Viewer);
        _app.AddAccount("disabled-user", AccountRole.Viewer);
        Disable("disabled-user");

        (string Name, object Body)[] attempts =
        [
            ("no such account", new { username = "nobody", password = PieApplication.Password }),
            ("wrong password", new { username = "maria", password = "not the password at all" }),
            ("disabled account, right password", new { username = "disabled-user", password = PieApplication.Password }),
            ("empty name", new { username = string.Empty, password = PieApplication.Password }),
            ("nothing at all", new { }),
        ];

        List<(string Status, string? Code, string? Message)> answers = [];

        foreach ((string name, object body) in attempts)
        {
            using HttpClient client = _app.NewClient();

            Answer answer = await PieApplication.SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", body);

            JsonElement error = answer.Body.GetProperty("error");

            answers.Add((answer.Status.ToString(), error.GetProperty("code").GetString(), error.GetProperty("message").GetString()));

            Assert.False(answer.Headers.Contains("Set-Cookie"), $"{name} must not open a session");
        }

        Assert.Single(answers.Distinct());
        Assert.Equal(("Unauthorized", "AuthenticationFailed"), (answers[0].Status, answers[0].Code));
    }

    [Theory]
    [InlineData("{ this is not json")]
    [InlineData("{\"username\":\"maria\",\"password\":\"\\ud800\"}")]
    [InlineData("[]")]
    public async Task A_sign_in_body_that_cannot_be_read_is_a_client_error_and_not_a_fault(string body)
    {
        using HttpClient client = _app.NewClient();
        using StringContent content = new(body, Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/auth/login", UriKind.Relative), content);

        // A password with characters that are not valid text can never match
        // one that was accepted. It must be refused, not crash the request.
        Assert.InRange((int)response.StatusCode, 400, 499);
    }

    // ------------------------------------------------------------------
    // Signing in
    // ------------------------------------------------------------------

    [Fact]
    public async Task Signing_in_says_who_is_signed_in_and_when_it_would_end()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        using HttpClient client = _app.NewClient();

        Answer answer = await PieApplication.SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", Credentials("maria"));

        JsonElement data = answer.Body.GetProperty("data");

        Assert.Equal(HttpStatusCode.OK, answer.Status);
        Assert.Equal("maria", data.GetProperty("username").GetString());
        Assert.Equal("Viewer", data.GetProperty("role").GetString());
        Assert.False(data.GetProperty("passwordChangeRequired").GetBoolean());
        Assert.Equal(PieApplication.Now + SessionService.IdleTimeout, data.GetProperty("expiresAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task The_name_is_accepted_however_it_is_written()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        using HttpClient client = _app.NewClient();

        Answer answer = await PieApplication.SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", Credentials("  MARIA "));

        Assert.Equal(HttpStatusCode.OK, answer.Status);
    }

    [Fact]
    public async Task The_cookie_cannot_be_read_by_the_page_and_goes_only_where_it_is_needed()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        using HttpClient client = _app.NewClient();

        Answer answer = await PieApplication.SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", Credentials("maria"));

        string cookie = answer.Headers.GetValues("Set-Cookie").Single().ToLowerInvariant();

        Assert.StartsWith("pie_session=", cookie, StringComparison.Ordinal);
        Assert.Contains("httponly", cookie, StringComparison.Ordinal);
        Assert.Contains("samesite=strict", cookie, StringComparison.Ordinal);
        Assert.Contains("path=/api", cookie, StringComparison.Ordinal);

        // No expiry of its own: it ends with the browser.
        Assert.DoesNotContain("expires", cookie, StringComparison.Ordinal);
        Assert.DoesNotContain("max-age", cookie, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_identifier_is_kept_only_as_a_hash()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        using HttpClient client = _app.NewClient();

        Answer answer = await PieApplication.SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", Credentials("maria"));

        string token = TokenFrom(answer);
        byte[] file = ReadDatabaseFile();

        // Positive control: the hash is in the file, so the search is looking at
        // the right place. Computed here from the specification, SHA-256 in
        // hexadecimal, and not by asking the code under test.
        string expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        Assert.True(Contains(file, expected), "the hash should be in the file");

        // Whoever reads the database cannot use a session that is still open.
        Assert.False(Contains(file, token), "the identifier must not be in the file");
    }

    [Fact]
    public async Task The_session_endpoint_says_who_is_signed_in()
    {
        using HttpClient client = await _app.SignedInAsync(AccountRole.Viewer, "maria");

        Answer answer = await _app.GetAsync("/api/v1/auth/session", client);

        Assert.Equal(HttpStatusCode.OK, answer.Status);
        Assert.Equal("maria", answer.Body.GetProperty("data").GetProperty("username").GetString());
    }

    [Fact]
    public async Task A_password_hashed_with_lighter_parameters_is_hashed_again_at_sign_in()
    {
        AccountRepository accounts = _app.Services.GetRequiredService<AccountRepository>();

        string old = new PasswordHasher(iterations: 100).Hash(PieApplication.Password);

        accounts.Create("maria", AccountRole.Viewer, old, passwordChangeRequired: false, PieApplication.Now);

        using HttpClient client = _app.NewClient();

        Answer first = await PieApplication.SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", Credentials("maria"));

        string stored = accounts.FindByUsername("maria")!.PasswordHash;

        Assert.Equal(HttpStatusCode.OK, first.Status);
        Assert.NotEqual(old, stored);
        Assert.StartsWith("pbkdf2-sha512$1000$", stored, StringComparison.Ordinal);

        // The new hash works.
        using HttpClient again = _app.NewClient();

        Answer second = await PieApplication.SendAsync(again, HttpMethod.Post, "/api/v1/auth/login", Credentials("maria"));

        Assert.Equal(HttpStatusCode.OK, second.Status);
    }

    // ------------------------------------------------------------------
    // V5: a session ends when it should
    // ------------------------------------------------------------------

    [Fact]
    public async Task Signing_out_ends_the_session_and_the_old_identifier_stops_working()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        using HttpClient client = _app.NewClient();

        Answer login = await PieApplication.SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", Credentials("maria"));
        string token = TokenFrom(login);

        Answer logout = await PieApplication.SendAsync(client, HttpMethod.Post, "/api/v1/auth/logout");

        Assert.Equal(HttpStatusCode.OK, logout.Status);

        // Presented by hand, as someone who copied it would.
        Assert.Equal(HttpStatusCode.Unauthorized, (await Replay(token)).Status);
    }

    [Fact]
    public async Task Signing_in_again_replaces_the_session_and_the_previous_identifier_stops_working()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        using HttpClient client = _app.NewClient();

        string first = TokenFrom(await PieApplication.SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", Credentials("maria")));
        string second = TokenFrom(await PieApplication.SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", Credentials("maria")));

        Assert.NotEqual(first, second);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Replay(first)).Status);
        Assert.Equal(HttpStatusCode.OK, (await Replay(second)).Status);
    }

    [Fact]
    public async Task An_identifier_nobody_issued_is_refused()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Replay("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Replay("x")).Status);
    }

    [Fact]
    public async Task A_session_ends_after_eight_hours_without_a_request()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        using HttpClient client = _app.NewClient();

        string token = TokenFrom(await PieApplication.SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", Credentials("maria")));

        _app.Clock.Advance(SessionService.IdleTimeout - TimeSpan.FromSeconds(1));
        Assert.Equal(HttpStatusCode.OK, (await Replay(token)).Status);

        // Activity renews it: eight hours after the last request, not after the
        // sign in.
        _app.Clock.Advance(SessionService.IdleTimeout - TimeSpan.FromSeconds(1));
        Assert.Equal(HttpStatusCode.OK, (await Replay(token)).Status);

        _app.Clock.Advance(SessionService.IdleTimeout);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Replay(token)).Status);
    }

    [Fact]
    public async Task A_session_ends_after_fourteen_days_however_active_it_is()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        using HttpClient client = _app.NewClient();

        string token = TokenFrom(await PieApplication.SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", Credentials("maria")));

        // Used every six hours, so that inactivity never ends it.
        TimeSpan step = TimeSpan.FromHours(6);
        TimeSpan elapsed = TimeSpan.Zero;

        while (elapsed + step < SessionService.AbsoluteLifetime)
        {
            _app.Clock.Advance(step);
            elapsed += step;

            Assert.Equal(HttpStatusCode.OK, (await Replay(token)).Status);
        }

        _app.Clock.Advance(SessionService.AbsoluteLifetime - elapsed);

        Assert.Equal(HttpStatusCode.Unauthorized, (await Replay(token)).Status);
    }

    [Fact]
    public async Task A_session_of_an_account_that_was_disabled_stops_working_at_once()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        using HttpClient client = _app.NewClient();

        string token = TokenFrom(await PieApplication.SendAsync(client, HttpMethod.Post, "/api/v1/auth/login", Credentials("maria")));

        Assert.Equal(HttpStatusCode.OK, (await Replay(token)).Status);

        Disable("maria");

        Assert.Equal(HttpStatusCode.Unauthorized, (await Replay(token)).Status);
    }

    [Fact]
    public async Task The_sessions_of_an_account_can_be_ended_all_or_all_but_the_one_in_use()
    {
        StoredAccount maria = _app.AddAccount("maria", AccountRole.Viewer);
        SessionService sessions = _app.Services.GetRequiredService<SessionService>();
        SessionRepository repository = _app.Services.GetRequiredService<SessionRepository>();

        using HttpClient here = _app.NewClient();
        using HttpClient elsewhere = _app.NewClient();

        string kept = TokenFrom(await PieApplication.SendAsync(here, HttpMethod.Post, "/api/v1/auth/login", Credentials("maria")));
        string dropped = TokenFrom(await PieApplication.SendAsync(elsewhere, HttpMethod.Post, "/api/v1/auth/login", Credentials("maria")));

        // What a password change does: the place where it was made goes on,
        // every other place stops.
        long keptId = repository.Find(SessionService.HashToken(kept))!.Id;

        Assert.Equal(1, sessions.EndAllFor(maria.Id, keptId));

        Assert.Equal(HttpStatusCode.OK, (await Replay(kept)).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Replay(dropped)).Status);
    }

    // ------------------------------------------------------------------
    // Roles
    // ------------------------------------------------------------------

    [Fact]
    public async Task A_viewer_reads_the_aggregate_and_is_refused_what_identifies_a_device()
    {
        Seed.Acquisition(_app, Seed.HoursAgo(0), devices: [Seed.Device("10.0.0.5", TivuStream.Pie.Model.Enums.DeviceIdentityBasis.NetworkAddress, Seed.HoursAgo(0))]);

        using HttpClient viewer = await _app.SignedInAsync(AccountRole.Viewer, "maria");

        Assert.Equal(HttpStatusCode.OK, (await _app.GetAsync("/api/v1/statistics", viewer)).Status);
        Assert.Equal(HttpStatusCode.OK, (await _app.GetAsync("/api/v1/domains", viewer)).Status);

        Answer devices = await _app.GetAsync("/api/v1/devices", viewer);

        Assert.Equal(HttpStatusCode.Forbidden, devices.Status);
        Assert.Equal("Forbidden", devices.Body.GetProperty("error").GetProperty("code").GetString());
        Assert.DoesNotContain("10.0.0.5", devices.Raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_administrator_reads_everything()
    {
        Seed.Acquisition(_app, Seed.HoursAgo(0), devices: [Seed.Device("10.0.0.5", TivuStream.Pie.Model.Enums.DeviceIdentityBasis.NetworkAddress, Seed.HoursAgo(0))]);

        Answer devices = await _app.GetAsync("/api/v1/devices");

        Assert.Equal(HttpStatusCode.OK, devices.Status);
        Assert.Contains("10.0.0.5", devices.Raw, StringComparison.Ordinal);
    }

    // ------------------------------------------------------------------
    // V12: nothing is kept
    // ------------------------------------------------------------------

    [Fact]
    public async Task No_answer_may_be_kept_by_the_browser_or_by_anything_between()
    {
        Seed.Acquisition(_app, Seed.HoursAgo(0));

        Answer signedIn = await _app.GetAsync("/api/v1/statistics");
        Answer refused = await _app.GetAnonymouslyAsync("/api/v1/statistics");

        Assert.Equal(HttpStatusCode.OK, signedIn.Status);
        Assert.Equal(HttpStatusCode.Unauthorized, refused.Status);

        Assert.True(signedIn.Headers.CacheControl?.NoStore, "an answer with data must not be cached");
        Assert.True(refused.Headers.CacheControl?.NoStore, "a refusal must not be cached either");
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static object Credentials(string username)
    {
        return new { username, password = PieApplication.Password };
    }

    private static ClaimsPrincipal Principal(string role)
    {
        return new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"));
    }

    private List<RouteEndpoint> Endpoints()
    {
        return _app.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();
    }

    private static bool IsAnonymous(RouteEndpoint endpoint)
    {
        return endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
    }

    private static HttpMethod MethodOf(RouteEndpoint endpoint)
    {
        string method = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods[0];

        return new HttpMethod(method);
    }

    private static string ConcretePath(RouteEndpoint endpoint)
    {
        // "/api/v1/domains/{domain}" becomes "/api/v1/domains/x".
        return System.Text.RegularExpressions.Regex.Replace(endpoint.RoutePattern.RawText!, @"\{[^}]+\}", "x");
    }

    private static string TokenFrom(Answer login)
    {
        string header = login.Headers.GetValues("Set-Cookie").Single();

        return header["pie_session=".Length..header.IndexOf(';', StringComparison.Ordinal)];
    }

    private async Task<Answer> Replay(string token)
    {
        // A client that keeps no cookies of its own, presenting the identifier
        // by hand.
        using HttpClient client = _app.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });

        return await PieApplication.SendAsync(client, HttpMethod.Get, "/api/v1/auth/session", cookie: token);
    }

    private void Disable(string username)
    {
        using SqliteConnection connection = _app.Services.GetRequiredService<SqliteConnectionFactory>().Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = "UPDATE account SET enabled = 0 WHERE username = $username;";
        command.Parameters.AddWithValue("$username", username);

        Assert.Equal(1, command.ExecuteNonQuery());
    }

    private byte[] ReadDatabaseFile()
    {
        string path = _app.Services.GetRequiredService<SqliteConnectionFactory>().DatabasePath;

        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using MemoryStream copy = new();

        stream.CopyTo(copy);

        return copy.ToArray();
    }

    private static bool Contains(byte[] haystack, string needle)
    {
        return haystack.AsSpan().IndexOf(Encoding.UTF8.GetBytes(needle)) >= 0;
    }
}
