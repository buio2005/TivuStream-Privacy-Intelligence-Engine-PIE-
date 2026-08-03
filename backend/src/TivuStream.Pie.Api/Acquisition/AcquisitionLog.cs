namespace TivuStream.Pie.Api.Acquisition;

/// <summary>
/// Messages recorded by the Acquisition Flow.
/// </summary>
/// <remarks>
/// The messages carry counts and never the data itself: no address, no
/// domain, nothing belonging to the network of the user.
/// </remarks>
internal static partial class AcquisitionLog
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Acquisition completed for {Provider}, period {Period}: {Devices} devices, {Domains} domains, {Activities} interactions.")]
    internal static partial void Completed(
        ILogger logger,
        string provider,
        DateTimeOffset period,
        int devices,
        int domains,
        int activities);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Score evaluated: {Score} out of 100, coverage {Coverage}.")]
    internal static partial void Scored(ILogger logger, int score, decimal coverage);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Acquisition failed: {Reason}")]
    internal static partial void Failed(ILogger logger, string reason);
}
