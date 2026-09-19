using Microsoft.Data.Sqlite;
using Xunit;

namespace TivuStream.Pie.Storage.Tests;

/// <summary>
/// Verifies what is kept of an account, and what happens when it is asked for.
/// </summary>
/// <remarks>
/// Authentication Specification, Accounts And Roles and Persistence: the name
/// is compared without regard to case, only a hash of the password is ever
/// held, and asking for an account that does not exist is an answer, not a
/// failure.
/// </remarks>
public sealed class AccountRepositoryTests : IDisposable
{
    private const string SomeHash = "pbkdf2-sha512$210000$c2FsdA==$aGFzaA==";

    private readonly TestDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    [Fact]
    public void An_account_comes_back_as_it_was_created()
    {
        StoredAccount created = Create("maria", AccountRole.Viewer, passwordChangeRequired: true);

        StoredAccount found = _database.Accounts.FindByUsername("maria")!;

        Assert.Equal(created.Id, found.Id);
        Assert.Equal("maria", found.Username);
        Assert.Equal(AccountRole.Viewer, found.Role);
        Assert.True(found.Enabled);
        Assert.True(found.PasswordChangeRequired);
        Assert.Equal(SomeHash, found.PasswordHash);
        Assert.Equal(TestDatabase.Noon, found.CreatedAt);
    }

    [Theory]
    [InlineData("MARIA")]
    [InlineData("Maria")]
    [InlineData("maria")]
    public void A_name_is_found_however_it_is_written(string asked)
    {
        Create("maria", AccountRole.Administrator);

        Assert.NotNull(_database.Accounts.FindByUsername(asked));
    }

    [Fact]
    public void A_name_already_taken_is_refused_whatever_its_case_and_nothing_is_overwritten()
    {
        Create("maria", AccountRole.Administrator);

        // Two people who differ only in a capital letter would be one person
        // to whoever types the name.
        StoredAccount? second = _database.Accounts.Create(
            "MARIA",
            AccountRole.Viewer,
            "another-hash",
            passwordChangeRequired: false,
            TestDatabase.Noon);

        Assert.Null(second);

        StoredAccount original = _database.Accounts.FindByUsername("maria")!;

        Assert.Equal(AccountRole.Administrator, original.Role);
        Assert.Equal(SomeHash, original.PasswordHash);
    }

    [Fact]
    public void An_account_that_does_not_exist_is_reported_as_absent()
    {
        Assert.Null(_database.Accounts.FindByUsername("nobody"));
    }

    [Fact]
    public void Replacing_the_hash_keeps_the_account_and_changes_only_what_it_should()
    {
        StoredAccount created = Create("maria", AccountRole.Administrator, passwordChangeRequired: true);

        _database.Accounts.ReplacePasswordHash(created.Id, "new-hash", passwordChangeRequired: false);

        StoredAccount after = _database.Accounts.FindByUsername("maria")!;

        Assert.Equal("new-hash", after.PasswordHash);
        Assert.False(after.PasswordChangeRequired);
        Assert.Equal(AccountRole.Administrator, after.Role);
        Assert.Equal(created.CreatedAt, after.CreatedAt);
    }

    [Fact]
    public void Replacing_the_hash_of_an_account_that_does_not_exist_is_a_failure_and_not_a_silent_no_op()
    {
        // A password change that changes nothing, and says it succeeded, is
        // worse than one that fails.
        Assert.Throws<StorageException>(
            () => _database.Accounts.ReplacePasswordHash(999, "hash", passwordChangeRequired: false));
    }

    // ------------------------------------------------------------------
    // The first account
    // ------------------------------------------------------------------

    [Fact]
    public void The_first_account_is_created_when_none_exists_and_is_an_administrator()
    {
        StoredAccount? first = _database.Accounts.CreateFirst("maria", SomeHash, TestDatabase.Noon);

        Assert.NotNull(first);
        Assert.Equal(AccountRole.Administrator, first.Role);
        Assert.False(first.PasswordChangeRequired);
        Assert.True(_database.Accounts.HasAny());
    }

    [Fact]
    public void The_first_account_is_refused_when_any_account_exists_and_nothing_is_added()
    {
        Create("maria", AccountRole.Viewer);

        StoredAccount? first = _database.Accounts.CreateFirst("luca", SomeHash, TestDatabase.Noon);

        Assert.Null(first);
        Assert.Null(_database.Accounts.FindByUsername("luca"));
    }

    [Fact]
    public void Requests_arriving_together_create_one_first_account_and_no_more()
    {
        // Checking that none exists and creating one are a single statement.
        // Done in two, several of these would find the installation empty.
        StoredAccount?[] results = new StoredAccount?[16];

        Parallel.For(
            0,
            results.Length,
            index => results[index] = _database.Accounts.CreateFirst($"admin-{index}", SomeHash, TestDatabase.Noon));

        Assert.Equal(1, results.Count(result => result is not null));
    }

    [Fact]
    public void An_installation_with_no_account_is_told_apart_from_one_with_accounts()
    {
        Assert.False(_database.Accounts.HasAny());

        Create("maria", AccountRole.Viewer);

        Assert.True(_database.Accounts.HasAny());
    }

    [Fact]
    public void Only_an_enabled_administrator_counts_as_a_way_back_in()
    {
        Assert.False(_database.Accounts.HasEnabledAdministrator());

        StoredAccount viewer = Create("maria", AccountRole.Viewer);
        Assert.False(_database.Accounts.HasEnabledAdministrator());

        StoredAccount admin = Create("root", AccountRole.Administrator);
        Assert.True(_database.Accounts.HasEnabledAdministrator());

        _database.Accounts.SetEnabled(admin.Id, enabled: false);
        Assert.False(_database.Accounts.HasEnabledAdministrator());

        _database.Accounts.SetEnabled(admin.Id, enabled: true);
        Assert.True(_database.Accounts.HasEnabledAdministrator());

        // The viewer never made a difference.
        Assert.True(_database.Accounts.FindByUsername(viewer.Username)!.Enabled);
    }

    [Fact]
    public void Enabling_an_account_that_does_not_exist_is_a_failure()
    {
        Assert.Throws<StorageException>(() => _database.Accounts.SetEnabled(999, enabled: true));
    }

    [Fact]
    public void The_schema_refuses_a_role_it_does_not_know()
    {
        using SqliteConnection connection = _database.Connections.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO account (username, role, enabled, password_hash, password_change_required, created_at)
            VALUES ('sneaky', 'Root', 1, 'hash', 0, '2026-09-01T12:00:00.0000000+00:00');
            """;

        Assert.Throws<SqliteException>(() => command.ExecuteNonQuery());
    }

    private StoredAccount Create(string username, AccountRole role, bool passwordChangeRequired = false)
    {
        return _database.Accounts.Create(
            username,
            role,
            SomeHash,
            passwordChangeRequired,
            TestDatabase.Noon)!;
    }
}
