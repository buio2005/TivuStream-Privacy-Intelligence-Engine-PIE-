namespace TivuStream.Pie.Api.Classification;

/// <summary>
/// Messages recorded while preparing the classification.
/// </summary>
/// <remarks>
/// The messages carry counts and list names. They never carry a domain: the
/// log of a privacy tool is not a place where the network of the person using
/// it may appear.
/// </remarks>
internal static partial class ClassificationLog
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Added {Count} classification lists absent from the database.")]
    internal static partial void DefaultsAdded(ILogger logger, int count);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Classification ready with {Lists} lists and {Entries} entries.")]
    internal static partial void Loaded(ILogger logger, int lists, int entries);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "List '{List}' has never been downloaded and classifies nothing.")]
    internal static partial void NeverDownloaded(ILogger logger, string list);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "List '{List}' yielded no entry from {Unreadable} unreadable lines. The format is not one the reader understands.")]
    internal static partial void NotUnderstood(ILogger logger, string list, int unreadable);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No list is available. Every domain will be reported as unclassified.")]
    internal static partial void NothingToClassifyWith(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "List '{List}' updated: {Entries} entries, {Unreadable} lines not understood.")]
    internal static partial void Updated(ILogger logger, string list, int entries, int unreadable);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "List '{List}' could not be updated ({Reason}). The copy from {UpdatedAt} stays in use and keeps ageing.")]
    internal static partial void UpdateFailedButHeld(
        ILogger logger,
        string list,
        DateTimeOffset? updatedAt,
        string reason);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "List '{List}' could not be downloaded ({Reason}) and no earlier copy exists. It classifies nothing.")]
    internal static partial void UpdateFailedAndAbsent(ILogger logger, string list, string reason);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "List '{List}' exceeds the size accepted and was not downloaded.")]
    internal static partial void TooLarge(ILogger logger, string list);
}
