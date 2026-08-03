namespace TivuStream.Pie.Adapters.Technitium.Responses;

/// <summary>
/// Payload of the query log call.
/// </summary>
internal sealed class QueryLogsResponse
{
    /// <summary>
    /// Page this answer refers to.
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of pages the interrogation produced.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Number of entries the interrogation produced.
    /// </summary>
    public long TotalEntries { get; set; }

    /// <summary>
    /// Entries of this page.
    /// </summary>
    public List<QueryLogEntry>? Entries { get; set; }
}

/// <summary>
/// Single logged query.
/// </summary>
/// <remarks>
/// This is the only place where the device and the domain appear together.
/// It is also the most sensitive record the system ever handles, being the
/// browsing history of a device, and it never leaves the Adapter: it is
/// aggregated here and discarded.
/// </remarks>
internal sealed class QueryLogEntry
{
    /// <summary>
    /// Moment the query was answered.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Address of the device that issued the query.
    /// </summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// Transport the query travelled on.
    /// </summary>
    public string? Protocol { get; set; }

    /// <summary>
    /// How the server answered.
    /// </summary>
    public string? ResponseType { get; set; }

    /// <summary>
    /// Domain that was queried.
    /// </summary>
    public string? QName { get; set; }
}
