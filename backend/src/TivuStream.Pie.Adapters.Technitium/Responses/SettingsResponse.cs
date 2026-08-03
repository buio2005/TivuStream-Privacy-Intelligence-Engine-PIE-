namespace TivuStream.Pie.Adapters.Technitium.Responses;

/// <summary>
/// Payload of the server settings call.
/// </summary>
/// <remarks>
/// The server exposes well over a hundred settings. Only those bearing on
/// privacy and security are mapped: reading the rest would mean carrying
/// around configuration the project has no use for.
/// </remarks>
internal sealed class SettingsResponse
{
    /// <summary>
    /// Indicates whether DNSSEC signatures are validated.
    /// </summary>
    public bool DnssecValidation { get; set; }

    /// <summary>
    /// Indicates whether queries are accepted over TLS.
    /// </summary>
    public bool EnableDnsOverTls { get; set; }

    /// <summary>
    /// Indicates whether queries are accepted over HTTPS.
    /// </summary>
    public bool EnableDnsOverHttps { get; set; }

    /// <summary>
    /// Indicates whether queries are accepted over QUIC.
    /// </summary>
    public bool EnableDnsOverQuic { get; set; }

    /// <summary>
    /// Indicates whether the name sent to authoritative servers is minimised.
    /// </summary>
    public bool QnameMinimization { get; set; }

    /// <summary>
    /// Indicates whether the subnet of the client is forwarded upstream.
    /// </summary>
    public bool EDnsClientSubnet { get; set; }

    /// <summary>
    /// Indicates whether domain filtering is active.
    /// </summary>
    public bool EnableBlocking { get; set; }

    /// <summary>
    /// Addresses of the configured filter lists.
    /// </summary>
    public List<string>? BlockListUrls { get; set; }

    /// <summary>
    /// Hours between two updates of the filter lists.
    /// </summary>
    public int BlockListUpdateIntervalHours { get; set; }
}
