namespace TivuStream.Pie.Model.Enums;

/// <summary>
/// Qualitative status corresponding to a Network Privacy and Security Score.
/// </summary>
/// <remarks>
/// The values are the Score Range defined by the NPSS Specification.
/// <para>
/// The specification defines no fallback value, so the members follow the
/// order of the published table. This makes <see cref="Excellent"/> the
/// default value of the type, which is only correct because the status is
/// always computed from a score and is never left unset.
/// </para>
/// </remarks>
public enum ScoreStatus
{
    /// <summary>
    /// Score between 90 and 100.
    /// </summary>
    Excellent = 0,

    /// <summary>
    /// Score between 75 and 89.
    /// </summary>
    Good = 1,

    /// <summary>
    /// Score between 60 and 74.
    /// </summary>
    Fair = 2,

    /// <summary>
    /// Score between 40 and 59.
    /// </summary>
    Warning = 3,

    /// <summary>
    /// Score between 0 and 39.
    /// </summary>
    Critical = 4,
}
