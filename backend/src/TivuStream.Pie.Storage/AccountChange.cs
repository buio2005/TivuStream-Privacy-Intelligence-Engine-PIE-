namespace TivuStream.Pie.Storage;

/// <summary>
/// What is to change in an account. A property left <c>null</c> stays as it is.
/// </summary>
public sealed record AccountChange
{
    /// <summary>
    /// New role.
    /// </summary>
    public AccountRole? Role { get; init; }

    /// <summary>
    /// Whether the account may sign in.
    /// </summary>
    public bool? Enabled { get; init; }

    /// <summary>
    /// New hash of the password, never the password.
    /// </summary>
    public string? PasswordHash { get; init; }

    /// <summary>
    /// Whether the password must be changed before anything else.
    /// </summary>
    public bool? PasswordChangeRequired { get; init; }
}

/// <summary>
/// How a change to an account, or its removal, ended.
/// </summary>
public enum AccountChangeOutcome
{
    /// <summary>
    /// Carried out.
    /// </summary>
    Changed,

    /// <summary>
    /// No account has that name. Nothing was done.
    /// </summary>
    NotFound,

    /// <summary>
    /// The installation would be left without an administrator able to sign
    /// in. Nothing was done.
    /// </summary>
    LastAdministrator,
}
