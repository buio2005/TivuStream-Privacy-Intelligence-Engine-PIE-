using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Api.Contracts;

/// <summary>
/// The statistics of the network over the window, with the window itself.
/// </summary>
public sealed record ObservedStatistics
{
    /// <summary>
    /// Interval actually covered, absent when nothing was observed in the
    /// window.
    /// </summary>
    public required ObservationPeriod? Period { get; init; }

    /// <summary>
    /// Hourly periods that exist within the window.
    /// </summary>
    public required int PeriodsObserved { get; init; }

    /// <summary>
    /// Hourly periods the window asked for.
    /// </summary>
    public required int PeriodsRequested { get; init; }

    /// <summary>
    /// Statistics aggregated over the periods included.
    /// </summary>
    /// <remarks>
    /// Absent, never a set of zeros, when no period falls in the window: zeros
    /// would say the network queried nothing, when nothing was observed.
    /// </remarks>
    public required Statistics? Statistics { get; init; }
}
