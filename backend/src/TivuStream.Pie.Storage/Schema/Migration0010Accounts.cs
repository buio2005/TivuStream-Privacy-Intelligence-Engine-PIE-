using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage.Schema;

/// <summary>
/// Creates the table holding the accounts of the people who use the system.
/// </summary>
/// <remarks>
/// Belongs to no observation period and is not subject to retention: an
/// account removed for its age would leave out the person who owns it.
/// <para>
/// The password is never a column. Only a hash is, with the algorithm and its
/// parameters, so that they can grow without a further migration.
/// </para>
/// </remarks>
internal sealed class Migration0010Accounts : IMigration
{
    /// <inheritdoc />
    public int Version => 10;

    /// <inheritdoc />
    public string Description => "Accounts";

    /// <inheritdoc />
    public void Apply(SqliteConnection connection, SqliteTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            CREATE TABLE account (
                id                       INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                username                 TEXT    NOT NULL,
                role                     TEXT    NOT NULL,
                enabled                  INTEGER NOT NULL,
                password_hash            TEXT    NOT NULL,
                password_change_required INTEGER NOT NULL,
                created_at               TEXT    NOT NULL,

                -- Kept in lower case by the repository, so that the name is
                -- unique whatever the case it was typed in.
                CONSTRAINT uq_account_username UNIQUE (username),

                -- A role the software does not know must not be storable: it
                -- would be read back as something nobody decided.
                CONSTRAINT ck_account_role CHECK (role IN ('Administrator', 'Viewer'))
            );
            """;

        command.ExecuteNonQuery();
    }
}
