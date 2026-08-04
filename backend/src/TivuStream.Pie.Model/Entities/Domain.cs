using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Model.Entities;

/// <summary>
/// Domain observed during the analysis.
/// </summary>
/// <remarks>
/// Defined by the Data Model Specification. A domain is independent of the
/// device that contacted it.
/// </remarks>
public sealed record Domain
{
    /// <summary>
    /// Name of the domain.
    /// </summary>
    /// <remarks>
    /// The specification names this property <c>domain</c>. C# does not allow
    /// a member to carry the same name as its declaring type, so the property
    /// is named <c>Name</c>.
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>
    /// Category assigned to the domain.
    /// </summary>
    public required ThreatCategory Category { get; init; }

    /// <summary>
    /// Reputation index of the domain, when it has been assessed.
    /// </summary>
    /// <remarks>
    /// The reputation is produced by the Threat Engine. An Adapter never
    /// assigns it, and leaves the property empty.
    /// <para>
    /// A null value means the reputation has not been assessed yet, which is
    /// a statement about the system rather than about the domain.
    /// </para>
    /// <para>
    /// The documentation does not define the scale or the admitted values.
    /// </para>
    /// </remarks>
    public string? Reputation { get; init; }

    /// <summary>
    /// Moment the domain was observed for the first time.
    /// </summary>
    public required DateTimeOffset FirstSeen { get; init; }

    /// <summary>
    /// Moment the domain was observed most recently.
    /// </summary>
    public required DateTimeOffset LastSeen { get; init; }

    /// <summary>
    /// How <see cref="FirstSeen"/> and <see cref="LastSeen"/> are known.
    /// </summary>
    public MeasurementQuality ObservationQuality { get; init; } = MeasurementQuality.Exact;

    /// <summary>
    /// Number of times the domain was observed.
    /// </summary>
    public required long Occurrences { get; init; }
}
