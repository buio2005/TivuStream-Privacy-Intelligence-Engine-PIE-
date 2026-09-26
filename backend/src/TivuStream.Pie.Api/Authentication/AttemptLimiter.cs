using System.Net;
using System.Net.Sockets;

namespace TivuStream.Pie.Api.Authentication;

/// <summary>
/// Slows down whoever tries secrets one after the other.
/// </summary>
/// <remarks>
/// Authentication Specification, Attempts. A counter per source address and
/// one per account name. Past its threshold, a counter blocks for a delay that
/// starts at thirty seconds and doubles at every further failure, up to
/// fifteen minutes. There is no permanent block: it would let anyone lock the
/// owner out.
/// <para>
/// The counters live in memory and are forgotten fifteen minutes after their
/// delay has ended without a new failure. Counting from the last failure
/// instead would make the longest delay and the forgetting end together, and
/// whoever insists would find the counter empty just as they came back.
/// </para>
/// <para>
/// Source addresses serve the counters and are recorded nowhere else.
/// </para>
/// </remarks>
internal sealed class AttemptLimiter
{
    internal const int SourceThreshold = 5;

    internal const int AccountThreshold = 10;

    internal static readonly TimeSpan FirstDelay = TimeSpan.FromSeconds(30);

    internal static readonly TimeSpan MaximumDelay = TimeSpan.FromMinutes(15);

    internal static readonly TimeSpan ForgetAfter = TimeSpan.FromMinutes(15);

    // A home network gives a single device a /64. Counting each IPv6 address
    // on its own would hand anyone billions of counters.
    private const int Ipv6PrefixBytes = 8;

    private readonly Dictionary<string, Counter> _sources = [];
    private readonly Dictionary<string, Counter> _accounts = [];
    private readonly Lock _gate = new();
    private readonly TimeProvider _time;
    private readonly ILogger<AttemptLimiter> _logger;

    public AttemptLimiter(TimeProvider time, ILogger<AttemptLimiter> logger)
    {
        _time = time;
        _logger = logger;
    }

    /// <summary>
    /// How long a request must wait before its credentials can be checked.
    /// </summary>
    /// <param name="source">Remote address of the connection.</param>
    /// <param name="account">Name tried, when the request carries one.</param>
    /// <returns>The time left, or <c>null</c> when the request may be checked now.</returns>
    internal TimeSpan? RetryAfter(IPAddress? source, string? account)
    {
        DateTimeOffset now = _time.GetUtcNow();

        lock (_gate)
        {
            DateTimeOffset blockedUntil = Max(
                BlockedUntil(_sources, SourceKey(source), now),
                account is null ? DateTimeOffset.MinValue : BlockedUntil(_accounts, account, now));

            return blockedUntil > now ? blockedUntil - now : null;
        }
    }

    /// <summary>
    /// Records a secret that was tried and was wrong.
    /// </summary>
    /// <param name="source">Remote address of the connection.</param>
    /// <param name="account">Name tried, when the request carries one.</param>
    internal void RecordFailure(IPAddress? source, string? account)
    {
        DateTimeOffset now = _time.GetUtcNow();

        lock (_gate)
        {
            Forget(_sources, now);
            Forget(_accounts, now);

            if (Fail(_sources, SourceKey(source), SourceThreshold, now) is TimeSpan sourceDelay)
            {
                AuthenticationLog.LimitReached(_logger, "source address", (int)sourceDelay.TotalSeconds);
            }

            if (account is not null && Fail(_accounts, account, AccountThreshold, now) is TimeSpan accountDelay)
            {
                AuthenticationLog.LimitReached(_logger, "account name", (int)accountDelay.TotalSeconds);
            }
        }
    }

    /// <summary>
    /// Records that the secret of an account was proven: its counter starts over.
    /// </summary>
    /// <remarks>
    /// Only the account's. The source's is left, or anyone holding one
    /// account could clear the way for guessing the others.
    /// </remarks>
    internal void RecordSuccess(string account)
    {
        lock (_gate)
        {
            _accounts.Remove(account);
        }
    }

    /// <summary>
    /// The key a source address is counted under.
    /// </summary>
    internal static string SourceKey(IPAddress? address)
    {
        // Every connection whose address is unknown shares one counter: to
        // give each its own would be to give each none.
        if (address is null)
        {
            return "unknown";
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return address.ToString();
        }

        byte[] bytes = address.GetAddressBytes();

        Array.Clear(bytes, Ipv6PrefixBytes, bytes.Length - Ipv6PrefixBytes);

        return new IPAddress(bytes) + "/64";
    }

    // Returns the delay the failure started, when it started one.
    private static TimeSpan? Fail(Dictionary<string, Counter> counters, string key, int threshold, DateTimeOffset now)
    {
        Counter counter = counters.TryGetValue(key, out Counter? existing) ? existing : counters[key] = new Counter();

        counter.Failures++;
        counter.LastFailure = now;

        if (counter.Failures >= threshold)
        {
            // Thirty seconds at the threshold, doubling at every failure past
            // it. The exponent is bounded before shifting: past the ceiling,
            // doubling further would only overflow.
            int doublings = Math.Min(counter.Failures - threshold, 10);
            TimeSpan delay = FirstDelay * (1L << doublings);

            counter.BlockedUntil = now + (delay < MaximumDelay ? delay : MaximumDelay);

            return counter.BlockedUntil - now;
        }

        return null;
    }

    private static DateTimeOffset BlockedUntil(Dictionary<string, Counter> counters, string key, DateTimeOffset now)
    {
        return counters.TryGetValue(key, out Counter? counter) && !counter.IsForgotten(now)
            ? counter.BlockedUntil
            : DateTimeOffset.MinValue;
    }

    private static void Forget(Dictionary<string, Counter> counters, DateTimeOffset now)
    {
        foreach (string key in counters.Where(entry => entry.Value.IsForgotten(now)).Select(entry => entry.Key).ToList())
        {
            counters.Remove(key);
        }
    }

    private static DateTimeOffset Max(DateTimeOffset first, DateTimeOffset second)
    {
        return first > second ? first : second;
    }

    private sealed class Counter
    {
        internal int Failures { get; set; }

        internal DateTimeOffset LastFailure { get; set; }

        internal DateTimeOffset BlockedUntil { get; set; } = DateTimeOffset.MinValue;

        internal bool IsForgotten(DateTimeOffset now)
        {
            DateTimeOffset quietSince = BlockedUntil > LastFailure ? BlockedUntil : LastFailure;

            return now - quietSince >= ForgetAfter;
        }
    }
}
