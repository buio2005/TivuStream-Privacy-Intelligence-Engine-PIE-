using System.Net;
using System.Text.Json;
using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies what the API says, and above all what it says when it has nothing
/// to say.
/// </summary>
/// <remarks>
/// API Specification: every answer has the standard envelope; what describes
/// an observation declares the period it refers to (1.2.0); and the absence
/// of a value is never rendered as a value. The Network Privacy Specification
/// rule behind the last one is Absent Versus Unmeasurable.
/// </remarks>
public sealed class EndpointTests : IDisposable
{
    private readonly PieApplication _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    // ------------------------------------------------------------------
    // Nothing yet is not zero
    // ------------------------------------------------------------------

    [Fact]
    public async Task Statistics_before_any_acquisition_are_a_refusal_and_not_a_set_of_zeros()
    {
        (HttpStatusCode status, JsonElement body, _) = await _app.GetAsync("/api/v1/statistics");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal("AcquisitionPending", body.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("data").ValueKind);
    }

    [Fact]
    public async Task The_score_before_any_evaluation_is_a_refusal()
    {
        (HttpStatusCode status, JsonElement body, _) = await _app.GetAsync("/api/v1/npss");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, status);
        Assert.Equal("ScorePending", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Domains_with_nothing_observed_say_so_and_carry_no_period()
    {
        (HttpStatusCode status, JsonElement body, _) = await _app.GetAsync("/api/v1/domains");

        JsonElement data = body.GetProperty("data");

        Assert.Equal(HttpStatusCode.OK, status);

        // An empty list on its own cannot be told apart from a network that
        // contacted nothing. The absent period is what tells them apart.
        Assert.Equal(JsonValueKind.Null, data.GetProperty("period").ValueKind);
        Assert.Equal(0, data.GetProperty("periodsObserved").GetInt32());
        Assert.Equal(24, data.GetProperty("periodsRequested").GetInt32());
        Assert.Equal(0, data.GetProperty("domains").GetArrayLength());
    }

    // ------------------------------------------------------------------
    // The envelope, and values that keep their qualification
    // ------------------------------------------------------------------

    [Fact]
    public async Task Every_answer_has_the_standard_envelope()
    {
        Seed.Acquisition(_app, Seed.HoursAgo(0));

        foreach (string path in new[] { "/api/v1/health", "/api/v1/statistics", "/api/v1/devices", "/api/v1/domains" })
        {
            (HttpStatusCode status, JsonElement body, _) = await _app.GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, status);
            Assert.True(body.GetProperty("success").GetBoolean(), path);
            Assert.Equal("v1", body.GetProperty("apiVersion").GetString());
            Assert.True(body.TryGetProperty("timestamp", out _), path);
        }
    }

    [Fact]
    public async Task A_count_that_is_a_lower_bound_says_so_on_the_wire()
    {
        Seed.Acquisition(_app, Seed.HoursAgo(0));

        (_, JsonElement body, _) = await _app.GetAsync("/api/v1/statistics");

        // Enumerations travel as names. A number would mean nothing to whoever
        // reads the answer.
        Assert.Equal("LowerBound", body.GetProperty("data").GetProperty("uniqueDomainsQuality").GetString());
    }

    [Fact]
    public async Task A_withheld_score_travels_as_null_and_never_as_zero()
    {
        ObservationPeriod period = Seed.HoursAgo(0);

        Seed.Acquisition(_app, period);
        Seed.Score(
            _app,
            period,
            new Npss
            {
                OverallScore = null,
                Status = null,
                Coverage = 40m,
                AlgorithmVersion = "test-1",
                GeneratedAt = PieApplication.Now,
                Breakdown =
                [
                    new ScoreComponent
                    {
                        Component = ScoreComponentType.NetworkIntegrity,
                        State = ScoreComponentState.PartiallyMeasured,
                        Score = 5m,
                        MaxScore = 10m,
                        Weight = 10,
                        Factors = [ScoreFactor.Of("ObservationContinuity", "observed", 6, "expected", 24)],
                    },
                ],
            });

        (HttpStatusCode status, JsonElement body, _) = await _app.GetAsync("/api/v1/npss");

        JsonElement data = body.GetProperty("data");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(JsonValueKind.Null, data.GetProperty("overallScore").ValueKind);
        Assert.Equal(JsonValueKind.Null, data.GetProperty("status").ValueKind);
        Assert.Equal(40m, data.GetProperty("coverage").GetDecimal());
    }

