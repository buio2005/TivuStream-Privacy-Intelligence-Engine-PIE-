namespace TivuStream.Pie.Model.Enums;

/// <summary>
/// Severity assigned to a detected threat or to a generated alert.
/// </summary>
/// <remarks>
/// The values are the severity levels defined by the Threat Intelligence
/// Specification, listed from the least to the most severe.
/// <para>
/// That specification defines the scale for threats and states that the
/// severity of an alert depends on the severity of the threat. Applying the
/// same scale to alerts is therefore an inference and awaits confirmation in
/// the documentation.
/// </para>
/// </remarks>
public enum SeverityLevel
{
    /// <summary>
    /// Informational only. No action is required.
    /// </summary>
    Informational = 0,

    /// <summary>
    /// Low severity.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium severity.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High severity.
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical severity.
    /// </summary>
    Critical = 4,
}
