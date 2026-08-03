using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Api.Acquisition;

/// <summary>
/// Outcome of a single acquisition cycle.
/// </summary>
/// <remarks>
/// Holds what the Adapter returned. It is not an entity of the Unified Data
/// Model: the Core, which will assemble the Network Snapshot, does not exist
/// yet, and this type stands in for its result only while that is the case.
/// </remarks>
public sealed record AcquisitionResult
{
    /// <summary>
    /// Moment the cycle was attempted.
    /// </summary>
    public required DateTimeOffset AttemptedAt { get; init; }

    /// <summary>
    /// Indicates whether the cycle completed.
    /// </summary>
    public required bool Succeeded { get; init; }

    /// <summary>
    /// Reason the cycle did not complete.
    /// </summary>
    public string? Failure { get; init; }

    /// <summary>
    /// Beginning of the observation period.
    /// </summary>
    public DateTimeOffset? PeriodStart { get; init; }

    /// <summary>
    /// End of the observation period.
    /// </summary>
    public DateTimeOffset? PeriodEnd { get; init; }

    /// <summary>
    /// Description of the Data Source as it was at the time of the cycle.
    /// </summary>
    public DataSource? DataSource { get; init; }

    /// <summary>
    /// Statistics acquired during the cycle.
    /// </summary>
    public Statistics? Statistics { get; init; }
}
