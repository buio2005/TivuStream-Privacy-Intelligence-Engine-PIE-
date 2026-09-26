using TivuStream.Pie.Model;

namespace TivuStream.Pie.Api.Contracts;

/// <summary>
/// The window over which everything describing the network is read.
/// </summary>
/// <remarks>
/// Domains, the detail of a domain, devices and statistics cover the same
/// window. Were they to differ, a domain the list shows as observed ten hours
/// earlier would be denied by its own detail, and two pages would contradict
/// each other without saying so.
/// <para>
/// A whole day rather than the current hour: a fixed hourly bucket empties at
/// every turn of the clock, which is the opposite of what someone asking what
/// their network is doing wants to see.
/// </para>
/// </remarks>
internal static class ObservationWindow
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
