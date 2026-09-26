using System.Globalization;
using Microsoft.Data.Sqlite;
using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Storage;

/// <summary>
/// Keeps and returns what acquisitions observed.
/// </summary>
public sealed class AcquisitionRepository
{
    /// <summary>
    /// The least precise observation quality among the rows aggregated, as the
    /// numeric value of <see cref="MeasurementQuality"/>.
    /// </summary>
    /// <remarks>
    /// A total is known no better than its vaguest part.
    /// </remarks>
    private const string LeastPreciseQuality =
        """
        MAX(CASE observation_quality
                WHEN 'Exact'         THEN 0
                WHEN 'LowerBound'    THEN 1
                WHEN 'PeriodBounded' THEN 2
                ELSE 3
            END)
        """;

    private readonly SqliteConnectionFactory _connectionFactory;

    /// <summary>
    /// Creates the repository.
    /// </summary>
    /// <param name="connectionFactory">Source of connections to the database.</param>
    public AcquisitionRepository(SqliteConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Records an observation.
    /// </summary>
    /// <remarks>
    /// An observation of a period that has already been observed replaces the
    /// previous one. The previous period is removed and written again, so
    /// that nothing of the earlier observation survives alongside the new one.
    /// <para>
    /// The whole operation takes place in a single transaction: an observation
    /// is either recorded in full or not at all.
    /// </para>
    /// </remarks>
    /// <param name="acquisition">Observation to record.</param>
    public void Save(StoredAcquisition acquisition)
    {
        ArgumentNullException.ThrowIfNull(acquisition);

        using SqliteConnection connection = _connectionFactory.Open();
        using SqliteTransaction transaction = connection.BeginTransaction();

        try
        {
            SaveDataSource(connection, transaction, acquisition.DataSource);

            RemovePeriod(connection, transaction, acquisition.DataSource.Id, acquisition.Period);

            long periodId = InsertPeriod(connection, transaction, acquisition);

            InsertStatistics(connection, transaction, periodId, acquisition.Statistics);

            if (acquisition.Configuration is not null)
            {
                InsertConfiguration(connection, transaction, periodId, acquisition.Configuration);
            }

            foreach (Device device in acquisition.Devices)
            {
                InsertDevice(connection, transaction, periodId, device);
            }

            foreach (Domain domain in acquisition.Domains)
            {
                InsertDomain(connection, transaction, periodId, domain);
            }

            foreach (DomainActivity activity in acquisition.DomainActivities)
            {
                InsertDomainActivity(connection, transaction, periodId, activity);
            }

            transaction.Commit();
        }
        catch (SqliteException exception)
        {
            transaction.Rollback();

            throw new StorageException("The observation could not be recorded.", exception);
        }
    }

    /// <summary>
    /// Returns the most recent observation, when one exists.
    /// </summary>
    /// <remarks>
    /// Devices and domains are recorded but not returned here. They will be
    /// read when the endpoints that present them exist.
    /// </remarks>
    public StoredAcquisition? GetLatest()
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT  p.period_start,
                    p.period_end,
                    p.observed_at,
                    d.id,
                    d.name,
                    d.provider,
                    d.version,
                    d.status,
                    d.capabilities,
                    d.last_update,
                    s.total_queries,
                    s.blocked_queries,
                    s.cached_queries,
                    s.failed_queries,
                    s.unique_domains,
                    s.active_devices,
                    s.encrypted_queries,
                    s.dnssec_enabled,
                    s.unique_domains_quality,
                    c.dnssec_validation_enabled,
                    c.encrypted_transports,
                    c.query_minimisation_enabled,
                    c.client_subnet_forwarding_enabled,
                    c.filtering_enabled,
                    c.filter_list_count,
                    c.filter_list_update_hours
            FROM        observation_period p
            INNER JOIN  data_source d ON d.id = p.data_source_id
            INNER JOIN  statistics  s ON s.observation_period_id = p.id
            LEFT  JOIN  source_configuration c ON c.observation_period_id = p.id
            ORDER BY    p.period_start DESC
            LIMIT       1;
            """;

        using SqliteDataReader reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return new StoredAcquisition
        {
            Period = new ObservationPeriod(ReadInstant(reader, 0), ReadInstant(reader, 1)),
            ObservedAt = ReadInstant(reader, 2),

            DataSource = new DataSource
            {
                Id = Guid.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
                Name = reader.GetString(4),
                Provider = reader.GetString(5),
                Version = reader.GetString(6),
                Status = Enum.Parse<DataSourceStatus>(reader.GetString(7)),
                Capabilities = SplitCapabilities(reader.GetString(8)),
                LastUpdate = ReadInstant(reader, 9),
            },

            Statistics = new Statistics
            {
                TotalQueries = reader.GetInt64(10),
                BlockedQueries = reader.GetInt64(11),
                CachedQueries = reader.GetInt64(12),
                FailedQueries = reader.GetInt64(13),
                UniqueDomains = reader.GetInt32(14),
                ActiveDevices = reader.GetInt32(15),
                EncryptedQueries = reader.GetInt64(16),
                DnssecEnabled = reader.GetInt64(17) != 0,
                UniqueDomainsQuality = Enum.Parse<MeasurementQuality>(reader.GetString(18)),
            },

            // Absent when the source did not provide the capability during
            // that period, which is not the same as a configuration of zeros.
            Configuration = reader.IsDBNull(19)
                ? null
                : new SourceConfiguration
                {
                    DnssecValidationEnabled = reader.GetInt64(19) != 0,
                    EncryptedTransports = SplitCapabilities(reader.GetString(20)),
                    QueryMinimisationEnabled = reader.GetInt64(21) != 0,
                    ClientSubnetForwardingEnabled = reader.GetInt64(22) != 0,
                    FilteringEnabled = reader.GetInt64(23) != 0,
                    FilterListCount = reader.GetInt32(24),
                    FilterListUpdateIntervalHours = reader.GetInt32(25),
                },
        };
    }

    /// <summary>
    /// Returns the devices observed since the given instant, one per
    /// identifier.
    /// </summary>
    /// <remarks>
    /// Lawful because the identifier is derived deterministically from the
    /// address or the hardware address. A device recognised by its network
    /// address that changed address within the window appears twice, and its
    /// identity basis says so.
    /// </remarks>
    /// <param name="since">Beginning of the window, inclusive.</param>
    public List<ObservedDevice> GetDevicesSince(DateTimeOffset since)
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            WITH windowed AS (
                SELECT      d.*, p.period_start
                FROM        device d
                INNER JOIN  observation_period p ON p.id = d.observation_period_id
                WHERE       p.period_start >= $since
            ),
            aggregated AS (
                SELECT   device_id,
                         MIN(first_seen)   AS first_seen,
                         MAX(last_seen)    AS last_seen,
                         MAX(period_start) AS latest_period,
                         {LeastPreciseQuality} AS quality_rank
                FROM     windowed
                GROUP BY device_id
            )
            SELECT      a.device_id, w.hostname, w.ip_address, w.mac_address,
                        w.vendor, w.operating_system, w.identity_basis,
                        a.first_seen, a.last_seen, a.quality_rank
            FROM        aggregated a
            INNER JOIN  windowed w
                    ON  w.device_id = a.device_id AND w.period_start = a.latest_period
            ORDER BY    w.ip_address, a.device_id;
            """;

        command.Parameters.AddWithValue("$since", Format(since));

        List<ObservedDevice> devices = [];

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            devices.Add(new ObservedDevice
            {
                DeviceId = Guid.Parse(reader.GetString(0), CultureInfo.InvariantCulture),
                Hostname = reader.IsDBNull(1) ? null : reader.GetString(1),
                IpAddress = reader.GetString(2),
                MacAddress = reader.IsDBNull(3) ? null : reader.GetString(3),
                Vendor = reader.IsDBNull(4) ? null : reader.GetString(4),
                OperatingSystem = reader.IsDBNull(5) ? null : reader.GetString(5),
                IdentityBasis = Enum.Parse<DeviceIdentityBasis>(reader.GetString(6)),
                FirstSeen = ReadInstant(reader, 7),
                LastSeen = ReadInstant(reader, 8),
                ObservationQuality = (MeasurementQuality)reader.GetInt32(9),
            });
        }

