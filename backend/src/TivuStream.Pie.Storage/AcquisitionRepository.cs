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
    /// Returns the devices of the most recent observation period.
    /// </summary>
    public IReadOnlyList<Device> GetLatestDevices()
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT  d.device_id, d.hostname, d.ip_address, d.mac_address,
                    d.vendor, d.operating_system, d.first_seen, d.last_seen, d.status,
                    d.observation_quality, d.identity_basis
            FROM    device d
            WHERE   d.observation_period_id = (SELECT id FROM observation_period ORDER BY period_start DESC LIMIT 1)
            ORDER BY d.ip_address;
            """;

        List<Device> devices = [];

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            devices.Add(new Device
            {
                DeviceId = Guid.Parse(reader.GetString(0), CultureInfo.InvariantCulture),
                Hostname = reader.IsDBNull(1) ? null : reader.GetString(1),
                IpAddress = reader.GetString(2),
                MacAddress = reader.IsDBNull(3) ? null : reader.GetString(3),
                Vendor = reader.IsDBNull(4) ? null : reader.GetString(4),
                OperatingSystem = reader.IsDBNull(5) ? null : reader.GetString(5),
                FirstSeen = ReadInstant(reader, 6),
                LastSeen = ReadInstant(reader, 7),
                Status = Enum.Parse<DeviceStatus>(reader.GetString(8)),
                ObservationQuality = Enum.Parse<MeasurementQuality>(reader.GetString(9)),
                IdentityBasis = Enum.Parse<DeviceIdentityBasis>(reader.GetString(10)),
            });
        }

        return devices;
    }

    /// <summary>
    /// Returns the domains of the most recent observation period.
    /// </summary>
    public IReadOnlyList<Domain> GetLatestDomains()
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT  d.name, d.category, d.reputation, d.first_seen, d.last_seen,
                    d.occurrences, d.observation_quality, d.category_confidence,
                    d.category_source, d.category_source_updated_at
            FROM    domain d
            WHERE   d.observation_period_id = (SELECT id FROM observation_period ORDER BY period_start DESC LIMIT 1)
            ORDER BY d.occurrences DESC, d.name;
            """;

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
                ObservationQuality = Enum.Parse<MeasurementQuality>(reader.GetString(6)),
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
    /// Returns the interactions recorded for a domain in the most recent
    /// observation period.
    /// </summary>
    /// <param name="domain">Domain to look for.</param>
    public IReadOnlyList<DomainActivity> GetLatestActivitiesFor(string domain)
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT  a.device_id, a.domain, a.query_count, a.blocked,
                    a.protocol, a.first_seen, a.last_seen, a.observation_quality
            FROM    domain_activity a
            WHERE   a.observation_period_id = (SELECT id FROM observation_period ORDER BY period_start DESC LIMIT 1)
              AND   a.domain = $domain
            ORDER BY a.query_count DESC;
            """;

        command.Parameters.AddWithValue("$domain", domain);

        List<DomainActivity> activities = [];

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            activities.Add(new DomainActivity
            {
                DeviceId = Guid.Parse(reader.GetString(0), CultureInfo.InvariantCulture),
                Domain = reader.GetString(1),
                QueryCount = reader.GetInt64(2),
                Blocked = reader.GetInt64(3) != 0,
                Protocol = reader.GetString(4),
                FirstSeen = ReadInstant(reader, 5),
                LastSeen = ReadInstant(reader, 6),
                ObservationQuality = Enum.Parse<MeasurementQuality>(reader.GetString(7)),
            });
        }

        return activities;
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
            """
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

                         -- The least precise quality among those aggregated:
                         -- a total is known no better than its vaguest part.
                         MAX(CASE observation_quality
                                 WHEN 'Exact'         THEN 0
                                 WHEN 'LowerBound'    THEN 1
                                 WHEN 'PeriodBounded' THEN 2
                                 ELSE 3
                             END) AS quality_rank
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
    /// Returns the interval actually covered by the periods recorded since the
    /// given instant.
    /// </summary>
    /// <remarks>
    /// What was asked for and what exists are not the same thing. An
    /// installation running for six hours must not report a day.
    /// </remarks>
    /// <param name="since">Beginning of the window, inclusive.</param>
    public ObservationPeriod? GetPeriodRangeSince(DateTimeOffset since)
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT MIN(period_start), MAX(period_end)
            FROM   observation_period
            WHERE  period_start >= $since;
            """;

        command.Parameters.AddWithValue("$since", Format(since));

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
    public int CountPeriodsSince(DateTimeOffset since)
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT COUNT(*)
            FROM   observation_period
            WHERE  period_start >= $since;
            """;

        command.Parameters.AddWithValue("$since", Format(since));

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
