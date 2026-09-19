using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage.Schema;

/// <summary>
/// Creates the table holding the sessions of signed in accounts.
/// </summary>
/// <remarks>
/// A session is identified by a random value the browser holds. The database
/// keeps only the hash of it, so that whoever reads the file cannot use a
/// session that is still open.
/// <para>
/// A session belongs to an account and does not outlive it.
/// </para>
/// </remarks>
internal sealed class Migration0011Sessions : IMigration
{
    /// <inheritdoc />
    public int Version => 11;

    /// <inheritdoc />
    public string Description => "Sessions";

    /// <inheritdoc />
    public void Apply(SqliteConnection connection, SqliteTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        command.CommandText =
            """
            CREATE TABLE session (
                id           INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                token_hash   TEXT    NOT NULL,
                account_id   INTEGER NOT NULL,
                created_at   TEXT    NOT NULL,
                last_seen_at TEXT    NOT NULL,
                expires_at   TEXT    NOT NULL,

                CONSTRAINT uq_session_token UNIQUE (token_hash),

                CONSTRAINT fk_session_account
                    FOREIGN KEY (account_id)
                    REFERENCES account (id)
                    ON DELETE CASCADE
            );

            CREATE INDEX ix_session_account ON session (account_id);
            """;

        command.ExecuteNonQuery();
    }
}
