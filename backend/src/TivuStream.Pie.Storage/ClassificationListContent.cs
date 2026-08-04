namespace TivuStream.Pie.Storage;

/// <summary>
/// What was read from the file of a classification list.
/// </summary>
public sealed record ClassificationListContent
{
    /// <summary>
    /// A list that has never been downloaded.
    /// </summary>
    /// <remarks>
    /// A described list with no file is not a list that has aged: it is a list
    /// that does not exist yet, and classifies nothing.
    /// </remarks>
    public static ClassificationListContent Absent { get; } = new()
    {
        Present = false,
        Entries = new HashSet<string>(StringComparer.Ordinal),
        UnreadableLines = 0,
    };

    /// <summary>
    /// Whether the file exists.
    /// </summary>
    public required bool Present { get; init; }

    /// <summary>
    /// Domains read from the file, in lower case.
    /// </summary>
    public required IReadOnlySet<string> Entries { get; init; }

    /// <summary>
    /// Lines the reader did not recognise as carrying a domain.
    /// </summary>
    /// <remarks>
    /// Comments and blank lines are counted here as well. The figure is kept
    /// so that a list understood in part is never reported as one understood
    /// in full.
    /// </remarks>
    public required int UnreadableLines { get; init; }
}
