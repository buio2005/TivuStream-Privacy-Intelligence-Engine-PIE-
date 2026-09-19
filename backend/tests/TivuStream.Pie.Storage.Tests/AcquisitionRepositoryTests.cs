using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;
using Xunit;

namespace TivuStream.Pie.Storage.Tests;

/// <summary>
/// Verifies what is kept of an observation, and what comes back.
/// </summary>
/// <remarks>
/// Persistence Specification: periods are fixed and do not overlap, a further
/// observation of a period supersedes the previous one, and an observation is
/// recorded in full or not at all. The rule for reading a window back is set
/// out in API Specification 1.2.0.
/// </remarks>
public sealed class AcquisitionRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    // ------------------------------------------------------------------
    // What is stored keeps what was known about it
    // ------------------------------------------------------------------

    [Fact]
    public void A_count_qualified_as_a_lower_bound_is_still_qualified_when_read_back()
    {
        ObservationPeriod period = TestDatabase.PeriodAt(0);

        _database.Acquisitions.Save(TestDatabase.Acquisition(period));

        StoredAcquisition? stored = _database.Acquisitions.GetLatest();

        // Rounding this to a plain number on the way through would turn a
        // floor into a total.
        Assert.NotNull(stored);
        Assert.Equal(MeasurementQuality.LowerBound, stored.Statistics.UniqueDomainsQuality);
        Assert.Equal(16, stored.Statistics.EncryptedQueries);
        Assert.Equal(period, stored.Period);
    }

    [Fact]
    public void A_domain_keeps_its_observation_quality_and_the_source_of_its_classification()
    {
        ObservationPeriod period = TestDatabase.PeriodAt(0);

        _database.Acquisitions.Save(TestDatabase.Acquisition(
            period,
            domains:
            [
                TestDatabase.DomainSeen(
                    "tracker.example",
                    period,
                    occurrences: 7,
                    category: ThreatCategory.Tracking,
                    confidence: ConfidenceLevel.Medium,
                    source: "list-one"),
            ]));

        Domain domain = Assert.Single(_database.Acquisitions.GetLatestDomains());

        Assert.Equal(MeasurementQuality.PeriodBounded, domain.ObservationQuality);
        Assert.Equal(ThreatCategory.Tracking, domain.Category);
        Assert.Equal(ConfidenceLevel.Medium, domain.CategoryConfidence);
        Assert.Equal("list-one", domain.CategorySource);
    }

    [Fact]
    public void An_unclassified_domain_stays_unclassified_and_names_no_source()
    {
        ObservationPeriod period = TestDatabase.PeriodAt(0);

        _database.Acquisitions.Save(TestDatabase.Acquisition(
            period,
            domains: [TestDatabase.DomainSeen("unknown.example", period, occurrences: 1)]));

        Domain domain = Assert.Single(_database.Acquisitions.GetLatestDomains());

        // Not presented as safe, and not attributed to a list that never said
        // anything about it.
        Assert.Equal(ThreatCategory.Unknown, domain.Category);
        Assert.Null(domain.CategorySource);
        Assert.Null(domain.CategoryConfidence);
    }

    [Fact]
    public void Blocked_and_answered_activity_of_the_same_device_are_kept_apart()
    {
        ObservationPeriod period = TestDatabase.PeriodAt(0);
        Guid device = Guid.NewGuid();

        _database.Acquisitions.Save(TestDatabase.Acquisition(
            period,
            activities:
            [
                Activity(device, "tracker.example", period, queries: 3, blocked: false),
                Activity(device, "tracker.example", period, queries: 1, blocked: true),
                Activity(device, "other.example", period, queries: 9, blocked: false),
            ]));

        IReadOnlyList<DomainActivity> found = _database.Acquisitions.GetLatestActivitiesFor("tracker.example");

        Assert.Equal(2, found.Count);
        Assert.Equal(3, found.Single(activity => !activity.Blocked).QueryCount);
        Assert.Equal(1, found.Single(activity => activity.Blocked).QueryCount);
    }

    // ------------------------------------------------------------------
    // Superseding and atomicity
    // ------------------------------------------------------------------

    [Fact]
    public void Observing_a_period_again_replaces_it_and_leaves_nothing_of_the_earlier_observation()
    {
        ObservationPeriod period = TestDatabase.PeriodAt(0);

        _database.Acquisitions.Save(TestDatabase.Acquisition(
            period,
            domains: [TestDatabase.DomainSeen("first.example", period, occurrences: 1)]));

        _database.Acquisitions.Save(TestDatabase.Acquisition(
            period,
            domains: [TestDatabase.DomainSeen("second.example", period, occurrences: 2)]));

        Assert.Equal(1, _database.Acquisitions.CountPeriods());

        Domain only = Assert.Single(_database.Acquisitions.GetLatestDomains());

        Assert.Equal("second.example", only.Name);
    }

    [Fact]
    public void An_observation_is_recorded_in_full_or_not_at_all()
    {
        ObservationPeriod period = TestDatabase.PeriodAt(0);

        // The same domain twice in one period violates the key of the table,
        // and does so after the period and its statistics were already
        // written.
        StoredAcquisition broken = TestDatabase.Acquisition(
            period,
            domains:
            [
                TestDatabase.DomainSeen("twice.example", period, occurrences: 1),
                TestDatabase.DomainSeen("twice.example", period, occurrences: 2),
            ]);

        Assert.Throws<StorageException>(() => _database.Acquisitions.Save(broken));

        Assert.Equal(0, _database.Acquisitions.CountPeriods());
        Assert.Null(_database.Acquisitions.GetLatest());
    }

    // ------------------------------------------------------------------
    // Reading a window
    // ------------------------------------------------------------------

    [Fact]
    public void Occurrences_over_a_window_are_summed_and_the_sightings_span_it()
    {
        ObservationPeriod first = TestDatabase.PeriodAt(0);
        ObservationPeriod second = TestDatabase.PeriodAt(1);
        ObservationPeriod third = TestDatabase.PeriodAt(2);

        SaveDomain("a.example", first, 10);
        SaveDomain("a.example", second, 5);
        SaveDomain("a.example", third, 1);

        Domain aggregated = Assert.Single(_database.Acquisitions.GetDomainsSince(first.Start));

        // Periods do not overlap, so the same traffic is never counted twice.
        Assert.Equal(16, aggregated.Occurrences);
        Assert.Equal(first.Start, aggregated.FirstSeen);
        Assert.Equal(third.End, aggregated.LastSeen);
    }

    [Fact]
    public void An_aggregate_is_known_no_better_than_its_vaguest_part()
    {
        ObservationPeriod first = TestDatabase.PeriodAt(0);
        ObservationPeriod second = TestDatabase.PeriodAt(1);

        SaveDomain("a.example", first, 10, MeasurementQuality.Exact);
        SaveDomain("a.example", second, 5, MeasurementQuality.LowerBound);

        Domain aggregated = Assert.Single(_database.Acquisitions.GetDomainsSince(first.Start));

        Assert.Equal(MeasurementQuality.LowerBound, aggregated.ObservationQuality);
    }

    [Fact]
    public void The_classification_is_the_one_of_the_most_recent_period_whatever_the_order_of_writing()
    {
        ObservationPeriod older = TestDatabase.PeriodAt(0);
        ObservationPeriod newer = TestDatabase.PeriodAt(1);

        // The newer period is written first on purpose: recency is a property
        // of the period, not of when the row happened to be saved.
        _database.Acquisitions.Save(TestDatabase.Acquisition(
            newer,
            domains:
            [
                TestDatabase.DomainSeen(
                    "a.example",
                    newer,
                    occurrences: 1,
                    category: ThreatCategory.Tracking,
                    confidence: ConfidenceLevel.High,
                    source: "list-now"),
            ]));

        SaveDomain("a.example", older, 1);

        Domain aggregated = Assert.Single(_database.Acquisitions.GetDomainsSince(older.Start));

        Assert.Equal(ThreatCategory.Tracking, aggregated.Category);
        Assert.Equal("list-now", aggregated.CategorySource);
    }

    [Fact]
    public void A_window_leaves_out_the_periods_before_it_and_counts_those_that_exist()
    {
        ObservationPeriod before = TestDatabase.PeriodAt(0);
        ObservationPeriod inside = TestDatabase.PeriodAt(1);
        ObservationPeriod last = TestDatabase.PeriodAt(2);

        SaveDomain("old.example", before, 100);
        SaveDomain("new.example", inside, 1);
        SaveDomain("new.example", last, 1);

        DateTimeOffset since = inside.Start;

        Domain only = Assert.Single(_database.Acquisitions.GetDomainsSince(since));

        Assert.Equal("new.example", only.Name);

        // What was asked for and what exists are different things, and the
        // API declares both.
        Assert.Equal(2, _database.Acquisitions.CountPeriodsSince(since));
        Assert.Equal(new ObservationPeriod(inside.Start, last.End), _database.Acquisitions.GetPeriodRangeSince(since));
    }

    [Fact]
    public void Nothing_observed_is_reported_as_nothing_observed_and_not_as_an_empty_day()
    {
        Assert.Null(_database.Acquisitions.GetPeriodRangeSince(TestDatabase.Noon));
        Assert.Equal(0, _database.Acquisitions.CountPeriodsSince(TestDatabase.Noon));
        Assert.Null(_database.Acquisitions.GetLatestPeriod());
        Assert.Empty(_database.Acquisitions.GetDomainsSince(TestDatabase.Noon));
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private void SaveDomain(
        string name,
        ObservationPeriod period,
        long occurrences,
        MeasurementQuality quality = MeasurementQuality.PeriodBounded)
    {
        _database.Acquisitions.Save(TestDatabase.Acquisition(
            period,
            domains: [TestDatabase.DomainSeen(name, period, occurrences, quality)]));
    }

    private static DomainActivity Activity(
        Guid device,
        string domain,
        ObservationPeriod period,
        long queries,
        bool blocked)
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
        };
    }
}
