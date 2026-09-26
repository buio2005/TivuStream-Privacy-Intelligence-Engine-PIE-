namespace TivuStream.Pie.Storage;

/// <summary>
/// The days and months of the person reading, as intervals of observation.
/// </summary>
/// <remarks>
/// Days and months follow the time zone of the computer PIE runs on: a day
/// that begins at two in the morning is nobody's day. The boundaries are
/// returned in UTC and on the hour, so that every hourly period falls in
/// exactly one day. Where the offset is not a whole number of hours, the day
/// begins with the hour its midnight falls in.
/// <para>
/// A day of a change of clock lasts twenty-three or twenty-five hours. The
/// interval says so, since it has a start and an end, not a length.
/// </para>
/// </remarks>
internal static class LocalCalendar
{
    /// <summary>
    /// The local day an instant falls in.
    /// </summary>
    internal static (DateTimeOffset Start, DateTimeOffset End) DayContaining(DateTimeOffset instant, TimeZoneInfo zone)
    {
        DateOnly date = DateOf(instant, zone);

        return (StartOf(date, zone), StartOf(date.AddDays(1), zone));
    }

    /// <summary>
    /// The local month an instant falls in.
    /// </summary>
    internal static (DateTimeOffset Start, DateTimeOffset End) MonthContaining(DateTimeOffset instant, TimeZoneInfo zone)
    {
        DateOnly date = DateOf(instant, zone);

        DateOnly first = new(date.Year, date.Month, 1);

        return (StartOf(first, zone), StartOf(first.AddMonths(1), zone));
    }

    private static DateOnly DateOf(DateTimeOffset instant, TimeZoneInfo zone)
    {
        DateOnly date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, zone).DateTime);

        // Rounding the boundaries to the hour can move an instant close to
        // midnight into the neighbouring day.
        if (instant < StartOf(date, zone))
        {
            return date.AddDays(-1);
        }

        return instant >= StartOf(date.AddDays(1), zone) ? date.AddDays(1) : date;
    }

    private static DateTimeOffset StartOf(DateOnly date, TimeZoneInfo zone)
    {
        DateTime local = date.ToDateTime(TimeOnly.MinValue);

        // Where the clock skips midnight, the day begins at the first local
        // time that exists.
        while (zone.IsInvalidTime(local))
        {
            local = local.AddMinutes(15);
        }

        // Where midnight happens twice, the day begins at the first.
        TimeSpan offset = zone.IsAmbiguousTime(local)
            ? zone.GetAmbiguousTimeOffsets(local).Max()
            : zone.GetUtcOffset(local);

        DateTimeOffset utc = new DateTimeOffset(local, offset).ToUniversalTime();

        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero);
    }
}
