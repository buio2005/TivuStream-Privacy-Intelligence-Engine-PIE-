using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Model.Entities;

/// <summary>
/// Significant event produced by the Core.
/// </summary>
/// <remarks>
/// Defined by the Data Model Specification.
/// </remarks>
public sealed record Alert
{
    /// <summary>
    /// Unique identifier of the alert.
    /// </summary>
    public required Guid AlertId { get; init; }

    /// <summary>
    /// Category assigned to the alert.
    /// </summary>
    /// <remarks>
    /// The documentation does not define the admitted values.
    /// </remarks>
    public required string Category { get; init; }

    /// <summary>
    /// Severity assigned to the alert.
    /// </summary>
    public required SeverityLevel Severity { get; init; }

    /// <summary>
    /// Short title of the alert.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Description of the alert.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Moment the alert was generated.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Current status of the alert.
    /// </summary>
    /// <remarks>
    /// The documentation does not define the admitted values.
    /// </remarks>
    public required string Status { get; init; }
}
