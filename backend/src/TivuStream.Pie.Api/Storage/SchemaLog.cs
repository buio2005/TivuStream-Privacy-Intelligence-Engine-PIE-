namespace TivuStream.Pie.Api.Storage;

/// <summary>
/// Messages recorded while preparing the database.
/// </summary>
/// <remarks>
/// A change to the shape of stored data is never carried out silently.
/// </remarks>
internal static partial class SchemaLog
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Database created at schema version {Version}.")]
    internal static partial void Created(ILogger logger, int version);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Database schema updated from version {From} to version {To}.")]
    internal static partial void Updated(ILogger logger, int from, int to);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Copy of the database taken before the update: {Path}")]
    internal static partial void BackedUp(ILogger logger, string path);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Database schema already at version {Version}.")]
    internal static partial void Unchanged(ILogger logger, int version);
}
