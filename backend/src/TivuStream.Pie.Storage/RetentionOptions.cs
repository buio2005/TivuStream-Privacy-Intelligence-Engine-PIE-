namespace TivuStream.Pie.Storage;

/// <summary>
/// How long each level of the history is kept.
/// </summary>
/// <remarks>
/// Persistence Specification, Retention Configuration.
/// </remarks>
public sealed class RetentionOptions
{
    /// <summary>
    /// Name of the section the values are read from.
    /// </summary>
    public const string SectionName = "Storage:Retention";

    /// <summary>
    /// The least hourly retention accepted, in days.
    /// </summary>
    /// <remarks>
    /// The twenty-four hour window is read from the hourly detail. A day still
    /// inside it cannot be consolidated without taking from the score and the
    /// pages what they rest on.
    /// </remarks>
    public const int MinimumHourlyDays = 2;

    /// <summary>
    /// The least daily retention accepted, in months.
    /// </summary>
    public const int MinimumDailyMonths = 1;

    /// <summary>
    /// The least monthly retention accepted, in years.
    /// </summary>
    public const int MinimumMonthlyYears = 1;

    /// <summary>
    /// Days the hourly detail is kept before it is consolidated into days.
    /// </summary>
    public int HourlyDays { get; set; } = 30;

    /// <summary>
    /// Months the daily aggregates are kept before they are consolidated into months.
    /// </summary>
    public int DailyMonths { get; set; } = 12;

    /// <summary>
    /// Years the monthly aggregates are kept before they are deleted.
    /// </summary>
    public int MonthlyYears { get; set; } = 5;

    /// <summary>
    /// Refuses a value below its minimum.
    /// </summary>
    /// <remarks>
    /// A value corrected in silence would change what is deleted without the
    /// person knowing. The start stops instead, and says which value and why.
    /// </remarks>
    /// <exception cref="InvalidOperationException">A value is below its minimum.</exception>
    public void Validate()
    {
        Require(nameof(HourlyDays), HourlyDays, MinimumHourlyDays);
        Require(nameof(DailyMonths), DailyMonths, MinimumDailyMonths);
        Require(nameof(MonthlyYears), MonthlyYears, MinimumMonthlyYears);
    }

    private static void Require(string name, int value, int minimum)
    {
        if (value < minimum)
        {
            throw new InvalidOperationException(
                $"'{SectionName}:{name}' is {value}, and must be at least {minimum}. "
                + "Consolidation cannot be undone, so the value is not corrected on your behalf.");
        }
    }
}
