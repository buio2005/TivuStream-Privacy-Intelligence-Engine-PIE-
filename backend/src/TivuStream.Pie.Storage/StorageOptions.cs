namespace TivuStream.Pie.Storage;

/// <summary>
/// Settings governing where and how data is kept.
/// </summary>
public sealed class StorageOptions
{
    /// <summary>
    /// Path of the database file.
    /// </summary>
    /// <remarks>
    /// A single local file, in keeping with the Local First and Self Hosted
    /// principles: no service to install, no port to expose, no separate
    /// process to look after.
    /// </remarks>
    public string DatabasePath { get; set; } = "data/pie.db";

    /// <summary>
    /// Directory holding the classification list files.
    /// </summary>
    /// <remarks>
    /// The domains a list contains are kept as text files rather than in the
    /// database. A list can hold hundreds of thousands of names, and a text
    /// file can be opened with any editor, which is what makes a
    /// classification something the person can check rather than accept.
    /// </remarks>
    public string ListDirectoryPath { get; set; } = "data/lists";
}
