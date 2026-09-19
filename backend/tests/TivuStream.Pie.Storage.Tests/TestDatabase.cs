using Microsoft.Data.Sqlite;
using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;
using TivuStream.Pie.Storage.Schema;

namespace TivuStream.Pie.Storage.Tests;

/// <summary>
/// A real database file, migrated to the current schema, thrown away after
/// the test.
/// </summary>
/// <remarks>
/// The repositories are verified against SQLite itself rather than a stand-in:
/// what is being checked is what the schema and the queries do, and a
/// substitute would check only itself.
/// </remarks>
internal sealed class TestDatabase : IDisposable
{
    internal static readonly Guid DataSourceId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    internal static readonly DateTimeOffset Noon = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "pie-db-" + Guid.NewGuid().ToString("N"));

    internal TestDatabase()
    {
        Connections = new SqliteConnectionFactory(
            new StorageOptions { DatabasePath = Path.Combine(_directory, "pie.db") });

        new SchemaMigrator(Connections).Migrate();

        Acquisitions = new AcquisitionRepository(Connections);
        Scores = new ScoreRepository(Connections);
    }

    internal SqliteConnectionFactory Connections { get; }

    internal AcquisitionRepository Acquisitions { get; }

    internal ScoreRepository Scores { get; }

    public void Dispose()
    {
        // Pooled connections hold the file open.
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    internal static ObservationPeriod PeriodAt(int hoursAfterNoon)
    {
        return ObservationPeriod.Containing(Noon.AddHours(hoursAfterNoon));
    }

    internal static StoredAcquisition Acquisition(
        ObservationPeriod period,
        IReadOnlyList<Domain>? domains = null,
        IReadOnlyList<DomainActivity>? activities = null,
        Statistics? statistics = null)
    {
        return new StoredAcquisition
        {
            Period = period,
            ObservedAt = period.End.AddMinutes(-1),
            DataSource = new DataSource
            {
                Id = DataSourceId,
                Name = "dns.home.test",
                Provider = "Technitium DNS Server",
                Version = "14.3",
                Status = DataSourceStatus.Online,
                Capabilities = ["Statistics", "Device", "Domain", "DomainActivity"],
                LastUpdate = period.End.AddMinutes(-1),
            },
            Statistics = statistics ?? new Statistics
            {
                TotalQueries = 1000,
                BlockedQueries = 100,
                CachedQueries = 400,
                FailedQueries = 9,
                UniqueDomains = 2,
                UniqueDomainsQuality = MeasurementQuality.LowerBound,
                ActiveDevices = 3,
                EncryptedQueries = 16,
                DnssecEnabled = true,
            },
            Domains = domains ?? [],
            DomainActivities = activities ?? [],
        };
    }

    internal static Domain DomainSeen(
        string name,
        ObservationPeriod period,
        long occurrences,
        MeasurementQuality quality = MeasurementQuality.PeriodBounded,
        ThreatCategory category = ThreatCategory.Unknown,
        ConfidenceLevel? confidence = null,
        string? source = null)
    {
        return new Domain
        {
            Name = name,
            Category = category,
            CategoryConfidence = confidence,
            CategorySource = source,
            CategorySourceUpdatedAt = source is null ? null : period.Start,
            FirstSeen = period.Start,
            LastSeen = period.End,
            ObservationQuality = quality,
            Occurrences = occurrences,
        };
    }
}
