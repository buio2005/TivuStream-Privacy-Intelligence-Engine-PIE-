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
}
