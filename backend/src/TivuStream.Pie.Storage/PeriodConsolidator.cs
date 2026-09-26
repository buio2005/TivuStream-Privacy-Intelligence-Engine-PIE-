using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage;

/// <summary>
/// Consolidates the history into days and months as it ages, and deletes what
/// has outlived every level.
/// </summary>
/// <remarks>
/// Persistence Specification, Retention. A consolidated day is an observation
/// period like the hours it replaces, only longer: what adds up is added up,
/// what does not is declared for what it is, and the hours actually observed
/// travel with it.
/// <para>
/// A whole day or a whole month is consolidated at a time, each in a single
/// transaction: the aggregate is written and the detail removed together. An
/// interruption leaves the detail untouched for the next run, and running
/// again produces no duplicates, since detail already consolidated no longer
/// exists.
/// </para>
/// </remarks>
public sealed class PeriodConsolidator
{
    private const string Hour = "Hour";
    private const string Day = "Day";
    private const string Month = "Month";

    // A period may exist only once per start and Data Source, and the day
    // being written begins where its first hour does. It is written under a
    // provisional start, which never outlives the transaction, and moved to
    // its own once the hours are gone.
    private const string ProvisionalStart = "consolidating";

    private static readonly Level Days = new(Day, [Hour], KeepsDeviceActivity: true, LocalCalendar.DayContaining);

    // Device activity is not carried into months: after a year it is known
    // which domains the network contacted, no longer which device contacted
    // which. Persistence Specification, Device Activity Beyond Thirty Days.
    private static readonly Level Months = new(Month, [Hour, Day], KeepsDeviceActivity: false, LocalCalendar.MonthContaining);

    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly RetentionOptions _retention;
    private readonly TimeZoneInfo _zone;

    /// <summary>
    /// Creates the consolidator.
    /// </summary>
    /// <param name="connectionFactory">Source of connections to the database.</param>
    /// <param name="retention">How long each level is kept.</param>
    /// <param name="zone">Time zone the days and months of the person follow.</param>
    public PeriodConsolidator(SqliteConnectionFactory connectionFactory, RetentionOptions retention, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(retention);
        ArgumentNullException.ThrowIfNull(zone);

        _connectionFactory = connectionFactory;
        _retention = retention;
        _zone = zone;
    }

    /// <summary>
    /// Consolidates every day and month that has ended long enough ago, and
    /// deletes what has outlived the monthly retention.
    /// </summary>
    /// <remarks>
    /// A finer retention prevails over a coarser one: a month is neither
    /// consolidated nor deleted while it holds data a finer level still has
    /// to keep.
    /// </remarks>
    /// <param name="now">The present instant.</param>
    public ConsolidationOutcome Consolidate(DateTimeOffset now)
    {
        DateTimeOffset hourly = now.AddDays(-_retention.HourlyDays);
        DateTimeOffset daily = Earlier(now.AddMonths(-_retention.DailyMonths), hourly);
        DateTimeOffset monthly = Earlier(now.AddYears(-_retention.MonthlyYears), daily);

        try
        {
            int days = ConsolidateLevel(Days, hourly);
            int months = ConsolidateLevel(Months, daily);
            int deleted = DeleteEndedBy(monthly);

            return new ConsolidationOutcome(days, months, deleted);
        }
        catch (SqliteException exception)
        {
            throw new StorageException("The history could not be consolidated.", exception);
        }
    }

    private static DateTimeOffset Earlier(DateTimeOffset first, DateTimeOffset second)
    {
        return first < second ? first : second;
    }

    private int ConsolidateLevel(Level level, DateTimeOffset cutoff)
    {
        int written = 0;

        // The earliest finer period decides the next interval to consolidate.
        // Every pass removes it, so the loop ends; once its interval has not
        // ended long enough ago, no later one has either.
        while (FindEarliestFinerPeriod(level, cutoff) is ({ } dataSourceId, { } start))
        {
            (DateTimeOffset intervalStart, DateTimeOffset intervalEnd) = level.Interval(start, _zone);

            if (intervalEnd > cutoff)
            {
                break;
            }

            ConsolidateInterval(level, dataSourceId, intervalStart, intervalEnd);

            written++;
        }

        return written;
    }

