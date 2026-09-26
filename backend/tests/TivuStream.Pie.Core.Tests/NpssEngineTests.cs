using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;
using Xunit;

namespace TivuStream.Pie.Core.Tests;

/// <summary>
/// Verifies the rules the NPSS Specification states.
/// </summary>
/// <remarks>
/// Each test refers to a commitment made to the person using the tool, not to
/// an implementation detail.
/// </remarks>
public sealed class NpssEngineTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);

    // ------------------------------------------------------------------
    // What was not observed is excluded, never scored and never assumed
    // ------------------------------------------------------------------

    [Fact]
    public void An_area_without_data_is_not_measurable_and_carries_no_obtainable_points()
    {
        Npss score = Evaluate(WellConfigured());

        ScoreComponent threats = Component(score, ScoreComponentType.ThreatProtection);

        Assert.Equal(ScoreComponentState.NotMeasurable, threats.State);
        Assert.Equal(0, threats.MaxScore);
        Assert.Equal(0, threats.Score);
        Assert.NotEmpty(threats.Factors);
    }

    [Fact]
    public void An_area_without_data_keeps_its_nominal_weight()
    {
        Npss score = Evaluate(WellConfigured());

        // The weight states how much the area would count. Losing it would
        // hide the fact that something is missing.
        Assert.Equal(25, Component(score, ScoreComponentType.ThreatProtection).Weight);
    }

    [Fact]
    public void Missing_data_does_not_improve_the_result()
    {
        NpssEvaluationInput withConfiguration = WellConfigured();
        NpssEvaluationInput withoutConfiguration = withConfiguration with { Configuration = null };

        Npss complete = Evaluate(withConfiguration);
        Npss reduced = Evaluate(withoutConfiguration);

        // Removing a source of data lowers what can be observed. It must never
        // raise what is obtained.
        Assert.True(reduced.Coverage < complete.Coverage);
        Assert.True(reduced.Breakdown.Sum(c => c.Score) <= complete.Breakdown.Sum(c => c.Score));
    }

    // ------------------------------------------------------------------
    // Partial measurement
    // ------------------------------------------------------------------

    [Fact]
    public void An_area_measured_in_part_is_declared_as_such()
    {
        // No traffic: usage of encrypted transport and error rate cannot be
        // judged, while the configuration indicators still can.
        Npss score = Evaluate(WellConfigured() with { Statistics = NoTraffic() });

        ScoreComponent dns = Component(score, ScoreComponentType.DnsSecurity);

        Assert.Equal(ScoreComponentState.PartiallyMeasured, dns.State);
        Assert.True(dns.MaxScore > 0);
        Assert.True(dns.MaxScore < dns.Weight);
    }

    [Fact]
    public void A_fully_measured_area_can_reach_its_nominal_weight()
    {
        Npss score = Evaluate(WellConfigured());

        ScoreComponent dns = Component(score, ScoreComponentType.DnsSecurity);

        Assert.Equal(ScoreComponentState.Measured, dns.State);
        Assert.Equal(dns.Weight, dns.MaxScore);
    }

    [Fact]
    public void Coverage_is_the_sum_of_the_obtainable_points()
    {
        Npss score = Evaluate(WellConfigured());

        Assert.Equal(score.Breakdown.Sum(component => component.MaxScore), score.Coverage);
    }

    // ------------------------------------------------------------------
    // No summary below the minimum coverage
    // ------------------------------------------------------------------

    [Fact]
    public void No_overall_score_is_produced_below_the_minimum_coverage()
    {
        Npss score = Evaluate(WellConfigured());

        Assert.True(score.Coverage < NpssEngine.MinimumCoverageForOverallScore);
        Assert.Null(score.OverallScore);
        Assert.Null(score.Status);
    }

    [Fact]
    public void The_breakdown_is_produced_even_when_the_overall_score_is_not()
    {
        Npss score = Evaluate(WellConfigured());

        // Declining the summary must not mean declining the measurements.
        Assert.Null(score.OverallScore);
        Assert.Equal(6, score.Breakdown.Count);
        Assert.All(score.Breakdown, component => Assert.NotEmpty(component.Factors));
    }

    // ------------------------------------------------------------------
    // Indicators
    // ------------------------------------------------------------------

    [Fact]
    public void Disabled_dnssec_lowers_the_points_without_lowering_what_is_observable()
    {
        NpssEvaluationInput enabled = WellConfigured();
        NpssEvaluationInput disabled = enabled with
        {
            Configuration = enabled.Configuration! with { DnssecValidationEnabled = false },
        };

        ScoreComponent withDnssec = Component(Evaluate(enabled), ScoreComponentType.DnsSecurity);
        ScoreComponent withoutDnssec = Component(Evaluate(disabled), ScoreComponentType.DnsSecurity);

        // A setting that is off is a fact about the network, not a gap in the
        // observation: the score drops, the obtainable points do not.
        Assert.True(withoutDnssec.Score < withDnssec.Score);
        Assert.Equal(withDnssec.MaxScore, withoutDnssec.MaxScore);
    }

    [Fact]
    public void Forwarding_the_client_subnet_lowers_the_score()
    {
        NpssEvaluationInput off = WellConfigured();
        NpssEvaluationInput on = off with
        {
            Configuration = off.Configuration! with { ClientSubnetForwardingEnabled = true },
        };

        // The feature improves accuracy of answers and reduces privacy. The
        // score rewards its absence.
        Assert.True(
            Component(Evaluate(on), ScoreComponentType.DnsSecurity).Score
            < Component(Evaluate(off), ScoreComponentType.DnsSecurity).Score);
    }

    [Fact]
    public void Filtering_without_any_list_does_not_earn_full_points()
    {
        NpssEvaluationInput withoutLists = WellConfigured();
        NpssEvaluationInput withLists = withoutLists with
        {
            Configuration = withoutLists.Configuration! with { FilterListCount = 3 },
        };

        ScoreComponent none = Component(Evaluate(withoutLists), ScoreComponentType.Configuration);
        ScoreComponent some = Component(Evaluate(withLists), ScoreComponentType.Configuration);

        // Filtering enabled with no list has no effect, and the score says so.
        Assert.True(none.Score < some.Score);
        Assert.Equal(some.MaxScore, some.Score);
    }

    [Fact]
    public void Nothing_is_expected_before_the_first_observation()
    {
        NpssEvaluationInput recent = WellConfigured() with
        {
            ObservedPeriods = 5,
            ExpectedPeriods = 5,
        };

        ScoreComponent integrity = Component(Evaluate(recent), ScoreComponentType.NetworkIntegrity);

        // An instance observing for five hours without interruption is not
        // penalised for the nineteen hours before it existed.
        Assert.Equal(5, integrity.Score);
    }

    [Fact]
    public void Missing_periods_lower_the_continuity()
    {
        NpssEvaluationInput withGaps = WellConfigured() with
        {
            ObservedPeriods = 6,
            ExpectedPeriods = 24,
        };

        Assert.True(Component(Evaluate(withGaps), ScoreComponentType.NetworkIntegrity).Score < 5);
    }

    // ------------------------------------------------------------------
    // Not having the tool is not a result
    // ------------------------------------------------------------------

    [Fact]
    public void Without_lists_the_areas_based_on_classification_are_not_measurable()
    {
        Npss score = Evaluate(Observed() with { ClassificationAvailable = false });

        ScoreComponent privacy = Component(score, ScoreComponentType.PrivacyProtection);

        Assert.Equal(ScoreComponentState.NotMeasurable, privacy.State);
        Assert.Equal(0, privacy.MaxScore);
    }

    [Fact]
    public void Having_no_list_is_not_worth_the_same_as_having_found_nothing()
    {
        // Two networks with identical traffic. One has lists and no tracking
        // was recognised; the other has no lists at all.
        //
        // Reading the second as an absence of tracking would turn a missing
        // tool into a good result, which is the failure this whole area exists
        // to avoid.
        Npss withLists = Evaluate(Observed());
        Npss withoutLists = Evaluate(Observed() with { ClassificationAvailable = false });

        ScoreComponent measured = Component(withLists, ScoreComponentType.PrivacyProtection);
        ScoreComponent blind = Component(withoutLists, ScoreComponentType.PrivacyProtection);

        Assert.Equal(10, measured.Score);
        Assert.Equal(0, blind.Score);
        Assert.True(blind.MaxScore < measured.MaxScore);
    }

    // ------------------------------------------------------------------
    // A network barely observed is not a network protected
    // ------------------------------------------------------------------

    [Fact]
    public void Below_the_minimum_number_of_queries_nothing_is_judged()
    {
        Npss score = Evaluate(Observed() with
        {
            Statistics = WithTraffic() with { TotalQueries = 99, EncryptedQueries = 99 },
        });

        Assert.Equal(
            ScoreComponentState.NotMeasurable,
            Component(score, ScoreComponentType.PrivacyProtection).State);

        Assert.Equal(
            ScoreComponentState.NotMeasurable,
            Component(score, ScoreComponentType.ThreatProtection).State);
    }

    [Fact]
    public void The_reason_for_not_judging_is_stated_in_full()
    {
        Npss score = Evaluate(Observed() with
        {
            Statistics = WithTraffic() with { TotalQueries = 99, EncryptedQueries = 99 },
        });

        // The person is told what is missing and how much, not merely that
        // something is missing. The words are the interface's; the figures are
        // the engine's, and must be there for the words to be written.
        ScoreFactor reason = Assert.Single(
            Component(score, ScoreComponentType.PrivacyProtection).Factors);

        Assert.Equal(FactorCodes.ObservationInsufficient, reason.Code);
        Assert.Equal(99L, reason.Values["queries"]);
        Assert.Equal(NpssEngine.MinimumQueriesForClassification, reason.Values["minimum"]);
    }

    // ------------------------------------------------------------------
    // Exposure to tracking is counted per query
    // ------------------------------------------------------------------

    [Fact]
    public void One_domain_contacted_often_weighs_more_than_many_contacted_once()
    {
        NpssEvaluationInput spread = Observed() with
        {
            Domains =
            [
                .. Enumerable
                    .Range(0, 10)
                    .Select(index => Tracker($"tracker{index}.example.com", occurrences: 1)),
            ],
        };

        NpssEvaluationInput concentrated = Observed() with
        {
            Domains = [Tracker("tracker.example.com", occurrences: 400)],
        };

        // Ten domains against one. Counting domains would call the second
        // network the cleaner of the two.
        Assert.True(
            Component(Evaluate(concentrated), ScoreComponentType.PrivacyProtection).Score
            < Component(Evaluate(spread), ScoreComponentType.PrivacyProtection).Score);
    }

    [Fact]
    public void Full_marks_on_exposure_are_reported_as_nothing_known()
    {
        ScoreComponent privacy = Component(
            Evaluate(Observed()),
            ScoreComponentType.PrivacyProtection);

        // The lists assert that a domain tracks, never that it does not.
        //
        // The code says "none known", and the Network Privacy Specification
        // binds every translation of it to say the same. The engine cannot
        // check the words; it can make sure the distinction survives as far as
        // the interface.
        Assert.Contains(
            privacy.Factors,
            factor => factor.Code == FactorCodes.TrackingExposureNone);
    }

    // ------------------------------------------------------------------
    // A filter that was never tested is not a filter that failed
    // ------------------------------------------------------------------

    [Fact]
    public void With_nothing_to_block_the_indicator_is_excluded_rather_than_scored()
    {
        ScoreComponent privacy = Component(
            Evaluate(Observed()),
            ScoreComponentType.PrivacyProtection);

        // Ten obtainable points instead of twenty: the exposure was measured,
        // the blocking was not, and the missing half is removed from what
        // could be obtained rather than scored as zero.
        Assert.Equal(10, privacy.MaxScore);
        Assert.Equal(10, privacy.Score);
        Assert.Equal(ScoreComponentState.PartiallyMeasured, privacy.State);
    }

    [Fact]
    public void A_source_that_cannot_report_activity_leaves_blocking_unmeasured()
    {
        NpssEvaluationInput tracked = Observed() with
        {
            Domains = [Tracker("tracker.example.com", occurrences: 50)],
            DomainActivityAvailable = false,
        };

        ScoreComponent privacy = Component(Evaluate(tracked), ScoreComponentType.PrivacyProtection);

        Assert.Equal(10, privacy.MaxScore);
    }

    [Fact]
    public void Traffic_that_is_blocked_earns_the_points_that_traffic_let_through_does_not()
    {
        NpssEvaluationInput passing = Tracked(blocked: false);
        NpssEvaluationInput stopped = Tracked(blocked: true);

        ScoreComponent letThrough = Component(Evaluate(passing), ScoreComponentType.PrivacyProtection);
        ScoreComponent blocked = Component(Evaluate(stopped), ScoreComponentType.PrivacyProtection);

        Assert.Equal(20, letThrough.MaxScore);
        Assert.Equal(20, blocked.MaxScore);
        Assert.True(blocked.Score > letThrough.Score);
    }

    // ------------------------------------------------------------------
    // Threats are counted by name, not by proportion
    // ------------------------------------------------------------------

    [Fact]
    public void A_single_malware_domain_costs_points_however_little_traffic_it_drew()
    {
        NpssEvaluationInput exposed = Observed() with
        {
            Domains = [Classified("bad.example.com", ThreatCategory.Malware, occurrences: 1)],
        };

        ScoreComponent threats = Component(Evaluate(exposed), ScoreComponentType.ThreatProtection);

        // One query out of a thousand. As a share it would round to nothing.
        Assert.True(threats.Score < 12);
    }

    [Fact]
    public void A_suspicious_domain_lowers_the_score_without_emptying_it()
    {
        NpssEvaluationInput suspicious = Observed() with
        {
            Domains = [Classified("odd.example.com", ThreatCategory.Suspicious, occurrences: 5)],
        };

        NpssEvaluationInput confirmed = Observed() with
        {
            Domains = [Classified("bad.example.com", ThreatCategory.Malware, occurrences: 5)],
        };

        decimal clean = Component(Evaluate(Observed()), ScoreComponentType.ThreatProtection).Score;
        decimal unconfirmed = Component(Evaluate(suspicious), ScoreComponentType.ThreatProtection).Score;
        decimal established = Component(Evaluate(confirmed), ScoreComponentType.ThreatProtection).Score;

        // A report that was never confirmed must not be treated as an
        // established threat: it would attribute to the network a problem
        // nobody demonstrated.
        Assert.True(unconfirmed < clean);
        Assert.True(established < unconfirmed);
    }

    [Fact]
    public void The_bar_for_blocking_threats_is_higher_than_for_blocking_trackers()
    {
        Npss almost = Evaluate(BothKinds(blockedQueries: 99, passingQueries: 1));
        Npss complete = Evaluate(BothKinds(blockedQueries: 100, passingQueries: 0));

        decimal trackingAlmost = Component(almost, ScoreComponentType.PrivacyProtection).Score;
        decimal trackingComplete = Component(complete, ScoreComponentType.PrivacyProtection).Score;

        decimal threatAlmost = Component(almost, ScoreComponentType.ThreatProtection).Score;
        decimal threatComplete = Component(complete, ScoreComponentType.ThreatProtection).Score;

        // Ninety nine per cent already earns everything obtainable against
        // tracking, and does not against threats.
        //
        // A tracker that gets through costs privacy; a malware domain that
        // gets through can cost the machine.
        Assert.Equal(trackingComplete, trackingAlmost);
        Assert.True(threatAlmost < threatComplete);
    }

    // ------------------------------------------------------------------
    // Trend
    // ------------------------------------------------------------------

    [Fact]
    public void No_trend_is_declared_when_no_overall_score_exists()
    {
        Npss score = Evaluate(WellConfigured() with
        {
            PreviousOverallScore = 50,
            PreviousCoverage = 35,
        });

        // Without a summary there is nothing to compare, and a direction
        // would be an assertion about a quantity that was not produced.
        Assert.Null(score.OverallScore);
        Assert.Null(score.Trend);
    }

    [Fact]
    public void A_score_that_went_up_since_the_previous_one_of_the_same_kind_is_improving()
    {
        Npss first = Evaluate(Tracked(blocked: true));

        Assert.NotNull(first.OverallScore);

        Npss next = Evaluate(Tracked(blocked: true) with
        {
            PreviousOverallScore = first.OverallScore - 5,
            PreviousCoverage = first.Coverage,
            PreviousAlgorithmVersion = NpssEngine.AlgorithmVersion,
        });

        Assert.Equal(ScoreTrend.Improving, next.Trend);
    }

    [Fact]
    public void A_score_produced_by_another_version_of_the_algorithm_gives_no_trend()
    {
        Npss first = Evaluate(Tracked(blocked: true));

        Npss next = Evaluate(Tracked(blocked: true) with
        {
            PreviousOverallScore = first.OverallScore - 5,
            PreviousCoverage = first.Coverage,
            PreviousAlgorithmVersion = "3.0.0",
        });

        // Version 3 read one hour, version 4 reads a day: the two do not
        // measure the same thing, and a direction between them would be
        // invented.
        Assert.NotNull(next.OverallScore);
        Assert.Null(next.Trend);
    }

    [Fact]
    public void A_score_with_a_different_coverage_gives_no_trend()
    {
        Npss first = Evaluate(Tracked(blocked: true));

        Npss next = Evaluate(Tracked(blocked: true) with
        {
            PreviousOverallScore = first.OverallScore - 5,
            PreviousCoverage = first.Coverage - 10,
            PreviousAlgorithmVersion = NpssEngine.AlgorithmVersion,
        });

        Assert.Null(next.Trend);
    }

    // ------------------------------------------------------------------
    // Fixtures
    // ------------------------------------------------------------------

    private static Npss Evaluate(NpssEvaluationInput input)
    {
        return new NpssEngine(new FixedTimeProvider(Now)).Evaluate(input);
    }

    private static ScoreComponent Component(Npss score, ScoreComponentType component)
    {
        return score.Breakdown.Single(candidate => candidate.Component == component);
    }

    /// <summary>
    /// A source answering, well configured, with traffic and continuous
    /// observation. Coverage stays below the minimum because classification
    /// and the device engine do not exist yet.
    /// </summary>
    private static NpssEvaluationInput WellConfigured()
    {
        return new NpssEvaluationInput
        {
            Statistics = WithTraffic(),
            Configuration = new SourceConfiguration
            {
                DnssecValidationEnabled = true,
                EncryptedTransports = ["Tls"],
                QueryMinimisationEnabled = true,
                ClientSubnetForwardingEnabled = false,
                FilteringEnabled = true,
                FilterListCount = 0,
                FilterListUpdateIntervalHours = 24,
            },
            SourceReachable = true,
            ObservedPeriods = 24,
            ExpectedPeriods = 24,
        };
    }

    /// <summary>
    /// A well configured source, with lists loaded, activity available, and
    /// traffic above the minimum. No domain was recognised as anything.
    /// </summary>
    private static NpssEvaluationInput Observed()
    {
        return WellConfigured() with
        {
            ClassificationAvailable = true,
            DomainActivityAvailable = true,
        };
    }

    /// <summary>
    /// One tracking domain, contacted a hundred times, either stopped or let
    /// through in full.
    /// </summary>
    private static NpssEvaluationInput Tracked(bool blocked)
    {
        return Observed() with
        {
            Domains = [Tracker("tracker.example.com", occurrences: 100)],
            DomainActivities = [Activity("tracker.example.com", queries: 100, blocked)],
        };
    }

    /// <summary>
    /// A tracking domain and a malware domain, both blocked in the same
    /// proportion.
    /// </summary>
    private static NpssEvaluationInput BothKinds(long blockedQueries, long passingQueries)
    {
        long total = blockedQueries + passingQueries;

        List<DomainActivity> activities =
        [
            Activity("tracker.example.com", blockedQueries, blocked: true),
            Activity("bad.example.com", blockedQueries, blocked: true),
        ];

        if (passingQueries > 0)
        {
            activities.Add(Activity("tracker.example.com", passingQueries, blocked: false));
            activities.Add(Activity("bad.example.com", passingQueries, blocked: false));
        }

        return Observed() with
        {
            Domains =
            [
                Tracker("tracker.example.com", total),
                Classified("bad.example.com", ThreatCategory.Malware, total),
            ],
            DomainActivities = activities,
        };
    }

    private static Domain Tracker(string name, long occurrences)
    {
        return Classified(name, ThreatCategory.Tracking, occurrences);
    }

    private static Domain Classified(string name, ThreatCategory category, long occurrences)
    {
        return new Domain
        {
            Name = name,
            Category = category,
            CategoryConfidence = ConfidenceLevel.High,
            CategorySource = "Test list",
            CategorySourceUpdatedAt = Now.AddDays(-1),
            FirstSeen = Now.AddHours(-1),
            LastSeen = Now,
            ObservationQuality = MeasurementQuality.PeriodBounded,
            Occurrences = occurrences,
        };
    }

    private static DomainActivity Activity(string domain, long queries, bool blocked)
    {
        return new DomainActivity
        {
            DeviceId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Domain = domain,
            QueryCount = queries,
            Blocked = blocked,
            Protocol = "Udp",
            FirstSeen = Now.AddHours(-1),
            LastSeen = Now,
        };
    }

    private static Statistics WithTraffic()
    {
        return new Statistics
        {
            TotalQueries = 1000,
            BlockedQueries = 100,
            CachedQueries = 400,
            FailedQueries = 0,
            UniqueDomains = 50,
            UniqueDomainsQuality = MeasurementQuality.LowerBound,
            ActiveDevices = 5,
            EncryptedQueries = 1000,
            DnssecEnabled = true,
        };
    }

    private static Statistics NoTraffic()
    {
        return WithTraffic() with
        {
            TotalQueries = 0,
            BlockedQueries = 0,
            CachedQueries = 0,
            FailedQueries = 0,
            EncryptedQueries = 0,
            ActiveDevices = 0,
        };
    }
}
