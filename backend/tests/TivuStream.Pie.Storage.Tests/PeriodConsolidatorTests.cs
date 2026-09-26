using System.Globalization;
using Microsoft.Data.Sqlite;
using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;
using Xunit;

namespace TivuStream.Pie.Storage.Tests;

/// <summary>
/// Verifies how the history is consolidated as it ages.
/// </summary>
/// <remarks>
/// Persistence Specification 1.3.0, Retention: whole days and months, each in
/// one step; what adds up is added up, what does not is declared; the hours
/// actually observed travel with the period; device activity is not carried
/// into months; the last score is kept as it was; nothing outlives the monthly
/// retention; the detail removed is not left readable.
/// </remarks>
public sealed class PeriodConsolidatorTests : IDisposable
{
    // 2026-09-01, the day the test hours belong to.
    private static readonly DateTimeOffset DayStart = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    // Thirty-four days later: the first of September has ended more than
    // thirty days ago.
    private static readonly DateTimeOffset MonthLater = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    private readonly TestDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    // ------------------------------------------------------------------
    // Hours into days
    // ------------------------------------------------------------------

    [Fact]
    public void The_hours_of_a_day_old_enough_become_one_day_that_says_how_many_hours_were_observed()
    {
        SaveHours(0, 1, 2);

        ConsolidationOutcome outcome = Consolidator().Consolidate(MonthLater);

        Assert.Equal(1, outcome.DaysWritten);

        StoredPeriod day = Assert.Single(Periods());

        Assert.Equal("Day", day.Granularity);
        Assert.Equal(DayStart, day.Start);
        Assert.Equal(DayStart.AddDays(1), day.End);

        // Three hours of twenty-four: without this, the day would pass for a
        // quiet one.
        Assert.Equal(3, day.ObservedHours);
    }

    [Fact]
    public void Counts_of_queries_add_up_exactly_and_distinct_domains_become_a_floor()
    {
        SaveHours(0, 1, 2);

        Consolidator().Consolidate(MonthLater);

        Statistics? day = _database.Acquisitions.GetStatisticsSince(DayStart);

        Assert.NotNull(day);
        Assert.Equal(3000, day.TotalQueries);
        Assert.Equal(300, day.BlockedQueries);
        Assert.Equal(48, day.EncryptedQueries);
        Assert.Equal(MeasurementQuality.LowerBound, day.UniqueDomainsQuality);

        // Every hour said two, and four names were kept over the day: the
        // floor is four, not two and not six.
        Assert.Equal(4, day.UniqueDomains);
    }

    [Fact]
    public void A_domain_keeps_its_occurrences_summed_and_the_most_recent_classification()
    {
        ObservationPeriod first = Hour(0);
        ObservationPeriod last = Hour(5);

        _database.Acquisitions.Save(TestDatabase.Acquisition(
            first,
            domains: [TestDatabase.DomainSeen("tracker.example", first, occurrences: 4)]));

        _database.Acquisitions.Save(TestDatabase.Acquisition(
            last,
            domains:
            [
                TestDatabase.DomainSeen(
                    "tracker.example",
                    last,
                    occurrences: 6,
                    quality: MeasurementQuality.Exact,
                    category: ThreatCategory.Tracking,
                    confidence: ConfidenceLevel.High,
                    source: "list-one"),
            ]));

        Consolidator().Consolidate(MonthLater);

        Domain domain = Assert.Single(_database.Acquisitions.GetDomainsSince(DayStart));

        Assert.Equal(10, domain.Occurrences);
        Assert.Equal(ThreatCategory.Tracking, domain.Category);
        Assert.Equal("list-one", domain.CategorySource);
        Assert.Equal(first.Start, domain.FirstSeen);
        Assert.Equal(last.End, domain.LastSeen);

        // Known no better than its vaguest hour.
        Assert.Equal(MeasurementQuality.PeriodBounded, domain.ObservationQuality);
    }

