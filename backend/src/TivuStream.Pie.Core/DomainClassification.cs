using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Core;

/// <summary>
/// What the system knows about the category of a domain, and how it knows it.
/// </summary>
public sealed record DomainClassification
{
    /// <summary>
    /// A domain present in no list.
    /// </summary>
    /// <remarks>
    /// Unclassified means the system did not find the domain, not that it
    /// found it harmless. No provenance is carried, because there is no list
    /// to attribute the statement to.
    /// </remarks>
    public static DomainClassification Unclassified { get; } = new()
    {
        Category = ThreatCategory.Unknown,
    };

    /// <summary>
    /// Category assigned to the domain.
    /// </summary>
    public required ThreatCategory Category { get; init; }

    /// <summary>
    /// Reliability of the classification.
    /// </summary>
    public ConfidenceLevel? Confidence { get; init; }

    /// <summary>
    /// Name of the list the classification comes from.
    /// </summary>
    public string? SourceName { get; init; }

    /// <summary>
    /// Last successful update of that list.
    /// </summary>
    /// <remarks>
    /// Null when the classification comes from a list that has never been
    /// updated successfully, which is a statement the interface is required to
    /// pass on rather than hide.
    /// </remarks>
    public DateTimeOffset? SourceUpdatedAt { get; init; }
}
