namespace TivuStream.Pie.Adapters.Technitium.Responses;

/// <summary>
/// Payload of the session information call.
/// </summary>
/// <remarks>
/// A single call provides the version of the server, its name, the state of
/// DNSSEC validation and the permissions granted to the token in use.
/// <para>
/// This call returns its fields at the root of the answer rather than inside
/// a payload property.
/// </para>
/// </remarks>
internal sealed class SessionResponse : TechnitiumRootResponse
{
    /// <summary>
    /// Details of the server and of the current session.
    /// </summary>
    public SessionInfo? Info { get; set; }
}

/// <summary>
/// Details of the server and of the current session.
/// </summary>
internal sealed class SessionInfo
{
    /// <summary>
    /// Version of the DNS server.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Name the DNS server is configured with.
    /// </summary>
    public string? DnsServerDomain { get; set; }

    /// <summary>
    /// Indicates whether DNSSEC validation is enabled.
    /// </summary>
    public bool DnssecValidation { get; set; }

    /// <summary>
    /// Permissions granted to the account the token belongs to.
    /// </summary>
    public Dictionary<string, SessionPermission>? Permissions { get; set; }
}

/// <summary>
/// Permissions granted over a single section of the server.
/// </summary>
internal sealed class SessionPermission
{
    /// <summary>
    /// Indicates whether the section can be read.
    /// </summary>
    public bool CanView { get; set; }
}
