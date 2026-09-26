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

        Domain domain = Assert.Single(_database.Acquisitions.GetDomainsSince(period.Start));

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

        Domain domain = Assert.Single(_database.Acquisitions.GetDomainsSince(period.Start));

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

        List<ObservedActivity> found = _database.Acquisitions.GetActivitiesSince("tracker.example", period.Start);

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

        Domain only = Assert.Single(_database.Acquisitions.GetDomainsSince(period.Start));

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
    // Reading the activity towards a domain over a window
    // ------------------------------------------------------------------

    [Fact]
    public void Activity_over_a_window_is_summed_per_device_outcome_and_transport()
    {
        ObservationPeriod first = TestDatabase.PeriodAt(0);
        ObservationPeriod second = TestDatabase.PeriodAt(1);
        Guid device = Guid.NewGuid();

        _database.Acquisitions.Save(TestDatabase.Acquisition(
            first,
            activities:
            [
                Activity(device, "a.example", first, queries: 10, blocked: false),
                Activity(device, "a.example", first, queries: 2, blocked: false, protocol: "Https"),
            ]));

        _database.Acquisitions.Save(TestDatabase.Acquisition(
            second,
            activities:
            [
                Activity(device, "a.example", second, queries: 5, blocked: false, quality: MeasurementQuality.LowerBound),
                Activity(device, "a.example", second, queries: 1, blocked: true),
            ]));

        List<ObservedActivity> found = _database.Acquisitions.GetActivitiesSince("a.example", first.Start);

        // Three facts: answered over UDP in both hours, answered over HTTPS,
        // blocked. The hours add up because they do not overlap.
        Assert.Equal(3, found.Count);

        ObservedActivity udp = found.Single(activity => !activity.Blocked && activity.Protocol == "Udp");

        Assert.Equal(15, udp.QueryCount);
        Assert.Equal(first.Start, udp.FirstSeen);
        Assert.Equal(second.End, udp.LastSeen);
        Assert.Equal(MeasurementQuality.LowerBound, udp.ObservationQuality);

        Assert.Equal(2, found.Single(activity => activity.Protocol == "Https").QueryCount);
        Assert.Equal(1, found.Single(activity => activity.Blocked).QueryCount);

        // The largest first.
        Assert.Same(udp, found[0]);
    }

    [Fact]
    public void Activity_before_the_window_is_left_out()
    {
        ObservationPeriod before = TestDatabase.PeriodAt(0);
        ObservationPeriod inside = TestDatabase.PeriodAt(1);
        Guid device = Guid.NewGuid();

        _database.Acquisitions.Save(TestDatabase.Acquisition(
            before,
            activities: [Activity(device, "a.example", before, queries: 100, blocked: false)]));

        _database.Acquisitions.Save(TestDatabase.Acquisition(
            inside,
            activities: [Activity(device, "a.example", inside, queries: 1, blocked: false)]));

        ObservedActivity only = Assert.Single(_database.Acquisitions.GetActivitiesSince("a.example", inside.Start));

        Assert.Equal(1, only.QueryCount);
    }

    [Fact]
    public void A_device_is_described_as_in_the_most_recent_period_it_appears_in_whatever_the_order_of_writing()
    {
        ObservationPeriod older = TestDatabase.PeriodAt(0);
        ObservationPeriod newer = TestDatabase.PeriodAt(1);
        Guid device = Guid.NewGuid();

        // The newer period is written first on purpose: recency is a property
        // of the period, not of when the row happened to be saved.
        _database.Acquisitions.Save(TestDatabase.Acquisition(
            newer,
            devices: [DeviceSeen(device, newer, "192.168.1.30", "laptop-now", DeviceIdentityBasis.HardwareAddress)],
            activities: [Activity(device, "a.example", newer, queries: 1, blocked: false)]));

        _database.Acquisitions.Save(TestDatabase.Acquisition(
            older,
            devices: [DeviceSeen(device, older, "192.168.1.20", "laptop-then", DeviceIdentityBasis.NetworkAddress)],
            activities: [Activity(device, "a.example", older, queries: 1, blocked: false)]));

        ObservedActivity only = Assert.Single(_database.Acquisitions.GetActivitiesSince("a.example", older.Start));

        Assert.Equal(device, only.Device.DeviceId);
        Assert.Equal("laptop-now", only.Device.Hostname);
        Assert.Equal("192.168.1.30", only.Device.IpAddress);
        Assert.Equal(DeviceIdentityBasis.HardwareAddress, only.Device.IdentityBasis);
    }

    [Fact]
    public void A_device_active_in_an_earlier_hour_keeps_its_description()
    {
        ObservationPeriod earlier = TestDatabase.PeriodAt(0);
        ObservationPeriod latest = TestDatabase.PeriodAt(1);
        Guid device = Guid.NewGuid();

        _database.Acquisitions.Save(TestDatabase.Acquisition(
            earlier,
            devices: [DeviceSeen(device, earlier, "192.168.1.20", hostname: null, DeviceIdentityBasis.NetworkAddress)],
            activities: [Activity(device, "a.example", earlier, queries: 4, blocked: false)]));

        // The latest hour knows nothing of that device: /devices alone would
        // leave it without a name.
        _database.Acquisitions.Save(TestDatabase.Acquisition(latest));

        ObservedActivity only = Assert.Single(_database.Acquisitions.GetActivitiesSince("a.example", earlier.Start));

        Assert.Equal("192.168.1.20", only.Device.IpAddress);
        Assert.Null(only.Device.Hostname);
        Assert.Equal(DeviceIdentityBasis.NetworkAddress, only.Device.IdentityBasis);
    }

    [Fact]
    public void A_device_described_in_no_period_of_the_window_is_not_described_at_all()
    {
        ObservationPeriod period = TestDatabase.PeriodAt(0);
        Guid device = Guid.NewGuid();

        _database.Acquisitions.Save(TestDatabase.Acquisition(
            period,
            activities: [Activity(device, "a.example", period, queries: 2, blocked: false)]));

        ObservedActivity only = Assert.Single(_database.Acquisitions.GetActivitiesSince("a.example", period.Start));

        // The activity stays: the traffic happened. What is not known about
        // the device is absent, never guessed.
        Assert.Equal(2, only.QueryCount);
        Assert.Equal(device, only.Device.DeviceId);
        Assert.Null(only.Device.IpAddress);
        Assert.Null(only.Device.Hostname);
        Assert.Null(only.Device.IdentityBasis);
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
        bool blocked,
        string protocol = "Udp",
        MeasurementQuality quality = MeasurementQuality.Exact)
    {
        return new DomainActivity
        {
            DeviceId = device,
            Domain = domain,
            QueryCount = queries,
            Blocked = blocked,
            Protocol = protocol,
            FirstSeen = period.Start,
            LastSeen = period.End,
            ObservationQuality = quality,
        };
    }

    private static Device DeviceSeen(
        Guid device,
        ObservationPeriod period,
        string address,
        string? hostname,
        DeviceIdentityBasis basis)
    {
        return new Device
        {
            DeviceId = device,
            Hostname = hostname,
            IpAddress = address,
            IdentityBasis = basis,
            FirstSeen = period.Start,
            LastSeen = period.End,
            ObservationQuality = MeasurementQuality.PeriodBounded,
            Status = DeviceStatus.Active,
        };
    }
}
