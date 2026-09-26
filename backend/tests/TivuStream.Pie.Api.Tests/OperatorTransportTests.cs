using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc.Testing;
using TivuStream.Pie.Api.Transport;
using TivuStream.Pie.Storage;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies a certificate provided by the operator, and a proxy in front of
/// PIE.
/// </summary>
/// <remarks>
/// Transport Security Specification, T6, T7, T9 and T10.
/// </remarks>
public sealed class OperatorTransportTests : IDisposable
{
    private const string Remote = "192.168.1.50";

    private const string CertificatePassword = "a-password-for-the-certificate";

    private readonly PieApplication _base = new();

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "pie-operator-" + Guid.NewGuid().ToString("N"));

    public OperatorTransportTests()
    {
        Directory.CreateDirectory(_directory);
        _base.AddAccount("maria", AccountRole.Viewer);
    }

    public void Dispose()
    {
        _base.Dispose();
        Directory.Delete(_directory, recursive: true);
    }

    private WebApplicationFactory<Program> With(params (string Key, string Value)[] settings)
    {
        return _base.WithWebHostBuilder(builder =>
        {
            foreach ((string key, string value) in settings)
            {
                builder.UseSetting(key, value);
            }
        });
    }

    private static X509Certificate2 Certificate(DateTimeOffset issued)
    {
        return GeneratedCertificate.Create(new MachineNames(["localhost"], [IPAddress.Loopback]), issued);
    }

    private string Pfx(DateTimeOffset issued, string? password = CertificatePassword)
    {
        using X509Certificate2 certificate = Certificate(issued);

        string path = Path.Combine(_directory, Guid.NewGuid().ToString("N") + ".pfx");
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pkcs12, password));

        return path;
    }

    private static Task<Answer> SignIn(HttpClient client, string? from = null, Dictionary<string, string>? headers = null)
    {
        return PieApplication.SendAsync(
            client,
            HttpMethod.Post,
            "/api/v1/auth/login",
            new { username = "maria", password = PieApplication.Password },
            from: from,
            headers: headers);
    }

    // ------------------------------------------------------------------
    // T6 and T7: the operator's certificate
    // ------------------------------------------------------------------

    [Fact]
    public async Task With_the_operators_certificate_an_encrypted_answer_carries_strict_transport_security()
    {
        using WebApplicationFactory<Program> host = With(
            ("Transport:Certificate:Path", Pfx(DateTimeOffset.UtcNow)),
            ("Transport:Certificate:Password", CertificatePassword));

        using HttpClient client = host.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

        HttpResponseMessage response = await client.GetAsync("/api/v1/health");

        // Browsers recognise it, so the promise can be made.
        Assert.Equal("max-age=31536000", response.Headers.GetValues("Strict-Transport-Security").Single());
    }

    [Fact]
    public async Task With_the_operators_certificate_a_plain_answer_still_carries_no_strict_transport_security()
    {
        using WebApplicationFactory<Program> host = With(
            ("Transport:Certificate:Path", Pfx(DateTimeOffset.UtcNow)),
            ("Transport:Certificate:Password", CertificatePassword));

        using HttpClient client = host.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/health");

        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
    }

    [Fact]
    public void A_certificate_that_does_not_exist_stops_the_start_and_says_why()
    {
        using WebApplicationFactory<Program> host = With(("Transport:Certificate:Path", Path.Combine(_directory, "missing.pfx")));

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => host.Services);

        // Falling back on a generated certificate would change the
        // fingerprint without telling anyone.
        Assert.Contains("does not exist", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_expired_certificate_stops_the_start_and_says_why()
    {
        using WebApplicationFactory<Program> host = With(
            ("Transport:Certificate:Path", Pfx(DateTimeOffset.UtcNow.AddDays(-500))),
            ("Transport:Certificate:Password", CertificatePassword));

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => host.Services);

        Assert.Contains("expired", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wrong_password_stops_the_start_and_never_shows_the_password()
    {
        using WebApplicationFactory<Program> host = With(
            ("Transport:Certificate:Path", Pfx(DateTimeOffset.UtcNow)),
            ("Transport:Certificate:Password", "not-the-password-of-the-file"));

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => host.Services);

        Assert.Contains("cannot be read", refusal.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("not-the-password-of-the-file", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_certificate_without_its_key_stops_the_start_and_says_why()
    {
        using X509Certificate2 certificate = Certificate(DateTimeOffset.UtcNow);

        string path = Path.Combine(_directory, "public-only.pem");
        File.WriteAllText(path, certificate.ExportCertificatePem());

        CertificateOptions options = new() { Path = path };

        // A PEM file without a key: read as PKCS#12 it is not readable at all;
        // a PKCS#12 without a key is refused for the missing key.
        string pfx = Path.Combine(_directory, "public-only.pfx");
        File.WriteAllBytes(pfx, X509CertificateLoader.LoadCertificate(certificate.RawData).Export(X509ContentType.Pkcs12));

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(
            () => OperatorCertificate.Load(new CertificateOptions { Path = pfx }, DateTimeOffset.UtcNow));

        Assert.Contains("no private key", refusal.Message, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() => OperatorCertificate.Load(options, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void A_pem_certificate_with_its_key_is_used()
    {
        using X509Certificate2 certificate = Certificate(DateTimeOffset.UtcNow);

        string path = Path.Combine(_directory, "server.pem");
        string key = Path.Combine(_directory, "server.key");
        File.WriteAllText(path, certificate.ExportCertificatePem());
        File.WriteAllText(key, certificate.GetECDsaPrivateKey()!.ExportPkcs8PrivateKeyPem());

        using X509Certificate2 loaded = OperatorCertificate.Load(new CertificateOptions { Path = path, KeyPath = key }, DateTimeOffset.UtcNow);

        Assert.True(loaded.HasPrivateKey);
        Assert.Equal(certificate.Thumbprint, loaded.Thumbprint);
    }

    // ------------------------------------------------------------------
    // T9: a proxy nobody declared does not make a request local
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("X-Forwarded-For", Remote)]
    [InlineData("X-Forwarded-Proto", "https")]
    [InlineData("Forwarded", "for=192.168.1.50;proto=https")]
    public async Task A_request_from_the_loopback_carrying_forwarding_headers_is_not_local(string header, string value)
    {
        using HttpClient client = _base.NewClient();

        Answer answer = await SignIn(client, headers: new() { [header] = value });

        // A proxy on this machine, serving the network in clear, would
        // otherwise pass a password that crossed the network unencrypted.
        Assert.Equal(HttpStatusCode.Forbidden, answer.Status);
        Assert.Equal("TransportNotSecure", answer.Body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_request_from_the_loopback_without_forwarding_headers_is_still_local()
    {
        using HttpClient client = _base.NewClient();

        Answer answer = await SignIn(client);

        Assert.Equal(HttpStatusCode.OK, answer.Status);
    }

    // ------------------------------------------------------------------
    // T10: a declared proxy passes on the client and the channel
    // ------------------------------------------------------------------

    [Fact]
    public async Task A_trusted_proxy_declaring_an_encrypted_channel_lets_a_password_through_with_a_secure_cookie()
    {
        using WebApplicationFactory<Program> host = With(("Transport:TrustedProxies:0", "127.0.0.1"));
        using HttpClient client = host.CreateClient();

        Answer answer = await SignIn(client, headers: new() { ["X-Forwarded-For"] = Remote, ["X-Forwarded-Proto"] = "https" });

        Assert.Equal(HttpStatusCode.OK, answer.Status);
        Assert.Contains("secure", answer.Headers.GetValues("Set-Cookie").Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_trusted_proxy_declaring_a_plain_channel_from_the_network_is_refused()
    {
        using WebApplicationFactory<Program> host = With(("Transport:TrustedProxies:0", "127.0.0.1"));
        using HttpClient client = host.CreateClient();

        Answer answer = await SignIn(client, headers: new() { ["X-Forwarded-For"] = Remote, ["X-Forwarded-Proto"] = "http" });

        // The client is on the network and the channel is in clear: what the
        // proxy says is believed, and it says no.
        Assert.Equal(HttpStatusCode.Forbidden, answer.Status);
        Assert.Equal("TransportNotSecure", answer.Body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Forwarding_headers_from_an_address_that_is_not_a_trusted_proxy_are_ignored()
    {
        using WebApplicationFactory<Program> host = With(("Transport:TrustedProxies:0", "127.0.0.1"));
        using HttpClient client = host.CreateClient();

        Answer answer = await SignIn(client, from: "192.168.1.9", headers: new() { ["X-Forwarded-Proto"] = "https" });

        Assert.Equal(HttpStatusCode.Forbidden, answer.Status);
        Assert.Equal("TransportNotSecure", answer.Body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Declaring_a_proxy_elsewhere_does_not_make_the_loopback_trusted()
    {
        using WebApplicationFactory<Program> host = With(("Transport:TrustedProxies:0", "192.168.1.2"));
        using HttpClient client = host.CreateClient();

        Answer answer = await SignIn(client, headers: new() { ["X-Forwarded-Proto"] = "https" });

        // The framework trusts the loopback unless told otherwise. Only the
        // proxy the operator named is believed.
        Assert.Equal(HttpStatusCode.Forbidden, answer.Status);
        Assert.Equal("TransportNotSecure", answer.Body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public void A_trusted_proxy_given_by_name_stops_the_start()
    {
        using WebApplicationFactory<Program> host = With(("Transport:TrustedProxies:0", "proxy.home.arpa"));

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => host.Services);

        // Whoever controls the resolver would choose whom PIE believes.
        Assert.Contains("proxy.home.arpa", refusal.Message, StringComparison.Ordinal);
    }
}
