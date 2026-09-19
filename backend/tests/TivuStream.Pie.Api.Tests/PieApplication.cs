using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// The real host, in memory, on a database of its own.
/// </summary>
/// <remarks>
/// Two things are taken away on purpose. The background services would reach
/// the Data Source and download classification lists from the Internet, and a
/// test must do neither. And the developer's own <c>appsettings.Local.json</c>
/// must not be read: it holds a real token, and pointing the content root at
/// an empty folder is what keeps it out.
/// </remarks>
internal sealed class PieApplication : WebApplicationFactory<Program>
{
    /// <summary>
    /// Credential configured for the Data Source. It must never appear in an
    /// answer.
    /// </summary>
    internal const string SourceToken = "source-token-that-must-never-leak";

    /// <summary>
    /// The instant the host believes it is: half past noon.
    /// </summary>
    internal static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 30, 0, TimeSpan.Zero);

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "pie-api-" + Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_directory);

        builder.UseContentRoot(_directory);

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:DatabasePath"] = Path.Combine(_directory, "pie.db"),
                ["Storage:ListDirectoryPath"] = Path.Combine(_directory, "lists"),
                ["Technitium:BaseAddress"] = "http://source.invalid",
                ["Technitium:ApiToken"] = SourceToken,
            }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        // Pooled connections hold the file open.
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    /// <summary>
    /// Calls an endpoint and returns the status with the body already parsed.
    /// </summary>
    internal async Task<(HttpStatusCode Status, JsonElement Body, string Raw)> GetAsync(string path)
    {
        using HttpClient client = CreateClient();

        using HttpResponseMessage response = await client.GetAsync(new Uri(path, UriKind.Relative));

        string raw = await response.Content.ReadAsStringAsync();

        using JsonDocument document = JsonDocument.Parse(raw);

        return (response.StatusCode, document.RootElement.Clone(), raw);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return now;
        }
    }
}
