using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;
using TivuStream.Pie.Adapters.Technitium;
using TivuStream.Pie.Api.Installation;
using TivuStream.Pie.Api.Transport;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies the commands the installation runs at the terminal.
/// </summary>
/// <remarks>
/// Installation Specification 1.3.0, Commands: <c>configure</c> writes only a
/// connection that works, says what the Data Source offers and what it costs
/// in privacy, never shows the token; <c>access</c> says where PIE opens and
/// with which fingerprint, and never creates a certificate.
/// </remarks>
public sealed class InstallationCommandTests : IDisposable
{
    private const string Token = "a-technitium-token-never-shown";

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "pie-install-" + Guid.NewGuid().ToString("N"));

    private readonly StringWriter _output = new();

    public InstallationCommandTests()
    {
        Directory.CreateDirectory(_directory);
    }

    private string SettingsPath => Path.Combine(_directory, "appsettings.Local.json");

    public void Dispose()
    {
        _output.Dispose();
        Directory.Delete(_directory, recursive: true);
    }

    // ------------------------------------------------------------------
    // configure
    // ------------------------------------------------------------------

    [Fact]
    public async Task A_connection_that_works_is_written_and_the_token_is_never_shown()
    {
        int exit = await Configure(Technitium.Working(), "http://192.168.1.10:5380\n");

        Assert.Equal(ConfigureCommand.Done, exit);

        JsonNode written = JsonNode.Parse(File.ReadAllText(SettingsPath))!;

        Assert.Equal("http://192.168.1.10:5380/", written["Technitium"]!["BaseAddress"]!.GetValue<string>());
        Assert.Equal(Token, written["Technitium"]!["ApiToken"]!.GetValue<string>());
        Assert.True(Guid.TryParse(written["Technitium"]!["DataSourceId"]!.GetValue<string>(), out Guid id) && id != Guid.Empty);
        Assert.DoesNotContain(Token, _output.ToString(), StringComparison.Ordinal);

        // Not the token, only how much of it arrived.
        Assert.Contains($"Received {Token.Length} characters.", _output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_empty_address_means_technitium_on_this_computer()
    {
        await Configure(Technitium.Working(), "\n");

        JsonNode written = JsonNode.Parse(File.ReadAllText(SettingsPath))!;

        Assert.Equal("http://localhost:5380/", written["Technitium"]!["BaseAddress"]!.GetValue<string>());
    }

    [Fact]
    public async Task What_else_the_settings_hold_is_kept_and_so_is_the_identity_of_the_source()
    {
        Guid existing = Guid.NewGuid();

        File.WriteAllText(
            SettingsPath,
            $$"""
            {
              "AllowedHosts": "localhost;pie.home.arpa",
              "Technitium": { "DataSourceId": "{{existing}}", "BaseAddress": "http://old:5380", "ApiToken": "old" }
            }
            """);

        await Configure(Technitium.Working(), "\n");

        JsonNode written = JsonNode.Parse(File.ReadAllText(SettingsPath))!;

        Assert.Equal("localhost;pie.home.arpa", written["AllowedHosts"]!.GetValue<string>());

        // The history already recorded belongs to this source.
        Assert.Equal(existing.ToString(), written["Technitium"]!["DataSourceId"]!.GetValue<string>());
    }

    [Fact]
    public async Task A_refused_token_is_said_in_plain_words_and_nothing_is_written()
    {
        int exit = await Configure(Technitium.RefusingTheToken(), "\nn\n");

        Assert.Equal(ConfigureCommand.Refused, exit);
        Assert.False(File.Exists(SettingsPath));
        Assert.Contains("refused the token", _output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(Token, _output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_address_where_nothing_answers_is_said_in_plain_words_and_nothing_is_written()
    {
        int exit = await Configure(Technitium.Unreachable(), "\nn\n");

        Assert.Equal(ConfigureCommand.Refused, exit);
        Assert.False(File.Exists(SettingsPath));
        Assert.Contains("Nothing answered at that address", _output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_token_that_cannot_read_the_dashboard_is_not_written_since_PIE_would_see_nothing()
    {
        int exit = await Configure(Technitium.Working(dashboard: false), "\nn\n");

        Assert.Equal(ConfigureCommand.Refused, exit);
        Assert.False(File.Exists(SettingsPath));
        Assert.Contains("permission to view the Dashboard", _output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Trying_again_after_a_failure_writes_the_connection_that_works()
    {
        Technitium server = Technitium.Working();

        ConfigureCommand command = new(options => server.AdapterFor(options), SettingsPath);

        int exit = await command.RunAsync(
            new StringReader("\ny\n\n"),
            new ScriptedPrompt("a-mistyped-token", Token),
            _output,
            CancellationToken.None);

        JsonNode written = JsonNode.Parse(File.ReadAllText(SettingsPath))!;

        Assert.Equal(ConfigureCommand.Done, exit);
        Assert.Equal(Token, written["Technitium"]!["ApiToken"]!.GetValue<string>());
    }

    [Fact]
    public async Task A_missing_capability_says_what_is_lost_and_how_to_add_it()
    {
        await Configure(Technitium.Working(settings: false, queryLogs: false), "\n");

        string said = _output.ToString();

        Assert.Contains("that part of the score stays out", said, StringComparison.Ordinal);
        Assert.Contains("permission to view Settings", said, StringComparison.Ordinal);
        Assert.Contains("install the 'Query Logs (Sqlite)' app", said, StringComparison.Ordinal);

        // What the app costs is said before it is installed, not after.
        Assert.Contains("keep every single query", said, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Device_activity_that_is_available_is_declared_with_what_technitium_keeps()
    {
        await Configure(Technitium.Working(queryLogs: true), "\n");

        string said = _output.ToString();

        Assert.Contains("which device contacted which domain", said, StringComparison.Ordinal);
        Assert.Contains("Technitium keep every single query", said, StringComparison.Ordinal);
        Assert.Contains("PIE keeps only hourly totals", said, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_settings_file_that_cannot_be_read_is_left_as_it_is()
    {
        File.WriteAllText(SettingsPath, "{ this is not JSON");

        int exit = await Configure(Technitium.Working(), "\n");

        Assert.Equal(ConfigureCommand.Refused, exit);
        Assert.Equal("{ this is not JSON", File.ReadAllText(SettingsPath));
    }

    [Fact]
    public async Task An_address_that_is_not_one_is_asked_again()
    {
        int exit = await Configure(Technitium.Working(), "not an address\n\n");

        Assert.Equal(ConfigureCommand.Done, exit);
        Assert.Contains("That is not an address PIE can use", _output.ToString(), StringComparison.Ordinal);
    }

    // ------------------------------------------------------------------
    // access
    // ------------------------------------------------------------------

    [Fact]
    public void Closed_to_the_network_access_says_where_PIE_opens_on_this_computer()
    {
        int exit = new AccessCommand(new TransportOptions { HttpsPort = 0 }, machine: null, provided: null).Run(_output);

        Assert.Equal(AccessCommand.Done, exit);
        Assert.Contains("only on this computer, at http://localhost:5000", _output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Before_the_service_has_created_a_certificate_access_says_so_and_creates_none()
    {
        TransportOptions transport = new() { CertificateDirectory = Path.Combine(_directory, "tls") };

        int exit = new AccessCommand(transport, Names(), provided: null).Run(_output);

        Assert.Equal(AccessCommand.Refused, exit);
        Assert.Contains("Start the service", _output.ToString(), StringComparison.Ordinal);
        Assert.False(Directory.Exists(transport.CertificateDirectory));
    }

    [Fact]
    public void Access_names_the_addresses_and_the_fingerprint_of_the_certificate_in_use()
    {
        TransportOptions transport = new() { CertificateDirectory = Path.Combine(_directory, "tls") };

        (X509Certificate2 certificate, _) = GeneratedCertificate.LoadOrCreate(transport.CertificateDirectory, Names(), DateTimeOffset.UtcNow);

        using (certificate)
        {
            int exit = new AccessCommand(transport, Names(), provided: null).Run(_output);

            string said = _output.ToString();

            Assert.Equal(AccessCommand.Done, exit);
            Assert.Contains("https://pie-test-host:5443", said, StringComparison.Ordinal);
            Assert.Contains("https://192.168.1.5:5443", said, StringComparison.Ordinal);
            Assert.Contains(GeneratedCertificate.Fingerprint(certificate), said, StringComparison.Ordinal);
            Assert.Contains("do not enter your password", said, StringComparison.Ordinal);

            // The plain address of this computer comes first, with why it is
            // plain, so that the warning is not read as being about it.
            Assert.True(
                said.IndexOf("http://localhost:5000", StringComparison.Ordinal) < said.IndexOf("not private", StringComparison.Ordinal));
            Assert.Contains("never leaves the computer", said, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_copy_access_reads_carries_the_certificate_and_not_its_key()
    {
        string directory = Path.Combine(_directory, "tls");

        (X509Certificate2 certificate, _) = GeneratedCertificate.LoadOrCreate(directory, Names(), DateTimeOffset.UtcNow);

        using (certificate)
        using (X509Certificate2 copy = X509CertificateLoader.LoadCertificateFromFile(Path.Combine(directory, GeneratedCertificate.PublicFileName)))
        {
            Assert.False(copy.HasPrivateKey);
            Assert.Equal(certificate.Thumbprint, copy.Thumbprint);
        }
    }

    // ------------------------------------------------------------------

    private async Task<int> Configure(Technitium server, string typed)
    {
        ConfigureCommand command = new(options => server.AdapterFor(options), SettingsPath);

        return await command.RunAsync(new StringReader(typed), new ScriptedPrompt(Token, Token, Token), _output, CancellationToken.None);
    }

    private static MachineNames Names()
    {
        return new MachineNames(
            ["localhost", "pie-test-host"],
            [IPAddress.Loopback, IPAddress.IPv6Loopback, IPAddress.Parse("192.168.1.5")]);
    }

    /// <summary>
    /// A stand-in for Technitium, answering the calls <c>configure</c> makes.
    /// </summary>
    private sealed class Technitium : HttpMessageHandler
    {
        private bool _unreachable;
        private bool _refusing;
        private bool _dashboard = true;
        private bool _settings = true;
        private bool _queryLogs;

        internal static Technitium Working(bool dashboard = true, bool settings = true, bool queryLogs = false)
        {
            return new Technitium { _dashboard = dashboard, _settings = settings, _queryLogs = queryLogs };
        }

        internal static Technitium RefusingTheToken()
        {
            return new Technitium { _refusing = true };
        }

        internal static Technitium Unreachable()
        {
            return new Technitium { _unreachable = true };
        }

        internal TechnitiumAdapter AdapterFor(TechnitiumOptions options)
        {
            return new TechnitiumAdapter(new HttpClient(this, disposeHandler: false), options);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_unreachable)
            {
                throw new HttpRequestException("No connection could be made.");
            }

            string path = request.RequestUri!.AbsolutePath;

            // Only the token of the tests is known to this Technitium.
            bool known = request.Headers.Authorization?.Parameter == Token;

            string body = _refusing || !known
                ? """{ "status": "invalid-token", "errorMessage": "Invalid token or session expired." }"""
                : path.EndsWith("/api/user/session/get", StringComparison.Ordinal)
                    ? Session()
                    : Apps();

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) });
        }

        private string Session()
        {
            return $$"""
                {
                  "status": "ok",
                  "info": {
                    "version": "14.3",
                    "dnsServerDomain": "dns.home.test",
                    "permissions": {
                      "Dashboard": { "canView": {{Json(_dashboard)}} },
                      "Settings":  { "canView": {{Json(_settings)}} }
                    }
                  }
                }
                """;
        }

        private string Apps()
        {
            string classPath = _queryLogs ? "QueryLogsSqlite.App" : "AdvancedBlocking.App";

            return $$"""
                { "status": "ok", "response": { "apps": [ { "name": "Some App", "version": "1.0", "dnsApps": [ { "classPath": "{{classPath}}" } ] } ] } }
                """;
        }

        private static string Json(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
