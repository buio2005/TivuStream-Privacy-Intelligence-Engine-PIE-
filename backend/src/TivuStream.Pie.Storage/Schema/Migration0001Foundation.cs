using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage.Schema;

/// <summary>
/// Creates the tables the rest of the schema hangs off.
/// </summary>
/// <remarks>
/// Two tables only: the Data Sources that have been observed, and the
/// observation periods.
/// <para>
/// Instants are stored as text in ISO 8601 form and always in UTC, because
/// SQLite has no instant type of its own. Identifiers are stored as text for
/// the same reason.
/// </para>
/// </remarks>
internal sealed class Migration0001Foundation : IMigration
{
    /// <inheritdoc />
    public int Version => 1;

    /// <inheritdoc />
    public string Description => "Data sources and observation periods";

    /// <inheritdoc />
    public void Apply(SqliteConnection connection, SqliteTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            CREATE TABLE data_source (
                id            TEXT    NOT NULL PRIMARY KEY,
                name          TEXT    NOT NULL,
                provider      TEXT    NOT NULL,
                version       TEXT    NOT NULL,
                status        TEXT    NOT NULL,
                capabilities  TEXT    NOT NULL,
                last_update   TEXT    NOT NULL
            );

            CREATE TABLE observation_period (
                id              INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                data_source_id  TEXT    NOT NULL,
                period_start    TEXT    NOT NULL,
                period_end      TEXT    NOT NULL,
                observed_at     TEXT    NOT NULL,

                CONSTRAINT fk_observation_period_data_source
                    FOREIGN KEY (data_source_id)
                    REFERENCES data_source (id)
                    ON DELETE CASCADE,

                -- A period may be observed only once per Data Source.
                -- A further observation of the same period replaces the
                -- previous one instead of adding to it: two observations of
                -- overlapping intervals describe part of the same traffic and
                -- are not additive.
                --
                -- The rule is stated in the Persistence Specification, and
                -- this constraint is what makes it impossible to break.
                CONSTRAINT uq_observation_period
                    UNIQUE (data_source_id, period_start)
            );

            CREATE INDEX ix_observation_period_start
                ON observation_period (period_start);
            """;

        command.ExecuteNonQuery();
    }
}
