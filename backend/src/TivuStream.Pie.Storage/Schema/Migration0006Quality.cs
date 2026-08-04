using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage.Schema;

/// <summary>
/// Records how each value is known, what the identity of a device rests upon,
/// and allows a score to have no overall figure.
/// </summary>
/// <remarks>
/// Without the quality columns the history would lose the distinction between
/// a figure measured directly and one known with less precision, and every
/// stored value would read as exact.
/// <para>
/// Existing rows receive the quality they were actually produced with, so
/// that the history does not claim a precision it never had.
/// </para>
/// </remarks>
internal sealed class Migration0006Quality : IMigration
{
    /// <inheritdoc />
    public int Version => 6;

    /// <inheritdoc />
    public string Description => "Measurement quality, identity basis, optional overall score";

    /// <inheritdoc />
    public void Apply(SqliteConnection connection, SqliteTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        // The scoring algorithm moves to a new major version, so the scores
        // recorded so far are not comparable with the ones that follow.
        // They are removed rather than carried forward: keeping them would
        // produce a history that appears continuous and is not.
        //
        // No measurement is lost. Scores are derived from the acquisitions,
        // which remain, and are produced again at the next evaluation.
        command.CommandText =
            """
            ALTER TABLE statistics
                ADD COLUMN unique_domains_quality TEXT NOT NULL DEFAULT 'LowerBound';

            ALTER TABLE device
                ADD COLUMN observation_quality TEXT NOT NULL DEFAULT 'PeriodBounded';

            ALTER TABLE device
                ADD COLUMN identity_basis TEXT NOT NULL DEFAULT 'NetworkAddress';

            ALTER TABLE domain
                ADD COLUMN observation_quality TEXT NOT NULL DEFAULT 'PeriodBounded';

            ALTER TABLE domain_activity
                ADD COLUMN observation_quality TEXT NOT NULL DEFAULT 'Exact';

            DROP TABLE score_component;
            DROP TABLE score;

            CREATE TABLE score (
                observation_period_id INTEGER NOT NULL PRIMARY KEY,
                overall_score         INTEGER     NULL,
                status                TEXT        NULL,
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
