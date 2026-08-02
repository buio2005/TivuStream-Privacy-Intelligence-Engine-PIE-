namespace TivuStream.Pie.Model.Enums;

/// <summary>
/// Reliability of a classification produced by the system.
/// </summary>
/// <remarks>
/// The values are defined by the Threat Intelligence Specification. The
/// confidence level distinguishes a certain classification from a
/// probabilistic one.
/// </remarks>
public enum ConfidenceLevel
{
    /// <summary>
    /// Low confidence.
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium confidence.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// High confidence.
    /// </summary>
    High = 2,
}
