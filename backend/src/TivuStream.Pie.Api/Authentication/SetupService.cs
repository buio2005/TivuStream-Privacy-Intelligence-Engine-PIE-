using System.Security.Cryptography;
using System.Text;
using TivuStream.Pie.Storage;

namespace TivuStream.Pie.Api.Authentication;

/// <summary>
/// Where the setup code is shown.
/// </summary>
/// <remarks>
/// A type of its own, and not a bare writer, so that it can be replaced by
/// something else in a test. In the running host it is the standard output of
/// the process, written to directly: never through the logging system, which
/// feeds files and collectors that must not receive a secret.
/// </remarks>
/// <param name="Writer">Where the code is written.</param>
internal sealed record SetupOutput(TextWriter Writer);

/// <summary>
/// Result of an attempt to create the first administrator.
/// </summary>
internal enum SetupOutcome
{
    Created,
    CodeRejected,
    UsernameRejected,
    PasswordRejected,
    AlreadyCompleted,
}

/// <summary>
/// What an attempt to create the first administrator came to.
/// </summary>
/// <param name="Outcome">How it ended.</param>
/// <param name="Account">The administrator, when one was created.</param>
/// <param name="PasswordProblem">Why the password was not accepted, when that was the reason.</param>
internal sealed record SetupResult(SetupOutcome Outcome, StoredAccount? Account = null, PasswordProblem? PasswordProblem = null);

/// <summary>
/// Lets whoever has access to the machine create the first administrator.
/// </summary>
/// <remarks>
/// Authentication Specification, First Run. A new installation has no
/// account, and whoever reaches the address first must not become its
/// administrator merely by arriving before the owner. So a random code is
/// shown on the standard output of the process, and only someone who can read
/// that output can use it: the proof is access to the machine.
/// <para>
/// The code lives in memory as a hash, exists only while no account does, and
/// is made again at every start until it is used.
/// </para>
/// </remarks>
internal sealed class SetupService
{
    // Without the characters that are mistaken for one another: I and 1, O
    // and 0. Thirty two symbols, so that each carries exactly five bits.
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private const int CodeLength = 12;

    private const int GroupLength = 4;

    private readonly AccountRepository _accounts;
    private readonly PasswordHasher _hasher;
    private readonly TimeProvider _time;
    private readonly SetupOutput _output;
    private readonly object _gate = new();

    private byte[]? _codeHash;

    // An installation with an account never goes back to having none, so once
    // seen the answer is kept and the database is not asked again.
    private volatile bool _initialized;

    public SetupService(AccountRepository accounts, PasswordHasher hasher, TimeProvider time, SetupOutput output)
    {
        _accounts = accounts;
        _hasher = hasher;
        _time = time;
        _output = output;
    }

    /// <summary>
    /// Indicates whether the installation still has no account.
    /// </summary>
    internal bool IsRequired
    {
        get
        {
            if (_initialized)
            {
                return false;
            }

            if (_accounts.HasAny())
            {
                _initialized = true;

                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Makes a code and shows it, when the installation has no account.
    /// </summary>
    internal void AnnounceIfRequired()
    {
        if (!IsRequired)
        {
            return;
        }

        StringBuilder code = new(CodeLength);

        for (int index = 0; index < CodeLength; index++)
        {
            code.Append(Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]);
        }

        string plain = code.ToString();

        lock (_gate)
        {
            _codeHash = HashCode(plain);
        }

        string shown = string.Join('-', Enumerable.Range(0, CodeLength / GroupLength).Select(group => plain.Substring(group * GroupLength, GroupLength)));

        _output.Writer.WriteLine();
        _output.Writer.WriteLine("No account exists yet.");
        _output.Writer.WriteLine("To create the first administrator, open the interface and enter this setup code:");
        _output.Writer.WriteLine();
        _output.Writer.WriteLine($"    {shown}");
        _output.Writer.WriteLine();
        _output.Writer.WriteLine("The code is valid until it is used or this process stops.");
        _output.Writer.WriteLine();
        _output.Writer.Flush();
    }

    /// <summary>
    /// Creates the first administrator, when the code is right.
    /// </summary>
    /// <remarks>
    /// The code is checked first. A name or a password that is not acceptable
    /// is refused without using the code up: whoever mistypes must not have to
    /// restart the service to be given another.
    /// </remarks>
    internal SetupResult Complete(string? code, string? username, string? password)
    {
        if (!IsRequired)
        {
            return new SetupResult(SetupOutcome.AlreadyCompleted);
        }

        if (!CodeMatches(code))
        {
            return new SetupResult(SetupOutcome.CodeRejected);
        }

        string name = (username ?? string.Empty).Trim().ToLowerInvariant();

        if (!AccountPolicy.IsValidUsername(name))
        {
            return new SetupResult(SetupOutcome.UsernameRejected);
        }

        PasswordProblem? problem = AccountPolicy.CheckPassword(password ?? string.Empty, name);

        if (problem is not null)
        {
            return new SetupResult(SetupOutcome.PasswordRejected, PasswordProblem: problem);
        }

        // Creation and the check that no account exists are one statement. If
        // another request got there first, this one finds it already done.
        StoredAccount? account = _accounts.CreateFirst(name, _hasher.Hash(password!), _time.GetUtcNow());

        if (account is null)
        {
            return new SetupResult(SetupOutcome.AlreadyCompleted);
        }

        lock (_gate)
        {
            _codeHash = null;
        }

        _initialized = true;

        return new SetupResult(SetupOutcome.Created, account);
    }

    private bool CodeMatches(string? presented)
    {
        byte[]? expected;

        lock (_gate)
        {
            expected = _codeHash;
        }

        // No code was ever shown: nothing can match.
        if (expected is null || string.IsNullOrEmpty(presented))
        {
            return false;
        }

        // Accepted in any case, and with or without the separators.
        string normalized = new string(presented.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

        return CryptographicOperations.FixedTimeEquals(HashCode(normalized), expected);
    }

    private static byte[] HashCode(string code)
    {
        return SHA256.HashData(Encoding.UTF8.GetBytes(code));
    }
}
