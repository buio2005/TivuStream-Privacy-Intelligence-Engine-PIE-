using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TivuStream.Pie.Api.Authentication;

/// <summary>
/// Outcome of checking a password against what is stored.
/// </summary>
internal enum PasswordVerification
{
    /// <summary>
    /// The password does not match, or what is stored cannot be read.
    /// </summary>
    Failed,

    /// <summary>
    /// The password matches.
    /// </summary>
    Valid,

    /// <summary>
    /// The password matches, but was hashed with lighter parameters than the
    /// ones now in force.
    /// </summary>
    ValidButOutdated,
}

/// <summary>
/// Makes and checks the hash of a password.
/// </summary>
/// <remarks>
/// Authentication Specification, Credentials: PBKDF2 with HMAC-SHA-512, a
/// random salt per password, and the algorithm and parameters recorded with
/// the hash so that they can grow.
/// <para>
/// This is the algorithm the base library of .NET offers without any added
/// dependency. Argon2id resists better an adversary with dedicated hardware,
/// and would need a third party library; the choice is recorded in the
/// specification as decision D3.
/// </para>
/// </remarks>
internal sealed class PasswordHasher
{
    /// <summary>
    /// Iterations in force.
    /// </summary>
    internal const int CurrentIterations = 210_000;

    private const string Algorithm = "pbkdf2-sha512";

    private const int SaltSize = 16;

    private const int HashSize = 64;

    // A record can only be altered by someone who already has the database,
    // but a tampered value must not turn a login into a computation that
    // never ends.
    private const int MaximumAcceptedIterations = 10_000_000;

    private readonly int _iterations;

    /// <summary>
    /// Creates a hasher with the given number of iterations.
    /// </summary>
    /// <remarks>
    /// Other than in tests, which need a hash made "in the past" without
    /// waiting for it, use <see cref="Standard"/>.
    /// </remarks>
    /// <param name="iterations">Iterations of the derivation.</param>
    internal PasswordHasher(int iterations)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        _iterations = iterations;
    }

    /// <summary>
    /// The hasher with the parameters in force.
    /// </summary>
    internal static PasswordHasher Standard { get; } = new(CurrentIterations);

    /// <summary>
    /// Makes the hash of a password.
    /// </summary>
    /// <param name="password">Password as it was typed. It must already have been checked by the policy.</param>
    /// <returns>Algorithm, iterations, salt and hash, in a single value.</returns>
    internal string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Derive(password, salt, _iterations, HashSize);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Algorithm}${_iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}");
    }

    /// <summary>
    /// Checks a password against a stored hash.
    /// </summary>
    /// <remarks>
    /// Fails closed. A stored value that cannot be read is a failure, never
    /// an absence of requirements.
    /// <para>
    /// The comparison takes the same time whatever the first differing byte
    /// is.
    /// </para>
    /// </remarks>
    /// <param name="password">Password as it was typed.</param>
    /// <param name="stored">Value produced by <see cref="Hash"/>.</param>
    internal PasswordVerification Verify(string password, string stored)
    {
        if (!TryRead(stored, out int iterations, out byte[] salt, out byte[] expected))
        {
            return PasswordVerification.Failed;
        }

        byte[] actual = Derive(password, salt, iterations, expected.Length);

        if (!CryptographicOperations.FixedTimeEquals(actual, expected))
        {
            return PasswordVerification.Failed;
        }

        return iterations < _iterations
            ? PasswordVerification.ValidButOutdated
            : PasswordVerification.Valid;
    }

    private static byte[] Derive(string password, byte[] salt, int iterations, int length)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(AccountPolicy.NormalizePassword(password));

        try
        {
            return Rfc2898DeriveBytes.Pbkdf2(bytes, salt, iterations, HashAlgorithmName.SHA512, length);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    private static bool TryRead(string stored, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = [];
        hash = [];

        string[] parts = stored.Split('$');

        return parts.Length == 4
            && parts[0] == Algorithm
            && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations)
            && iterations is >= 1 and <= MaximumAcceptedIterations
            && TryDecode(parts[2], out salt)
            && TryDecode(parts[3], out hash);
    }

    private static bool TryDecode(string text, out byte[] bytes)
    {
        byte[] buffer = new byte[text.Length];

        if (text.Length > 0 && Convert.TryFromBase64String(text, buffer, out int written) && written > 0)
        {
            bytes = buffer[..written];

            return true;
        }

        bytes = [];

        return false;
    }
}