        return devices;
    }

    /// <summary>
    /// Returns the statistics of the network since the given instant,
    /// aggregated, or nothing when no period falls in the window.
    /// </summary>
    /// <remarks>
    /// Counts of queries add up, since periods do not overlap. Distinct
    /// domains and devices do not: the same domain in two hours would count
    /// twice. The highest hourly value and the distinct names or identifiers
    /// kept are both lower bounds, the sources returning truncated lists, and
    /// the greater of the two is still one.
    /// <para>
    /// Nothing, rather than a set of zeros, when the window is empty: zeros
    /// would say the network queried nothing, when nothing was observed.
    /// </para>
    /// </remarks>
    /// <param name="since">Beginning of the window, inclusive.</param>
    public Statistics? GetStatisticsSince(DateTimeOffset since)
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            WITH windowed AS (
                SELECT      s.*, p.period_start
                FROM        statistics s
                INNER JOIN  observation_period p ON p.id = s.observation_period_id
                WHERE       p.period_start >= $since
            ),
            latest AS (
                SELECT dnssec_enabled, unique_domains_quality
                FROM   windowed
                ORDER BY period_start DESC
                LIMIT  1
            )
            SELECT  COUNT(*),
                    SUM(total_queries), SUM(blocked_queries), SUM(cached_queries),
                    SUM(failed_queries), SUM(encrypted_queries),
                    MAX(unique_domains), MAX(active_devices),
                    (SELECT     COUNT(DISTINCT d.name)
                     FROM       domain d
                     INNER JOIN observation_period p ON p.id = d.observation_period_id
                     WHERE      p.period_start >= $since),
                    (SELECT     COUNT(DISTINCT v.device_id)
                     FROM       device v
                     INNER JOIN observation_period p ON p.id = v.observation_period_id
                     WHERE      p.period_start >= $since),
                    (SELECT dnssec_enabled FROM latest),
                    (SELECT unique_domains_quality FROM latest)
            FROM    windowed;
            """;

        command.Parameters.AddWithValue("$since", Format(since));

        using SqliteDataReader reader = command.ExecuteReader();

        if (!reader.Read() || reader.GetInt64(0) == 0)
        {
            return null;
        }

        long periods = reader.GetInt64(0);

        return new Statistics
        {
            TotalQueries = reader.GetInt64(1),
            BlockedQueries = reader.GetInt64(2),
            CachedQueries = reader.GetInt64(3),
            FailedQueries = reader.GetInt64(4),
            EncryptedQueries = reader.GetInt64(5),
            UniqueDomains = (int)Math.Max(reader.GetInt64(6), reader.GetInt64(8)),

            // More than one hour: the distinct domains of the window are known
            // only as a floor, whatever each hour said of its own.
            UniqueDomainsQuality = periods > 1
                ? MeasurementQuality.LowerBound
                : Enum.Parse<MeasurementQuality>(reader.GetString(11)),

            ActiveDevices = (int)Math.Max(reader.GetInt64(7), reader.GetInt64(9)),
            DnssecEnabled = reader.GetInt64(10) != 0,
        };
    }

    /// <summary>
    /// Returns the beginning of the earliest observation period recorded.
    /// </summary>
    /// <remarks>
    /// Nothing can be expected of the system before this instant: it was not
    /// observing the network yet.
    /// </remarks>
    public DateTimeOffset? GetFirstPeriodStart()
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT MIN(period_start) FROM observation_period;";

        object? result = command.ExecuteScalar();

        return result is null or DBNull
            ? null
            : DateTimeOffset.Parse(
                Convert.ToString(result, CultureInfo.InvariantCulture)!,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
    }

    /// <summary>
    /// Returns the domains observed since the given instant, aggregated.
    /// </summary>
    /// <remarks>
    /// Observation periods are fixed and do not overlap, so the occurrences of
    /// the same domain in different periods add up without counting the same
    /// traffic twice. That property comes from the Persistence Specification,
    /// and is what makes this reading lawful at all.
    /// <para>
    /// The classification is taken from the most recent period the domain
    /// appears in: each period holds what could be said then, and the most
    /// recent one is what is known now. The age of the list travels with it
    /// and says how recent that "now" is.
    /// </para>
    /// </remarks>
    /// <param name="since">Beginning of the window, inclusive.</param>
    public List<Domain> GetDomainsSince(DateTimeOffset since)
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            WITH windowed AS (
                SELECT      d.*, p.period_start
                FROM        domain d
                INNER JOIN  observation_period p ON p.id = d.observation_period_id
                WHERE       p.period_start >= $since
            ),
            aggregated AS (
                SELECT   name,
                         MIN(first_seen)   AS first_seen,
                         MAX(last_seen)    AS last_seen,
                         SUM(occurrences)  AS occurrences,
                         MAX(period_start) AS latest_period,
                         {LeastPreciseQuality} AS quality_rank
                FROM     windowed
                GROUP BY name
            )
            SELECT      a.name, w.category, w.reputation, a.first_seen, a.last_seen,
                        a.occurrences, a.quality_rank, w.category_confidence,
                        w.category_source, w.category_source_updated_at
            FROM        aggregated a
            INNER JOIN  windowed w
                    ON  w.name = a.name AND w.period_start = a.latest_period
            ORDER BY    a.occurrences DESC, a.name;
            """;

        command.Parameters.AddWithValue("$since", Format(since));

        List<Domain> domains = [];

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            domains.Add(new Domain
            {
                Name = reader.GetString(0),
                Category = Enum.Parse<ThreatCategory>(reader.GetString(1)),
                Reputation = reader.IsDBNull(2) ? null : reader.GetString(2),
                FirstSeen = ReadInstant(reader, 3),
                LastSeen = ReadInstant(reader, 4),
                Occurrences = reader.GetInt64(5),
                ObservationQuality = (MeasurementQuality)reader.GetInt32(6),
                CategoryConfidence = reader.IsDBNull(7)
                    ? null
                    : Enum.Parse<ConfidenceLevel>(reader.GetString(7)),
                CategorySource = reader.IsDBNull(8) ? null : reader.GetString(8),
                CategorySourceUpdatedAt = reader.IsDBNull(9) ? null : ReadInstant(reader, 9),
            });
        }

        return domains;
    }

    /// <summary>
    /// Returns the activity towards a domain since the given instant,
    /// aggregated per device, outcome and transport.
    /// </summary>
    /// <remarks>
    /// The sum is lawful for the reason that makes the one of the domains
    /// lawful, periods that do not overlap, and because the identifier of a
    /// device is derived deterministically: the same address yields the same
    /// identifier in every period. How solid that identity is travels with
    /// the device.
    /// </remarks>
    /// <param name="domain">Domain, exactly as stored.</param>
    /// <param name="since">Beginning of the window, inclusive.</param>
    public List<ObservedActivity> GetActivitiesSince(string domain, DateTimeOffset since)
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            WITH windowed AS (
                SELECT      a.*
                FROM        domain_activity a
                INNER JOIN  observation_period p ON p.id = a.observation_period_id
                WHERE       p.period_start >= $since
                  AND       a.domain = $domain
            ),
            aggregated AS (
                SELECT   device_id, blocked, protocol,
                         SUM(query_count) AS query_count,
                         MIN(first_seen)  AS first_seen,
                         MAX(last_seen)   AS last_seen,
                         {LeastPreciseQuality} AS quality_rank
                FROM     windowed
                GROUP BY device_id, blocked, protocol
            ),
            described AS (
                SELECT      d.device_id, d.hostname, d.ip_address, d.identity_basis,
                            ROW_NUMBER() OVER (PARTITION BY d.device_id ORDER BY p.period_start DESC) AS recency
                FROM        device d
                INNER JOIN  observation_period p ON p.id = d.observation_period_id
                WHERE       p.period_start >= $since
            )
            SELECT      a.device_id, s.hostname, s.ip_address, s.identity_basis,
                        a.query_count, a.blocked, a.protocol, a.first_seen, a.last_seen,
                        a.quality_rank
            FROM        aggregated a
            LEFT JOIN   described s
                    ON  s.device_id = a.device_id AND s.recency = 1
            ORDER BY    a.query_count DESC, a.device_id, a.blocked, a.protocol;
            """;

        command.Parameters.AddWithValue("$domain", domain);
        command.Parameters.AddWithValue("$since", Format(since));

        List<ObservedActivity> activities = [];

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            // A device with no description in the interval has none of it:
            // the address and the basis are absent together.
            bool described = !reader.IsDBNull(2);

            activities.Add(new ObservedActivity
            {
                Device = new DeviceIdentification
                {
                    DeviceId = Guid.Parse(reader.GetString(0), CultureInfo.InvariantCulture),
                    Hostname = reader.IsDBNull(1) ? null : reader.GetString(1),
                    IpAddress = described ? reader.GetString(2) : null,
                    IdentityBasis = described ? Enum.Parse<DeviceIdentityBasis>(reader.GetString(3)) : null,
                },
                QueryCount = reader.GetInt64(4),
                Blocked = reader.GetInt64(5) != 0,
                Protocol = reader.GetString(6),
                FirstSeen = ReadInstant(reader, 7),
                LastSeen = ReadInstant(reader, 8),
                ObservationQuality = (MeasurementQuality)reader.GetInt32(9),
            });
        }

        return activities;
    }

    /// <summary>
    /// Returns the activity towards every domain since the given instant,
    /// aggregated per device, domain, outcome and transport.
    /// </summary>
    /// <remarks>
    /// What the score reads to tell blocked queries from answered ones over
    /// its window (NPSS Specification, Evaluation Window). Summed for the
    /// reason that makes the detail of a domain lawful: periods that do not
    /// overlap, and a device identifier derived deterministically.
    /// </remarks>
    /// <param name="since">Beginning of the window, inclusive.</param>
    public List<DomainActivity> GetAllActivitiesSince(DateTimeOffset since)
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            $"""
            SELECT      a.device_id, a.domain, a.blocked, a.protocol,
                        SUM(a.query_count), MIN(a.first_seen), MAX(a.last_seen),
                        {LeastPreciseQuality}
            FROM        domain_activity a
            INNER JOIN  observation_period p ON p.id = a.observation_period_id
            WHERE       p.period_start >= $since
            GROUP BY    a.device_id, a.domain, a.blocked, a.protocol;
            """;

        command.Parameters.AddWithValue("$since", Format(since));

        List<DomainActivity> activities = [];

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            activities.Add(new DomainActivity
            {
                DeviceId = Guid.Parse(reader.GetString(0), CultureInfo.InvariantCulture),
                Domain = reader.GetString(1),
                Blocked = reader.GetInt64(2) != 0,
                Protocol = reader.GetString(3),
                QueryCount = reader.GetInt64(4),
                FirstSeen = ReadInstant(reader, 5),
                LastSeen = ReadInstant(reader, 6),
                ObservationQuality = (MeasurementQuality)reader.GetInt32(7),
            });
        }

        return activities;
    }

    /// <summary>
    /// Returns the interval actually covered by the periods recorded since the
    /// given instant.
    /// </summary>
    /// <remarks>
    /// What was asked for and what exists are not the same thing. An
    /// installation running for six hours must not report a day.
    /// </remarks>
    /// <param name="since">Beginning of the window, inclusive.</param>
    /// <param name="until">
    /// Beginning of the last period to include, when the window ends before
    /// the present: the window a stored score was computed over.
    /// </param>
    public ObservationPeriod? GetPeriodRangeSince(DateTimeOffset since, DateTimeOffset? until = null)
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT MIN(period_start), MAX(period_end)
            FROM   observation_period
            WHERE  period_start >= $since
              AND  ($until IS NULL OR period_start <= $until);
            """;

        command.Parameters.AddWithValue("$since", Format(since));
        command.Parameters.AddWithValue("$until", until is null ? DBNull.Value : Format(until.Value));

        using SqliteDataReader reader = command.ExecuteReader();

        return reader.Read() && !reader.IsDBNull(0)
            ? new ObservationPeriod(ReadInstant(reader, 0), ReadInstant(reader, 1))
            : null;
    }

    /// <summary>
    /// Returns the most recent observation period, when one exists.
    /// </summary>
    /// <remarks>
    /// Read so that a result can declare the interval it refers to. A list
    /// without its period cannot be told apart from a period without a list.
    /// </remarks>
    public ObservationPeriod? GetLatestPeriod()
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT   period_start, period_end
            FROM     observation_period
            ORDER BY period_start DESC
            LIMIT    1;
            """;

        using SqliteDataReader reader = command.ExecuteReader();

        return reader.Read()
            ? new ObservationPeriod(ReadInstant(reader, 0), ReadInstant(reader, 1))
            : null;
    }

    /// <summary>
    /// Returns how many observation periods were recorded since the given
    /// instant.
    /// </summary>
    /// <param name="since">Beginning of the interval to count over.</param>
    /// <param name="until">Beginning of the last period to count, when the window ends before the present.</param>
    public int CountPeriodsSince(DateTimeOffset since, DateTimeOffset? until = null)
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT COUNT(*)
            FROM   observation_period
            WHERE  period_start >= $since
              AND  ($until IS NULL OR period_start <= $until);
            """;

        command.Parameters.AddWithValue("$since", Format(since));
        command.Parameters.AddWithValue("$until", until is null ? DBNull.Value : Format(until.Value));

        object? result = command.ExecuteScalar();

        return result is null or DBNull
            ? 0
            : Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns how many observation periods are kept.
    /// </summary>
    public long CountPeriods()
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM observation_period;";

        object? result = command.ExecuteScalar();

        return result is null or DBNull
            ? 0
            : Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private static void SaveDataSource(
        SqliteConnection connection,
        SqliteTransaction transaction,
        DataSource dataSource)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            INSERT INTO data_source (id, name, provider, version, status, capabilities, last_update)
            VALUES ($id, $name, $provider, $version, $status, $capabilities, $lastUpdate)
            ON CONFLICT (id) DO UPDATE SET
                name         = excluded.name,
                provider     = excluded.provider,
                version      = excluded.version,
                status       = excluded.status,
                capabilities = excluded.capabilities,
                last_update  = excluded.last_update;
            """;

        command.Parameters.AddWithValue("$id", dataSource.Id.ToString());
        command.Parameters.AddWithValue("$name", dataSource.Name);
        command.Parameters.AddWithValue("$provider", dataSource.Provider);
        command.Parameters.AddWithValue("$version", dataSource.Version);
        command.Parameters.AddWithValue("$status", dataSource.Status.ToString());
        command.Parameters.AddWithValue("$capabilities", string.Join(',', dataSource.Capabilities));
        command.Parameters.AddWithValue("$lastUpdate", Format(dataSource.LastUpdate));

        command.ExecuteNonQuery();
    }

    private static void RemovePeriod(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid dataSourceId,
        ObservationPeriod period)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            DELETE FROM observation_period
            WHERE data_source_id = $dataSourceId
              AND period_start = $periodStart;
            """;

        command.Parameters.AddWithValue("$dataSourceId", dataSourceId.ToString());
        command.Parameters.AddWithValue("$periodStart", Format(period.Start));

        command.ExecuteNonQuery();
    }

    private static long InsertPeriod(
        SqliteConnection connection,
        SqliteTransaction transaction,
        StoredAcquisition acquisition)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            INSERT INTO observation_period (data_source_id, period_start, period_end, observed_at)
            VALUES ($dataSourceId, $periodStart, $periodEnd, $observedAt)
            RETURNING id;
            """;

        command.Parameters.AddWithValue("$dataSourceId", acquisition.DataSource.Id.ToString());
        command.Parameters.AddWithValue("$periodStart", Format(acquisition.Period.Start));
        command.Parameters.AddWithValue("$periodEnd", Format(acquisition.Period.End));
        command.Parameters.AddWithValue("$observedAt", Format(acquisition.ObservedAt));

        object? result = command.ExecuteScalar();

        return result is null or DBNull
            ? throw new StorageException("The observation period could not be recorded.")
            : Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private static void InsertStatistics(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long periodId,
        Statistics statistics)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            INSERT INTO statistics (
                observation_period_id, total_queries, blocked_queries, cached_queries,
                failed_queries, unique_domains, active_devices, encrypted_queries, dnssec_enabled,
                unique_domains_quality)
            VALUES (
                $periodId, $totalQueries, $blockedQueries, $cachedQueries,
                $failedQueries, $uniqueDomains, $activeDevices, $encryptedQueries, $dnssecEnabled,
                $uniqueDomainsQuality);
            """;

        command.Parameters.AddWithValue("$periodId", periodId);
        command.Parameters.AddWithValue("$totalQueries", statistics.TotalQueries);
        command.Parameters.AddWithValue("$blockedQueries", statistics.BlockedQueries);
        command.Parameters.AddWithValue("$cachedQueries", statistics.CachedQueries);
        command.Parameters.AddWithValue("$failedQueries", statistics.FailedQueries);
        command.Parameters.AddWithValue("$uniqueDomains", statistics.UniqueDomains);
        command.Parameters.AddWithValue("$activeDevices", statistics.ActiveDevices);
        command.Parameters.AddWithValue("$encryptedQueries", statistics.EncryptedQueries);
        command.Parameters.AddWithValue("$dnssecEnabled", statistics.DnssecEnabled ? 1 : 0);
        command.Parameters.AddWithValue("$uniqueDomainsQuality", statistics.UniqueDomainsQuality.ToString());

        command.ExecuteNonQuery();
    }

    private static void InsertConfiguration(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long periodId,
        SourceConfiguration configuration)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            INSERT INTO source_configuration (
                observation_period_id, dnssec_validation_enabled, encrypted_transports,
                query_minimisation_enabled, client_subnet_forwarding_enabled,
                filtering_enabled, filter_list_count, filter_list_update_hours)
            VALUES (
                $periodId, $dnssec, $transports,
                $minimisation, $clientSubnet,
                $filtering, $listCount, $updateHours);
            """;

        command.Parameters.AddWithValue("$periodId", periodId);
        command.Parameters.AddWithValue("$dnssec", configuration.DnssecValidationEnabled ? 1 : 0);
        command.Parameters.AddWithValue("$transports", string.Join(',', configuration.EncryptedTransports));
        command.Parameters.AddWithValue("$minimisation", configuration.QueryMinimisationEnabled ? 1 : 0);
        command.Parameters.AddWithValue("$clientSubnet", configuration.ClientSubnetForwardingEnabled ? 1 : 0);
        command.Parameters.AddWithValue("$filtering", configuration.FilteringEnabled ? 1 : 0);
        command.Parameters.AddWithValue("$listCount", configuration.FilterListCount);
        command.Parameters.AddWithValue("$updateHours", configuration.FilterListUpdateIntervalHours);

        command.ExecuteNonQuery();
    }

    private static void InsertDevice(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long periodId,
        Device device)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            INSERT INTO device (
                observation_period_id, device_id, hostname, ip_address, mac_address,
                vendor, operating_system, first_seen, last_seen, status,
                observation_quality, identity_basis)
            VALUES (
                $periodId, $deviceId, $hostname, $ipAddress, $macAddress,
                $vendor, $operatingSystem, $firstSeen, $lastSeen, $status,
                $observationQuality, $identityBasis);
            """;

        command.Parameters.AddWithValue("$periodId", periodId);
        command.Parameters.AddWithValue("$deviceId", device.DeviceId.ToString());
        command.Parameters.AddWithValue("$hostname", (object?)device.Hostname ?? DBNull.Value);
        command.Parameters.AddWithValue("$ipAddress", device.IpAddress);
        command.Parameters.AddWithValue("$macAddress", (object?)device.MacAddress ?? DBNull.Value);
        command.Parameters.AddWithValue("$vendor", (object?)device.Vendor ?? DBNull.Value);
        command.Parameters.AddWithValue("$operatingSystem", (object?)device.OperatingSystem ?? DBNull.Value);
        command.Parameters.AddWithValue("$firstSeen", Format(device.FirstSeen));
        command.Parameters.AddWithValue("$lastSeen", Format(device.LastSeen));
        command.Parameters.AddWithValue("$status", device.Status.ToString());
        command.Parameters.AddWithValue("$observationQuality", device.ObservationQuality.ToString());
        command.Parameters.AddWithValue("$identityBasis", device.IdentityBasis.ToString());

        command.ExecuteNonQuery();
    }

    private static void InsertDomain(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long periodId,
        Domain domain)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            INSERT INTO domain (
                observation_period_id, name, category, reputation,
                first_seen, last_seen, occurrences, observation_quality,
                category_confidence, category_source, category_source_updated_at)
            VALUES (
                $periodId, $name, $category, $reputation,
                $firstSeen, $lastSeen, $occurrences, $observationQuality,
                $categoryConfidence, $categorySource, $categorySourceUpdatedAt);
            """;

        command.Parameters.AddWithValue("$periodId", periodId);
        command.Parameters.AddWithValue("$name", domain.Name);
        command.Parameters.AddWithValue("$category", domain.Category.ToString());
        command.Parameters.AddWithValue("$reputation", (object?)domain.Reputation ?? DBNull.Value);
        command.Parameters.AddWithValue("$firstSeen", Format(domain.FirstSeen));
        command.Parameters.AddWithValue("$lastSeen", Format(domain.LastSeen));
        command.Parameters.AddWithValue("$occurrences", domain.Occurrences);
        command.Parameters.AddWithValue("$observationQuality", domain.ObservationQuality.ToString());
        command.Parameters.AddWithValue(
            "$categoryConfidence",
            (object?)domain.CategoryConfidence?.ToString() ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "$categorySource",
            (object?)domain.CategorySource ?? DBNull.Value);
        command.Parameters.AddWithValue(
            "$categorySourceUpdatedAt",
            domain.CategorySourceUpdatedAt is { } updatedAt
                ? Format(updatedAt)
                : DBNull.Value);

        command.ExecuteNonQuery();
    }

    private static void InsertDomainActivity(
        SqliteConnection connection,
        SqliteTransaction transaction,
        long periodId,
        DomainActivity activity)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            INSERT INTO domain_activity (
                observation_period_id, device_id, domain, blocked, protocol,
                query_count, first_seen, last_seen, observation_quality)
            VALUES (
                $periodId, $deviceId, $domain, $blocked, $protocol,
                $queryCount, $firstSeen, $lastSeen, $observationQuality);
            """;

        command.Parameters.AddWithValue("$periodId", periodId);
        command.Parameters.AddWithValue("$deviceId", activity.DeviceId.ToString());
        command.Parameters.AddWithValue("$domain", activity.Domain);
        command.Parameters.AddWithValue("$blocked", activity.Blocked ? 1 : 0);
        command.Parameters.AddWithValue("$protocol", activity.Protocol);
        command.Parameters.AddWithValue("$queryCount", activity.QueryCount);
        command.Parameters.AddWithValue("$firstSeen", Format(activity.FirstSeen));
        command.Parameters.AddWithValue("$lastSeen", Format(activity.LastSeen));
        command.Parameters.AddWithValue("$observationQuality", activity.ObservationQuality.ToString());

        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Writes an instant in a form that sorts correctly as text.
    /// </summary>
    private static string Format(DateTimeOffset instant)
    {
        return instant.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset ReadInstant(SqliteDataReader reader, int ordinal)
    {
        return DateTimeOffset.Parse(
            reader.GetString(ordinal),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
    }

    private static string[] SplitCapabilities(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
