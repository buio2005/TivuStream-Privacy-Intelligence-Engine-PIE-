using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using TivuStream.Pie.Api.Authentication;
using TivuStream.Pie.Storage;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies the protections that do not depend on who is asking: repeated
/// attempts, the channel a password travels on, the site a request comes
/// from and the name it is addressed to.
/// </summary>
/// <remarks>
/// Authentication Specification, Attempts, Transport and Headers: V8, V9,
/// V13 and V14.
/// </remarks>
public sealed class ProtectionTests : IDisposable
{
    private const string Remote = "192.168.1.20";

    private const string WrongPassword = "certainly-not-the-password";

    private readonly PieApplication _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    // ------------------------------------------------------------------
    // V8: repeated attempts slow down
    // ------------------------------------------------------------------

    [Fact]
    public async Task After_five_failures_a_source_waits_even_with_the_right_password()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        for (int attempt = 0; attempt < 5; attempt++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await LogIn("maria", WrongPassword)).Status);
        }

        Answer refused = await LogIn("maria", PieApplication.Password);

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.Status);
        Assert.Equal("TooManyAttempts", Code(refused));
        Assert.Equal("30", refused.Headers.GetValues("Retry-After").Single());
        Assert.False(refused.Headers.Contains("Set-Cookie"));

        // Another device of the network is not held up by this one.
        Assert.Equal(HttpStatusCode.OK, (await LogIn("maria", PieApplication.Password, from: "127.0.0.2")).Status);

        _app.Clock.Advance(TimeSpan.FromSeconds(30));

        Assert.Equal(HttpStatusCode.OK, (await LogIn("maria", PieApplication.Password)).Status);
    }

    [Theory]
    [InlineData("maria")]
    [InlineData("nobody")]
    public async Task After_ten_failures_a_name_waits_from_anywhere_whether_or_not_it_exists(string name)
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        for (int attempt = 1; attempt <= 10; attempt++)
        {
            await LogIn(name, WrongPassword, from: $"127.0.1.{attempt}");
        }

        Answer refused = await LogIn(name, PieApplication.Password, from: "127.0.2.1");

        // The same answer for a name that exists and one that does not:
        // slowing down only the first would tell which names exist.
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.Status);
        Assert.Equal("30", refused.Headers.GetValues("Retry-After").Single());
    }

    [Fact]
    public async Task A_successful_sign_in_clears_the_count_of_its_account()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        for (int round = 0; round < 2; round++)
        {
            for (int attempt = 1; attempt <= 9; attempt++)
            {
                await LogIn("maria", WrongPassword, from: $"127.0.{round}.{attempt}");
            }

            Assert.Equal(HttpStatusCode.OK, (await LogIn("maria", PieApplication.Password, from: $"127.0.{round}.99")).Status);
        }
    }

    [Fact]
    public async Task Wrong_setup_codes_slow_down_the_source_and_the_right_code_still_works_afterwards()
    {
        string code = _app.SetupCode();

        for (int attempt = 0; attempt < 5; attempt++)
        {
            Assert.Equal("SetupCodeRejected", Code(await Setup("AAAA-BBBB-CCCC")));
        }

        Answer refused = await Setup(code);

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.Status);

        _app.Clock.Advance(TimeSpan.FromSeconds(30));

        Assert.Equal(HttpStatusCode.Created, (await Setup(code)).Status);
    }

    [Fact]
    public async Task A_name_or_a_password_that_is_not_acceptable_is_not_a_failed_attempt()
    {
        string code = _app.SetupCode();

        // The right code each time: no secret is being guessed.
        for (int attempt = 0; attempt < 6; attempt++)
        {
            Assert.Equal("PasswordRejected", Code(await Setup(code, password: "short")));
        }

        Assert.Equal(HttpStatusCode.Created, (await Setup(code)).Status);
    }

    [Fact]
    public async Task A_wrong_current_password_counts_as_a_failure_and_a_right_one_does_not_get_through_the_delay()
    {
        using HttpClient maria = await _app.SignedInAsync(AccountRole.Viewer, "maria");
        StoredAccount before = _app.Account("maria")!;

        for (int attempt = 0; attempt < 5; attempt++)
        {
            Answer wrong = await ChangePassword(maria, WrongPassword);

            Assert.Equal("CurrentPasswordRejected", Code(wrong));
        }

        Answer refused = await ChangePassword(maria, PieApplication.Password);

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.Status);
        Assert.Equal(before, _app.Account("maria"));
    }

    // ------------------------------------------------------------------
    // V9: passwords only over a suitable channel
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("POST", "/api/v1/setup")]
    [InlineData("POST", "/api/v1/auth/login")]
    [InlineData("POST", "/api/v1/auth/password")]
    [InlineData("POST", "/api/v1/accounts")]
    [InlineData("PATCH", "/api/v1/accounts/maria")]
    public async Task A_password_over_plain_http_from_another_device_is_refused_before_anything_else(string method, string path)
    {
        _app.AddAccount("maria", AccountRole.Viewer);
        StoredAccount before = _app.Account("maria")!;

        HttpClient administrator = await _app.AdministratorAsync();

        Answer answer = await Send(
            administrator,
            new HttpMethod(method),
            path,
            new { setupCode = "x", username = "maria", role = "Viewer", password = PieApplication.Password, currentPassword = PieApplication.Password, newPassword = "a-new-password-for-the-tests" },
            from: Remote);

        Assert.Equal(HttpStatusCode.Forbidden, answer.Status);
        Assert.Equal("TransportNotSecure", Code(answer));
        Assert.Equal(before, _app.Account("maria"));
    }

    [Fact]
    public async Task A_change_without_a_password_is_not_a_credential_and_is_accepted_from_another_device()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        Answer answer = await Send(await _app.AdministratorAsync(), HttpMethod.Patch, "/api/v1/accounts/maria", new { enabled = false }, from: Remote);

        Assert.Equal(HttpStatusCode.OK, answer.Status);
    }

    [Theory]
    [InlineData("::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:127.5.6.7")]
    [InlineData("127.10.20.30")]
    public async Task The_loopback_is_suitable_however_it_is_written(string address)
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        Assert.Equal(HttpStatusCode.OK, (await LogIn("maria", PieApplication.Password, from: address)).Status);
    }

    [Fact]
    public async Task An_unknown_remote_address_is_not_the_loopback()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        Assert.Equal("TransportNotSecure", Code(await LogIn("maria", PieApplication.Password, from: "unknown")));
    }

    [Fact]
    public async Task An_encrypted_connection_is_suitable_from_anywhere_and_says_so_in_its_headers()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        using HttpClient client = _app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        Answer answer = await Send(client, HttpMethod.Post, "/api/v1/auth/login", new { username = "maria", password = PieApplication.Password }, from: Remote);

        Assert.Equal(HttpStatusCode.OK, answer.Status);
        Assert.Equal("max-age=31536000", answer.Headers.GetValues("Strict-Transport-Security").Single());
        Assert.Contains("secure", answer.Headers.GetValues("Set-Cookie").Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_plain_connection_carries_no_strict_transport_security()
    {
        Answer answer = await _app.GetAsync("/api/v1/health");

        Assert.False(answer.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public async Task A_refusal_for_the_channel_is_not_a_failed_attempt()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        for (int attempt = 0; attempt < 6; attempt++)
        {
            await LogIn("maria", WrongPassword, from: Remote);
        }

        using HttpClient client = _app.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        Answer answer = await Send(client, HttpMethod.Post, "/api/v1/auth/login", new { username = "maria", password = PieApplication.Password }, from: Remote);

        Assert.Equal(HttpStatusCode.OK, answer.Status);
    }

    // ------------------------------------------------------------------
    // V13: requests from other sites
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("http://evil.example")]
    [InlineData("http://localhost:8080")]
    [InlineData("https://localhost")]
    [InlineData("null")]
    public async Task A_request_that_changes_data_from_another_origin_is_refused(string origin)
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        Answer answer = await Send(
            await _app.AdministratorAsync(),
            HttpMethod.Patch,
            "/api/v1/accounts/maria",
            new { enabled = false },
            origin: origin);

        Assert.Equal(HttpStatusCode.Forbidden, answer.Status);
        Assert.Equal("OriginNotAllowed", Code(answer));
        Assert.True(_app.Account("maria")!.Enabled);
    }

    [Fact]
    public async Task Signing_in_from_another_origin_is_refused_too()
    {
        _app.AddAccount("maria", AccountRole.Viewer);

        using HttpClient client = _app.NewClient();

        Answer answer = await Send(client, HttpMethod.Post, "/api/v1/auth/login", new { username = "maria", password = PieApplication.Password }, origin: "http://evil.example");

        Assert.Equal("OriginNotAllowed", Code(answer));
        Assert.False(answer.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task A_request_from_the_services_own_origin_or_without_one_is_accepted()
    {
        _app.AddAccount("maria", AccountRole.Viewer);
        HttpClient administrator = await _app.AdministratorAsync();

        Assert.Equal(HttpStatusCode.OK, (await Send(administrator, HttpMethod.Patch, "/api/v1/accounts/maria", new { enabled = false }, origin: "http://LOCALHOST")).Status);
        Assert.Equal(HttpStatusCode.OK, (await Send(administrator, HttpMethod.Patch, "/api/v1/accounts/maria", new { enabled = true })).Status);
    }

    [Fact]
    public async Task Reading_is_not_refused_for_its_origin()
    {
        Answer answer = await Send(await _app.AdministratorAsync(), HttpMethod.Get, "/api/v1/auth/session", origin: "http://evil.example");

        Assert.Equal(HttpStatusCode.OK, answer.Status);
    }

    [Fact]
    public async Task No_answer_can_be_framed_by_another_site()
    {
        Answer answer = await _app.GetAnonymouslyAsync("/api/v1/health");

        Assert.Equal("frame-ancestors 'none'", answer.Headers.GetValues("Content-Security-Policy").Single());
    }

    // ------------------------------------------------------------------
    // V14: allowed hosts
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("evil.example")]
    [InlineData("pie.rebound.example")]
    public async Task A_request_addressed_to_a_name_not_listed_is_refused(string host)
    {
        using HttpClient client = _app.NewClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/health");

        request.Headers.Host = host;

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("localhost:5000")]
    [InlineData("127.0.0.1")]
    [InlineData("[::1]:5000")]
    public async Task The_loopback_names_are_accepted_without_any_setting(string host)
    {
        using HttpClient client = _app.NewClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/health");

        request.Headers.Host = host;

        using HttpResponseMessage response = await client.SendAsync(request);

        // Refused for want of a session, not for the name.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void Accepting_any_name_stops_the_start()
    {
        using PieApplication app = new();

        using WebApplicationFactory<Program> open = app.WithWebHostBuilder(builder => builder.UseSetting(HostPolicy.SettingName, "localhost;*"));

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => open.Services);

        Assert.Contains(HostPolicy.SettingName, refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_name_added_to_the_setting_is_accepted()
    {
        using PieApplication app = new();

        using WebApplicationFactory<Program> named = app.WithWebHostBuilder(builder => builder.UseSetting(HostPolicy.SettingName, "pie.home.arpa"));

        using HttpClient client = named.CreateClient();
        using HttpRequestMessage request = new(HttpMethod.Get, "/api/v1/health");

        request.Headers.Host = "pie.home.arpa";

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ------------------------------------------------------------------

    private async Task<Answer> LogIn(string username, string password, string? from = null)
    {
        using HttpClient client = _app.NewClient();

        return await Send(client, HttpMethod.Post, "/api/v1/auth/login", new { username, password }, from: from);
    }

    private async Task<Answer> Setup(string code, string password = "a-password-for-the-setup")
    {
        using HttpClient client = _app.NewClient();

        return await Send(client, HttpMethod.Post, "/api/v1/setup", new { setupCode = code, username = "root", password });
    }

    private static Task<Answer> ChangePassword(HttpClient client, string current)
    {
        return Send(client, HttpMethod.Post, "/api/v1/auth/password", new { currentPassword = current, newPassword = "a-new-password-for-the-tests" });
    }

    private static Task<Answer> Send(HttpClient client, HttpMethod method, string path, object? body = null, string? from = null, string? origin = null)
    {
        return PieApplication.SendAsync(client, method, path, body, from: from, origin: origin);
    }

    private static string? Code(Answer answer)
    {
        return answer.Body.GetProperty("error").GetProperty("code").GetString();
    }
}
