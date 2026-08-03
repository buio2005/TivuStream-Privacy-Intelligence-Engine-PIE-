using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage.Schema;

/// <summary>
/// Creates the tables holding what an acquisition observed.
/// </summary>
/// <remarks>
/// Every table hangs off an observation period and is removed with it, so
/// that applying retention means deleting periods and nothing else.
/// </remarks>
internal sealed class Migration0002Acquisitions : IMigration
{
    /// <inheritdoc />
    public int Version => 2;

    /// <inheritdoc />
    public string Description => "Statistics, devices and domains per period";

    /// <inheritdoc />
    public void Apply(SqliteConnection connection, SqliteTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            CREATE TABLE statistics (
                observation_period_id INTEGER NOT NULL PRIMARY KEY,
                total_queries         INTEGER NOT NULL,
                blocked_queries       INTEGER NOT NULL,
                cached_queries        INTEGER NOT NULL,
                failed_queries        INTEGER NOT NULL,
                unique_domains        INTEGER NOT NULL,
                active_devices        INTEGER NOT NULL,
                encrypted_queries     INTEGER NOT NULL,
                dnssec_enabled        INTEGER NOT NULL,

                CONSTRAINT fk_statistics_period
                    FOREIGN KEY (observation_period_id)
                    REFERENCES observation_period (id)
                    ON DELETE CASCADE
            );

            CREATE TABLE device (
                observation_period_id INTEGER NOT NULL,
                device_id             TEXT    NOT NULL,
                hostname              TEXT        NULL,
                ip_address            TEXT    NOT NULL,
                mac_address           TEXT        NULL,
                vendor                TEXT        NULL,
                operating_system      TEXT        NULL,
                first_seen            TEXT    NOT NULL,
                last_seen             TEXT    NOT NULL,
                status                TEXT    NOT NULL,

                CONSTRAINT pk_device
                    PRIMARY KEY (observation_period_id, device_id),

                CONSTRAINT fk_device_period
                    FOREIGN KEY (observation_period_id)
                    REFERENCES observation_period (id)
                    ON DELETE CASCADE
            );

            CREATE TABLE domain (
                observation_period_id INTEGER NOT NULL,
                name                  TEXT    NOT NULL,
                category              TEXT    NOT NULL,
                reputation            TEXT        NULL,
                first_seen            TEXT    NOT NULL,
                last_seen             TEXT    NOT NULL,
                occurrences           INTEGER NOT NULL,

                CONSTRAINT pk_domain
                    PRIMARY KEY (observation_period_id, name),

                CONSTRAINT fk_domain_period
                    FOREIGN KEY (observation_period_id)
                    REFERENCES observation_period (id)
                    ON DELETE CASCADE
            );

            CREATE INDEX ix_device_address ON device (ip_address);
            CREATE INDEX ix_domain_name ON domain (name);
            """;

        command.ExecuteNonQuery();
    }
}