    [Fact]
    public async Task A_factor_travels_as_a_code_and_numbers_and_never_as_a_sentence()
    {
        ObservationPeriod period = Seed.HoursAgo(0);

        Seed.Acquisition(_app, period);
        Seed.Score(
            _app,
            period,
            new Npss
            {
                OverallScore = 60,
                Status = ScoreStatus.Fair,
                Coverage = 85m,
                AlgorithmVersion = "test-1",
                GeneratedAt = PieApplication.Now,
                Breakdown =
                [
                    new ScoreComponent
                    {
                        Component = ScoreComponentType.NetworkIntegrity,
                        State = ScoreComponentState.PartiallyMeasured,
                        Score = 5m,
                        MaxScore = 10m,
                        Weight = 10,
                        Factors = [ScoreFactor.Of("ObservationContinuity", "observed", 6, "expected", 24)],
                    },
                ],
            });

        (_, JsonElement body, _) = await _app.GetAsync("/api/v1/npss");

        JsonElement area = body.GetProperty("data").GetProperty("breakdown")[0];
        JsonElement factor = area.GetProperty("factors")[0];

        Assert.Equal("NetworkIntegrity", area.GetProperty("component").GetString());
        Assert.Equal("PartiallyMeasured", area.GetProperty("state").GetString());
        Assert.Equal("ObservationContinuity", factor.GetProperty("code").GetString());

        // The interface writes the number in the language being read. A
        // string would leave it nothing to format.
        Assert.Equal(JsonValueKind.Number, factor.GetProperty("values").GetProperty("observed").ValueKind);
        Assert.Equal(24, factor.GetProperty("values").GetProperty("expected").GetInt32());
    }

    [Fact]
    public async Task A_device_declares_the_basis_of_its_identity()
    {
        ObservationPeriod period = Seed.HoursAgo(0);

        Seed.Acquisition(
            _app,
            period,
            devices: [Seed.Device("10.0.0.5", DeviceIdentityBasis.HardwareAddress, period)]);

        (_, JsonElement body, _) = await _app.GetAsync("/api/v1/devices");

        Assert.Equal(
            "HardwareAddress",
            body.GetProperty("data")[0].GetProperty("identityBasis").GetString());
    }

    // ------------------------------------------------------------------
    // Domains over a window
    // ------------------------------------------------------------------

    [Fact]
    public async Task Domains_declare_how_many_hours_exist_out_of_those_requested()
    {
        Seed.Acquisition(_app, Seed.HoursAgo(2), domains: [Seed.Domain("a.example", Seed.HoursAgo(2), 4)]);
        Seed.Acquisition(_app, Seed.HoursAgo(1), domains: [Seed.Domain("a.example", Seed.HoursAgo(1), 6)]);
        Seed.Acquisition(_app, Seed.HoursAgo(0), domains: [Seed.Domain("a.example", Seed.HoursAgo(0), 1)]);

        (_, JsonElement body, _) = await _app.GetAsync("/api/v1/domains");

        JsonElement data = body.GetProperty("data");

        // Three hours exist. Saying "the last day" would claim twenty one
        // that were never observed.
        Assert.Equal(3, data.GetProperty("periodsObserved").GetInt32());
        Assert.Equal(24, data.GetProperty("periodsRequested").GetInt32());

        JsonElement period = data.GetProperty("period");

        Assert.Equal(Seed.HoursAgo(2).Start, period.GetProperty("start").GetDateTimeOffset());
        Assert.Equal(Seed.HoursAgo(0).End, period.GetProperty("end").GetDateTimeOffset());
    }

    [Fact]
    public async Task Domains_are_summed_over_the_window_and_named_by_their_qualities()
    {
        Seed.Acquisition(_app, Seed.HoursAgo(1), domains: [Seed.Domain("a.example", Seed.HoursAgo(1), 6)]);
        Seed.Acquisition(_app, Seed.HoursAgo(0), domains: [Seed.Domain("a.example", Seed.HoursAgo(0), 1)]);

        (_, JsonElement body, _) = await _app.GetAsync("/api/v1/domains");

        JsonElement domain = Assert.Single(body.GetProperty("data").GetProperty("domains").EnumerateArray());

        Assert.Equal(7, domain.GetProperty("occurrences").GetInt64());

        // Nobody classified it, and nothing on the wire suggests otherwise.
        Assert.Equal("Unknown", domain.GetProperty("category").GetString());
        Assert.Equal("PeriodBounded", domain.GetProperty("observationQuality").GetString());
    }

