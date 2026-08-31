using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage.Schema;

/// <summary>
/// Removes the scores whose factors were stored as text.
/// </summary>
/// <remarks>
/// A factor is now a code with its values rather than a sentence, so the rows
/// written earlier cannot be read back as factors.
/// <para>
/// They are removed rather than kept with an empty explanation. A score whose
/// reasons are lost is a number the system can no longer justify, and keeping
/// it would leave in the history exactly the kind of unexplained figure this
/// project exists to avoid.
/// </para>
/// <para>
/// No measurement is lost. Scores derive from the acquisitions, which remain.
/// </para>
/// </remarks>
internal sealed class Migration0009Factors : IMigration
{
    /// <inheritdoc />
    public int Version => 9;

    /// <inheritdoc />
    public string Description => "Scores with textual factors removed";

    /// <inheritdoc />
    public void Apply(SqliteConnection connection, SqliteTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            DELETE FROM score_component;
            DELETE FROM score;
            """;

        command.ExecuteNonQuery();
    }
}
