using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage.Schema;

/// <summary>
/// Removes the scores produced by an earlier version of the algorithm.
/// </summary>
/// <remarks>
/// The scoring algorithm moves to version 3.0.0: two areas that were not
/// measurable become measurable, so the figures recorded so far are not
/// comparable with the ones that follow.
/// <para>
/// They are removed rather than carried forward. Keeping them would produce a
/// history that appears continuous and is not, and a trend drawn across the
/// change would describe an improvement nobody made.
/// </para>
/// <para>
/// No measurement is lost. Scores derive from the acquisitions, which remain,
/// and are produced again at the next evaluation.
/// </para>
/// </remarks>
internal sealed class Migration0008ScoreAlgorithm : IMigration
{
    /// <inheritdoc />
    public int Version => 8;

    /// <inheritdoc />
    public string Description => "Scores of algorithm 2.0.0 removed";

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
