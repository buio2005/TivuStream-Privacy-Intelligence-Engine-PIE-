namespace TivuStream.Pie.Storage;

/// <summary>
/// A session as it is kept, together with the account it belongs to.
/// </summary>
/// <remarks>
/// The account is read in the same query. Deciding what a session may do
/// needs the role and whether the account is still enabled, and both must be
/// the current ones, not those of the moment the session began.
/// </remarks>
public sealed record StoredSession
{
    /// <summary>
    /// Identifier assigned when the session was recorded.
    /// </summary>
    public required long Id { get; init; }

    /// <summary>
    /// Account the session belongs to, as it is now.
    /// </summary>
    public required StoredAccount Account { get; init; }

    /// <summary>
    /// Instant the session began.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Instant of the last request that renewed the session.
    /// </summary>
    public required DateTimeOffset LastSeenAt { get; init; }

    /// <summary>
    /// Instant the session ends whatever its activity.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }
}
