using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage.Schema;

/// <summary>
/// Brings the database schema to the version the software expects.
/// </summary>
public sealed class SchemaMigrator
{
    private static readonly IMigration[] Migrations =
    [
        new Migration0001Foundation(),
        new Migration0002Acquisitions(),
        new Migration0003DomainActivity(),
    ];

    private readonly SqliteConnectionFactory _connectionFactory;

    /// <summary>
    /// Creates the migrator.
    /// </summary>
    /// <param name="connectionFactory">Source of connections to the database.</param>
    public SchemaMigrator(SqliteConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Version of the schema this software expects.
    /// </summary>
    public static int ExpectedVersion => Migrations.Max(migration => migration.Version);

    /// <summary>
    /// Applies every migration the database is missing.
    /// </summary>
    /// <remarks>
    /// Running this more than once has no further effect: a schema already at
    /// the expected version is left untouched.
    /// </remarks>
    /// <returns>Result describing what was found and what was applied.</returns>
    public MigrationOutcome Migrate()
    {
        using SqliteConnection connection = _connectionFactory.Open();

        EnsureVersionTable(connection);

        int currentVersion = ReadVersion(connection);

        if (currentVersion > ExpectedVersion)
        {
            // The data was produced by a later version of the software.
            // Carrying on would write records this version does not
            // understand, and the damage would surface much later.
            throw new StorageException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The database is at schema version {currentVersion}, while this version of the software expects {ExpectedVersion}. Use a matching version of the software, or restore a backup."));
        }

        if (currentVersion == ExpectedVersion)
        {
            return new MigrationOutcome
            {
                InitialVersion = currentVersion,
                FinalVersion = currentVersion,
                AppliedMigrations = [],
            };
        }

        List<string> applied = [];

        foreach (IMigration migration in Migrations.OrderBy(migration => migration.Version))
        {
            if (migration.Version <= currentVersion)
            {
                continue;
            }

            Apply(connection, migration);

            applied.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{migration.Version:0000} {migration.Description}"));
        }

        return new MigrationOutcome
        {
            InitialVersion = currentVersion,
            FinalVersion = ExpectedVersion,
            AppliedMigrations = applied,
        };
    }

    private static void Apply(SqliteConnection connection, IMigration migration)
    {
        using SqliteTransaction transaction = connection.BeginTransaction();

        try
        {
            migration.Apply(connection, transaction);

            using (SqliteCommand command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText =
                    """
                    INSERT INTO schema_version (version, description, applied_at)
                    VALUES ($version, $description, $appliedAt);
                    """;

                command.Parameters.AddWithValue("$version", migration.Version);
                command.Parameters.AddWithValue("$description", migration.Description);
                command.Parameters.AddWithValue(
                    "$appliedAt",
                    DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));

                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch (SqliteException exception)
        {
            transaction.Rollback();

            throw new StorageException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"Migration {migration.Version:0000} could not be applied."),
                exception);
        }
    }

    private static void EnsureVersionTable(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS schema_version (
                version     INTEGER NOT NULL PRIMARY KEY,
                description TEXT    NOT NULL,
                applied_at  TEXT    NOT NULL
            );
            """;

        command.ExecuteNonQuery();
    }

    private static int ReadVersion(SqliteConnection connection)
    {
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = "SELECT COALESCE(MAX(version), 0) FROM schema_version;";

        object? result = command.ExecuteScalar();

        return result is null or DBNull
            ? 0
            : Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }
}
