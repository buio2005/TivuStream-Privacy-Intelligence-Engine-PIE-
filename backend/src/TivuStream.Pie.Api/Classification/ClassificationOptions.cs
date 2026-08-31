namespace TivuStream.Pie.Api.Classification;

/// <summary>
/// Settings governing how the classification lists are kept current.
/// </summary>
public sealed class ClassificationOptions
{
    /// <summary>
    /// Hours between two updates of a list.
    /// </summary>
    /// <remarks>
    /// A list is fetched only when it has never been fetched, or when what is
    /// held is older than this. Downloading on every start would hammer the
    /// source and would tell the person nothing new.
    /// </remarks>
    public int UpdateIntervalHours { get; set; } = 24;
}
