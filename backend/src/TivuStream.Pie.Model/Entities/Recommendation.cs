namespace TivuStream.Pie.Model.Entities;

/// <summary>
/// Suggestion generated automatically by the system.
/// </summary>
/// <remarks>
/// Defined by the Data Model Specification.
/// <para>
/// The Threat Intelligence Specification states that every recommendation is
/// linked to the threat that generated it, but the Data Model Specification
/// defines no property carrying that link. The relation is therefore not
/// represented here.
/// </para>
/// </remarks>
public sealed record Recommendation
{
    /// <summary>
    /// Unique identifier of the recommendation.
    /// </summary>
    public required Guid RecommendationId { get; init; }

    /// <summary>
    /// Priority assigned to the recommendation.
    /// </summary>
    /// <remarks>
    /// The documentation does not define the admitted values.
    /// </remarks>
    public required string Priority { get; init; }

    /// <summary>
    /// Short title of the recommendation.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Description of the recommendation.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Moment the recommendation was generated.
    /// </summary>
    public required DateTimeOffset GeneratedAt { get; init; }
}
