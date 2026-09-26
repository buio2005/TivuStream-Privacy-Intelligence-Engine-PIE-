namespace TivuStream.Pie.Api.Installation;

/// <summary>
/// The folder an installed PIE keeps its data and configuration in.
/// </summary>
/// <remarks>
/// Installation Specification, Where Things Live. A service is started from
/// a folder of the system's choosing, <c>C:\Windows\System32</c> on Windows:
/// a path relative to it would put the database of the person there, or
/// nowhere. Installed, PIE is told where its data lives, and every relative
/// path of the configuration is taken from there.
/// <para>
/// Without it, as during development, paths stay relative to the folder the
/// program is started from.
/// </para>
/// </remarks>
internal static class DataDirectory
{
    internal const string SettingName = "DataDirectory";

    internal const string LocalSettingsFile = "appsettings.Local.json";

    /// <summary>
    /// The folder configured, as a full path, or nothing when none is.
    /// </summary>
    internal static string? From(IConfiguration configuration)
    {
        string? configured = configuration[SettingName];

        return string.IsNullOrWhiteSpace(configured) ? null : Path.GetFullPath(configured);
    }

    /// <summary>
    /// A path of the configuration, taken from the data folder when it is
    /// relative and a data folder is configured.
    /// </summary>
    internal static string Resolve(string? dataDirectory, string path)
    {
        return dataDirectory is null || string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path)
            ? path
            : Path.Combine(dataDirectory, path);
    }
}
