using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage;

/// <summary>
/// Keeps and returns the sessions of signed in accounts.
/// </summary>
/// <remarks>
/// A session is looked up by the hash of its identifier. The repository never
/// sees the identifier itself, and has no way of producing one.
/// <para>
/// Whether a session has expired is not decided here. The repository returns
/// what it holds, and the rules of the lifetime belong to the caller.
/// </para>
/// </remarks>
public sealed class SessionRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    /// <summary>
    /// Creates the repository.
    /// </summary>
    /// <param name="connectionFactory">Source of connections to the database.</param>
    public SessionRepository(SqliteConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Records a new session.
    /// </summary>
    /// <param name="tokenHash">Hash of the identifier the browser holds.</param>
    /// <param name="accountId">Account the session belongs to.</param>
    /// <param name="createdAt">Instant the session begins.</param>
    /// <param name="expiresAt">Instant the session ends whatever its activity.</param>
    public void Create(string tokenHash, long accountId, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        using SqliteConnection connection = _connectionFactory.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO session (token_hash, account_id, created_at, last_seen_at, expires_at)
            VALUES ($tokenHash, $accountId, $createdAt, $createdAt, $expiresAt);
            """;

        command.Parameters.AddWithValue("$tokenHash", tokenHash);
        command.Parameters.AddWithValue("$accountId", accountId);
        command.Parameters.AddWithValue("$createdAt", Format(createdAt));
        command.Parameters.AddWithValue("$expiresAt", Format(expiresAt));

        Execute(command, "The session could not be recorded.");
    }

    /// <summary>
    /// Returns the session with the given hash, when one exists.
    /// </summary>
    /// <param name="tokenHash">Hash of the identifier presented.</param>
    public StoredSession? Find(string tokenHash)
    {
        ArgumentNullException.ThrowIfNull(tokenHash);

        using SqliteConnection connection = _connectionFactory.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT s.id, s.created_at, s.last_seen_at, s.expires_at,
                   a.id, a.username, a.role, a.enabled, a.password_hash,
                   a.password_change_required, a.created_at
            FROM   session s
            INNER JOIN account a ON a.id = s.account_id
            WHERE  s.token_hash = $tokenHash;
            """;

        command.Parameters.AddWithValue("$tokenHash", tokenHash);

        using SqliteDataReader reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return new StoredSession
        {
            Id = reader.GetInt64(0),
            CreatedAt = ReadInstant(reader, 1),
            LastSeenAt = ReadInstant(reader, 2),
            ExpiresAt = ReadInstant(reader, 3),
            Account = new StoredAccount
            {
                Id = reader.GetInt64(4),
                Username = reader.GetString(5),
                Role = Enum.Parse<AccountRole>(reader.GetString(6)),
                Enabled = reader.GetInt64(7) != 0,
                PasswordHash = reader.GetString(8),
                PasswordChangeRequired = reader.GetInt64(9) != 0,
                CreatedAt = ReadInstant(reader, 10),
            },
        };
    }

    /// <summary>
    /// Records that the session was used.
    /// </summary>
    /// <param name="sessionId">Session to renew.</param>
    /// <param name="lastSeenAt">Instant of the request.</param>
    public void Touch(long sessionId, DateTimeOffset lastSeenAt)
    {
        using SqliteConnection connection = _connectionFactory.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = "UPDATE session SET last_seen_at = $lastSeenAt WHERE id = $id;";

        command.Parameters.AddWithValue("$id", sessionId);
        command.Parameters.AddWithValue("$lastSeenAt", Format(lastSeenAt));

        Execute(command, "The session could not be renewed.");
    }

    /// <summary>
    /// Removes a session.
    /// </summary>
    /// <remarks>
    /// A session that is already gone is not an error: what was asked for is
    /// the state in which it does not exist.
    /// </remarks>
    /// <param name="sessionId">Session to remove.</param>
    public void Delete(long sessionId)
    {
        using SqliteConnection connection = _connectionFactory.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = "DELETE FROM session WHERE id = $id;";

        command.Parameters.AddWithValue("$id", sessionId);

        Execute(command, "The session could not be removed.");
    }

    /// <summary>
    /// Removes the sessions of an account, all or all but one.
    /// </summary>
    /// <remarks>
    /// Used when the password changes: every other place the account was
    /// signed in must stop working, while the one where the change was made
    /// goes on.
    /// </remarks>
    /// <param name="accountId">Account whose sessions are removed.</param>
    /// <param name="exceptSessionId">Session to leave, when there is one.</param>
    /// <returns>How many sessions were removed.</returns>
    public int DeleteForAccount(long accountId, long? exceptSessionId)
    {
        using SqliteConnection connection = _connectionFactory.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = "DELETE FROM session WHERE account_id = $accountId AND id <> $except;";

        command.Parameters.AddWithValue("$accountId", accountId);

        // Session identifiers start at 1, so 0 matches none of them.
        command.Parameters.AddWithValue("$except", exceptSessionId ?? 0);

        return Execute(command, "The sessions could not be removed.");
    }

    /// <summary>
    /// Removes the sessions that can no longer be used.
    /// </summary>
    /// <param name="now">The present instant.</param>
    /// <param name="idleTimeout">How long a session may go without a request.</param>
    /// <returns>How many sessions were removed.</returns>
    public int DeleteExpired(DateTimeOffset now, TimeSpan idleTimeout)
    {
        using SqliteConnection connection = _connectionFactory.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = "DELETE FROM session WHERE expires_at <= $now OR last_seen_at <= $idleLimit;";

        command.Parameters.AddWithValue("$now", Format(now));
        command.Parameters.AddWithValue("$idleLimit", Format(now - idleTimeout));

        return Execute(command, "The expired sessions could not be removed.");
    }

    private static int Execute(SqliteCommand command, string failure)
    {
        try
        {
            return command.ExecuteNonQuery();
        }
        catch (SqliteException exception)
        {
            throw new StorageException(failure, exception);
        }
    }

    // Instants are written in a fixed, sortable form, so that comparing two of
    // them as text is comparing them in time.
    private static string Format(DateTimeOffset instant)
    {
        return instant.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset ReadInstant(SqliteDataReader reader, int ordinal)
    {
        return DateTimeOffset.Parse(
            reader.GetString(ordinal),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);
    }
}
