using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage.Schema;

/// <summary>
/// Records the lists used to classify domains, and how each classification
/// was obtained.
/// </summary>
/// <remarks>
/// Without these columns a stored category would be an assertion with no
/// provenance and no age, and a classification produced from a list six days
/// old would read exactly like one produced today.
/// <para>
/// Existing rows keep an empty provenance. They were produced before any list
/// existed, and inventing a source for them would be a false record.
/// </para>
/// </remarks>
internal sealed class Migration0007Classification : IMigration
{
    /// <inheritdoc />
    public int Version => 7;

    /// <inheritdoc />
    public string Description => "Classification lists and classification provenance";

    /// <inheritdoc />
    public void Apply(SqliteConnection connection, SqliteTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(connection);

        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;

        // The list table deliberately hangs off no observation period.
        //
        // A list is not something the network was observed doing: it is
        // reference data the installation holds. Tying it to a period would
        // place it under retention, and applying retention would silently
        // remove the means of classifying.
        command.CommandText =
            """
            CREATE TABLE classification_list (
                name        TEXT    NOT NULL PRIMARY KEY,
                source_url  TEXT    NOT NULL,
                category    TEXT    NOT NULL,
                licence     TEXT    NOT NULL,
                updated_at  TEXT        NULL,
                entry_count INTEGER NOT NULL,
                enabled     INTEGER NOT NULL
            );

            ALTER TABLE domain
                ADD COLUMN category_confidence TEXT NULL;

            ALTER TABLE domain
                ADD COLUMN category_source TEXT NULL;

            ALTER TABLE domain
                ADD COLUMN category_source_updated_at TEXT NULL;
            """;

        command.ExecuteNonQuery();
    }
}
