using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Model.Entities;

/// <summary>
/// List of domains associated with a category, kept locally.
/// </summary>
/// <remarks>
/// Defined by the Data Model Specification. Its behaviour is described by the
/// Threat Intelligence Specification.
/// <para>
/// Matching happens on the device alone. A list is downloaded; the domains
/// observed on the network are never sent anywhere.
/// </para>
/// </remarks>
public sealed record ClassificationList
{
    /// <summary>
    /// Name of the list.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Address the list is downloaded from.
    /// </summary>
    public required Uri SourceUrl { get; init; }

    /// <summary>
    /// Category attributed to the domains the list contains.
    /// </summary>
    public required ThreatCategory Category { get; init; }

    /// <summary>
    /// Licence of the list.
    /// </summary>
    /// <remarks>
    /// The specification makes the licence mandatory. A list with no declared
    /// licence is not distributed with the project.
    /// </remarks>
    public required string Licence { get; init; }

    /// <summary>
    /// Moment of the last successful update.
    /// </summary>
    /// <remarks>
    /// A failed attempt does not change this value: the stored list remains in
    /// use and simply grows older.
    /// <para>
    /// A null value means the list has never been downloaded, and is therefore
    /// not a list that has aged but a list that does not exist yet.
    /// </para>
    /// </remarks>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// Number of domains the list contains.
    /// </summary>
    public required int EntryCount { get; init; }

    /// <summary>
    /// Indicates whether the list takes part in the classification.
    /// </summary>
    public required bool Enabled { get; init; }
}
