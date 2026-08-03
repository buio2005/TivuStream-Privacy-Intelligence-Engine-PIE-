using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage;

/// <summary>
/// Opens connections towards the database file.
/// </summary>
public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// Creates the factory.
    /// </summary>
    /// <param name="options">Settings describing where the database lives.</param>
    public SqliteConnectionFactory(StorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.DatabasePath))
        {
            throw new StorageException("The path of the database is not configured.");
        }

        string fullPath = Path.GetFullPath(options.DatabasePath);

        string? directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,

            // The database is reached by several components of the same
            // process. A shared cache would trade a little concurrency for a
            // class of locking problems that is hard to diagnose.
            Cache = SqliteCacheMode.Private,
        };

        _connectionString = builder.ToString();

        DatabasePath = fullPath;
    }

    /// <summary>
    /// Full path of the database file.
    /// </summary>
    public string DatabasePath { get; }

    /// <summary>
    /// Opens a connection.
    /// </summary>
    /// <remarks>
    /// Foreign keys are enabled on every connection: SQLite leaves them off
    /// by default, and a reference that is not enforced is a reference that
    /// will eventually be wrong.
    /// </remarks>
    public SqliteConnection Open()
    {
        SqliteConnection connection = new(_connectionString);

        try
        {
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA foreign_keys = ON;";
            command.ExecuteNonQuery();

            return connection;
        }
        catch (SqliteException exception)
        {
            connection.Dispose();

            throw new StorageException("The database could not be opened.", exception);
        }
    }
}
