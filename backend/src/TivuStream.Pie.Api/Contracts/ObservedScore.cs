using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Api.Contracts;

/// <summary>
/// The most recent score, with the window it evaluated.
/// </summary>
/// <remarks>
/// A score describes the twenty-four hours ending with the period in which it
/// was produced (NPSS Specification, Evaluation Window). When acquisitions
/// have stopped, that is not the window ending now, and the answer says which
/// one it is.
/// </remarks>
public sealed record ObservedScore
{
    /// <summary>
    /// Interval actually covered by the window the score evaluated.
    /// </summary>
    public required ObservationPeriod? Period { get; init; }

    /// <summary>
    /// Hourly periods that exist within that window.
    /// </summary>
    public required int PeriodsObserved { get; init; }

    /// <summary>
    /// Hourly periods the window asked for.
    /// </summary>
    public required int PeriodsRequested { get; init; }

    /// <summary>
    /// The score.
    /// </summary>
    public required Npss Score { get; init; }
}
