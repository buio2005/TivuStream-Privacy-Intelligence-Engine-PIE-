namespace TivuStream.Pie.Adapters.Technitium.Responses;

/// <summary>
/// Payload of the installed applications call.
/// </summary>
/// <remarks>
/// Technitium keeps query logging in an optional application. Its presence
/// decides whether the correlation between devices and domains can be
/// obtained at all.
/// </remarks>
internal sealed class AppsResponse
{
    /// <summary>
    /// Applications installed on the server.
    /// </summary>
    public List<InstalledApp>? Apps { get; set; }
}

/// <summary>
/// Application installed on the server.
/// </summary>
internal sealed class InstalledApp
{
    /// <summary>
    /// Name of the application.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Version of the application.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Components the application provides.
    /// </summary>
    public List<AppComponent>? DnsApps { get; set; }
}

/// <summary>
/// Component provided by an installed application.
/// </summary>
internal sealed class AppComponent
{
    /// <summary>
    /// Identifier of the component, required when querying it.
    /// </summary>
    public string? ClassPath { get; set; }
}
