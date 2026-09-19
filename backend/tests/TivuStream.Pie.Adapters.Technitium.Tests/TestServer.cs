namespace TivuStream.Pie.Adapters.Technitium.Tests;

/// <summary>
/// Builds an Adapter against a fake server, with answers shaped like the ones
/// recorded in the Technitium API Reconnaissance.
/// </summary>
internal static class TestServer
{
    internal const string Token = "token-that-must-never-leak";

    internal static readonly AcquisitionWindow Window = new(
        new DateTimeOffset(2026, 9, 1, 10, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 9, 1, 11, 0, 0, TimeSpan.Zero));

    internal static TechnitiumAdapter AdapterFor(FakeTechnitiumHandler handler)
    {
        HttpClient client = new(handler, disposeHandler: false);

        return new TechnitiumAdapter(
            client,
            new TechnitiumOptions
            {
                DataSourceId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                BaseAddress = new Uri("http://technitium.test"),
                ApiToken = Token,
            });
    }

    /// <summary>
    /// Session answer. The session call returns its fields at the root, unlike
    /// the dashboard calls, which nest them under <c>response</c>.
    /// </summary>
    internal static string Session(bool dashboard = true, bool settings = true)
    {
        return $$"""
            {
              "status": "ok",
              "info": {
                "version": "14.3",
                "dnsServerDomain": "dns.home.test",
                "dnssecValidation": true,
                "permissions": {
                  "Dashboard": { "canView": {{Json(dashboard)}} },
                  "Settings":  { "canView": {{Json(settings)}} }
                }
              }
            }
            """;
    }

    internal static string Apps(bool queryLogsInstalled)
    {
        string components = queryLogsInstalled
            ? """{ "classPath": "QueryLogsSqlite.App" }"""
            : """{ "classPath": "AdvancedBlocking.App" }""";

        return $$"""
            {
              "status": "ok",
              "response": {
                "apps": [ { "name": "Some App", "version": "1.0", "dnsApps": [ {{components}} ] } ]
              }
            }
            """;
    }

    internal static string Ok(string payload)
    {
        return $$"""{ "status": "ok", "response": {{payload}} }""";
    }

    private static string Json(bool value)
    {
        return value ? "true" : "false";
    }
}
