namespace TivuStream.Pie.Storage;

/// <summary>
/// An account as it is kept.
/// </summary>
/// <remarks>
/// Only a hash of the password is held, never the password.
/// </remarks>
public sealed record StoredAccount
{
    /// <summary>
    /// Identifier assigned when the account was recorded.
    /// </summary>
    public required long Id { get; init; }

    /// <summary>
    /// Name in its canonical form, lower case.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// What the account is allowed to read and do.
    /// </summary>
    public required AccountRole Role { get; init; }

    /// <summary>
    /// Indicates whether the account can sign in.
    /// </summary>
    public required bool Enabled { get; init; }

    /// <summary>
    /// Hash of the password, with the algorithm and parameters it was made
    /// with.
    /// </summary>
    public required string PasswordHash { get; init; }

    /// <summary>
    /// Indicates that the password must be changed before anything else.
    /// </summary>
    public required bool PasswordChangeRequired { get; init; }

    /// <summary>
    /// Instant the account was recorded.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
