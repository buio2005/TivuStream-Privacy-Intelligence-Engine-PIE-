using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;
using Xunit;

namespace TivuStream.Pie.Storage.Tests;

/// <summary>
/// Verifies that a score comes back as it was given, reasons included.
/// </summary>
/// <remarks>
/// A score whose reasons were lost is a number the system can no longer
/// justify, which is the thing this project exists not to produce. Persistence
/// Specification, and NPSS Specification 3.1.0 for the shape of a factor.
/// </remarks>
public sealed class ScoreRepositoryTests : IDisposable
{
    private readonly TestDatabase _database = new();

    private readonly ObservationPeriod _period = TestDatabase.PeriodAt(0);

    public ScoreRepositoryTests()
    {
        _database.Acquisitions.Save(TestDatabase.Acquisition(_period));
    }

    public void Dispose()
    {
        _database.Dispose();
    }

    [Fact]
    public void The_values_of_a_factor_come_back_as_numbers_and_not_as_text()
    {
        _database.Scores.Save(
            TestDatabase.DataSourceId,
            _period,
            Score(
                Component(
                    ScoreComponentType.NetworkIntegrity,
                    ScoreComponentState.PartiallyMeasured,
                    ScoreFactor.Of("ObservationContinuity", "observed", 6, "expected", 24))));

        ScoreFactor factor = Assert.Single(Assert.Single(_database.Scores.GetLatest()!.Breakdown).Factors);

        // The interface formats a number according to the language. Text
        // would leave it nothing to format.
        Assert.Equal("ObservationContinuity", factor.Code);

        JsonElement observed = Assert.IsType<JsonElement>(factor.Values["observed"]);

        Assert.Equal(JsonValueKind.Number, observed.ValueKind);
        Assert.Equal(6, observed.GetInt32());
        Assert.Equal(24, Assert.IsType<JsonElement>(factor.Values["expected"]).GetInt32());
    }

    [Fact]
    public void A_score_that_was_withheld_comes_back_withheld_and_not_as_zero()
    {
        _database.Scores.Save(
            TestDatabase.DataSourceId,
            _period,
            Score(
                Component(
                    ScoreComponentType.DnsSecurity,
                    ScoreComponentState.Measured,
                    ScoreFactor.Of("ObservationInsufficient", "queries", 99, "minimum", 100)),
                overall: null,
                status: null,
                coverage: 0.4m));

        Npss read = _database.Scores.GetLatest()!;

        Assert.Null(read.OverallScore);
        Assert.Null(read.Status);
        Assert.Equal(0.4m, read.Coverage);
    }

    [Fact]
    public void An_area_that_could_not_be_measured_keeps_that_state()
    {
        _database.Scores.Save(
            TestDatabase.DataSourceId,
            _period,
            Score(Component(ScoreComponentType.DeviceHealth, ScoreComponentState.NotMeasurable)));

        ScoreComponent area = Assert.Single(_database.Scores.GetLatest()!.Breakdown);

        Assert.Equal(ScoreComponentState.NotMeasurable, area.State);
    }

    [Fact]
    public void Decimals_are_written_and_read_the_same_whatever_the_language_of_the_machine()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("it-IT");

            _database.Scores.Save(
                TestDatabase.DataSourceId,
                _period,
                Score(
                    Component(ScoreComponentType.Configuration, ScoreComponentState.Measured),
                    coverage: 0.7m));

            Npss read = _database.Scores.GetLatest()!;

            // A comma written and a point expected would surface as an
            // exception, or worse as a value ten times too large.
            Assert.Equal(0.7m, read.Coverage);
            Assert.Equal(12.5m, Assert.Single(read.Breakdown).Score);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void A_score_recorded_again_for_the_same_period_replaces_the_earlier_one()
    {
        _database.Scores.Save(
            TestDatabase.DataSourceId,
            _period,
            Score(Component(ScoreComponentType.DnsSecurity, ScoreComponentState.Measured), overall: 40));

        _database.Scores.Save(
            TestDatabase.DataSourceId,
            _period,
            Score(Component(ScoreComponentType.Configuration, ScoreComponentState.Measured), overall: 80));

        Npss read = _database.Scores.GetLatest()!;

        Assert.Equal(80, read.OverallScore);

        // The area of the first score must not survive alongside the second.
        Assert.Equal(ScoreComponentType.Configuration, Assert.Single(read.Breakdown).Component);
    }

    [Fact]
    public void A_score_cannot_be_recorded_for_a_period_that_was_never_observed()
    {
        ObservationPeriod unobserved = TestDatabase.PeriodAt(5);

        Assert.Throws<StorageException>(() => _database.Scores.Save(
            TestDatabase.DataSourceId,
            unobserved,
            Score(Component(ScoreComponentType.DnsSecurity, ScoreComponentState.Measured))));

        // No trace of a failed write, the previous state being untouched.
        Assert.Null(_database.Scores.GetLatest());
    }

    [Fact]
    public void Factors_written_as_text_by_an_earlier_version_are_returned_empty_and_not_guessed_at()
    {
        _database.Scores.Save(
            TestDatabase.DataSourceId,
            _period,
            Score(Component(ScoreComponentType.DnsSecurity, ScoreComponentState.Measured)));

        using (SqliteConnection connection = _database.Connections.Open())
        using (SqliteCommand command = connection.CreateCommand())
        {
            command.CommandText = "UPDATE score_component SET factors = 'A sentence written by the engine.';";
            command.ExecuteNonQuery();
        }

        ScoreComponent area = Assert.Single(_database.Scores.GetLatest()!.Breakdown);

        // Inventing a code for a sentence would attribute to the system a
        // statement it never made.
        Assert.Empty(area.Factors);
    }

    [Fact]
    public void No_score_is_reported_when_none_was_ever_recorded()
    {
        Assert.Null(_database.Scores.GetLatest());
    }

    private static ScoreComponent Component(
        ScoreComponentType type,
        ScoreComponentState state,
        params ScoreFactor[] factors)
    {
        return new ScoreComponent
        {
            Component = type,
            State = state,
            Score = 12.5m,
            MaxScore = 25m,
            Weight = 25,
            Factors = factors,
        };
    }

    private static Npss Score(
        ScoreComponent component,
        int? overall = 60,
        ScoreStatus? status = ScoreStatus.Fair,
        decimal coverage = 0.85m)
    {
        return new Npss
        {
            OverallScore = overall,
            Status = status,
            Trend = null,
            Coverage = coverage,
            AlgorithmVersion = "test-1",
            GeneratedAt = TestDatabase.Noon,
            Breakdown = [component],
        };
    }
}
