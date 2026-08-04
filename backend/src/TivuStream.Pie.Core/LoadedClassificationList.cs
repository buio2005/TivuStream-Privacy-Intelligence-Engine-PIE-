using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Core;

/// <summary>
/// A classification list together with the domains it contains.
/// </summary>
/// <remarks>
/// The Core receives what it needs and fetches nothing: it does not open the
/// list files and does not know where they are kept.
/// </remarks>
public sealed record LoadedClassificationList
{
    /// <summary>
    /// What the list declares about itself.
    /// </summary>
    public required ClassificationList Descriptor { get; init; }

    /// <summary>
    /// Domains the list contains, in lower case.
    /// </summary>
    /// <remarks>
    /// A set is expected rather than a sequence: a list is consulted once per
    /// label of every domain observed, and a linear search would make the cost
    /// of classifying grow with the size of the lists.
    /// </remarks>
    public required IReadOnlySet<string> Entries { get; init; }
}
