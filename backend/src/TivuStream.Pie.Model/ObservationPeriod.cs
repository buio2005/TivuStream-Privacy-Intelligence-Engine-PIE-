namespace TivuStream.Pie.Model;

/// <summary>
/// Fixed interval an acquisition observes.
/// </summary>
/// <remarks>
/// An acquisition is not a set of events but the observation of an interval.
/// Two observations of overlapping intervals describe part of the same
/// traffic and are not additive.
/// <para>
/// Acquisitions are therefore aligned to fixed periods. A further observation
/// of the same period replaces the previous one; a period that has elapsed is
/// never revised; history is the sequence of elapsed periods, which do not
/// overlap and can be aggregated.
/// </para>
/// <para>
/// Defined by the Persistence Specification.
/// </para>
/// </remarks>
/// <param name="Start">Beginning of the period, inclusive.</param>
/// <param name="End">End of the period, exclusive.</param>
public readonly record struct ObservationPeriod(DateTimeOffset Start, DateTimeOffset End)
{
    /// <summary>
    /// Returns the period the given instant falls into.
    /// </summary>
    /// <remarks>
    /// The instant is taken in UTC and truncated to the beginning of its
    /// hour, so that the same period is obtained regardless of when within it
    /// the acquisition happens.
    /// </remarks>
    /// <param name="instant">Instant to place.</param>
    public static ObservationPeriod Containing(DateTimeOffset instant)
    {
        DateTimeOffset utc = instant.ToUniversalTime();

        DateTimeOffset start = new(
            utc.Year,
            utc.Month,
            utc.Day,
            utc.Hour,
            minute: 0,
            second: 0,
            TimeSpan.Zero);

        return new ObservationPeriod(start, start.AddHours(1));
    }

    /// <summary>
    /// Indicates whether the period has entirely elapsed at the given instant.
    /// </summary>
    /// <remarks>
    /// An elapsed period is immutable. The period in progress is the only one
    /// a further acquisition may still change.
    /// </remarks>
    /// <param name="instant">Instant to compare the period against.</param>
    public bool HasElapsedAt(DateTimeOffset instant)
    {
        return instant >= End;
    }
}
