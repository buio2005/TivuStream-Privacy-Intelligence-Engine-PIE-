namespace TivuStream.Pie.Api.Acquisition;

/// <summary>
/// Settings governing the Acquisition Flow.
/// </summary>
public sealed class AcquisitionOptions
{
    /// <summary>
    /// Minutes between two acquisitions.
    /// </summary>
    /// <remarks>
    /// Acquisitions observe fixed periods, so this interval governs how fresh
    /// the current period is kept. It does not affect correctness: observing
    /// the same period again replaces the previous observation rather than
    /// adding to it.
    /// <para>
    /// The interval must stay below the retention of the Data Source. A Data
    /// Source that rotates its data faster than the acquisition discards it
    /// without reporting anything, which produces analyses that look
    /// plausible and are wrong.
    /// </para>
    /// </remarks>
    public int IntervalMinutes { get; set; } = 5;
}
