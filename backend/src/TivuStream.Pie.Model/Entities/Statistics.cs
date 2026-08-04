using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Model.Entities;

/// <summary>
/// Aggregated statistics of the network.
/// </summary>
/// <remarks>
/// Defined by the Data Model Specification.
/// </remarks>
public sealed record Statistics
{
    /// <summary>
    /// Total number of DNS queries observed.
    /// </summary>
    public required long TotalQueries { get; init; }

    /// <summary>
    /// Number of queries blocked by the Data Source.
    /// </summary>
    public required long BlockedQueries { get; init; }

    /// <summary>
    /// Number of queries answered from cache.
    /// </summary>
    public required long CachedQueries { get; init; }

    /// <summary>
    /// Number of queries that ended in an error.
    /// </summary>
    public required long FailedQueries { get; init; }

    /// <summary>
    /// Number of distinct domains observed.
    /// </summary>
    /// <remarks>
    /// Typically a lower bound: sources return truncated lists, so the
    /// distinct domains observed are at least this many.
    /// </remarks>
    public required int UniqueDomains { get; init; }

    /// <summary>
    /// How <see cref="UniqueDomains"/> is known.
    /// </summary>
    public MeasurementQuality UniqueDomainsQuality { get; init; } = MeasurementQuality.Exact;

    /// <summary>
    /// Number of devices considered active.
    /// </summary>
    public required int ActiveDevices { get; init; }

    /// <summary>
    /// Number of queries carried over an encrypted transport.
    /// </summary>
    public required long EncryptedQueries { get; init; }

    /// <summary>
    /// Indicates whether DNSSEC validation is enabled.
    /// </summary>
    public required bool DnssecEnabled { get; init; }
}
