using TivuStream.Pie.Api.Authentication;
using TivuStream.Pie.Storage;
using TivuStream.Pie.Storage.Schema;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// A database of its own, migrated, with the services that use it.
/// </summary>
internal sealed class TempDatabase : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "pie-recovery-" + Guid.NewGuid().ToString("N"));

    internal TempDatabase()
    {
        Connections = new SqliteConnectionFactory(
            new StorageOptions { DatabasePath = Path.Combine(_directory, "pie.db") });

        new SchemaMigrator(Connections).Migrate();

        Accounts = new AccountRepository(Connections);
        SessionRows = new SessionRepository(Connections);
        Hasher = new PasswordHasher(iterations: 1_000);
        Sessions = new SessionService(SessionRows, TimeProvider.System);
        Recovery = new RecoveryCommand(Accounts, Hasher, Sessions, TimeProvider.System);
    }

    internal SqliteConnectionFactory Connections { get; }

    internal AccountRepository Accounts { get; }

    internal SessionRepository SessionRows { get; }

    internal PasswordHasher Hasher { get; }

    internal SessionService Sessions { get; }

    internal RecoveryCommand Recovery { get; }

    public void Dispose()
    {
        // Pooled connections hold the file open. Only this database's: other tests
        // run at the same time on databases of their own.
        SqliteConnectionFactory.ReleaseConnections(Path.Combine(_directory, "pie.db"));

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    internal StoredAccount Add(string username, AccountRole role, string password = "the-old-password-1234")
    {
        return Accounts.Create(username, role, Hasher.Hash(password), false, DateTimeOffset.UtcNow)!;
    }

    internal void Disable(string username)
    {
        Accounts.SetEnabled(Accounts.FindByUsername(username)!.Id, enabled: false);
    }
}

/// <summary>
/// Answers a prompt from a list, and remembers what it was asked.
/// </summary>
internal sealed class ScriptedPrompt : IPasswordPrompt
{
    private readonly Queue<string?> _answers;

    internal ScriptedPrompt(params string?[] answers)
    {
        _answers = new Queue<string?>(answers);
    }

    internal int Asked { get; private set; }

    public string? Read(string label)
    {
        Asked++;

        return _answers.Count > 0 ? _answers.Dequeue() : null;
    }
}

/// <summary>
/// Verifies how access is restored from the machine that hosts the system.
/// </summary>
/// <remarks>
/// Authentication Specification, Recovery: run at the terminal by someone who
/// already has the machine. It restores an administrator; it does not add
/// accounts by accident and does not promote anyone.
/// </remarks>
public sealed class RecoveryCommandTests : IDisposable
{
    private const string NewPassword = "the-new-password-5678";

    private readonly TempDatabase _database = new();

    private readonly StringWriter _output = new();

    public void Dispose()
    {
        _database.Dispose();
        _output.Dispose();
    }

    // ------------------------------------------------------------------
    // With an administrator who can sign in
    // ------------------------------------------------------------------

    [Fact]
    public void The_password_of_an_existing_account_is_reset_and_must_be_changed()
    {
        _database.Add("root", AccountRole.Administrator);
        _database.Add("maria", AccountRole.Viewer);

        int exit = _database.Recovery.Run("maria", new ScriptedPrompt(NewPassword, NewPassword), _output);

        StoredAccount maria = _database.Accounts.FindByUsername("maria")!;

        Assert.Equal(RecoveryCommand.Done, exit);
        Assert.Equal(PasswordVerification.Valid, _database.Hasher.Verify(NewPassword, maria.PasswordHash));
        Assert.Equal(PasswordVerification.Failed, _database.Hasher.Verify("the-old-password-1234", maria.PasswordHash));
        Assert.True(maria.PasswordChangeRequired);
    }

    [Fact]
    public void Every_open_session_of_the_account_ends_and_those_of_others_do_not()
    {
        StoredAccount root = _database.Add("root", AccountRole.Administrator);
        StoredAccount maria = _database.Add("maria", AccountRole.Viewer);

        (string mariaToken, _) = _database.Sessions.Start(maria);
        (string rootToken, _) = _database.Sessions.Start(root);

        _database.Recovery.Run("maria", new ScriptedPrompt(NewPassword, NewPassword), _output);

        // The person who knew the old password may no longer be the one who
        // holds the session.
        Assert.Null(_database.Sessions.Validate(mariaToken));
        Assert.NotNull(_database.Sessions.Validate(rootToken));
    }

    [Fact]
    public void An_unknown_name_is_refused_and_no_account_is_added_by_a_typing_error()
    {
        _database.Add("root", AccountRole.Administrator);

        ScriptedPrompt prompt = new(NewPassword, NewPassword);

        int exit = _database.Recovery.Run("mria", prompt, _output);

        Assert.Equal(RecoveryCommand.Refused, exit);
        Assert.Null(_database.Accounts.FindByUsername("mria"));

        // Refused before the password was even asked for.
        Assert.Equal(0, prompt.Asked);
    }

    // ------------------------------------------------------------------
    // With no administrator who can sign in
    // ------------------------------------------------------------------

