namespace TivuStream.Pie.Adapters;

/// <summary>
/// Time interval an acquisition refers to.
/// </summary>
/// <remarks>
/// Data Sources expose their data over time intervals, and different
/// intervals do not necessarily contain the same information. The interval is
/// therefore always explicit and never left to the Adapter to assume.
/// <para>
/// The interval also supports incremental acquisition: the Adapter Manager
/// can request only what followed the previous cycle.
/// </para>
/// </remarks>
/// <param name="Start">Beginning of the interval, inclusive.</param>
/// <param name="End">End of the interval, exclusive.</param>
public readonly record struct AcquisitionWindow(DateTimeOffset Start, DateTimeOffset End);
