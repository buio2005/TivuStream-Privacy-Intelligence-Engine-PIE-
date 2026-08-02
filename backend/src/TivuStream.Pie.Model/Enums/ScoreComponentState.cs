namespace TivuStream.Pie.Model.Enums;

/// <summary>
/// Extent to which an evaluation area of the score could be assessed.
/// </summary>
/// <remarks>
/// Defined by the NPSS Specification.
/// <para>
/// The portion of an area that was not observed is excluded from the
/// calculation. It is never scored as zero and never assumed favourable, so
/// missing data can neither penalise nor improve the result.
/// </para>
/// </remarks>
public enum ScoreComponentState
{
    /// <summary>
    /// No indicator of the area could be assessed.
    /// </summary>
    /// <remarks>
    /// The score of the component carries no meaning in this state.
    /// </remarks>
    NotMeasurable = 0,

    /// <summary>
    /// Only some of the indicators of the area could be assessed.
    /// </summary>
    PartiallyMeasured = 1,

    /// <summary>
    /// Every indicator of the area could be assessed.
    /// </summary>
    Measured = 2,
}
