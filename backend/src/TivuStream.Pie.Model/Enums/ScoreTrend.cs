namespace TivuStream.Pie.Model.Enums;

/// <summary>
/// Direction of change of the Network Privacy and Security Score over time.
/// </summary>
/// <remarks>
/// The values are defined by the NPSS Specification. The specification
/// designates no fallback value, so the members follow the published order.
/// </remarks>
public enum ScoreTrend
{
    /// <summary>
    /// The score is increasing.
    /// </summary>
    Improving = 0,

    /// <summary>
    /// The score shows no significant change.
    /// </summary>
    Stable = 1,

    /// <summary>
    /// The score is decreasing.
    /// </summary>
    Decreasing = 2,
}
