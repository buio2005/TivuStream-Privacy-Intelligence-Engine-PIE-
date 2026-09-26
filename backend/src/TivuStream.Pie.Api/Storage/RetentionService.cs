using TivuStream.Pie.Storage;

namespace TivuStream.Pie.Api.Storage;

/// <summary>
/// Consolidates the history at start and then once an hour.
/// </summary>
/// <remarks>
/// A scheduled operation, never part of a request (Persistence Specification,
/// Performance). A failure leaves the detail as it was, and the next run takes
/// it up again.
/// </remarks>
internal sealed class RetentionService : BackgroundService
{
    private readonly PeriodConsolidator _consolidator;
    private readonly TimeProvider _time;
    private readonly ILogger<RetentionService> _logger;

    public RetentionService(PeriodConsolidator consolidator, TimeProvider time, ILogger<RetentionService> logger)
    {
        _consolidator = consolidator;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromHours(1), _time);

        do
        {
            Consolidate();
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private void Consolidate()
    {
        try
        {
            ConsolidationOutcome outcome = _consolidator.Consolidate(_time.GetUtcNow());

            if (outcome != new ConsolidationOutcome(0, 0, 0))
            {
                RetentionLog.Consolidated(_logger, outcome.DaysWritten, outcome.MonthsWritten, outcome.PeriodsDeleted);
            }
        }
        catch (StorageException exception)
        {
            RetentionLog.Failed(_logger, exception);
        }
    }
}
