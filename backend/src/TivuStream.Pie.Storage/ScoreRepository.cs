using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Storage;

/// <summary>
/// Keeps and returns the scores produced by the Core.
/// </summary>
public sealed class ScoreRepository
{
    // The factors are kept as JSON: a factor is a code with its values, and
    // a flat separator could not represent it without inventing an encoding of
    // our own.
    private static readonly JsonSerializerOptions FactorFormat = new(JsonSerializerDefaults.Web);

    private readonly SqliteConnectionFactory _connectionFactory;

    /// <summary>
    /// Creates the repository.
    /// </summary>
    /// <param name="connectionFactory">Source of connections to the database.</param>
    public ScoreRepository(SqliteConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Records the score of an observation period.
    /// </summary>
    /// <remarks>
    /// A score already recorded for the same period is replaced, in keeping
    /// with the rule that a further observation of a period supersedes the
    /// previous one.
    /// </remarks>
    /// <param name="dataSourceId">Data Source the period belongs to.</param>
    /// <param name="period">Period the score refers to.</param>
    /// <param name="score">Score to record.</param>
    public void Save(Guid dataSourceId, ObservationPeriod period, Npss score)
    {
        ArgumentNullException.ThrowIfNull(score);

        using SqliteConnection connection = _connectionFactory.Open();
        using SqliteTransaction transaction = connection.BeginTransaction();

        try
        {
            long? periodId = FindPeriod(connection, transaction, dataSourceId, period);

            if (periodId is null)
            {
                throw new StorageException("The observation period the score refers to does not exist.");
            }

            Remove(connection, transaction, periodId.Value);

            InsertScore(connection, transaction, periodId.Value, score);

            foreach (ScoreComponent component in score.Breakdown)
            {
                InsertComponent(connection, transaction, periodId.Value, component);
            }

            transaction.Commit();
        }
        catch (SqliteException exception)
        {
            transaction.Rollback();

            throw new StorageException("The score could not be recorded.", exception);
        }
    }

    /// <summary>
    /// Returns the most recent score, when one exists.
    /// </summary>
    public Npss? GetLatest()
    {
        using SqliteConnection connection = _connectionFactory.Open();

        long? periodId = FindLatestScoredPeriod(connection);

        if (periodId is null)
        {
            return null;
        }

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT overall_score, status, trend, coverage, algorithm_version, generated_at
            FROM   score
            WHERE  observation_period_id = $periodId;
            """;

        command.Parameters.AddWithValue("$periodId", periodId.Value);

        using SqliteDataReader reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        Npss score = new()
        {
            OverallScore = reader.IsDBNull(0) ? null : reader.GetInt32(0),
            Status = reader.IsDBNull(1) ? null : Enum.Parse<ScoreStatus>(reader.GetString(1)),
            Trend = reader.IsDBNull(2) ? null : Enum.Parse<ScoreTrend>(reader.GetString(2)),
            Coverage = ReadDecimal(reader, 3),
            AlgorithmVersion = reader.GetString(4),
            GeneratedAt = ReadInstant(reader, 5),
            Breakdown = ReadComponents(connection, periodId.Value),
        };

        return score;
    }

    private static List<ScoreComponent> ReadComponents(SqliteConnection connection, long periodId)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT component, state, score, max_score, weight, factors
            FROM   score_component
            WHERE  observation_period_id = $periodId
            ORDER BY weight DESC, component;
            """;

        command.Parameters.AddWithValue("$periodId", periodId);

        List<ScoreComponent> components = [];

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            components.Add(new ScoreComponent
            {
                Component = Enum.Parse<ScoreComponentType>(reader.GetString(0)),
                State = Enum.Parse<ScoreComponentState>(reader.GetString(1)),
                Score = ReadDecimal(reader, 2),
                MaxScore = ReadDecimal(reader, 3),
                Weight = reader.GetInt32(4),
                Factors = ReadFactors(reader.GetString(5)),
            });
        }

        return components;
    }

    /// <summary>
    /// Reads the factors of a component.
    /// </summary>
    /// <remarks>
    /// A row written by an earlier version holds text rather than factors. It
    /// is returned empty rather than guessed at: inventing a code for a
    /// sentence would attribute to the system a statement it never made.
    /// </remarks>
    private static List<ScoreFactor> ReadFactors(string stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<ScoreFactor>>(stored, FactorFormat) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static long? FindLatestScoredPeriod(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT   p.id
            FROM     observation_period p
            INNER JOIN score s ON s.observation_period_id = p.id
            ORDER BY p.period_start DESC
            LIMIT    1;
            """;

        object? result = command.ExecuteScalar();

        return result is null or DBNull
            ? null
            : Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private static long? FindPeriod(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid dataSourceId,
        ObservationPeriod period)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            SELECT id
            FROM   observation_period
            WHERE  data_source_id = $dataSourceId
              AND  period_start = $periodStart;
            """;

        command.Parameters.AddWithValue("$dataSourceId", dataSourceId.ToString());
        command.Parameters.AddWithValue("$periodStart", Format(period.Start));

        object? result = command.ExecuteScalar();

        return result is null or DBNull
            ? null
            : Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private static void Remove(SqliteConnection connection, SqliteTransaction transaction, long periodId)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            DELETE FROM score_component WHERE observation_period_id = $periodId;
            DELETE FROM score WHERE observation_period_id = $periodId;
            """;

        command.Parameters.AddWithValue("$periodId", periodId);

        command.ExecuteNonQuery();
    }

    private static void InsertScore(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long periodId,
        Npss score)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            INSERT INTO score (
                observation_period_id, overall_score, status, trend,
                coverage, algorithm_version, generated_at)
            VALUES (
                $periodId, $overall, $status, $trend,
                $coverage, $algorithmVersion, $generatedAt);
            """;

        command.Parameters.AddWithValue("$periodId", periodId);
        command.Parameters.AddWithValue("$overall", (object?)score.OverallScore ?? DBNull.Value);
        command.Parameters.AddWithValue("$status", (object?)score.Status?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("$trend", (object?)score.Trend?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue("$coverage", Format(score.Coverage));
        command.Parameters.AddWithValue("$algorithmVersion", score.AlgorithmVersion);
        command.Parameters.AddWithValue("$generatedAt", Format(score.GeneratedAt));

        command.ExecuteNonQuery();
    }

    private static void InsertComponent(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long periodId,
        ScoreComponent component)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            INSERT INTO score_component (
                observation_period_id, component, state, score, max_score, weight, factors)
            VALUES (
                $periodId, $component, $state, $score, $maxScore, $weight, $factors);
            """;

        command.Parameters.AddWithValue("$periodId", periodId);
        command.Parameters.AddWithValue("$component", component.Component.ToString());
        command.Parameters.AddWithValue("$state", component.State.ToString());
        command.Parameters.AddWithValue("$score", Format(component.Score));
        command.Parameters.AddWithValue("$maxScore", Format(component.MaxScore));
        command.Parameters.AddWithValue("$weight", component.Weight);
        command.Parameters.AddWithValue(
            "$factors",
            JsonSerializer.Serialize(component.Factors, FactorFormat));

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Writes a decimal without depending on the conventions of a locale.
    /// </summary>
    private static string Format(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Format(DateTimeOffset instant)
    {
        return instant.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static decimal ReadDecimal(SqliteDataReader reader, int ordinal)
    {
        return decimal.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset ReadInstant(SqliteDataReader reader, int ordinal)
    {
        return DateTimeOffset.Parse(
            reader.GetString(ordinal),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
    }
}
