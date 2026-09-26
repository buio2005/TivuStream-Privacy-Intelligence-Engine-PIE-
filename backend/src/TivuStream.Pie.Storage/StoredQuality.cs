using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Storage;

/// <summary>
/// How the quality of a measurement is combined when rows are aggregated.
/// </summary>
/// <remarks>
/// A total is known no better than its vaguest part. Reading a window and
/// consolidating a day apply the same rule, so it is written once.
/// </remarks>
internal static class StoredQuality
{
    /// <summary>
    /// The least precise observation quality among the rows aggregated, as the
    /// numeric value of <see cref="MeasurementQuality"/>.
    /// </summary>
    internal const string LeastPreciseRank =
        """
        MAX(CASE observation_quality
                WHEN 'Exact'         THEN 0
                WHEN 'LowerBound'    THEN 1
                WHEN 'PeriodBounded' THEN 2
                ELSE 3
            END)
        """;

    /// <summary>
    /// The least precise observation quality among the rows aggregated, as
    /// the name it is stored under.
    /// </summary>
    internal const string LeastPreciseName =
        $"""
        CASE {LeastPreciseRank}
            WHEN 0 THEN 'Exact'
            WHEN 1 THEN 'LowerBound'
            WHEN 2 THEN 'PeriodBounded'
            ELSE 'Estimated'
        END
        """;
}
