namespace TivuStream.Pie.Model.Enums;

/// <summary>
/// How a value is known.
/// </summary>
/// <remarks>
/// The score already treats knowledge as graded: an area may be measured,
/// partially measured or not measurable. The same applies to a single value.
/// <para>
/// Treating knowledge of a value as binary forces a choice between two
/// mistakes whenever precision is limited: inventing a plausible figure, or
/// discarding information actually held. A value known with less precision is
/// therefore qualified rather than deleted or rounded to the plausible.
/// </para>
/// <para>
/// Defined by the Data Model Specification.
/// </para>
/// </remarks>
public enum MeasurementQuality
{
    /// <summary>
    /// Measured directly.
    /// </summary>
    Exact = 0,

    /// <summary>
    /// The real value is at least the one given, possibly higher.
    /// </summary>
    /// <remarks>
    /// Applies where a source returns truncated lists: what was counted is
    /// certain, what was left out is not.
    /// </remarks>
    LowerBound = 1,

    /// <summary>
    /// The event happened within the observation period, at an unknown
    /// instant.
    /// </summary>
    /// <remarks>
    /// Applies where a source reports activity over an interval rather than
    /// the moments of individual events.
    /// </remarks>
    PeriodBounded = 2,

    /// <summary>
    /// Derived by a method that must be stated.
    /// </summary>
    Estimated = 3,
}
