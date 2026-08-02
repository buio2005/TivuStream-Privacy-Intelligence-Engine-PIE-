namespace TivuStream.Pie.Adapters.Technitium.Responses;

/// <summary>
/// Payload of the top clients call.
/// </summary>
internal sealed class TopClientsResponse
{
    /// <summary>
    /// Clients that produced the most queries within the interval.
    /// </summary>
    public List<TopClient>? TopClients { get; set; }
}

/// <summary>
/// Payload of the top domains call.
/// </summary>
internal sealed class TopDomainsResponse
{
    /// <summary>
    /// Domains most frequently requested within the interval.
    /// </summary>
    public List<TopDomain>? TopDomains { get; set; }
}

/// <summary>
/// Client observed by the server.
/// </summary>
internal sealed class TopClient
{
    /// <summary>
    /// Network address of the client.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Host name obtained through a reverse lookup, when available.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Number of queries produced by the client.
    /// </summary>
    public long Hits { get; set; }
}

/// <summary>
/// Domain observed by the server.
/// </summary>
internal sealed class TopDomain
{
    /// <summary>
    /// Name of the domain.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Number of times the domain was requested.
    /// </summary>
    public long Hits { get; set; }
}
