using System.Text.RegularExpressions;
using TivuStream.Pie.Api.Authentication;
using TivuStream.Pie.Storage;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies the life of the setup code across starts of the service.
/// </summary>
/// <remarks>
/// Authentication Specification, First Run: the code exists only while no
/// account does, lives in memory, and is made again at every start until it is
/// used.
/// </remarks>
public sealed class SetupServiceTests : IDisposable
{
    private const string GoodPassword = "a-password-worth-twelve";

    private readonly TempDatabase _database = new();

    public void Dispose()
    {
        _database.Dispose();
    }

    [Fact]
    public void An_installation_that_has_an_account_shows_no_code()
    {
        _database.Add("root", AccountRole.Administrator);

        (SetupService service, StringWriter output) = Start();

        service.AnnounceIfRequired();

        Assert.False(service.IsRequired);
        Assert.Equal(string.Empty, output.ToString());
    }

    [Fact]
    public void An_installation_with_no_account_shows_a_code_and_it_works_once()
    {
        (SetupService service, StringWriter output) = Start();

        service.AnnounceIfRequired();

        string code = CodeIn(output);

        Assert.True(service.IsRequired);
        Assert.Equal(SetupOutcome.Created, service.Complete(code, "maria", GoodPassword).Outcome);
        Assert.False(service.IsRequired);

        // Once used, nothing remains that could be used again.
        Assert.Equal(SetupOutcome.AlreadyCompleted, service.Complete(code, "luca", GoodPassword).Outcome);
    }

    [Fact]
    public void A_restart_makes_a_new_code_and_the_old_one_no_longer_works()
    {
        (SetupService first, StringWriter firstOutput) = Start();
        first.AnnounceIfRequired();

        string oldCode = CodeIn(firstOutput);

        // The process stopped and started again.
        (SetupService second, StringWriter secondOutput) = Start();
        second.AnnounceIfRequired();

        string newCode = CodeIn(secondOutput);

        Assert.NotEqual(oldCode, newCode);
        Assert.Equal(SetupOutcome.CodeRejected, second.Complete(oldCode, "maria", GoodPassword).Outcome);
        Assert.Equal(SetupOutcome.Created, second.Complete(newCode, "maria", GoodPassword).Outcome);
    }

    [Fact]
    public void Before_any_code_was_shown_nothing_can_match()
    {
        (SetupService service, _) = Start();

        // The service never announced: no code exists, so no guess is right,
        // however the guess is written.
        Assert.Equal(SetupOutcome.CodeRejected, service.Complete("AAAA-AAAA-AAAA", "maria", GoodPassword).Outcome);
        Assert.Equal(SetupOutcome.CodeRejected, service.Complete(null, "maria", GoodPassword).Outcome);
    }

    [Fact]
    public void The_code_is_not_kept_in_the_database()
    {
        (SetupService service, StringWriter output) = Start();

        service.AnnounceIfRequired();

        string code = CodeIn(output);

        service.Complete(code, "maria", GoodPassword);

        // The database may still be open in this process.
        using FileStream stream = new(_database.Connections.DatabasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using StreamReader reader = new(stream, System.Text.Encoding.Latin1);

        string database = reader.ReadToEnd();

        // Positive control: the account just created is in the file, so this is
        // the right place to look.
        Assert.Contains("maria", database, StringComparison.Ordinal);

        Assert.DoesNotContain(code.Replace("-", string.Empty, StringComparison.Ordinal), database, StringComparison.Ordinal);
    }

    private (SetupService Service, StringWriter Output) Start()
    {
        StringWriter output = new();

        SetupService service = new(_database.Accounts, _database.Hasher, TimeProvider.System, new SetupOutput(output));

        return (service, output);
    }

    private static string CodeIn(StringWriter output)
    {
        Match match = Regex.Match(output.ToString(), "[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}");

        Assert.True(match.Success, "a code should have been shown");

        return match.Value;
    }
}
