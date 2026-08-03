using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Storage;

/// <summary>
/// What was observed during a single observation period.
/// </summary>
public sealed record StoredAcquisition
{
    /// <summary>
    /// Period the observation refers to.
    /// </summary>
    public required ObservationPeriod Period { get; init; }

    /// <summary>
    /// Moment the observation was made.
    /// </summary>
    /// <remarks>
    /// Distinct from the period: the same period may be observed several
    /// times while it is in progress, and only the last observation is kept.
    /// </remarks>
    public required DateTimeOffset ObservedAt { get; init; }

    /// <summary>
    /// Data Source the observation came from.
    /// </summary>
    public required DataSource DataSource { get; init; }

    /// <summary>
    /// Statistics observed during the period.
    /// </summary>
    public required Statistics Statistics { get; init; }

    /// <summary>
    /// Devices observed during the period.
    /// </summary>
    public IReadOnlyList<Device> Devices { get; init; } = [];

    /// <summary>
    /// Domains observed during the period.
    /// </summary>
    public IReadOnlyList<Domain> Domains { get; init; } = [];
}
