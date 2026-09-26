using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TivuStream.Pie.Api.Authentication;
using TivuStream.Pie.Storage;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies that no secret leaves the engine, in an answer or in a log.
/// </summary>
/// <remarks>
/// Authentication Specification, Logging and V11. No answer and no line of any
/// log, the framework's included and at its most detailed level, carries a
/// password, a session identifier, the setup code or a source address. And
/// the events the specification asks to record are there: a test that found
/// nothing in an empty log would prove nothing.
/// </remarks>
public sealed class SecrecyTests : IDisposable
{
    private const string AdministratorPassword = "first-secret-of-the-administrator";
    private const string AdministratorNewPassword = "second-secret-of-the-administrator";
    private const string InitialPassword = "initial-secret-chosen-for-maria";
    private const string ResetPassword = "reset-secret-chosen-for-maria";
    private const string MariaPassword = "secret-maria-chose-herself";

    // What a person types into the name field when they mean the password.
    private const string PasswordAsName = "typed-into-the-name-field";

    // Loopback, so that passwords are accepted, and recognisable in a log.
    private const string Source = "127.0.0.77";

    private readonly PieApplication _base = new();
    private readonly WebApplicationFactory<Program> _app;
    private readonly List<string> _answers = [];

    public SecrecyTests()
    {
        _app = _base.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Trace)));
    }

    public void Dispose()
    {
        _app.Dispose();
        _base.Dispose();
    }

    [Fact]
    public async Task No_answer_and_no_log_line_carries_a_secret_and_the_events_are_recorded()
    {
        using HttpClient root = NewClient();

        // First run: two wrong codes, then the right one.
        _ = _app.Services;
        string code = Regex.Match(_base.Output.ToString(), "[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}").Value;

        Assert.NotEmpty(code);

        await Send(root, HttpMethod.Post, "/api/v1/setup", new { setupCode = "AAAA-BBBB-CCCC", username = "root", password = AdministratorPassword });
        await Send(root, HttpMethod.Post, "/api/v1/setup", new { setupCode = "DDDD-EEEE-FFFF", username = "root", password = AdministratorPassword });

        Answer setup = await Send(root, HttpMethod.Post, "/api/v1/setup", new { setupCode = code, username = "root", password = AdministratorPassword });
        Assert.Equal(HttpStatusCode.Created, setup.Status);

        // Refused sign ins, one of them with a password typed as the name,
        // until the source reaches its limit.
        using HttpClient stranger = NewClient();

        await Send(stranger, HttpMethod.Post, "/api/v1/auth/login", new { username = PasswordAsName, password = "whatever-was-typed" }, Source);

        for (int attempt = 0; attempt < 5; attempt++)
        {
            await Send(stranger, HttpMethod.Post, "/api/v1/auth/login", new { username = "root", password = "not-the-password-at-all" }, Source);
        }

        // The administrator manages an account and changes their own password.
        await Send(root, HttpMethod.Post, "/api/v1/accounts", new { username = "maria", role = "Viewer", password = InitialPassword });
        await Send(root, HttpMethod.Patch, "/api/v1/accounts/maria", new { password = ResetPassword });
        await Send(root, HttpMethod.Patch, "/api/v1/accounts/maria", new { role = "Administrator" });
        await Send(root, HttpMethod.Patch, "/api/v1/accounts/maria", new { role = "Viewer", enabled = false });
        await Send(root, HttpMethod.Patch, "/api/v1/accounts/maria", new { enabled = true });
        await Send(root, HttpMethod.Post, "/api/v1/auth/password", new { currentPassword = AdministratorPassword, newPassword = AdministratorNewPassword });

        // Maria signs in with the password she was given and replaces it.
        using HttpClient maria = NewClient();

        Answer mariaSignIn = await Send(maria, HttpMethod.Post, "/api/v1/auth/login", new { username = "maria", password = ResetPassword });
        Assert.Equal(HttpStatusCode.OK, mariaSignIn.Status);

        await Send(maria, HttpMethod.Post, "/api/v1/auth/password", new { currentPassword = ResetPassword, newPassword = MariaPassword });
        await Send(root, HttpMethod.Delete, "/api/v1/accounts/maria");

        string[] tokens = [TokenOf(setup), TokenOf(mariaSignIn)];

        // A failure the code did not foresee, with a session cookie present.
        DropSessions();

        Answer failure = await Send(root, HttpMethod.Get, "/api/v1/health");
        Assert.Equal(HttpStatusCode.InternalServerError, failure.Status);

        string[] secrets =
        [
            AdministratorPassword,
            AdministratorNewPassword,
            InitialPassword,
            ResetPassword,
            MariaPassword,
            PasswordAsName,
            code,
            code.Replace("-", string.Empty, StringComparison.Ordinal),
            Source,
            PieApplication.SourceToken,
            .. tokens,
        ];

        List<string> logs;

        lock (_base.Logs)
        {
            logs = [.. _base.Logs];
        }

        foreach (string secret in secrets)
        {
            Assert.DoesNotContain(_answers, answer => answer.Contains(secret, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(logs, line => line.Contains(secret, StringComparison.OrdinalIgnoreCase));
        }

        // The log was not simply empty: what the specification asks to record is there.
        string[] expected =
        [
            "First run setup completed. Administrator 'root' created.",
            "A sign in was refused. Refused since the service started: 5.",
            "Too many failed attempts from one source address.",
            "'root' performed 'create as Viewer' on account 'maria'.",
            "'root' performed 'reset password' on account 'maria'.",
            "'root' performed 'set role Administrator' on account 'maria'.",
            "'root' performed 'disable' on account 'maria'.",
            "'root' performed 'enable' on account 'maria'.",
            "'root' changed their own password.",
            "'maria' signed in.",
            "'maria' changed their own password.",
            "'root' performed 'remove' on account 'maria'.",
        ];

        foreach (string line in expected)
        {
            Assert.Contains(logs, logged => logged.Contains(line, StringComparison.Ordinal));
        }

        // And the framework's own detail was captured too, or the check above
        // would say nothing about it.
        Assert.Contains(logs, logged => logged.Contains("Request starting", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_failure_nobody_foresaw_answers_in_the_common_structure_and_not_with_the_request()
    {
        using HttpClient client = await _base.AdministratorAsync();

        using (SqliteConnection connection = _base.Services.GetRequiredService<SqliteConnectionFactory>().Open())
        using (SqliteCommand command = connection.CreateCommand())
        {
            command.CommandText = "DROP TABLE session;";
            command.ExecuteNonQuery();
        }

        Answer answer = await PieApplication.SendAsync(client, HttpMethod.Get, "/api/v1/health");

        // The diagnostic page of the framework would list the request's
        // headers, the session cookie among them.
        Assert.Equal(HttpStatusCode.InternalServerError, answer.Status);
        Assert.Equal("InternalError", answer.Body.GetProperty("error").GetProperty("code").GetString());
        Assert.DoesNotContain(SessionCookie.Name, answer.Raw, StringComparison.Ordinal);
        Assert.DoesNotContain("Exception", answer.Raw, StringComparison.Ordinal);
        Assert.True(answer.Headers.CacheControl?.NoStore);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task A_body_that_cannot_be_read_is_answered_alike_in_every_environment(string environment)
    {
        using WebApplicationFactory<Program> host = _base.WithWebHostBuilder(builder => builder.UseEnvironment(environment));
        using HttpClient client = host.CreateClient();
        using StringContent content = new("{ this is not json", System.Text.Encoding.UTF8, "application/json");

        using HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/auth/login", UriKind.Relative), content);

        string raw = await response.Content.ReadAsStringAsync();

        // In production the framework would otherwise answer a bare status,
        // and in development a page describing the request.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("\"RequestUnreadable\"", raw, StringComparison.Ordinal);
    }

    private HttpClient NewClient()
    {
        return _app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });
    }

    private async Task<Answer> Send(HttpClient client, HttpMethod method, string path, object? body = null, string? from = null)
    {
        Answer answer = await PieApplication.SendAsync(client, method, path, body, from: from);

        _answers.Add(answer.Raw);

        return answer;
    }

    private void DropSessions()
    {
        using SqliteConnection connection = _app.Services.GetRequiredService<SqliteConnectionFactory>().Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = "DROP TABLE session;";
        command.ExecuteNonQuery();
    }

    private static string TokenOf(Answer answer)
    {
        string header = answer.Headers.GetValues("Set-Cookie").Single();

        return header[(SessionCookie.Name.Length + 1)..header.IndexOf(';', StringComparison.Ordinal)];
    }
}