    private (string? DataSourceId, DateTimeOffset? Start) FindEarliestFinerPeriod(Level level, DateTimeOffset cutoff)
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT   data_source_id, period_start
            FROM     observation_period
            WHERE    granularity IN ({level.FinerList})
              AND    period_start < $cutoff
            ORDER BY period_start
            LIMIT    1;
            """;

        command.Parameters.AddWithValue("$cutoff", Format(cutoff));

        using SqliteDataReader reader = command.ExecuteReader();

        return reader.Read()
            ? (reader.GetString(0), ReadInstant(reader, 1))
            : (null, null);
    }

    private void ConsolidateInterval(Level level, string dataSourceId, DateTimeOffset start, DateTimeOffset end)
    {
        using SqliteConnection connection = _connectionFactory.Open();
        using SqliteTransaction transaction = connection.BeginTransaction();

        // The periods consolidated: the finer ones in the interval, and one of
        // the same level already there, should a stray hour have been observed
        // after its day was consolidated. The aggregate is written under a
        // provisional start and is never among them.
        string sources =
            $"""
            src AS (
                SELECT id, period_start
                FROM   observation_period
                WHERE  data_source_id = $dataSourceId
                  AND  granularity IN ({level.IncludedList})
                  AND  period_start >= $start
                  AND  period_start < $end
                  AND  id <> $id
            )
            """;

        long id = 0;

        SqliteCommand Prepare(string sql, long? chosen = null)
        {
            SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;

            command.Parameters.AddWithValue("$dataSourceId", dataSourceId);
            command.Parameters.AddWithValue("$start", Format(start));
            command.Parameters.AddWithValue("$end", Format(end));
            command.Parameters.AddWithValue("$id", id);
            command.Parameters.AddWithValue("$granularity", level.Granularity);
            command.Parameters.AddWithValue("$provisional", ProvisionalStart);
            command.Parameters.AddWithValue("$chosen", (object?)chosen ?? DBNull.Value);

            return command;
        }

        void Run(string sql, long? chosen = null)
        {
            using SqliteCommand command = Prepare(sql, chosen);

            command.ExecuteNonQuery();
        }

        object? Scalar(string sql)
        {
            using SqliteCommand command = Prepare(sql);

            return command.ExecuteScalar();
        }

        try
        {
            DateTimeOffset declaredStart = DeclaredStart(Scalar, start);

            id = Convert.ToInt64(
                Scalar(
                    $"""
                    WITH {sources}
                    INSERT INTO observation_period (
                        data_source_id, period_start, period_end, observed_at, granularity, observed_hours)
                    SELECT $dataSourceId, $provisional, $end, MAX(p.observed_at), $granularity, SUM(p.observed_hours)
                    FROM   observation_period p
                    INNER JOIN src ON src.id = p.id
                    RETURNING id;
                    """),
                CultureInfo.InvariantCulture);

            Run(
                $"""
                WITH {sources}
                INSERT INTO statistics (
                    observation_period_id, total_queries, blocked_queries, cached_queries,
                    failed_queries, unique_domains, active_devices, encrypted_queries, dnssec_enabled,
                    unique_domains_quality)
                SELECT $id,
                       SUM(s.total_queries), SUM(s.blocked_queries), SUM(s.cached_queries),
                       SUM(s.failed_queries),
                       MAX(MAX(s.unique_domains),
                           (SELECT COUNT(DISTINCT d.name) FROM domain d
                            WHERE  d.observation_period_id IN (SELECT id FROM src))),
                       MAX(MAX(s.active_devices),
                           (SELECT COUNT(DISTINCT v.device_id) FROM device v
                            WHERE  v.observation_period_id IN (SELECT id FROM src))),
                       SUM(s.encrypted_queries),
                       (SELECT     l.dnssec_enabled
                        FROM       statistics l
                        INNER JOIN src ON src.id = l.observation_period_id
                        ORDER BY   src.period_start DESC
                        LIMIT      1),
                       -- Distinct domains over more than an hour are a floor at
                       -- best, and never better than the vaguest hour.
                       CASE MAX(CASE s.unique_domains_quality
                                    WHEN 'Exact'         THEN 1
                                    WHEN 'LowerBound'    THEN 1
                                    WHEN 'PeriodBounded' THEN 2
                                    ELSE 3
                                END)
                           WHEN 1 THEN 'LowerBound'
                           WHEN 2 THEN 'PeriodBounded'
                           ELSE 'Estimated'
                       END
                FROM   statistics s
                INNER JOIN src ON src.id = s.observation_period_id
                HAVING COUNT(*) > 0;
                """);

            Run(
                $"""
                WITH {sources}
                INSERT INTO source_configuration (
                    observation_period_id, dnssec_validation_enabled, encrypted_transports,
                    query_minimisation_enabled, client_subnet_forwarding_enabled,
                    filtering_enabled, filter_list_count, filter_list_update_hours)
                SELECT     $id, c.dnssec_validation_enabled, c.encrypted_transports,
                           c.query_minimisation_enabled, c.client_subnet_forwarding_enabled,
                           c.filtering_enabled, c.filter_list_count, c.filter_list_update_hours
                FROM       source_configuration c
                INNER JOIN src ON src.id = c.observation_period_id
                ORDER BY   src.period_start DESC
                LIMIT      1;
                """);

            Run(
                $"""
                WITH {sources},
                windowed AS (
                    SELECT d.*, src.period_start
                    FROM   device d
                    INNER JOIN src ON src.id = d.observation_period_id
                ),
                aggregated AS (
                    SELECT   device_id,
                             MIN(first_seen)   AS first_seen,
                             MAX(last_seen)    AS last_seen,
                             MAX(period_start) AS latest_period,
                             {StoredQuality.LeastPreciseName} AS quality
                    FROM     windowed
                    GROUP BY device_id
                )
                INSERT INTO device (
                    observation_period_id, device_id, hostname, ip_address, mac_address,
                    vendor, operating_system, first_seen, last_seen, status,
                    observation_quality, identity_basis)
                SELECT      $id, a.device_id, w.hostname, w.ip_address, w.mac_address,
                            w.vendor, w.operating_system, a.first_seen, a.last_seen, w.status,
                            a.quality, w.identity_basis
                FROM        aggregated a
                INNER JOIN  windowed w
                        ON  w.device_id = a.device_id AND w.period_start = a.latest_period;
                """);

            Run(
                $"""
                WITH {sources},
                windowed AS (
                    SELECT d.*, src.period_start
                    FROM   domain d
                    INNER JOIN src ON src.id = d.observation_period_id
                ),
                aggregated AS (
                    SELECT   name,
                             MIN(first_seen)   AS first_seen,
                             MAX(last_seen)    AS last_seen,
                             SUM(occurrences)  AS occurrences,
                             MAX(period_start) AS latest_period,
                             {StoredQuality.LeastPreciseName} AS quality
                    FROM     windowed
                    GROUP BY name
                )
                INSERT INTO domain (
                    observation_period_id, name, category, reputation,
                    first_seen, last_seen, occurrences, observation_quality,
                    category_confidence, category_source, category_source_updated_at)
                SELECT      $id, a.name, w.category, w.reputation,
                            a.first_seen, a.last_seen, a.occurrences, a.quality,
                            w.category_confidence, w.category_source, w.category_source_updated_at
                FROM        aggregated a
                INNER JOIN  windowed w
                        ON  w.name = a.name AND w.period_start = a.latest_period;
                """);

            if (level.KeepsDeviceActivity)
            {
                Run(
                    $"""
                    WITH {sources}
                    INSERT INTO domain_activity (
                        observation_period_id, device_id, domain, blocked, protocol,
                        query_count, first_seen, last_seen, observation_quality)
                    SELECT     $id, a.device_id, a.domain, a.blocked, a.protocol,
                               SUM(a.query_count), MIN(a.first_seen), MAX(a.last_seen),
                               {StoredQuality.LeastPreciseName}
                    FROM       domain_activity a
                    INNER JOIN src ON src.id = a.observation_period_id
                    GROUP BY   a.device_id, a.domain, a.blocked, a.protocol;
                    """);
            }

            // A score is neither added nor averaged. The last one produced in
            // the interval is kept as it was; an interval without one has none.
            object? chosen = Scalar(
                $"""
                WITH {sources}
                SELECT     s.observation_period_id
                FROM       score s
                INNER JOIN src ON src.id = s.observation_period_id
                ORDER BY   src.period_start DESC
                LIMIT      1;
                """);

            if (chosen is not null and not DBNull)
            {
                long scored = Convert.ToInt64(chosen, CultureInfo.InvariantCulture);

                Run(
                    """
                    INSERT INTO score (
                        observation_period_id, overall_score, status, trend,
                        coverage, algorithm_version, generated_at)
                    SELECT $id, overall_score, status, trend, coverage, algorithm_version, generated_at
                    FROM   score
                    WHERE  observation_period_id = $chosen;

