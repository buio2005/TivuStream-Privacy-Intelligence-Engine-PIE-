using System.Text;
using TivuStream.Pie.Storage;

namespace TivuStream.Pie.Api.Authentication;

/// <summary>
/// Asks the person at the terminal for a password.
/// </summary>
internal interface IPasswordPrompt
{
    /// <summary>
    /// Asks for a password.
    /// </summary>
    /// <param name="label">What is being asked for.</param>
    /// <returns>The password, or <c>null</c> when there is no one to answer.</returns>
    string? Read(string label);
}

/// <summary>
/// Asks on the console, without showing what is typed.
/// </summary>
/// <remarks>
/// When the input is redirected, as in a script or a container run without a
/// terminal, the line is read as it comes.
/// </remarks>
internal sealed class ConsolePasswordPrompt : IPasswordPrompt
{
    public string? Read(string label)
    {
        Console.Out.Write($"{label}: ");
        Console.Out.Flush();

        if (Console.IsInputRedirected)
        {
            return Console.In.ReadLine();
        }

        StringBuilder typed = new();

        while (true)
        {
            ConsoleKeyInfo key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (typed.Length > 0)
                {
                    typed.Length--;
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                typed.Append(key.KeyChar);
            }
        }

        Console.Out.WriteLine();

        return typed.ToString();
    }
}

/// <summary>
/// Restores access to an account, from the machine that hosts the system.
/// </summary>
/// <remarks>
/// Authentication Specification, Recovery. There is no "I forgot my password"
/// over the network and no message sent by post: those are external services,
/// and a channel of recovery is a second way in. This is run at the terminal
/// by someone who already has the machine, and grants nothing that access to
/// the machine does not already grant.
/// <para>
/// With an administrator able to sign in, it resets the password of any account
/// that exists, and refuses an unknown name so that a typing error creates
/// nothing. With none, it reactivates a disabled administrator or creates one,
/// and refuses to promote anyone.
/// </para>
/// </remarks>
internal sealed class RecoveryCommand
{
    /// <summary>
    /// The password was set.
    /// </summary>
    internal const int Done = 0;

    /// <summary>
    /// The request could not be carried out.
    /// </summary>
    internal const int Refused = 1;

    /// <summary>
    /// The name or the password given was not acceptable.
    /// </summary>
    internal const int NotAcceptable = 2;

    private readonly AccountRepository _accounts;
    private readonly PasswordHasher _hasher;
    private readonly SessionService _sessions;
    private readonly TimeProvider _time;

    public RecoveryCommand(AccountRepository accounts, PasswordHasher hasher, SessionService sessions, TimeProvider time)
    {
        _accounts = accounts;
        _hasher = hasher;
        _sessions = sessions;
        _time = time;
    }

    /// <summary>
    /// Sets a new password for an account, or creates an administrator.
    /// </summary>
    /// <param name="username">Name of the account, in any case.</param>
    /// <param name="prompt">Where the password is asked for.</param>
    /// <param name="output">Where the outcome is told.</param>
    /// <returns>An exit code: <see cref="Done"/>, <see cref="Refused"/> or <see cref="NotAcceptable"/>.</returns>
    internal int Run(string username, IPasswordPrompt prompt, TextWriter output)
    {
        string name = AccountPolicy.CanonicalUsername(username);

        if (!AccountPolicy.IsValidUsername(name))
        {
            output.WriteLine("The name is not acceptable: 3 to 32 characters, lower case letters, digits, dot, hyphen and underscore.");

            return NotAcceptable;
        }

        StoredAccount? account = _accounts.FindByUsername(name);
        bool administratorCanSignIn = _accounts.HasEnabledAdministrator();

        string? refusal = Refusal(account, administratorCanSignIn);

        if (refusal is not null)
        {
            output.WriteLine(refusal);

            return Refused;
        }

        string? password = prompt.Read("New password");
        string? again = password is null ? null : prompt.Read("Repeat the password");

        if (password is null || again is null)
        {
            output.WriteLine("No password was given. Nothing was changed.");

            return Refused;
        }

        if (!string.Equals(password, again, StringComparison.Ordinal))
        {
            output.WriteLine("The two passwords differ. Nothing was changed.");

            return Refused;
        }

        PasswordProblem? problem = AccountPolicy.CheckPassword(password, name);

        if (problem is not null)
        {
            output.WriteLine($"The password is not acceptable ({problem}). Nothing was changed.");

            return NotAcceptable;
        }

        string hash = _hasher.Hash(password);

        if (account is null)
        {
            _accounts.Create(name, AccountRole.Administrator, hash, passwordChangeRequired: true, _time.GetUtcNow());

            output.WriteLine($"Administrator '{name}' created. The password must be changed at the first sign in.");

            return Done;
        }

        // The person who had the password may no longer be the one who has
        // the session: every place the account was signed in stops working.
        _accounts.ReplacePasswordHash(account.Id, hash, passwordChangeRequired: true);
        _sessions.EndAllFor(account.Id, exceptSessionId: null);

        if (!account.Enabled && !administratorCanSignIn)
        {
            _accounts.SetEnabled(account.Id, enabled: true);

            output.WriteLine($"Administrator '{name}' was disabled and has been enabled again.");
        }

        output.WriteLine($"The password of '{name}' was reset. It must be changed at the next sign in, and every open session of the account has ended.");

        return Done;
    }

    private static string? Refusal(StoredAccount? account, bool administratorCanSignIn)
    {
        if (administratorCanSignIn)
        {
            return account is null
                ? "No account has that name. Nothing was created: an administrator can already sign in, and a typing error must not add an account."
                : null;
        }

        // Nobody can sign in as an administrator. This restores one; it does
        // not choose one.
        return account is { Role: not AccountRole.Administrator }
            ? "No administrator can sign in, and that account is not one. Name the administrator to restore, or a new name to create one."
            : null;
    }
}
