using System.Globalization;
using Microsoft.Data.Sqlite;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Storage;

/// <summary>
/// Keeps and returns the lists used to classify domains.
/// </summary>
/// <remarks>
/// Only what describes a list is kept here. Where the domains a list contains
/// are held is a separate decision, and is not settled by this class.
/// </remarks>
public sealed class ClassificationListRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    /// <summary>
    /// Creates the repository.
    /// </summary>
    /// <param name="connectionFactory">Source of connections to the database.</param>
    public ClassificationListRepository(SqliteConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Returns every list the installation holds, whether enabled or not.
    /// </summary>
    /// <remarks>
    /// A list left out of the classification is still shown, so that the
    /// person can tell a category that was never looked for from one that was
    /// looked for and not found.
    /// </remarks>
    /// <returns>The lists, ordered by name.</returns>
    public IReadOnlyList<ClassificationList> GetAll()
    {
        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT  name, source_url, category, licence, updated_at,
                    entry_count, enabled
            FROM    classification_list
            ORDER BY name;
            """;

        List<ClassificationList> lists = [];

        using SqliteDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            lists.Add(new ClassificationList
            {
                Name = reader.GetString(0),
                SourceUrl = new Uri(reader.GetString(1)),
                Category = Enum.Parse<ThreatCategory>(reader.GetString(2)),
                Licence = reader.GetString(3),
                UpdatedAt = reader.IsDBNull(4) ? null : ReadInstant(reader, 4),
                EntryCount = reader.GetInt32(5),
                Enabled = reader.GetBoolean(6),
            });
        }

        return lists;
    }

    /// <summary>
    /// Records a list, replacing what was held under the same name.
    /// </summary>
    /// <param name="list">List to record.</param>
    public void Save(ClassificationList list)
    {
        ArgumentNullException.ThrowIfNull(list);

        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO classification_list (
                name, source_url, category, licence, updated_at,
                entry_count, enabled)
            VALUES (
                $name, $sourceUrl, $category, $licence, $updatedAt,
                $entryCount, $enabled)
            ON CONFLICT (name) DO UPDATE SET
                source_url  = excluded.source_url,
                category    = excluded.category,
                licence     = excluded.licence,
                updated_at  = excluded.updated_at,
                entry_count = excluded.entry_count,
                enabled     = excluded.enabled;
            """;

        command.Parameters.AddWithValue("$name", list.Name);
        command.Parameters.AddWithValue("$sourceUrl", list.SourceUrl.ToString());
        command.Parameters.AddWithValue("$category", list.Category.ToString());
        command.Parameters.AddWithValue("$licence", list.Licence);
        command.Parameters.AddWithValue(
            "$updatedAt",
            list.UpdatedAt is { } updatedAt ? Format(updatedAt) : DBNull.Value);
        command.Parameters.AddWithValue("$entryCount", list.EntryCount);
        command.Parameters.AddWithValue("$enabled", list.Enabled);

        try
        {
            command.ExecuteNonQuery();
        }
        catch (SqliteException exception)
        {
            throw new StorageException("The classification list could not be recorded.", exception);
        }
    }

    /// <summary>
    /// Records a list only if no list is held under that name.
    /// </summary>
    /// <remarks>
    /// This is how the lists the project ships with are put in place on first
    /// start. It must never overwrite: a list the person disabled, or whose
    /// address they changed, is a decision of theirs, and restarting the
    /// program is not an occasion to undo it.
    /// </remarks>
    /// <param name="list">List to record when absent.</param>
    /// <returns><c>true</c> when the list was added.</returns>
    public bool AddIfAbsent(ClassificationList list)
    {
        ArgumentNullException.ThrowIfNull(list);

        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO classification_list (
                name, source_url, category, licence, updated_at,
                entry_count, enabled)
            VALUES (
                $name, $sourceUrl, $category, $licence, NULL,
                0, $enabled)
            ON CONFLICT (name) DO NOTHING;
            """;

        command.Parameters.AddWithValue("$name", list.Name);
        command.Parameters.AddWithValue("$sourceUrl", list.SourceUrl.ToString());
        command.Parameters.AddWithValue("$category", list.Category.ToString());
        command.Parameters.AddWithValue("$licence", list.Licence);
        command.Parameters.AddWithValue("$enabled", list.Enabled);

        try
        {
            return command.ExecuteNonQuery() > 0;
        }
        catch (SqliteException exception)
        {
            throw new StorageException("The classification list could not be recorded.", exception);
        }
    }

    /// <summary>
    /// Removes a list.
    /// </summary>
    /// <remarks>
    /// Classifications already recorded are left untouched. They state which
    /// list produced them and when that list was last updated, and remain a
    /// truthful record of what was known at the time.
    /// </remarks>
    /// <param name="name">Name of the list to remove.</param>
    public void Remove(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using SqliteConnection connection = _connectionFactory.Open();

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = "DELETE FROM classification_list WHERE name = $name;";
        command.Parameters.AddWithValue("$name", name);

        command.ExecuteNonQuery();
    }

    private static string Format(DateTimeOffset instant)
    {
        return instant.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset ReadInstant(SqliteDataReader reader, int ordinal)
    {
        return DateTimeOffset.Parse(
            reader.GetString(ordinal),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
    }
}
