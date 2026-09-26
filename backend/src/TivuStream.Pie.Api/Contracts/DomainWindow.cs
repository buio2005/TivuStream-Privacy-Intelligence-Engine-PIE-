using TivuStream.Pie.Model;

namespace TivuStream.Pie.Api.Contracts;

/// <summary>
/// The window over which domains are read.
/// </summary>
/// <remarks>
/// The list of domains and the detail of a domain cover the same window. Were
/// they to differ, a domain the list shows as observed ten hours earlier would
/// be denied by its own detail.
/// <para>
/// A whole day rather than the current hour: a fixed hourly bucket empties at
/// every turn of the clock, which is the opposite of what someone asking what
/// their network is doing wants to see.
/// </para>
/// </remarks>
internal static class DomainWindow
{
    /// <summary>
    /// Hourly periods the window asks for, the current one included.
    /// </summary>
    public const int RequestedHours = 24;

    /// <summary>
    /// Beginning of the window that ends with the period containing
    /// <paramref name="now"/>.
    /// </summary>
    public static DateTimeOffset StartFor(DateTimeOffset now)
    {
        return ObservationPeriod.Containing(now).Start.AddHours(-(RequestedHours - 1));
    }
}
