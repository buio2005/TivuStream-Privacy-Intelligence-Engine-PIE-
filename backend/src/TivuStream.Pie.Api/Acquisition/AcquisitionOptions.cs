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
    /// The interval must stay below the retention of the Data Source.
    /// A Data Source that rotates its data faster than the acquisition
    /// discards it without reporting anything, which produces analyses that
    /// look plausible and are wrong.
    /// </remarks>
    public int IntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Width in minutes of the interval each acquisition asks for.
    /// </summary>
    /// <remarks>
    /// Kept separate from the interval between acquisitions: overlapping the
    /// requested interval avoids losing data at the boundaries.
    /// </remarks>
    public int WindowMinutes { get; set; } = 60;
}
