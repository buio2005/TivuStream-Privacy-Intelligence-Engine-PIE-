using System.Net;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using TivuStream.Pie.Storage;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies how the first administrator is created.
/// </summary>
/// <remarks>
/// Authentication Specification, First Run (V10): whoever reaches the address
/// first must not become the administrator by arriving before the owner. The
/// proof is a code shown only on the standard output of the process, so it is
/// access to the machine that is being asked for.
/// </remarks>
public sealed class SetupTests : IDisposable
{
    private const string Username = "maria";

    private const string GoodPassword = "a-password-worth-twelve";

    private readonly PieApplication _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    // ------------------------------------------------------------------
    // Before the first account
    // ------------------------------------------------------------------

    [Fact]
    public async Task With_no_account_every_endpoint_says_that_the_first_one_is_needed()
    {
        Answer statistics = await _app.GetAnonymouslyAsync("/api/v1/statistics");
        Answer devices = await _app.GetAnonymouslyAsync("/api/v1/devices");

        // "Authenticate" would be an instruction nobody can follow.
        Assert.Equal(HttpStatusCode.Unauthorized, statistics.Status);
        Assert.Equal("SetupRequired", Code(statistics));
        Assert.Equal("SetupRequired", Code(devices));
    }

    [Fact]
    public async Task With_no_account_signing_in_does_not_pretend_the_credentials_were_wrong()
    {
        using HttpClient client = _app.NewClient();

        Answer answer = await PieApplication.SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/login",
            new { username = Username, password = GoodPassword });

