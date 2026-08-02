namespace TivuStream.Pie.Api.Acquisition;

/// <summary>
/// Messages recorded by the Acquisition Flow.
/// </summary>
/// <remarks>
/// No message carries data belonging to the network of the user.
/// </remarks>
internal static partial class AcquisitionLog
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Acquisition completed for {Provider}, interval {Start} to {End}.")]
    internal static partial void Completed(
        ILogger logger,
        string provider,
        DateTimeOffset start,
        DateTimeOffset end);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Acquisition failed: {Reason}")]
    internal static partial void Failed(ILogger logger, string reason);
}
