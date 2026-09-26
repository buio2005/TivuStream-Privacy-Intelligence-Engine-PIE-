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

        _connectionString = ConnectionString(fullPath);

        DatabasePath = fullPath;
    }

    /// <summary>
    /// Closes the idle connections kept open towards one database file, and
    /// only that one.
    /// </summary>
    /// <remarks>
    /// Pooled connections hold the file open, which stops it being deleted.
    /// Clearing every pool of the process instead would close, under their
    /// feet, the connections other users of other databases are working with.
    /// </remarks>
    /// <param name="databasePath">Path of the database, as configured.</param>
    public static void ReleaseConnections(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        using SqliteConnection connection = new(ConnectionString(Path.GetFullPath(databasePath)));

        SqliteConnection.ClearPool(connection);
    }

    private static string ConnectionString(string fullPath)
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,

            // The database is reached by several components of the same
            // process. A shared cache would trade a little concurrency for a
            // class of locking problems that is hard to diagnose.
            Cache = SqliteCacheMode.Private,
        };

        return builder.ToString();
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
    /// <para>
    /// Secure deletion is enabled for the same reason on every connection:
    /// without it, a deleted row stays readable in the free pages of the file
    /// until they are reused, and the detail removed by consolidation, or a
    /// closed session, would not really be gone.
    /// </para>
    /// </remarks>
    public SqliteConnection Open()
    {
        SqliteConnection connection = new(_connectionString);

        try
        {
            connection.Open();

            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA foreign_keys = ON; PRAGMA secure_delete = ON;";
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
