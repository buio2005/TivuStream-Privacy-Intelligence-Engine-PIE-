namespace TivuStream.Pie.Model.Entities;

/// <summary>
/// Complete state of the network at a given moment.
/// </summary>
/// <remarks>
/// Defined by the Data Model Specification. A snapshot is the basis for
/// reports, temporal comparisons and historical analysis.
/// <para>
/// A snapshot describes a moment that has already passed, so it is never
/// modified once produced.
/// </para>
/// </remarks>
public sealed record NetworkSnapshot
{
    /// <summary>
    /// Unique identifier of the snapshot.
    /// </summary>
    public required Guid SnapshotId { get; init; }

    /// <summary>
    /// Moment the snapshot refers to.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Identifier of the Data Source the snapshot originates from.
    /// </summary>
    public required Guid SourceId { get; init; }

    /// <summary>
    /// Aggregated statistics of the network.
    /// </summary>
    public required Statistics Statistics { get; init; }

    /// <summary>
    /// Devices identified at the moment of the snapshot.
    /// </summary>
    public IReadOnlyList<Device> Devices { get; init; } = [];

    /// <summary>
    /// Alerts produced by the Core.
    /// </summary>
    public IReadOnlyList<Alert> Alerts { get; init; } = [];

    /// <summary>
    /// Recommendations produced by the Core.
    /// </summary>
    public IReadOnlyList<Recommendation> Recommendations { get; init; } = [];

    /// <summary>
    /// Network Privacy and Security Score computed for the snapshot.
    /// </summary>
    public required Npss Npss { get; init; }
}