    [Fact]
    public void Device_activity_is_summed_per_device_domain_and_outcome_in_a_day()
    {
        Guid device = Guid.NewGuid();

        foreach (int hour in new[] { 0, 1 })
        {
            ObservationPeriod period = Hour(hour);

            _database.Acquisitions.Save(TestDatabase.Acquisition(
                period,
                activities:
                [
                    Activity(device, "tracker.example", period, queries: 3, blocked: false),
                    Activity(device, "tracker.example", period, queries: 1, blocked: true),
                ]));
        }

        Consolidator().Consolidate(MonthLater);

        List<ObservedActivity> found = _database.Acquisitions.GetActivitiesSince("tracker.example", DayStart);

        Assert.Equal(2, found.Count);
        Assert.Equal(6, found.Single(activity => !activity.Blocked).QueryCount);
        Assert.Equal(2, found.Single(activity => activity.Blocked).QueryCount);
    }

    [Fact]
    public void A_day_is_consolidated_whole_and_only_once_it_has_ended_long_enough_ago()
    {
        SaveHours(0, 1);

        // The first of September ends on the second; thirty days after that is
        // the second of October.
        DateTimeOffset tooEarly = DayStart.AddDays(1).AddDays(30).AddMinutes(-1);

        Assert.Equal(0, Consolidator().Consolidate(tooEarly).DaysWritten);
        Assert.All(Periods(), period => Assert.Equal("Hour", period.Granularity));

        Assert.Equal(1, Consolidator().Consolidate(tooEarly.AddMinutes(1)).DaysWritten);
    }

    [Fact]
    public void Recent_hours_are_left_as_they_are()
    {
        SaveHours(0);

        ObservationPeriod recent = ObservationPeriod.Containing(MonthLater);
        _database.Acquisitions.Save(TestDatabase.Acquisition(recent));

        Consolidator().Consolidate(MonthLater);

        List<StoredPeriod> periods = Periods();

        Assert.Equal(["Day", "Hour"], periods.Select(period => period.Granularity));
        Assert.Equal(recent.Start, periods[1].Start);
    }

    [Fact]
    public void Consolidating_again_changes_nothing()
    {
        SaveHours(0, 1, 2);

        Consolidator().Consolidate(MonthLater);
        List<StoredPeriod> once = Periods();

        ConsolidationOutcome again = Consolidator().Consolidate(MonthLater);

        Assert.Equal(new ConsolidationOutcome(0, 0, 0), again);
        Assert.Equal(once, Periods());
        Assert.Equal(3000, _database.Acquisitions.GetStatisticsSince(DayStart)?.TotalQueries);
    }

    [Fact]
    public void An_hour_observed_after_its_day_was_consolidated_joins_the_day_instead_of_overlapping_it()
    {
        SaveHours(0, 1);
        Consolidator().Consolidate(MonthLater);

        SaveHours(5);
        Consolidator().Consolidate(MonthLater);

        StoredPeriod day = Assert.Single(Periods());

        Assert.Equal(DayStart, day.Start);
        Assert.Equal(3, day.ObservedHours);
        Assert.Equal(3000, _database.Acquisitions.GetStatisticsSince(DayStart)?.TotalQueries);
    }

    [Fact]
    public void Observing_an_hour_again_never_erases_a_day_that_begins_with_it()
    {
        SaveHours(0, 1);
        Consolidator().Consolidate(MonthLater);

        Assert.Throws<StorageException>(() => SaveHours(0));

        Assert.Equal(2, Assert.Single(Periods()).ObservedHours);
    }

    // ------------------------------------------------------------------
    // The day of the person
    // ------------------------------------------------------------------

    [Fact]
    public void A_day_follows_the_time_zone_of_the_computer()
    {
        TimeZoneInfo twoHoursAhead = TimeZoneInfo.CreateCustomTimeZone("Test+2", TimeSpan.FromHours(2), "Test+2", "Test+2");

        // 21:00 and 22:00 UTC: eleven at night on the first, and midnight on
        // the second, where the person reads.
        SaveHours(21, 22);

        Consolidator(twoHoursAhead).Consolidate(MonthLater);

        List<StoredPeriod> days = Periods();

        Assert.Equal(2, days.Count);
        Assert.Equal(new DateTimeOffset(2026, 8, 31, 22, 0, 0, TimeSpan.Zero), days[0].Start);
        Assert.Equal(new DateTimeOffset(2026, 9, 1, 22, 0, 0, TimeSpan.Zero), days[1].Start);
        Assert.All(days, day => Assert.Equal(1, day.ObservedHours));
    }