    [Fact]
    public void A_new_name_becomes_an_administrator_when_nobody_can_sign_in_as_one()
    {
        int exit = _database.Recovery.Run("Root", new ScriptedPrompt(NewPassword, NewPassword), _output);

        StoredAccount root = _database.Accounts.FindByUsername("root")!;

        Assert.Equal(RecoveryCommand.Done, exit);
        Assert.Equal(AccountRole.Administrator, root.Role);
        Assert.True(root.PasswordChangeRequired);
        Assert.Equal(PasswordVerification.Valid, _database.Hasher.Verify(NewPassword, root.PasswordHash));
    }

    [Fact]
    public void A_disabled_administrator_is_enabled_again_when_it_is_the_only_way_back()
    {
        _database.Add("root", AccountRole.Administrator);
        _database.Disable("root");

        int exit = _database.Recovery.Run("root", new ScriptedPrompt(NewPassword, NewPassword), _output);

        StoredAccount root = _database.Accounts.FindByUsername("root")!;

        Assert.Equal(RecoveryCommand.Done, exit);
        Assert.True(root.Enabled);
        Assert.Equal(PasswordVerification.Valid, _database.Hasher.Verify(NewPassword, root.PasswordHash));
    }

    [Fact]
    public void A_viewer_is_not_promoted_when_nobody_can_sign_in_as_an_administrator()
    {
        _database.Add("root", AccountRole.Administrator);
        _database.Disable("root");
        _database.Add("maria", AccountRole.Viewer);

        StoredAccount before = _database.Accounts.FindByUsername("maria")!;

        int exit = _database.Recovery.Run("maria", new ScriptedPrompt(NewPassword, NewPassword), _output);

        // Restoring an administrator is one thing, choosing one another.
        Assert.Equal(RecoveryCommand.Refused, exit);
        Assert.Equal(before, _database.Accounts.FindByUsername("maria"));
    }

    [Fact]
    public void A_disabled_account_stays_disabled_when_an_administrator_can_already_sign_in()
    {
        _database.Add("root", AccountRole.Administrator);
        _database.Add("maria", AccountRole.Viewer);
        _database.Disable("maria");

        _database.Recovery.Run("maria", new ScriptedPrompt(NewPassword, NewPassword), _output);

        // A password reset is not a decision to let someone back in.
        Assert.False(_database.Accounts.FindByUsername("maria")!.Enabled);
    }

    // ------------------------------------------------------------------
    // What is not acceptable changes nothing
    // ------------------------------------------------------------------

    [Fact]
    public void A_password_that_is_not_acceptable_changes_nothing()
    {
        _database.Add("root", AccountRole.Administrator);

        string before = _database.Accounts.FindByUsername("root")!.PasswordHash;

        int exit = _database.Recovery.Run("root", new ScriptedPrompt("short", "short"), _output);

        Assert.Equal(RecoveryCommand.NotAcceptable, exit);
        Assert.Equal(before, _database.Accounts.FindByUsername("root")!.PasswordHash);
    }

    [Fact]
    public void Two_passwords_that_differ_change_nothing()
    {
        _database.Add("root", AccountRole.Administrator);

        string before = _database.Accounts.FindByUsername("root")!.PasswordHash;

        int exit = _database.Recovery.Run("root", new ScriptedPrompt(NewPassword, NewPassword + "x"), _output);

        Assert.Equal(RecoveryCommand.Refused, exit);
        Assert.Equal(before, _database.Accounts.FindByUsername("root")!.PasswordHash);
    }

    [Fact]
    public void Nobody_to_answer_changes_nothing()
    {
        _database.Add("root", AccountRole.Administrator);

        string before = _database.Accounts.FindByUsername("root")!.PasswordHash;

        int exit = _database.Recovery.Run("root", new ScriptedPrompt(), _output);

        Assert.Equal(RecoveryCommand.Refused, exit);
        Assert.Equal(before, _database.Accounts.FindByUsername("root")!.PasswordHash);
    }

    [Fact]
    public void A_name_that_cannot_be_a_name_is_refused_before_anything_is_asked()
    {
        ScriptedPrompt prompt = new(NewPassword, NewPassword);

        int exit = _database.Recovery.Run("not a name!", prompt, _output);

        Assert.Equal(RecoveryCommand.NotAcceptable, exit);
        Assert.Equal(0, prompt.Asked);
    }

    [Fact]
    public void What_is_told_to_the_terminal_never_contains_the_password()
    {
        _database.Add("root", AccountRole.Administrator);

        // A rejected password is the one most likely to be echoed, to explain
        // why it was rejected. It must not be.
        const string Rejected = "rejected-x1";

        _database.Recovery.Run("root", new ScriptedPrompt(NewPassword, NewPassword), _output);
        _database.Recovery.Run("root", new ScriptedPrompt(Rejected, Rejected), _output);
        _database.Recovery.Run("root", new ScriptedPrompt(NewPassword, "other"), _output);

        Assert.NotEmpty(_output.ToString());
        Assert.DoesNotContain(NewPassword, _output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(Rejected, _output.ToString(), StringComparison.Ordinal);
    }
}