                    INSERT INTO score_component (
                        observation_period_id, component, state, score, max_score, weight, factors)
                    SELECT $id, component, state, score, max_score, weight, factors
                    FROM   score_component
                    WHERE  observation_period_id = $chosen;
                    """,
                    scored);
            }

            Run(
                $"""
                WITH {sources}
                DELETE FROM observation_period WHERE id IN (SELECT id FROM src);
                """);

            using (SqliteCommand move = connection.CreateCommand())
            {
                move.Transaction = transaction;
                move.CommandText = "UPDATE observation_period SET period_start = $declared WHERE id = $id;";
                move.Parameters.AddWithValue("$declared", Format(declaredStart));
                move.Parameters.AddWithValue("$id", id);
                move.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();

            throw;
        }
    }

    /// <summary>
    /// Where the consolidated period begins.
    /// </summary>
    /// <remarks>
    /// At the start of its day or month, unless a period of the same level
    /// already reaches past it, which happens when the time zone of the
    /// computer has changed since: consolidated periods are not rewritten, and
    /// the new one begins where the earlier ends, so that the two do not
    /// overlap.
    /// </remarks>
    private static DateTimeOffset DeclaredStart(Func<string, object?> scalar, DateTimeOffset start)
    {
        object? previousEnd = scalar(
            """
            SELECT MAX(period_end)
            FROM   observation_period
            WHERE  data_source_id = $dataSourceId
              AND  granularity = $granularity
              AND  period_start < $start;
            """);

        if (previousEnd is null or DBNull)
        {
            return start;
        }

        DateTimeOffset end = Parse(Convert.ToString(previousEnd, CultureInfo.InvariantCulture)!);

        return end > start ? end : start;
    }

    private int DeleteEndedBy(DateTimeOffset cutoff)
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        // Whatever its level: nothing that ended before the monthly retention
        // is kept, and no finer retention can still be holding it.
        command.CommandText = "DELETE FROM observation_period WHERE period_end <= $cutoff;";
        command.Parameters.AddWithValue("$cutoff", Format(cutoff));

        return command.ExecuteNonQuery();
    }

    private static string Format(DateTimeOffset instant)
    {
        return instant.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset Parse(string value)
    {
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    private static DateTimeOffset ReadInstant(SqliteDataReader reader, int ordinal)
    {
        return Parse(reader.GetString(ordinal));
    }

    /// <summary>
    /// A level of the history and the finer levels it is made of.
    /// </summary>
    private sealed record Level(
        string Granularity,
        string[] Finer,
        bool KeepsDeviceActivity,
        Func<DateTimeOffset, TimeZoneInfo, (DateTimeOffset Start, DateTimeOffset End)> Interval)
    {
        internal string FinerList => string.Join(", ", Finer.Select(name => $"'{name}'"));

        internal string IncludedList => string.Join(", ", Finer.Append(Granularity).Select(name => $"'{name}'"));
    }
}
