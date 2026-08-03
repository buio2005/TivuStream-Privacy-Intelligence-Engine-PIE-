using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage.Schema;

/// <summary>
/// Creates the tables holding the score and its breakdown.
/// </summary>
/// <remarks>
/// The breakdown is kept alongside the score rather than being recomputed on
/// demand. Retaining the factors that produced a value is what makes every
/// variation explainable, as the Transparency principle requires.
/// </remarks>
internal sealed class Migration0005Score : IMigration
{
    /// <inheritdoc />
    public int Version => 5;

    /// <inheritdoc />
    public string Description => "Score and its breakdown per period";

    /// <inheritdoc />
    public void Apply(SqliteConnection connection, SqliteTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            CREATE TABLE score (
                observation_period_id INTEGER NOT NULL PRIMARY KEY,
                overall_score         INTEGER NOT NULL,
                status                TEXT    NOT NULL,
                trend                 TEXT        NULL,
                coverage              TEXT    NOT NULL,
                algorithm_version     TEXT    NOT NULL,
                generated_at          TEXT    NOT NULL,

                CONSTRAINT fk_score_period
                    FOREIGN KEY (observation_period_id)
                    REFERENCES observation_period (id)
                    ON DELETE CASCADE
            );

            CREATE TABLE score_component (
                observation_period_id INTEGER NOT NULL,
                component             TEXT    NOT NULL,
                state                 TEXT    NOT NULL,
                score                 TEXT    NOT NULL,
                max_score             TEXT    NOT NULL,
                weight                INTEGER NOT NULL,
                factors               TEXT    NOT NULL,

                CONSTRAINT pk_score_component
                    PRIMARY KEY (observation_period_id, component),

                CONSTRAINT fk_score_component_period
                    FOREIGN KEY (observation_period_id)
                    REFERENCES observation_period (id)
                    ON DELETE CASCADE
            );
            """;

        command.ExecuteNonQuery();
    }
}
