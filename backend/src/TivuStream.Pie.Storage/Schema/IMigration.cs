using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage.Schema;

/// <summary>
/// Single, ordered step in the evolution of the database schema.
/// </summary>
/// <remarks>
/// Migrations are written by hand and applied in order. None is generated
/// from the model: a schema produced automatically changes whenever the model
/// changes, which is precisely what must not happen to data already stored on
/// the machine of a person.
/// <para>
/// An applied migration is never modified. A mistake is corrected by a
/// further migration.
/// </para>
/// </remarks>
public interface IMigration
{
    /// <summary>
    /// Version this migration brings the schema to.
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Short description of what the migration does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Applies the migration.
    /// </summary>
    /// <param name="connection">Open connection to the database.</param>
    /// <param name="transaction">Transaction the migration takes part in.</param>
    void Apply(SqliteConnection connection, SqliteTransaction transaction);
}
