using System.Security.Cryptography;
using TivuStream.Pie.Storage;

namespace TivuStream.Pie.Api.Authentication;

/// <summary>
/// Decides whether a name and a password belong to an account that may sign in.
/// </summary>
/// <remarks>
/// Authentication Specification, Principles: a refusal teaches nothing to
/// whoever receives it. A name that does not exist, a password that is wrong
/// and an account that is disabled must be indistinguishable, and that
/// includes the time the answer takes.
/// <para>
/// So the same computation is done in every case. For a name that does not
/// exist the password is checked against a hash made for the purpose; for a
/// disabled account it is checked against its own hash before the account is
/// refused.
/// </para>
/// </remarks>
internal sealed class CredentialVerifier
{
    private readonly AccountRepository _accounts;
    private readonly PasswordHasher _hasher;

    // Made once, from a value nobody knows, with the parameters in force. Its
    // only use is to cost the same as a real check.
    private readonly string _absentAccountHash;

    public CredentialVerifier(AccountRepository accounts, PasswordHasher hasher)
    {
        _accounts = accounts;
        _hasher = hasher;

        _absentAccountHash = hasher.Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }

    /// <summary>
    /// Checks a name and a password.
    /// </summary>
    /// <param name="username">Name as it was typed, in any case.</param>
    /// <param name="password">Password as it was typed.</param>
    /// <returns>The account when the credentials are right and it may sign in; otherwise <c>null</c>, whatever the reason.</returns>
    internal StoredAccount? Verify(string? username, string? password)
    {
        StoredAccount? account = string.IsNullOrEmpty(username)
            ? null
            : _accounts.FindByUsername(username.Trim());

        PasswordVerification result = _hasher.Verify(
            password ?? string.Empty,
            account?.PasswordHash ?? _absentAccountHash);

        if (account is null || !account.Enabled || result == PasswordVerification.Failed)
        {
            return null;
        }

        // The password is at hand now and at no other time, so this is when a
        // hash made with lighter parameters than the current ones is made again.
        if (result == PasswordVerification.ValidButOutdated)
        {
            _accounts.ReplacePasswordHash(
                account.Id,
                _hasher.Hash(password!),
                account.PasswordChangeRequired);
        }

        return account;
    }
}
