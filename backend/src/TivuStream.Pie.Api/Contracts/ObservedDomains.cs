using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Api.Contracts;

/// <summary>
/// The domains observed over a window, with the window itself.
/// </summary>
/// <remarks>
/// An empty list without its interval is ambiguous: whoever reads it cannot
/// tell "the network contacted nothing" from "the hour has just begun". The
/// first is a statement about the network, the second about the moment of
/// looking, and presenting them alike is false information.
/// </remarks>
public sealed record ObservedDomains
{
    /// <summary>
    /// Interval actually covered, absent when nothing was ever observed.
    /// </summary>
    public required ObservationPeriod? Period { get; init; }

    /// <summary>
    /// Hourly periods that exist within the window.
    /// </summary>
    /// <remarks>
    /// Told apart from what was asked for. An installation running for six
    /// hours that reported a day would be stating something untrue.
    /// </remarks>
    public required int PeriodsObserved { get; init; }

    /// <summary>
    /// Hourly periods the window asked for.
    /// </summary>
    public required int PeriodsRequested { get; init; }

    /// <summary>
    /// Domains observed, aggregated over the periods included.
    /// </summary>
    public required IReadOnlyList<Domain> Domains { get; init; }
}
