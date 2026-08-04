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

    // NOTE, recorded rather than worked around.
    //
    // The rules governing the trend when a summary does exist cannot be
    // exercised yet: no combination of inputs reaches the minimum coverage,
    // because the areas depending on domain classification and on the device
    // engine account for sixty of the hundred points and neither exists.
    //
    // That branch of the engine is therefore written but never executed, and
    // remains unverified until the classification engine is available.
    //
    // Lowering the threshold to make the tests pass would verify a rule the
    // product does not apply.

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
