using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Microsoft.Extensions.Configuration;
using TivuStream.Pie.Api.Authentication;
using TivuStream.Pie.Api.Transport;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies the certificate PIE generates, and the names it answers to.
/// </summary>
/// <remarks>
/// Transport Security Specification, T3, T4, T5 and T8.
/// </remarks>
public sealed class TransportTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "pie-tls-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private static MachineNames Names(params string[] extra)
    {
        List<string> names = ["localhost", "pie-test-host"];
        List<IPAddress> addresses = [IPAddress.Loopback, IPAddress.IPv6Loopback, IPAddress.Parse("192.168.1.5")];

        foreach (string value in extra)
        {
            if (IPAddress.TryParse(value, out IPAddress? address))
            {
                addresses.Add(address);
            }
            else
            {
                names.Add(value);
            }
        }

        return new MachineNames(names, addresses);
    }

    // ------------------------------------------------------------------
    // T3: the names, and nothing it could sign
    // ------------------------------------------------------------------

    [Fact]
    public void The_certificate_names_the_computer_by_every_name_and_address()
    {
        using X509Certificate2 certificate = GeneratedCertificate.Create(Names("pie.home.arpa"), Now);

        HashSet<string> carried = GeneratedCertificate.NamesOf(certificate);

        Assert.Superset(
            new HashSet<string>(["localhost", "pie-test-host", "pie.home.arpa", "127.0.0.1", "::1", "192.168.1.5"]),
            carried);
    }

    [Fact]
    public void The_certificate_is_for_a_server_and_cannot_sign_another_certificate()
    {
        using X509Certificate2 certificate = GeneratedCertificate.Create(Names(), Now);

        X509BasicConstraintsExtension constraints = certificate.Extensions.OfType<X509BasicConstraintsExtension>().Single();
        X509KeyUsageExtension usage = certificate.Extensions.OfType<X509KeyUsageExtension>().Single();
        X509EnhancedKeyUsageExtension purposes = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().Single();

        // Whoever obtained its key could impersonate PIE, and nothing else.
        Assert.False(constraints.CertificateAuthority);
        Assert.True(constraints.Critical);
        Assert.Equal(X509KeyUsageFlags.DigitalSignature, usage.KeyUsages);
        Assert.Equal("1.3.6.1.5.5.7.3.1", Assert.Single(purposes.EnhancedKeyUsages.Cast<Oid>()).Value);
        Assert.NotNull(certificate.GetECDsaPublicKey());
    }

    [Fact]
    public void The_certificate_lasts_as_long_as_every_browser_accepts()
    {
        using X509Certificate2 certificate = GeneratedCertificate.Create(Names(), Now);

        Assert.Equal(Now.AddDays(397).UtcDateTime, certificate.NotAfter.ToUniversalTime(), TimeSpan.FromSeconds(1));

        // Valid already for a device whose clock is slightly behind.
        Assert.True(certificate.NotBefore.ToUniversalTime() < Now.UtcDateTime);
    }

    // ------------------------------------------------------------------
    // T5: renewed when it must be, and only then
    // ------------------------------------------------------------------

    [Fact]
    public void A_kept_certificate_is_used_again_with_the_same_fingerprint()
    {
        (X509Certificate2 first, bool firstGenerated) = GeneratedCertificate.LoadOrCreate(_directory, Names(), Now);
        (X509Certificate2 again, bool againGenerated) = GeneratedCertificate.LoadOrCreate(_directory, Names(), Now.AddDays(10));

        using (first)
        using (again)
        {
            // A new certificate would bring the warning of the browser back
            // on every device, for nothing.
            Assert.True(firstGenerated);
            Assert.False(againGenerated);
            Assert.Equal(GeneratedCertificate.Fingerprint(first), GeneratedCertificate.Fingerprint(again));
            Assert.True(again.HasPrivateKey);
        }
    }

    [Fact]
    public void A_certificate_close_to_expiry_is_replaced()
    {
        (X509Certificate2 first, _) = GeneratedCertificate.LoadOrCreate(_directory, Names(), Now);
        (X509Certificate2 renewed, bool generated) = GeneratedCertificate.LoadOrCreate(_directory, Names(), Now.AddDays(370));

        using (first)
        using (renewed)
        {
            Assert.True(generated);
            Assert.NotEqual(GeneratedCertificate.Fingerprint(first), GeneratedCertificate.Fingerprint(renewed));
        }
    }

    [Fact]
    public void A_certificate_is_replaced_when_the_computer_has_an_address_it_does_not_name()
    {
        (X509Certificate2 first, _) = GeneratedCertificate.LoadOrCreate(_directory, Names(), Now);
        (X509Certificate2 renewed, bool generated) = GeneratedCertificate.LoadOrCreate(_directory, Names("192.168.1.77"), Now);

        using (first)
        using (renewed)
        {
            // The router gave the computer a new address: the browser would
            // refuse the old certificate for it.
            Assert.True(generated);
            Assert.Contains("192.168.1.77", GeneratedCertificate.NamesOf(renewed));
        }
    }

    [Fact]
    public void A_certificate_naming_more_than_the_computer_has_is_kept()
    {
        (X509Certificate2 first, _) = GeneratedCertificate.LoadOrCreate(_directory, Names("192.168.1.77"), Now);
        (X509Certificate2 again, bool generated) = GeneratedCertificate.LoadOrCreate(_directory, Names(), Now);

        using (first)
        using (again)
        {
            // An address the computer no longer has harms nobody.
            Assert.False(generated);
        }
    }

    [Fact]
    public void An_unreadable_certificate_is_replaced()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, GeneratedCertificate.FileName), "not a certificate");

        (X509Certificate2 certificate, bool generated) = GeneratedCertificate.LoadOrCreate(_directory, Names(), Now);

        using (certificate)
        {
            Assert.True(generated);
            Assert.True(certificate.HasPrivateKey);
        }
    }

    // ------------------------------------------------------------------
    // T4: the key is protected
    // ------------------------------------------------------------------

    [Fact]
    public void Only_the_user_running_pie_can_read_the_file_holding_the_key()
    {
        (X509Certificate2 certificate, _) = GeneratedCertificate.LoadOrCreate(_directory, Names(), Now);
        certificate.Dispose();

        string path = Path.Combine(_directory, GeneratedCertificate.FileName);

        if (OperatingSystem.IsWindows())
        {
            FileSecurity security = new FileInfo(path).GetAccessControl();

            AuthorizationRuleCollection rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));

            // Nothing inherited from the folder, and a single rule: this user.
            Assert.True(security.AreAccessRulesProtected);
            FileSystemAccessRule only = Assert.IsType<FileSystemAccessRule>(Assert.Single(rules.Cast<AuthorizationRule>()));
            Assert.Equal(WindowsIdentity.GetCurrent().User, only.IdentityReference);
        }
        else
        {
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
        }
    }

    [Fact]
    public void The_fingerprint_is_written_as_browsers_show_it()
    {
        using X509Certificate2 certificate = GeneratedCertificate.Create(Names(), Now);

        string fingerprint = GeneratedCertificate.Fingerprint(certificate);

        Assert.Matches("^([0-9A-F]{2}:){31}[0-9A-F]{2}$", fingerprint);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(certificate.RawData)), fingerprint.Replace(":", "", StringComparison.Ordinal));
    }

    // ------------------------------------------------------------------
    // T8: the names of the computer are accepted
    // ------------------------------------------------------------------

    [Fact]
    public void With_the_network_channel_open_the_names_of_the_computer_are_accepted()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        string[] hosts = HostPolicy.AllowedHosts(configuration, Names("2001:db8::5"));

        Assert.Contains("pie-test-host", hosts);
        Assert.Contains("192.168.1.5", hosts);

        // As an IPv6 address arrives in the Host header.
        Assert.Contains("[2001:db8::5]", hosts);
        Assert.Contains("localhost", hosts);
    }

    [Fact]
    public void Without_the_network_channel_only_the_loopback_is_accepted()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        Assert.Equal(["localhost", "127.0.0.1", "[::1]"], HostPolicy.AllowedHosts(configuration));
    }

    [Fact]
    public void The_names_of_the_computer_do_not_make_any_name_acceptable()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [HostPolicy.SettingName] = "*" })
            .Build();

        Assert.Throws<InvalidOperationException>(() => HostPolicy.AllowedHosts(configuration, Names()));
    }
}