    [Fact]
    public void The_day_the_clocks_go_forward_lasts_twenty_three_hours()
    {
        TimeZoneInfo rome = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

        ObservationPeriod period = ObservationPeriod.Containing(new DateTimeOffset(2026, 3, 29, 10, 0, 0, TimeSpan.Zero));
        _database.Acquisitions.Save(TestDatabase.Acquisition(period));

        Consolidator(rome).Consolidate(new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero));

        StoredPeriod day = Assert.Single(Periods());

        Assert.Equal(new DateTimeOffset(2026, 3, 28, 23, 0, 0, TimeSpan.Zero), day.Start);
        Assert.Equal(TimeSpan.FromHours(23), day.End - day.Start);
    }

    // ------------------------------------------------------------------
    // Score
    // ------------------------------------------------------------------

    [Fact]
    public void A_day_keeps_the_last_score_produced_in_it_as_it_was()
    {
        SaveHours(0, 1, 2);
        _database.Scores.Save(TestDatabase.DataSourceId, Hour(0), Score(overall: 40));
        _database.Scores.Save(TestDatabase.DataSourceId, Hour(2), Score(overall: 70));

        Consolidator().Consolidate(MonthLater);

        Npss? kept = _database.Scores.GetLatest();

        Assert.NotNull(kept);
        Assert.Equal(70, kept.OverallScore);
        Assert.Equal("test-1", kept.AlgorithmVersion);
        Assert.Equal(0.85m, kept.Coverage);
        Assert.Single(kept.Breakdown);
        Assert.Equal(1, Count("score"));
    }

    [Fact]
    public void A_day_in_which_no_score_was_produced_has_none()
    {
        SaveHours(0, 1);

        Consolidator().Consolidate(MonthLater);

        Assert.Null(_database.Scores.GetLatest());
    }

    // ------------------------------------------------------------------
    // Days into months, and deletion
    // ------------------------------------------------------------------

    [Fact]
    public void Days_old_enough_become_a_month_without_the_activity_of_each_device()
    {
        Guid device = Guid.NewGuid();
        ObservationPeriod period = Hour(0);

        _database.Acquisitions.Save(TestDatabase.Acquisition(
            period,
            domains: [TestDatabase.DomainSeen("tracker.example", period, occurrences: 4)],
            activities: [Activity(device, "tracker.example", period, queries: 4, blocked: false)]));

        SaveHours(24 * 3);

        ConsolidationOutcome outcome = Consolidator(dailyMonths: 1)
            .Consolidate(new DateTimeOffset(2026, 11, 15, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(2, outcome.DaysWritten);
        Assert.Equal(1, outcome.MonthsWritten);

        StoredPeriod month = Assert.Single(Periods());

        Assert.Equal("Month", month.Granularity);
        Assert.Equal(DayStart, month.Start);
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), month.End);
        Assert.Equal(2, month.ObservedHours);

        // Which domains the network contacted is still known; which device
        // contacted them is not.
        Assert.Equal(4, _database.Acquisitions.GetDomainsSince(DayStart).Single(domain => domain.Name == "tracker.example").Occurrences);
        Assert.Equal(0, Count("domain_activity"));
        Assert.Equal(2000, _database.Acquisitions.GetStatisticsSince(DayStart)?.TotalQueries);
    }

    [Fact]
    public void A_finer_retention_prevails_over_a_coarser_one()
    {
        SaveHours(0);

        // Months after one month, but hours for four hundred days: the hour
        // stays an hour.
        Consolidator(hourlyDays: 400, dailyMonths: 1)
            .Consolidate(new DateTimeOffset(2027, 3, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal("Hour", Assert.Single(Periods()).Granularity);
    }

    [Fact]
    public void Nothing_outlives_the_monthly_retention()
    {
        SaveHours(0);

        ConsolidationOutcome outcome = Consolidator(dailyMonths: 1, monthlyYears: 1)
            .Consolidate(new DateTimeOffset(2027, 11, 1, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(1, outcome.PeriodsDeleted);
        Assert.Empty(Periods());
        Assert.Equal(0, Count("statistics"));
        Assert.Equal(0, Count("domain"));
    }

    // ------------------------------------------------------------------
    // Effective deletion and configuration
    // ------------------------------------------------------------------

    [Fact]
    public void Deleted_rows_are_overwritten_rather_than_left_in_the_file()
    {
        using SqliteConnection connection = _database.Connections.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "PRAGMA secure_delete;";

        Assert.Equal(1L, command.ExecuteScalar());
    }

    [Theory]
    [InlineData(1, 12, 5, "HourlyDays")]
    [InlineData(30, 0, 5, "DailyMonths")]
    [InlineData(30, 12, 0, "MonthlyYears")]
    public void A_retention_below_its_minimum_is_refused_and_named(int hourly, int daily, int monthly, string name)
    {
        RetentionOptions retention = new() { HourlyDays = hourly, DailyMonths = daily, MonthlyYears = monthly };

        InvalidOperationException refused = Assert.Throws<InvalidOperationException>(retention.Validate);

        Assert.Contains($"Storage:Retention:{name}", refused.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_default_retention_is_accepted()
    {
        new RetentionOptions().Validate();
    }

    // ------------------------------------------------------------------

    private PeriodConsolidator Consolidator(
        TimeZoneInfo? zone = null,
        int hourlyDays = 30,
        int dailyMonths = 12,
        int monthlyYears = 5)
    {
        return new PeriodConsolidator(
            _database.Connections,
            new RetentionOptions { HourlyDays = hourlyDays, DailyMonths = dailyMonths, MonthlyYears = monthlyYears },
            zone ?? TimeZoneInfo.Utc);
    }

    private static ObservationPeriod Hour(int hoursAfterMidnight)
    {
        return ObservationPeriod.Containing(DayStart.AddHours(hoursAfterMidnight));
    }

    // Every hour names a domain shared with the others and one of its own,
    // so that the distinct names of a day are more than any hour said.
    private void SaveHours(params int[] hours)
    {
        foreach (int hour in hours)
        {
            ObservationPeriod period = Hour(hour);

            _database.Acquisitions.Save(TestDatabase.Acquisition(
                period,
                domains:
                [
                    TestDatabase.DomainSeen("every-hour.example", period, occurrences: 1),
                    TestDatabase.DomainSeen($"hour-{hour}.example", period, occurrences: 1),
                ],
                statistics: new Statistics
                {
                    TotalQueries = 1000,
                    BlockedQueries = 100,
                    CachedQueries = 400,
                    FailedQueries = 9,
                    UniqueDomains = 2,
                    UniqueDomainsQuality = MeasurementQuality.Exact,
                    ActiveDevices = 1,
                    EncryptedQueries = 16,
                    DnssecEnabled = true,
                }));
        }
    }

    private List<StoredPeriod> Periods()
    {
        using SqliteConnection connection = _database.Connections.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT granularity, period_start, period_end, observed_hours
            FROM   observation_period
            ORDER BY period_start;
            """;

        List<StoredPeriod> periods = [];

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            periods.Add(new StoredPeriod(
                reader.GetString(0),
                DateTimeOffset.Parse(reader.GetString(1), CultureInfo.InvariantCulture),
                DateTimeOffset.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
                reader.GetInt32(3)));
        }

        return periods;
    }

    private long Count(string table)
    {
        using SqliteConnection connection = _database.Connections.Open();
        using SqliteCommand command = connection.CreateCommand();

        // The name comes from the tests themselves, never from outside.
        command.CommandText = $"SELECT COUNT(*) FROM {table};";

        return (long)command.ExecuteScalar()!;
    }

    private static DomainActivity Activity(Guid device, string domain, ObservationPeriod period, long queries, bool blocked)
    {
        return new DomainActivity
        {
            DeviceId = device,
            Domain = domain,
            QueryCount = queries,
            Blocked = blocked,
            Protocol = "Udp",
            FirstSeen = period.Start,
            LastSeen = period.End,
            ObservationQuality = MeasurementQuality.Exact,
        };
    }

    private static Npss Score(int overall)
    {
        return new Npss
        {
            OverallScore = overall,
            Status = ScoreStatus.Fair,
            Trend = null,
            Coverage = 0.85m,
            AlgorithmVersion = "test-1",
            GeneratedAt = DayStart,
            Breakdown =
            [
                new ScoreComponent
                {
                    Component = ScoreComponentType.PrivacyProtection,
                    State = ScoreComponentState.Measured,
                    Score = 12.5m,
                    MaxScore = 25m,
                    Weight = 25,
                    Factors = [],
                },
            ],
        };
    }

    private sealed record StoredPeriod(string Granularity, DateTimeOffset Start, DateTimeOffset End, int ObservedHours);
}
