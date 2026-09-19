using Microsoft.Data.Sqlite;
using Xunit;

namespace TivuStream.Pie.Storage.Tests;

/// <summary>
/// Verifies what is kept of a session and what is removed.
/// </summary>
/// <remarks>
/// Authentication Specification, Sessions and Persistence: a session is
/// found by the hash of its identifier, belongs to an account and does not
/// outlive it, and expired sessions are removed.
/// </remarks>
public sealed class SessionRepositoryTests : IDisposable
{
    private static readonly TimeSpan Idle = TimeSpan.FromHours(8);

    private readonly TestDatabase _database = new();

    private readonly StoredAccount _maria;

    public SessionRepositoryTests()
    {
        _maria = _database.Accounts.Create("maria", AccountRole.Viewer, "hash", false, TestDatabase.Noon)!;
    }

    public void Dispose()
    {
        _database.Dispose();
    }

    [Fact]
    public void A_session_comes_back_with_the_account_it_belongs_to_as_it_is_now()
    {
        _database.Sessions.Create("hash-1", _maria.Id, TestDatabase.Noon, TestDatabase.Noon.AddDays(14));

        StoredSession found = _database.Sessions.Find("hash-1")!;

        Assert.Equal("maria", found.Account.Username);
        Assert.Equal(AccountRole.Viewer, found.Account.Role);
        Assert.Equal(TestDatabase.Noon, found.CreatedAt);
        Assert.Equal(TestDatabase.Noon, found.LastSeenAt);
        Assert.Equal(TestDatabase.Noon.AddDays(14), found.ExpiresAt);
    }

    [Fact]
    public void A_session_that_does_not_exist_is_reported_as_absent()
    {
        Assert.Null(_database.Sessions.Find("nothing"));
    }

    [Fact]
    public void Two_sessions_cannot_share_an_identifier()
    {
        _database.Sessions.Create("same", _maria.Id, TestDatabase.Noon, TestDatabase.Noon.AddDays(14));

        Assert.Throws<StorageException>(
            () => _database.Sessions.Create("same", _maria.Id, TestDatabase.Noon, TestDatabase.Noon.AddDays(14)));
    }

    [Fact]
    public void Using_a_session_moves_its_last_seen_and_nothing_else()
    {
        _database.Sessions.Create("hash-1", _maria.Id, TestDatabase.Noon, TestDatabase.Noon.AddDays(14));

        StoredSession before = _database.Sessions.Find("hash-1")!;

        _database.Sessions.Touch(before.Id, TestDatabase.Noon.AddHours(3));

        StoredSession after = _database.Sessions.Find("hash-1")!;

        Assert.Equal(TestDatabase.Noon.AddHours(3), after.LastSeenAt);
        Assert.Equal(before.CreatedAt, after.CreatedAt);
        Assert.Equal(before.ExpiresAt, after.ExpiresAt);
    }

    [Fact]
    public void A_removed_session_is_gone_and_removing_it_again_is_not_a_fault()
    {
        _database.Sessions.Create("hash-1", _maria.Id, TestDatabase.Noon, TestDatabase.Noon.AddDays(14));

        long id = _database.Sessions.Find("hash-1")!.Id;

        _database.Sessions.Delete(id);
        _database.Sessions.Delete(id);

        Assert.Null(_database.Sessions.Find("hash-1"));
    }

    [Fact]
    public void The_sessions_of_an_account_can_be_removed_all_or_all_but_one()
    {
        _database.Sessions.Create("keep", _maria.Id, TestDatabase.Noon, TestDatabase.Noon.AddDays(14));
        _database.Sessions.Create("drop-1", _maria.Id, TestDatabase.Noon, TestDatabase.Noon.AddDays(14));
        _database.Sessions.Create("drop-2", _maria.Id, TestDatabase.Noon, TestDatabase.Noon.AddDays(14));

        StoredAccount other = _database.Accounts.Create("luca", AccountRole.Viewer, "hash", false, TestDatabase.Noon)!;
        _database.Sessions.Create("elsewhere", other.Id, TestDatabase.Noon, TestDatabase.Noon.AddDays(14));

        int removed = _database.Sessions.DeleteForAccount(_maria.Id, _database.Sessions.Find("keep")!.Id);

        Assert.Equal(2, removed);
        Assert.NotNull(_database.Sessions.Find("keep"));
        Assert.Null(_database.Sessions.Find("drop-1"));
        Assert.Null(_database.Sessions.Find("drop-2"));

        // The account of another person is left alone.
        Assert.NotNull(_database.Sessions.Find("elsewhere"));

        Assert.Equal(1, _database.Sessions.DeleteForAccount(_maria.Id, exceptSessionId: null));
        Assert.Null(_database.Sessions.Find("keep"));
    }

    [Fact]
    public void Expired_sessions_are_removed_and_live_ones_are_kept()
    {
        DateTimeOffset now = TestDatabase.Noon.AddDays(20);

        // Ended by its lifetime.
        _database.Sessions.Create("too-old", _maria.Id, TestDatabase.Noon, TestDatabase.Noon.AddDays(14));

        // Within its lifetime, but idle for longer than allowed.
        _database.Sessions.Create("idle", _maria.Id, now.AddDays(-1), now.AddDays(13));

        // Recent, and still within its lifetime.
        _database.Sessions.Create("live", _maria.Id, now.AddHours(-1), now.AddDays(14));

        int removed = _database.Sessions.DeleteExpired(now, Idle);

        Assert.Equal(2, removed);
        Assert.Null(_database.Sessions.Find("too-old"));
        Assert.Null(_database.Sessions.Find("idle"));
        Assert.NotNull(_database.Sessions.Find("live"));
    }

    [Fact]
    public void A_session_does_not_outlive_its_account()
    {
        _database.Sessions.Create("hash-1", _maria.Id, TestDatabase.Noon, TestDatabase.Noon.AddDays(14));

        using (SqliteConnection connection = _database.Connections.Open())
        using (SqliteCommand command = connection.CreateCommand())
        {
            command.CommandText = "DELETE FROM account WHERE id = $id;";
            command.Parameters.AddWithValue("$id", _maria.Id);
            command.ExecuteNonQuery();
        }

        // A session left behind by a removed account would be a way in that
        // nobody granted.
        Assert.Null(_database.Sessions.Find("hash-1"));
    }
}
