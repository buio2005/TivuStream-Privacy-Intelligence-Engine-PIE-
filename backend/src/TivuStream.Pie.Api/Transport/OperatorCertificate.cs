using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace TivuStream.Pie.Api.Transport;

/// <summary>
/// The certificate the operator provides in place of the generated one.
/// </summary>
/// <remarks>
/// Transport Security Specification, Provided By The Operator. One that cannot
/// be used stops the start with the reason. Falling back on a generated
/// certificate in silence would change the fingerprint under the person's
/// eyes without telling them.
/// </remarks>
internal static class OperatorCertificate
{
    /// <summary>
    /// Loads the certificate, or says why it cannot be used.
    /// </summary>
    /// <param name="options">Where the certificate is, and how to open it.</param>
    /// <param name="now">The present instant.</param>
    /// <exception cref="InvalidOperationException">The certificate cannot be used.</exception>
    internal static X509Certificate2 Load(CertificateOptions options, DateTimeOffset now)
    {
        X509Certificate2 certificate = Read(options);

        if (!certificate.HasPrivateKey)
        {
            certificate.Dispose();

            throw Unusable(options, "it has no private key. A PEM certificate needs 'Transport:Certificate:KeyPath'.");
        }

        if (certificate.NotAfter.ToUniversalTime() <= now.UtcDateTime)
        {
            string expiry = certificate.NotAfter.ToUniversalTime().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

            certificate.Dispose();

            throw Unusable(options, $"it expired on {expiry}.");
        }

        if (certificate.NotBefore.ToUniversalTime() > now.UtcDateTime)
        {
            certificate.Dispose();

            throw Unusable(options, "it is not valid yet.");
        }

        return certificate;
    }

    private static X509Certificate2 Read(CertificateOptions options)
    {
        string path = Path.GetFullPath(options.Path);

        if (!File.Exists(path))
        {
            throw Unusable(options, "the file does not exist.");
        }

        try
        {
            if (string.IsNullOrWhiteSpace(options.KeyPath))
            {
                return X509CertificateLoader.LoadPkcs12FromFile(
                    path,
                    string.IsNullOrEmpty(options.Password) ? null : options.Password);
            }

            using X509Certificate2 pem = X509Certificate2.CreateFromPemFile(path, Path.GetFullPath(options.KeyPath));

            // Exported and read back: the operating system's TLS stack does
            // not accept the key of a PEM certificate held only in memory.
            return X509CertificateLoader.LoadPkcs12(pem.Export(X509ContentType.Pkcs12), password: null);
        }
        catch (CryptographicException exception)
        {
            throw Unusable(options, $"it cannot be read ({exception.Message.TrimEnd('.')}). Check the file and 'Transport:Certificate:Password'.");
        }
        catch (IOException)
        {
            throw Unusable(options, "a file cannot be read.");
        }
    }

    private static InvalidOperationException Unusable(CertificateOptions options, string reason)
    {
        return new InvalidOperationException($"The certificate in 'Transport:Certificate:Path' ({options.Path}) cannot be used: {reason}");
    }
}
