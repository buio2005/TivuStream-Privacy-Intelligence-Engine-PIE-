namespace TivuStream.Pie.Model.Entities;

/// <summary>
/// Interaction between a device and a domain.
/// </summary>
/// <remarks>
/// Defined by the Data Model Specification.
/// </remarks>
public sealed record DomainActivity
{
    /// <summary>
    /// Identifier of the device that produced the activity.
    /// </summary>
    public required Guid DeviceId { get; init; }

    /// <summary>
    /// Name of the domain that was contacted.
    /// </summary>
    public required string Domain { get; init; }

    /// <summary>
    /// Number of queries issued towards the domain.
    /// </summary>
    public required long QueryCount { get; init; }

    /// <summary>
    /// Indicates whether the activity was blocked by the Data Source.
    /// </summary>
    public required bool Blocked { get; init; }

    /// <summary>
    /// Transport used for the queries.
    /// </summary>
    /// <remarks>
    /// The documentation does not define the admitted values.
    /// </remarks>
    public required string Protocol { get; init; }

    /// <summary>
    /// Moment the activity was observed for the first time.
    /// </summary>
    public required DateTimeOffset FirstSeen { get; init; }

    /// <summary>
    /// Moment the activity was observed most recently.
    /// </summary>
    public required DateTimeOffset LastSeen { get; init; }
}
