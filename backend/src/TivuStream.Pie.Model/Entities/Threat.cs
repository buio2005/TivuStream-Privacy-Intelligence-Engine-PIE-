using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Model.Entities;

/// <summary>
/// Threat classified by the system.
/// </summary>
/// <remarks>
/// Defined by the Data Model Specification.
/// </remarks>
public sealed record Threat
{
    /// <summary>
    /// Unique identifier of the threat.
    /// </summary>
    public required Guid ThreatId { get; init; }

    /// <summary>
    /// Category assigned to the threat.
    /// </summary>
    public required ThreatCategory Category { get; init; }

    /// <summary>
    /// Severity assigned to the threat.
    /// </summary>
    public required SeverityLevel Severity { get; init; }

    /// <summary>
    /// Reliability of the classification.
    /// </summary>
    public required ConfidenceLevel Confidence { get; init; }

    /// <summary>
    /// Description of the threat.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Origin of the classification.
    /// </summary>
    /// <remarks>
    /// The Threat Intelligence Specification mentions internal rules,
    /// blocklists, external sources and correlation algorithms, but does not
    /// present them as a closed set of admitted values.
    /// </remarks>
    public required string Source { get; init; }

    /// <summary>
    /// Moment the threat was detected.
    /// </summary>
    public required DateTimeOffset DetectedAt { get; init; }
}
