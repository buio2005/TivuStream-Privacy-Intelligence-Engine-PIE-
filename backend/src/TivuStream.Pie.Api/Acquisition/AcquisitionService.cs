using Microsoft.Extensions.Options;
using TivuStream.Pie.Adapters;
using TivuStream.Pie.Adapters.Technitium;
using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Api.Acquisition;

/// <summary>
/// Runs the Acquisition Flow at a regular interval.
/// </summary>
/// <remarks>
/// Stands in for the Scheduler described by the Backend Specification. It is
/// the only component that reaches a Data Source, and it does so without ever
/// being triggered by a request coming from the Frontend.
/// </remarks>
internal sealed class AcquisitionService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AcquisitionState _state;
    private readonly AcquisitionOptions _options;
    private readonly ILogger<AcquisitionService> _logger;

    public AcquisitionService(
        IServiceScopeFactory scopeFactory,
        AcquisitionState state,
        IOptions<AcquisitionOptions> options,
        ILogger<AcquisitionService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _scopeFactory = scopeFactory;
        _state = state;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan interval = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));

        using PeriodicTimer timer = new(interval);

        do
        {
            await AcquireAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task AcquireAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset attemptedAt = DateTimeOffset.UtcNow;

        AcquisitionWindow window = new(
            attemptedAt.AddMinutes(-Math.Max(1, _options.WindowMinutes)),
            attemptedAt);

        using IServiceScope scope = _scopeFactory.CreateScope();

        try
        {
            // Resolved inside the guarded block on purpose: an Adapter whose
            // settings are missing fails while being created, and a host that
            // stops because a token has not been filled in would be a poor
            // way of reporting it.
            TechnitiumAdapter adapter = scope.ServiceProvider.GetRequiredService<TechnitiumAdapter>();

            DataSource dataSource = await adapter
                .DescribeAsync(cancellationToken)
                .ConfigureAwait(false);

            Statistics statistics = await adapter
                .GetStatisticsAsync(window, cancellationToken)
                .ConfigureAwait(false);

            _state.Update(new AcquisitionResult
            {
                AttemptedAt = attemptedAt,
                Succeeded = true,
                WindowStart = window.Start,
                WindowEnd = window.End,
                DataSource = dataSource,
                Statistics = statistics,
            });

            AcquisitionLog.Completed(_logger, adapter.Provider, window.Start, window.End);
        }
        catch (AdapterException exception)
        {
            _state.Update(new AcquisitionResult
            {
                AttemptedAt = attemptedAt,
                Succeeded = false,
                Failure = exception.Message,
                WindowStart = window.Start,
                WindowEnd = window.End,
            });

            AcquisitionLog.Failed(_logger, exception.Message);
        }
    }
}
