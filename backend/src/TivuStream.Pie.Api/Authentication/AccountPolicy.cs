using System.Text;

namespace TivuStream.Pie.Api.Authentication;

/// <summary>
/// Why a password was not accepted.
/// </summary>
internal enum PasswordProblem
{
    /// <summary>
    /// Fewer characters than the minimum.
    /// </summary>
    TooShort,

    /// <summary>
    /// More characters than the maximum.
    /// </summary>
    TooLong,

    /// <summary>
    /// The same as the name of the account.
    /// </summary>
    EqualsUsername,
}

/// <summary>
/// What is accepted as a name and as a password.
/// </summary>
/// <remarks>
/// Authentication Specification, Accounts And Roles and Credentials.
/// <para>
/// Length is what counts. No rule of composition is imposed: requiring capitals,
/// digits and symbols produces predictable passwords that are harder to
/// remember, not stronger ones.
/// </para>
/// </remarks>
internal static class AccountPolicy
{
    internal const int MinimumPasswordLength = 12;

    internal const int MaximumPasswordLength = 128;

    private const int MinimumUsernameLength = 3;

    private const int MaximumUsernameLength = 32;

    /// <summary>
    /// Brings a password to the form every comparison uses.
    /// </summary>
    /// <remarks>
    /// The same password written on different keyboards can differ in the
    /// bytes while looking identical. Normalising it first makes them one.
    /// The hash and the length check both start from this form, so that they
    /// cannot disagree about what the password is.
    /// </remarks>
    internal static string NormalizePassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        return password.Normalize(NormalizationForm.FormKC);
    }

    /// <summary>
    /// Checks a password against the rules.
    /// </summary>
    /// <param name="password">Password as it was typed.</param>
    /// <param name="username">Name of the account it is for.</param>
    /// <returns>The reason it is not accepted, or <c>null</c> when it is.</returns>
    internal static PasswordProblem? CheckPassword(string password, string username)
    {
        string normalized = NormalizePassword(password);

        // Characters, not bytes: whoever writes in an accented language must
        // not be penalised for it.
        int length = normalized.EnumerateRunes().Count();

        if (length < MinimumPasswordLength)
        {
            return PasswordProblem.TooShort;
        }

        if (length > MaximumPasswordLength)
        {
            return PasswordProblem.TooLong;
        }

        if (string.Equals(normalized, NormalizePassword(username), StringComparison.OrdinalIgnoreCase))
        {
            return PasswordProblem.EqualsUsername;
        }

        return null;
    }

    /// <summary>
    /// Indicates whether a name is acceptable in its canonical form.
    /// </summary>
    /// <remarks>
    /// Lower case letters, digits, dot, hyphen and underscore. Comparison
    /// ignores case, but a name is given in this form.
    /// </remarks>
    internal static bool IsValidUsername(string username)
    {
        ArgumentNullException.ThrowIfNull(username);

        return username.Length is >= MinimumUsernameLength and <= MaximumUsernameLength
            && username.All(IsAllowedInUsername);
    }

    private static bool IsAllowedInUsername(char character)
    {
        return character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '.' or '-' or '_';
    }
}
