using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using TivuStream.Pie.Storage;

namespace TivuStream.Pie.Api.Authentication;

/// <summary>
/// A valid session, as the rest of the request needs to know it.
/// </summary>
/// <param name="SessionId">Identifier of the session in the database.</param>
/// <param name="Account">The account, as it is now.</param>
/// <param name="ExpiresAt">The earlier of the two instants at which the session would end.</param>
internal sealed record SessionContext(long SessionId, StoredAccount Account, DateTimeOffset ExpiresAt);

/// <summary>
/// Opens, recognises and closes sessions.
/// </summary>
/// <remarks>
/// Authentication Specification, Sessions. The identifier is 32 random bytes;
/// only its SHA-256 hash is kept, so that whoever reads the database cannot
/// use a session that is still open. A session ends after eight hours without
/// a request, and in any case after fourteen days.
/// <para>
/// The role is not copied into the session. It is read from the account at
/// every request, so that a demotion takes effect at once.
/// </para>
/// </remarks>
internal sealed class SessionService
{
    internal static readonly TimeSpan IdleTimeout = TimeSpan.FromHours(8);

    internal static readonly TimeSpan AbsoluteLifetime = TimeSpan.FromDays(14);

    private const int TokenSize = 32;

    // Renewing on every request would write to the database on every request.
    // Once a minute is far finer than the eight hours it renews.
    private static readonly TimeSpan RenewalInterval = TimeSpan.FromMinutes(1);

    private readonly SessionRepository _sessions;
    private readonly TimeProvider _time;

    public SessionService(SessionRepository sessions, TimeProvider time)
    {
        _sessions = sessions;
        _time = time;
    }

    /// <summary>
    /// Opens a session for an account.
    /// </summary>
    /// <returns>The identifier to give to the browser, and the instant the session would end.</returns>
    internal (string Token, DateTimeOffset ExpiresAt) Start(StoredAccount account)
    {
        DateTimeOffset now = _time.GetUtcNow();

        // Expired sessions are removed here, where a new one is written
        // anyway, instead of by a task of their own.
        _sessions.DeleteExpired(now, IdleTimeout);

        string token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenSize));

        _sessions.Create(HashToken(token), account.Id, now, now + AbsoluteLifetime);

        return (token, now + IdleTimeout);
    }

    /// <summary>
    /// Recognises a session from the identifier the browser presented.
    /// </summary>
    /// <returns>The session, or <c>null</c> when it does not exist, has ended, or belongs to an account that can no longer sign in.</returns>
    internal SessionContext? Validate(string token)
    {
        StoredSession? stored = _sessions.Find(HashToken(token));

        if (stored is null)
        {
            return null;
        }

        DateTimeOffset now = _time.GetUtcNow();

        bool ended = now >= stored.ExpiresAt || now - stored.LastSeenAt >= IdleTimeout;

        // A session that has ended, or whose account was disabled, is removed
        // rather than left to be refused again and again.
        if (ended || !stored.Account.Enabled)
        {
            _sessions.Delete(stored.Id);

            return null;
        }

        DateTimeOffset lastSeen = stored.LastSeenAt;

        if (now - lastSeen >= RenewalInterval)
        {
            _sessions.Touch(stored.Id, now);
            lastSeen = now;
        }

        DateTimeOffset idleEnd = lastSeen + IdleTimeout;

        return new SessionContext(
            stored.Id,
            stored.Account,
            idleEnd < stored.ExpiresAt ? idleEnd : stored.ExpiresAt);
    }

    /// <summary>
    /// Closes a session.
    /// </summary>
    internal void End(long sessionId)
    {
        _sessions.Delete(sessionId);
    }

    /// <summary>
    /// Closes the session an identifier stands for, if it stands for one.
    /// </summary>
    internal void EndByToken(string token)
    {
        StoredSession? stored = _sessions.Find(HashToken(token));

        if (stored is not null)
        {
            _sessions.Delete(stored.Id);
        }
    }

    /// <summary>
    /// Closes the sessions of an account, all or all but one.
    /// </summary>
    /// <remarks>
    /// For when the password changes: every other place the account was
    /// signed in must stop working, and the one where the change was made
    /// goes on.
    /// </remarks>
    internal int EndAllFor(long accountId, long? exceptSessionId)
    {
        return _sessions.DeleteForAccount(accountId, exceptSessionId);
    }

    /// <summary>
    /// Hash of an identifier, as it is kept.
    /// </summary>
    internal static string HashToken(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
