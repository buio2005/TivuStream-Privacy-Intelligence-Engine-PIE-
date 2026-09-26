namespace TivuStream.Pie.Api.Installation;

/// <summary>
/// Messages about the installation PIE is running as.
/// </summary>
internal static partial class InstallationLog
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "TivuStream PIE {Version}, {Mode}, data in {DataDirectory}.")]
    internal static partial void Started(ILogger logger, string version, string mode, string dataDirectory);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No account exists. A service shows no setup code: create the first administrator on this machine with 'tivustream-pie reset-password <name>'.")]
    internal static partial void AdministratorMissing(ILogger logger);
}
