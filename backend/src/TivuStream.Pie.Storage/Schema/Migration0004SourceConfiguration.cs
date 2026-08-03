using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage.Schema;

/// <summary>
/// Creates the table holding the settings of the Data Source.
/// </summary>
/// <remarks>
/// The configuration is recorded per observation period, so that a change in
/// the settings can be seen in the history rather than only in the present.
/// A score that improves because DNSSEC was switched on should be traceable
/// to that moment.
/// </remarks>
internal sealed class Migration0004SourceConfiguration : IMigration
{
    /// <inheritdoc />
    public int Version => 4;

    /// <inheritdoc />
    public string Description => "Settings of the data source per period";

    /// <inheritdoc />
    public void Apply(SqliteConnection connection, SqliteTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            CREATE TABLE source_configuration (
                observation_period_id            INTEGER NOT NULL PRIMARY KEY,
                dnssec_validation_enabled        INTEGER NOT NULL,
                encrypted_transports             TEXT    NOT NULL,
                query_minimisation_enabled       INTEGER NOT NULL,
                client_subnet_forwarding_enabled INTEGER NOT NULL,
                filtering_enabled                INTEGER NOT NULL,
                filter_list_count                INTEGER NOT NULL,
                filter_list_update_hours         INTEGER NOT NULL,

                CONSTRAINT fk_source_configuration_period
                    FOREIGN KEY (observation_period_id)
                    REFERENCES observation_period (id)
                    ON DELETE CASCADE
            );
            """;

        command.ExecuteNonQuery();
    }
}
