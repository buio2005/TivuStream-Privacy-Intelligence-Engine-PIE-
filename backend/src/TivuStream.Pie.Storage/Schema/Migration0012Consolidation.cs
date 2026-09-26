using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage.Schema;

/// <summary>
/// Lets an observation period stand for a day or a month as well as an hour.
/// </summary>
/// <remarks>
/// A consolidated day is a period like any other, only longer: it does not
/// overlap the others and adds up with them. Two things set it apart, and
/// both stay inside the Storage.
/// <para>
/// The level says what the period stands for. The observed hours say how much
/// of it was actually watched: a day of which three hours were observed and a
/// day observed in full have the same shape, and without this number the
/// first would pass for a quiet day.
/// </para>
/// <para>
/// Every period recorded until now is an hour that was observed once.
/// </para>
/// </remarks>
internal sealed class Migration0012Consolidation : IMigration
{
    /// <inheritdoc />
    public int Version => 12;

    /// <inheritdoc />
    public string Description => "Consolidated periods";

    /// <inheritdoc />
    public void Apply(SqliteConnection connection, SqliteTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            ALTER TABLE observation_period
                ADD COLUMN granularity TEXT NOT NULL DEFAULT 'Hour';

            ALTER TABLE observation_period
                ADD COLUMN observed_hours INTEGER NOT NULL DEFAULT 1;

            CREATE INDEX ix_observation_period_granularity
                ON observation_period (granularity, period_start);
            """;

        command.ExecuteNonQuery();
    }
}
