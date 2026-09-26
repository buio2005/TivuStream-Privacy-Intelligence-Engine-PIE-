using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using TivuStream.Pie.Api.Transport;

namespace TivuStream.Pie.Api.Installation;

/// <summary>
/// Says where PIE can be opened, and which fingerprint the browser should show.
/// </summary>
/// <remarks>
/// Installation Specification, Commands. Started by hand, PIE writes this when
/// it starts; under a service nobody sees what it writes, and this command is
/// how the person finds out. It reads the certificate the service uses and
/// never creates one: a certificate made here would not be the one the
/// browser is shown.
/// </remarks>
internal sealed class AccessCommand
{
    internal const int Done = 0;

    internal const int Refused = 1;

    private readonly TransportOptions _transport;
    private readonly MachineNames? _machine;
    private readonly X509Certificate2? _provided;

    public AccessCommand(TransportOptions transport, MachineNames? machine, X509Certificate2? provided)
    {
        _transport = transport;
        _machine = machine;
        _provided = provided;
    }

    internal int Run(TextWriter output)
    {
        string local = $"http://localhost:{_transport.HttpPort}";

        if (_transport.HttpsPort <= 0 || _machine is null)
        {
            output.WriteLine($"PIE opens only on this computer, at {local}");
            output.WriteLine("It is closed to the network: Transport:HttpsPort is 0.");

            return Done;
        }

        using X509Certificate2? certificate = _provided is null ? Generated() : null;
        X509Certificate2? shown = _provided ?? certificate;

        if (shown is null)
        {
            output.WriteLine("PIE has not created its certificate yet. Start the service, then run this again.");

            return Refused;
        }

        // The local address first, and apart: the warning below is about the
        // other addresses, and placed after this one it read as if it were
        // about this one (field test of 2026-09-27).
        output.WriteLine($"On this computer, open {local}");
        output.WriteLine("  It starts with http, not https, and that is right: this address never leaves the computer.");
        output.WriteLine();
        output.WriteLine("From another device, open one of these addresses:");

        foreach (string address in TransportSetup.Addresses(_transport.HttpsPort, _machine))
        {
            output.WriteLine($"  {address}");
        }

        output.WriteLine();
        output.WriteLine("On the other device, the browser will warn that the connection is not private.");
        output.WriteLine("Open the details of the certificate and check that its SHA-256 fingerprint is this one:");
        output.WriteLine();
        output.WriteLine($"  {GeneratedCertificate.Fingerprint(shown)}");
        output.WriteLine();
        output.WriteLine("If it is not the same, do not enter your password.");
        output.WriteLine(string.Create(
            CultureInfo.InvariantCulture,
            $"The certificate is valid until {shown.NotAfter.ToUniversalTime():yyyy-MM-dd}."));

        return Done;
    }

    private X509Certificate2? Generated()
    {
        // The copy without the key: the key belongs to the account of the
        // service, and the fingerprint needs none of it.
        string path = Path.Combine(Path.GetFullPath(_transport.CertificateDirectory), GeneratedCertificate.PublicFileName);

        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return X509CertificateLoader.LoadCertificateFromFile(path);
        }
        catch (Exception exception) when (exception is CryptographicException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
