using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Core;

/// <summary>
/// Everything the score needs in order to be computed.
/// </summary>
/// <remarks>
/// The Core receives what it needs and fetches nothing. It does not know
/// where the data came from, nor where the result will go.
/// </remarks>
public sealed record NpssEvaluationInput
{
    /// <summary>
    /// Statistics of the period being evaluated.
    /// </summary>
    public required Statistics Statistics { get; init; }

    /// <summary>
    /// Settings of the Data Source, when it provides them.
    /// </summary>
    public SourceConfiguration? Configuration { get; init; }

    /// <summary>
    /// Whether the Data Source answered during the acquisition.
    /// </summary>
    public required bool SourceReachable { get; init; }

    /// <summary>
    /// Observation periods actually recorded over the continuity window.
    /// </summary>
    public required int ObservedPeriods { get; init; }

    /// <summary>
    /// Observation periods that were expected over the same window.
    /// </summary>
    public required int ExpectedPeriods { get; init; }

    /// <summary>
    /// Score of the previous evaluation, when one exists and was produced.
    /// </summary>
    public int? PreviousOverallScore { get; init; }

    /// <summary>
    /// Coverage of the previous evaluation, when one exists.
    /// </summary>
    public decimal? PreviousCoverage { get; init; }
}
