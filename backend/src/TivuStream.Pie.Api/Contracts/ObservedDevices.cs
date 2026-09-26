using TivuStream.Pie.Model;
using TivuStream.Pie.Storage;

namespace TivuStream.Pie.Api.Contracts;

/// <summary>
/// The devices observed over the window, with the window itself.
/// </summary>
/// <remarks>
/// A list of the current hour alone, a few minutes after it began, is nearly
/// empty, and cannot be told apart from a network where hardly any device is
/// active.
/// </remarks>
public sealed record ObservedDevices
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
    /// Devices observed, one per identifier.
    /// </summary>
    public required IReadOnlyList<ObservedDevice> Devices { get; init; }
}
