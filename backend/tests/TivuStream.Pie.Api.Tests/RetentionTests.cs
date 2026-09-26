using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TivuStream.Pie.Api.Storage;
using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Storage;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies how the host applies the retention of the history.
/// </summary>
/// <remarks>
/// Persistence Specification 1.3.0: a retention below its minimum stops the
/// start, consolidation runs as a scheduled operation at start, and the log
/// carries counts only.
/// </remarks>
public sealed class RetentionTests
{
    [Fact]
    public void A_retention_below_its_minimum_stops_the_start()
    {
        using PieApplication app = new();

        using WebApplicationFactory<Program> refused = app.WithWebHostBuilder(
            builder => builder.UseSetting("Storage:Retention:HourlyDays", "1"));

        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => refused.Services);

        Assert.Contains("Storage:Retention:HourlyDays", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_retention_in_force_is_stated_when_the_host_starts()
    {
        using PieApplication app = new();

        _ = app.Services;

        Assert.Contains(
            app.Logs,
            line => line.Contains("hourly detail 30 days, daily aggregates 12 months, monthly aggregates 5 years", StringComparison.Ordinal));
    }

    [Fact]
    public async Task At_start_the_hours_old_enough_are_consolidated_and_the_log_carries_only_counts()
    {
        using PieApplication app = new();

        // Two months before the clock of the host.
        ObservationPeriod old = Seed.HoursAgo(24 * 60);

        Seed.Acquisition(app, old, domains: [Seed.Domain("tracker.example", old, occurrences: 3)]);
        Seed.Acquisition(app, Seed.HoursAgo(0));

        using RetentionService service = ActivatorUtilities.CreateInstance<RetentionService>(app.Services);

        await service.StartAsync(CancellationToken.None);

        // The service runs on a thread of its own: its first pass is waited
        // for, not assumed.
        await ConsolidatedAsync(app);

        await service.StopAsync(CancellationToken.None);

        AcquisitionRepository repository = app.Services.GetRequiredService<AcquisitionRepository>();

        // The day stands where its hour was; the recent hour is untouched.
        Assert.Equal(2, repository.CountPeriods());
        Domain domain = Assert.Single(repository.GetDomainsSince(old.Start.AddDays(-1)), found => found.Name == "tracker.example");
        Assert.Equal(3, domain.Occurrences);

        Assert.Contains(app.Logs, line => line.Contains("History consolidated: 1 days", StringComparison.Ordinal));
        Assert.DoesNotContain(app.Logs, line => line.Contains("tracker.example", StringComparison.Ordinal));
    }

    private static async Task ConsolidatedAsync(PieApplication app)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            lock (app.Logs)
            {
                if (app.Logs.Exists(line => line.StartsWith("History consolidated", StringComparison.Ordinal)))
                {
                    return;
                }
            }

            await Task.Delay(50);
        }
    }
}
