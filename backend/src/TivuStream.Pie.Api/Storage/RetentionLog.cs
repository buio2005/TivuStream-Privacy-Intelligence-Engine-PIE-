namespace TivuStream.Pie.Api.Storage;

/// <summary>
/// Messages recorded while consolidating the history.
/// </summary>
/// <remarks>
/// Counts only. Nothing of what the periods contain reaches the log.
/// </remarks>
internal static partial class RetentionLog
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Retention: hourly detail {HourlyDays} days, daily aggregates {DailyMonths} months, monthly aggregates {MonthlyYears} years.")]
    internal static partial void Configured(ILogger logger, int hourlyDays, int dailyMonths, int monthlyYears);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "History consolidated: {Days} days, {Months} months written, {Deleted} periods deleted.")]
    internal static partial void Consolidated(ILogger logger, int days, int months, int deleted);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "The history could not be consolidated. The detail is untouched and will be consolidated at the next attempt.")]
    internal static partial void Failed(ILogger logger, Exception exception);
}
