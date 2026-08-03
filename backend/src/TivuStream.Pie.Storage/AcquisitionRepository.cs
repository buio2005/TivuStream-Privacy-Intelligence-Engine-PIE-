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

            foreach (Device device in acquisition.Devices)
            {
                InsertDevice(connection, transaction, periodId, device);
            }

            foreach (Domain domain in acquisition.Domains)
            {
                InsertDomain(connection, transaction, periodId, domain);
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
                    s.dnssec_enabled
            FROM        observation_period p
            INNER JOIN  data_source d ON d.id = p.data_source_id
            INNER JOIN  statistics  s ON s.observation_period_id = p.id
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
            },
        };
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
                failed_queries, unique_domains, active_devices, encrypted_queries, dnssec_enabled)
            VALUES (
                $periodId, $totalQueries, $blockedQueries, $cachedQueries,
                $failedQueries, $uniqueDomains, $activeDevices, $encryptedQueries, $dnssecEnabled);
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
                vendor, operating_system, first_seen, last_seen, status)
            VALUES (
                $periodId, $deviceId, $hostname, $ipAddress, $macAddress,
                $vendor, $operatingSystem, $firstSeen, $lastSeen, $status);
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
                first_seen, last_seen, occurrences)
            VALUES (
                $periodId, $name, $category, $reputation,
                $firstSeen, $lastSeen, $occurrences);
            """;

        command.Parameters.AddWithValue("$periodId", periodId);
        command.Parameters.AddWithValue("$name", domain.Name);
        command.Parameters.AddWithValue("$category", domain.Category.ToString());
        command.Parameters.AddWithValue("$reputation", (object?)domain.Reputation ?? DBNull.Value);
        command.Parameters.AddWithValue("$firstSeen", Format(domain.FirstSeen));
        command.Parameters.AddWithValue("$lastSeen", Format(domain.LastSeen));
        command.Parameters.AddWithValue("$occurrences", domain.Occurrences);

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
