using Microsoft.Data.Sqlite;
using TivuStream.Pie.Storage.Schema;
using Xunit;

namespace TivuStream.Pie.Storage.Tests;

/// <summary>
/// Verifies that a database holding data is copied before its schema changes.
/// </summary>
/// <remarks>
/// Persistence Specification, Schema Management: an older schema is migrated,
/// preceded by a backup.
/// </remarks>
public sealed class SchemaMigratorTests : IDisposable
{
    private readonly TestDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    [Fact]
    public void An_older_schema_is_copied_as_it_was_before_it_is_migrated()
    {
        _database.Acquisitions.Save(TestDatabase.Acquisition(TestDatabase.PeriodAt(0)));

        BackToVersionEleven();

        MigrationOutcome outcome = new SchemaMigrator(_database.Connections).Migrate();

        Assert.Equal(12, outcome.FinalVersion);
        Assert.NotNull(outcome.BackupPath);
        Assert.True(File.Exists(outcome.BackupPath));

        using SqliteConnection copy = new(
            new SqliteConnectionStringBuilder { DataSource = outcome.BackupPath, Pooling = false, Mode = SqliteOpenMode.ReadOnly }.ToString());
        copy.Open();

        Assert.Equal(11L, Scalar(copy, "SELECT MAX(version) FROM schema_version;"));
        Assert.Equal(1L, Scalar(copy, "SELECT COUNT(*) FROM observation_period;"));
    }

    [Fact]
    public void A_schema_already_current_is_not_copied()
    {
        MigrationOutcome outcome = new SchemaMigrator(_database.Connections).Migrate();

        Assert.Null(outcome.BackupPath);
    }

    [Fact]
    public void A_new_database_is_not_copied()
    {
        string directory = Path.Combine(Path.GetTempPath(), "pie-db-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "pie.db");

        try
        {
            MigrationOutcome outcome = new SchemaMigrator(
                new SqliteConnectionFactory(new StorageOptions { DatabasePath = path })).Migrate();

            Assert.True(outcome.DatabaseWasCreated);
            Assert.Null(outcome.BackupPath);
            Assert.Single(Directory.GetFiles(directory));
        }
        finally
        {
            SqliteConnectionFactory.ReleaseConnections(path);
            Directory.Delete(directory, recursive: true);
        }
    }

    // Undoes migration 12 by hand, so that the migrator finds a schema one
    // version behind and a period recorded under it.
    private void BackToVersionEleven()
    {
        using SqliteConnection connection = _database.Connections.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            DROP INDEX ix_observation_period_granularity;
            ALTER TABLE observation_period DROP COLUMN observed_hours;
            ALTER TABLE observation_period DROP COLUMN granularity;
            DELETE FROM schema_version WHERE version = 12;
            """;

        command.ExecuteNonQuery();
    }

    private static object? Scalar(SqliteConnection connection, string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;

        return command.ExecuteScalar();
    }
}
