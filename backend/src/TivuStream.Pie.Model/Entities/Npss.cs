using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Model.Entities;

/// <summary>
/// Network Privacy and Security Score.
/// </summary>
/// <remarks>
/// Defined by the Data Model Specification and by the NPSS Specification.
/// <para>
/// The type is named after the official acronym, which the Glossary requires
/// to be used consistently in place of the extended form.
/// </para>
/// </remarks>
public sealed record Npss
{
    /// <summary>
    /// Overall score, on a scale from 0 to 100.
    /// </summary>
    public required int OverallScore { get; init; }

    /// <summary>
    /// Qualitative status corresponding to the overall score.
    /// </summary>
    public required ScoreStatus Status { get; init; }

    /// <summary>
    /// Direction of change compared with the previous score, when a
    /// comparable one exists.
    /// </summary>
    /// <remarks>
    /// Absent for the first score, and whenever the coverage has changed.
    /// Scores computed over different portions of the evaluation system are
    /// not comparable, so declaring a direction would state something the
    /// system cannot know.
    /// </remarks>
    public ScoreTrend? Trend { get; init; }

    /// <summary>
    /// Portion of the evaluation system that was actually observed, on a
    /// scale from 0 to 100.
    /// </summary>
    /// <remarks>
    /// Equals the sum of the obtainable points of every area.
    /// <para>
    /// A value below 100 means the score was computed on part of the
    /// indicators only. Scores with different coverage are not comparable,
    /// and a change in coverage interrupts the historical trend.
    /// </para>
    /// </remarks>
    public required decimal Coverage { get; init; }

    /// <summary>
    /// Version of the algorithm that produced the score.
    /// </summary>
    /// <remarks>
    /// The NPSS Specification gives the algorithm a version of its own,
    /// independent of the version of the project.
    /// </remarks>
    public required string AlgorithmVersion { get; init; }

    /// <summary>
    /// Moment the score was produced.
    /// </summary>
    public required DateTimeOffset GeneratedAt { get; init; }

    /// <summary>
    /// Contribution of each evaluation area to the overall score.
    /// </summary>
    public IReadOnlyList<ScoreComponent> Breakdown { get; init; } = [];
}
