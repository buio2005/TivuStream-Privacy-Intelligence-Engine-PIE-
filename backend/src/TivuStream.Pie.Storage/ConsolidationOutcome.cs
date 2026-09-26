namespace TivuStream.Pie.Storage;

/// <summary>
/// What a consolidation did, in counts only.
/// </summary>
/// <param name="DaysWritten">Days consolidated from their hours.</param>
/// <param name="MonthsWritten">Months consolidated from their days.</param>
/// <param name="PeriodsDeleted">Periods deleted for having outlived the monthly retention.</param>
public sealed record ConsolidationOutcome(int DaysWritten, int MonthsWritten, int PeriodsDeleted);