        Assert.Equal(HttpStatusCode.Unauthorized, answer.Status);
        Assert.Equal("SetupRequired", Code(answer));
    }

    // ------------------------------------------------------------------
    // The code
    // ------------------------------------------------------------------

    [Fact]
    public void The_code_is_shown_on_the_standard_output_in_groups_of_four()
    {
        string code = _app.SetupCode();

        Assert.Matches("^[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}$", code);

        // No character that is mistaken for another.
        Assert.DoesNotMatch("[IO01]", code);
    }

    [Fact]
    public void The_code_never_reaches_the_logging_system()
    {
        string code = _app.SetupCode();

        string[] logs;

        lock (_app.Logs)
        {
            logs = [.. _app.Logs];
        }

        // Positive control: the host did log, so an empty list is not what
        // makes this pass.
        Assert.NotEmpty(logs);

        Assert.DoesNotContain(logs, line => line.Contains(code, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logs, line => line.Contains(code.Replace("-", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase));
    }

    // ------------------------------------------------------------------
    // Creating the administrator
    // ------------------------------------------------------------------

    [Fact]
    public async Task The_right_code_creates_the_first_administrator_and_signs_them_in()
    {
        string code = _app.SetupCode();

        using HttpClient client = _app.NewClient();

        Answer created = await Setup(client, code, Username, GoodPassword);

        JsonElement data = created.Body.GetProperty("data");

        Assert.Equal(HttpStatusCode.Created, created.Status);
        Assert.Equal(Username, data.GetProperty("username").GetString());
        Assert.Equal("Administrator", data.GetProperty("role").GetString());

        // They chose the password themselves: nothing to change.
        Assert.False(data.GetProperty("passwordChangeRequired").GetBoolean());

        // Signed in already, with no second step.
        Answer devices = await PieApplication.SendAsync(client, HttpMethod.Get, "/api/v1/devices");

        Assert.Equal(HttpStatusCode.OK, devices.Status);
    }

    [Fact]
    public async Task The_code_can_be_used_once()
    {
        string code = _app.SetupCode();

        using HttpClient first = _app.NewClient();
        using HttpClient second = _app.NewClient();

        await Setup(first, code, Username, GoodPassword);

        Answer again = await Setup(second, code, "someone-else", GoodPassword);

        Assert.Equal(HttpStatusCode.Conflict, again.Status);
        Assert.Equal("SetupAlreadyCompleted", Code(again));
        Assert.Null(_app.Services.GetRequiredService<AccountRepository>().FindByUsername("someone-else"));
    }

    [Fact]
    public async Task After_the_first_account_the_answer_is_no_longer_that_setup_is_required()
    {
        string code = _app.SetupCode();

        using HttpClient client = _app.NewClient();

        await Setup(client, code, Username, GoodPassword);

        Answer anonymous = await _app.GetAnonymouslyAsync("/api/v1/statistics");

        Assert.Equal("AuthenticationRequired", Code(anonymous));
    }

    [Theory]
    [InlineData("wrong")]
    [InlineData("AAAA-AAAA-AAAA")]
    [InlineData("")]
    [InlineData(null)]
    public async Task A_wrong_or_missing_code_is_refused_in_the_same_words_and_creates_nothing(string? presented)
    {
        _ = _app.SetupCode();

        using HttpClient client = _app.NewClient();

        Answer answer = await Setup(client, presented, Username, GoodPassword);

        Assert.Equal(HttpStatusCode.Unauthorized, answer.Status);
        Assert.Equal("SetupCodeRejected", Code(answer));
        Assert.False(answer.Headers.Contains("Set-Cookie"));

        // Still waiting for the first account.
        Assert.Equal("SetupRequired", Code(await _app.GetAnonymouslyAsync("/api/v1/statistics")));
    }

    [Fact]
    public async Task The_code_is_accepted_however_it_was_typed()
    {
        string code = _app.SetupCode();

        // Lower case, no dashes, spaces in their place.
        string typed = code.ToLowerInvariant().Replace('-', ' ');

        using HttpClient client = _app.NewClient();

        Answer answer = await Setup(client, typed, Username, GoodPassword);

        Assert.Equal(HttpStatusCode.Created, answer.Status);
    }

    [Fact]
    public async Task A_name_or_a_password_that_is_not_acceptable_does_not_use_the_code_up()
    {
        string code = _app.SetupCode();

        using HttpClient client = _app.NewClient();

        Answer badName = await Setup(client, code, "Not A Name!", GoodPassword);
        Answer shortPassword = await Setup(client, code, Username, "too short");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, badName.Status);
        Assert.Equal("UsernameRejected", Code(badName));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, shortPassword.Status);
        Assert.Equal("PasswordRejected", Code(shortPassword));
        Assert.Equal("TooShort", shortPassword.Body.GetProperty("error").GetProperty("reason").GetString());

        // Whoever mistypes must not have to restart the service.
        Answer right = await Setup(client, code, Username, GoodPassword);

        Assert.Equal(HttpStatusCode.Created, right.Status);
    }

    [Fact]
    public async Task A_password_equal_to_the_name_is_refused_with_its_reason()
    {
        string code = _app.SetupCode();

        using HttpClient client = _app.NewClient();

        Answer answer = await Setup(client, code, "administrator", "administrator");

        Assert.Equal("EqualsUsername", answer.Body.GetProperty("error").GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Several_requests_at_once_with_the_right_code_create_exactly_one_administrator()
    {
        string code = _app.SetupCode();

        // The check that no account exists and the creation are one statement.
        // Done in two, all of these would find the installation empty.
        Answer[] answers = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(async index =>
            {
                using HttpClient client = _app.NewClient();

                return await Setup(client, code, $"admin-{index}", GoodPassword);
            }));

        Assert.Equal(1, answers.Count(answer => answer.Status == HttpStatusCode.Created));
        Assert.Equal(7, answers.Count(answer => answer.Status == HttpStatusCode.Conflict));
        Assert.Equal(1, CountAccounts());
    }

    [Fact]
    public async Task An_error_carries_a_reason_only_when_there_is_one()
    {
        _app.AddAccount(Username, AccountRole.Viewer);

        using HttpClient client = _app.NewClient();

        Answer refused = await PieApplication.SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/login",
            new { username = Username, password = "not the password" });

        // Absent, and not null: a field that is always there says nothing.
        Assert.False(refused.Body.GetProperty("error").TryGetProperty("reason", out _));
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static string? Code(Answer answer)
    {
        return answer.Body.GetProperty("error").GetProperty("code").GetString();
    }

    private static Task<Answer> Setup(HttpClient client, string? code, string username, string password)
    {
        return PieApplication.SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/setup",
            new { setupCode = code, username, password });
    }

    private long CountAccounts()
    {
        using SqliteConnection connection = _app.Services.GetRequiredService<SqliteConnectionFactory>().Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = "SELECT COUNT(*) FROM account;";

        return Convert.ToInt64(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }
}
