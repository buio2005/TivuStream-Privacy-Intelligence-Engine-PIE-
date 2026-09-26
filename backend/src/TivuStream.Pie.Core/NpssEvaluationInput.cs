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
    /// Statistics of the window being evaluated, the last twenty-four hours.
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
    /// Domains observed in the window, already classified.
    /// </summary>
    public IReadOnlyList<Domain> Domains { get; init; } = [];

    /// <summary>
    /// Interactions between devices and domains in the window.
    /// </summary>
    public IReadOnlyList<DomainActivity> DomainActivities { get; init; } = [];

    /// <summary>
    /// Whether at least one classification list was available.
    /// </summary>
    /// <remarks>
    /// Told apart from an empty result on purpose. Without a list every domain
    /// is unclassified, and reading that as an absence of tracking would turn
    /// the lack of a tool into a good result.
    /// </remarks>
    public bool ClassificationAvailable { get; init; }

    /// <summary>
    /// Whether the Data Source provides the activity per domain.
    /// </summary>
    /// <remarks>
    /// Also told apart from an empty result: a source that cannot report which
    /// queries were blocked is a gap in the observation, while a source that
    /// reports none is a statement about the network.
    /// </remarks>
    public bool DomainActivityAvailable { get; init; }

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

    /// <summary>
    /// Algorithm version of the previous evaluation, when one exists.
    /// </summary>
    public string? PreviousAlgorithmVersion { get; init; }
}
