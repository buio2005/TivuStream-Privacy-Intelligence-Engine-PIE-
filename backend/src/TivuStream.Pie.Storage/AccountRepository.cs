using System.Globalization;
using Microsoft.Data.Sqlite;

namespace TivuStream.Pie.Storage;

/// <summary>
/// Keeps and returns the accounts of the people who use the system.
/// </summary>
/// <remarks>
/// The repository holds what it is given and applies no policy: what makes a
/// name or a password acceptable is decided before it gets here. It only
/// guarantees that two accounts cannot share a name.
/// </remarks>
public sealed class AccountRepository
{
    // SQLITE_CONSTRAINT_UNIQUE. Told apart from every other failure because a
    // name that is already taken is an expected answer, not a fault.
    private const int UniqueConstraintViolation = 2067;

    private readonly SqliteConnectionFactory _connectionFactory;

    /// <summary>
    /// Creates the repository.
    /// </summary>
    /// <param name="connectionFactory">Source of connections to the database.</param>
    public AccountRepository(SqliteConnectionFactory connectionFactory)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Records a new account.
    /// </summary>
    /// <remarks>
    /// The name is kept in lower case, so that two people who differ only in a
    /// capital letter cannot exist: to whoever types the name they would be
    /// one person.
    /// </remarks>
    /// <param name="username">Name of the account.</param>
    /// <param name="role">What the account may read and do.</param>
    /// <param name="passwordHash">Hash of the password, never the password.</param>
    /// <param name="passwordChangeRequired">Whether the password must be changed before anything else.</param>
    /// <param name="createdAt">Instant of creation.</param>
    /// <returns>The account, or <c>null</c> when the name is already taken.</returns>
    public StoredAccount? Create(
        string username,
        AccountRole role,
        string passwordHash,
        bool passwordChangeRequired,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        string canonical = Canonical(username);

        using SqliteConnection connection = _connectionFactory.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO account (
                username, role, enabled, password_hash, password_change_required, created_at)
            VALUES (
                $username, $role, 1, $passwordHash, $passwordChangeRequired, $createdAt)
            RETURNING id;
            """;

        command.Parameters.AddWithValue("$username", canonical);
        command.Parameters.AddWithValue("$role", role.ToString());
        command.Parameters.AddWithValue("$passwordHash", passwordHash);
        command.Parameters.AddWithValue("$passwordChangeRequired", passwordChangeRequired ? 1 : 0);
        command.Parameters.AddWithValue("$createdAt", createdAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

        try
        {
            long id = Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);

            return new StoredAccount
            {
                Id = id,
                Username = canonical,
                Role = role,
                Enabled = true,
                PasswordHash = passwordHash,
                PasswordChangeRequired = passwordChangeRequired,
                CreatedAt = createdAt,
            };
        }
        catch (SqliteException exception) when (exception.SqliteExtendedErrorCode == UniqueConstraintViolation)
        {
            return null;
        }
        catch (SqliteException exception)
        {
            throw new StorageException("The account could not be recorded.", exception);
        }
    }

    /// <summary>
    /// Returns the account with the given name, when one exists.
    /// </summary>
    /// <param name="username">Name to look for, in any case.</param>
    public StoredAccount? FindByUsername(string username)
    {
        ArgumentNullException.ThrowIfNull(username);

        using SqliteConnection connection = _connectionFactory.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT id, username, role, enabled, password_hash, password_change_required, created_at
            FROM   account
            WHERE  username = $username;
            """;

        command.Parameters.AddWithValue("$username", Canonical(username));

        using SqliteDataReader reader = command.ExecuteReader();

        if (!reader.Read())
        {
            return null;
        }

        return new StoredAccount
        {
            Id = reader.GetInt64(0),
            Username = reader.GetString(1),
            Role = Enum.Parse<AccountRole>(reader.GetString(2)),
            Enabled = reader.GetInt64(3) != 0,
            PasswordHash = reader.GetString(4),
            PasswordChangeRequired = reader.GetInt64(5) != 0,
            CreatedAt = DateTimeOffset.Parse(
                reader.GetString(6),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind),
        };
    }

    /// <summary>
    /// Replaces the hash of an account's password.
    /// </summary>
    /// <remarks>
    /// An account that does not exist is a failure and not a silent no-op: a
    /// password change that changes nothing, and reports success, is worse
    /// than one that fails.
    /// </remarks>
    /// <param name="accountId">Account to change.</param>
    /// <param name="passwordHash">New hash, never the password.</param>
    /// <param name="passwordChangeRequired">Whether the new password must be changed in turn.</param>
    public void ReplacePasswordHash(long accountId, string passwordHash, bool passwordChangeRequired)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        using SqliteConnection connection = _connectionFactory.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            UPDATE account
            SET    password_hash = $passwordHash,
                   password_change_required = $passwordChangeRequired
            WHERE  id = $id;
            """;

        command.Parameters.AddWithValue("$id", accountId);
        command.Parameters.AddWithValue("$passwordHash", passwordHash);
        command.Parameters.AddWithValue("$passwordChangeRequired", passwordChangeRequired ? 1 : 0);

        int changed;

        try
        {
            changed = command.ExecuteNonQuery();
        }
        catch (SqliteException exception)
        {
            throw new StorageException("The password could not be recorded.", exception);
        }

        if (changed == 0)
        {
            throw new StorageException("The account whose password was to be changed does not exist.");
        }
    }

    private static string Canonical(string username)
    {
        return username.ToLowerInvariant();
    }
}
