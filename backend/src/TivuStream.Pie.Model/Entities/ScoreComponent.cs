using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Model.Entities;

/// <summary>
/// Contribution of a single evaluation area to the score.
/// </summary>
/// <remarks>
/// Defined by the Data Model Specification. Retaining the factors is the
/// requirement that makes every variation of the score explainable, as
/// demanded by the Transparency principle.
/// </remarks>
public sealed record ScoreComponent
{
    /// <summary>
    /// Evaluation area this contribution refers to.
    /// </summary>
    public required ScoreComponentType Component { get; init; }

    /// <summary>
    /// Extent to which the area could be assessed.
    /// </summary>
    public required ScoreComponentState State { get; init; }

    /// <summary>
    /// Points obtained by the area.
    /// </summary>
    public required decimal Score { get; init; }

    /// <summary>
    /// Points the area could actually obtain, given the indicators that were
    /// assessed.
    /// </summary>
    /// <remarks>
    /// Equals <see cref="Weight"/> when the area is fully measured, is lower
    /// when it is partially measured, and is zero when it is not measurable.
    /// <para>
    /// The difference between <see cref="Weight"/> and this value is the
    /// portion that was not observed. That portion is excluded from the
    /// calculation and cannot improve the overall score.
    /// </para>
    /// </remarks>
    public required decimal MaxScore { get; init; }

    /// <summary>
    /// Nominal weight of the area within the overall score.
    /// </summary>
    /// <remarks>
    /// Defined by the scoring algorithm and independent of what could be
    /// observed.
    /// </remarks>
    public required int Weight { get; init; }

    /// <summary>
    /// Factors that determined the score of the area.
    /// </summary>
    /// <remarks>
    /// When the state is not <see cref="ScoreComponentState.Measured"/>, the
    /// factors state which indicators were assessed, which were not, and why.
    /// </remarks>
    public IReadOnlyList<ScoreFactor> Factors { get; init; } = [];
}