    [Fact]
    public async Task A_period_older_than_the_window_does_not_count_towards_it()
    {
        // Twenty four hours requested from half past noon start at 13:00 of
        // the day before. Thirty hours ago is outside, twenty three inside.
        Seed.Acquisition(_app, Seed.HoursAgo(30), domains: [Seed.Domain("old.example", Seed.HoursAgo(30), 100)]);
        Seed.Acquisition(_app, Seed.HoursAgo(23), domains: [Seed.Domain("edge.example", Seed.HoursAgo(23), 1)]);

        (_, JsonElement body, _) = await _app.GetAsync("/api/v1/domains");

        JsonElement data = body.GetProperty("data");

        string name = Assert.Single(data.GetProperty("domains").EnumerateArray()).GetProperty("name").GetString()!;

        Assert.Equal("edge.example", name);
        Assert.Equal(1, data.GetProperty("periodsObserved").GetInt32());
    }

    // ------------------------------------------------------------------
    // A single domain
    // ------------------------------------------------------------------

    [Fact]
    public async Task A_domain_never_observed_is_reported_as_such()
    {
        Seed.Acquisition(_app, Seed.HoursAgo(0), domains: [Seed.Domain("a.example", Seed.HoursAgo(0), 1)]);

        (HttpStatusCode status, JsonElement body, _) = await _app.GetAsync("/api/v1/domains/nowhere.example");

        Assert.Equal(HttpStatusCode.NotFound, status);
        Assert.Equal("DomainNotObserved", body.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task A_domain_is_found_whatever_the_case_it_was_asked_for_in()
    {
        Seed.Acquisition(_app, Seed.HoursAgo(0), domains: [Seed.Domain("a.example", Seed.HoursAgo(0), 1)]);

        (HttpStatusCode status, _, _) = await _app.GetAsync("/api/v1/domains/A.EXAMPLE");

        Assert.Equal(HttpStatusCode.OK, status);
    }

    [Fact]
    public async Task Activity_is_returned_when_the_source_offers_it()
    {
        ObservationPeriod period = Seed.HoursAgo(0);

        Seed.Acquisition(
            _app,
            period,
            domains: [Seed.Domain("a.example", period, 5)],
            activities: [Seed.Activity("a.example", period, 5)]);

        (_, JsonElement body, _) = await _app.GetAsync("/api/v1/domains/a.example");

        JsonElement data = body.GetProperty("data");

        Assert.Equal("Available", data.GetProperty("activityAccess").GetString());
        Assert.Equal(1, data.GetProperty("activities").GetArrayLength());
    }

    [Fact]
    public async Task Activity_is_declared_unavailable_when_the_source_does_not_offer_it_and_not_shown_as_empty()
    {
        ObservationPeriod period = Seed.HoursAgo(0);

        // Rows exist, as after an earlier acquisition from a source that did
        // offer activity. The source no longer declares the capability.
        Seed.Acquisition(
            _app,
            period,
            domains: [Seed.Domain("a.example", period, 5)],
            activities: [Seed.Activity("a.example", period, 5)],
            offersActivity: false);

        (_, JsonElement body, _) = await _app.GetAsync("/api/v1/domains/a.example");

        JsonElement data = body.GetProperty("data");

        // An empty list would read as "no device contacted it".
        Assert.Equal("Unavailable", data.GetProperty("activityAccess").GetString());
        Assert.Equal(0, data.GetProperty("activities").GetArrayLength());
    }

    // ------------------------------------------------------------------
    // What must never leave
    // ------------------------------------------------------------------

    [Fact]
    public async Task No_answer_carries_the_credential_of_the_data_source()
    {
        ObservationPeriod period = Seed.HoursAgo(0);

        Seed.Acquisition(
            _app,
            period,
            domains: [Seed.Domain("a.example", period, 5)],
            devices: [Seed.Device("10.0.0.5", DeviceIdentityBasis.NetworkAddress, period)],
            activities: [Seed.Activity("a.example", period, 5)]);

        foreach (string path in new[]
        {
            "/api/v1/health",
            "/api/v1/statistics",
            "/api/v1/npss",
            "/api/v1/devices",
            "/api/v1/domains",
            "/api/v1/domains/a.example",
        })
        {
            (_, _, string raw) = await _app.GetAsync(path);

            Assert.DoesNotContain(PieApplication.SourceToken, raw, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task Health_reports_the_state_of_storage_and_that_nothing_was_acquired_yet()
    {
        (HttpStatusCode status, JsonElement body, _) = await _app.GetAsync("/api/v1/health");

        JsonElement data = body.GetProperty("data");

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("Online", data.GetProperty("api").GetString());
        Assert.Equal("Idle", data.GetProperty("adapter").GetString());
        Assert.Equal("Ready", data.GetProperty("storage").GetString());
        Assert.Equal(0, data.GetProperty("storedPeriods").GetInt64());
        Assert.Equal(JsonValueKind.Null, data.GetProperty("lastAcquisitionAt").ValueKind);
    }
}
