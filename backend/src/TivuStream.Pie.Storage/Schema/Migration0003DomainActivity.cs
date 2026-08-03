using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage.Schema;

/// <summary>
/// Creates the table holding the interactions between devices and domains.
/// </summary>
/// <remarks>
/// The primary key repeats the grouping the Adapter applies. Outcome and
/// transport take part in it rather than being collapsed: a device that
/// reached a domain both directly and through a block produced two different
/// facts, and the schema keeps them apart.
/// <para>
/// The table holds aggregates only. The individual query never reaches the
/// database.
/// </para>
/// </remarks>
internal sealed class Migration0003DomainActivity : IMigration
{
    /// <inheritdoc />
    public int Version => 3;

    /// <inheritdoc />
    public string Description => "Interactions between devices and domains";

    /// <inheritdoc />
    public void Apply(SqliteConnection connection, SqliteTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            CREATE TABLE domain_activity (
                observation_period_id INTEGER NOT NULL,
                device_id             TEXT    NOT NULL,
                domain                TEXT    NOT NULL,
                blocked               INTEGER NOT NULL,
                protocol              TEXT    NOT NULL,
                query_count           INTEGER NOT NULL,
                first_seen            TEXT    NOT NULL,
                last_seen             TEXT    NOT NULL,

                CONSTRAINT pk_domain_activity
                    PRIMARY KEY (observation_period_id, device_id, domain, blocked, protocol),

                CONSTRAINT fk_domain_activity_period
                    FOREIGN KEY (observation_period_id)
                    REFERENCES observation_period (id)
                    ON DELETE CASCADE
            );

            CREATE INDEX ix_domain_activity_device ON domain_activity (device_id);
            CREATE INDEX ix_domain_activity_domain ON domain_activity (domain);
            """;

        command.ExecuteNonQuery();
    }
}
