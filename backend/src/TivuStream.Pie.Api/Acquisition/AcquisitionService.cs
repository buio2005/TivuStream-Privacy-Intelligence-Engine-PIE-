using Microsoft.Extensions.Options;
using TivuStream.Pie.Adapters;
using TivuStream.Pie.Adapters.Technitium;
using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Storage;

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
    private readonly AcquisitionRepository _repository;
    private readonly AcquisitionOptions _options;
    private readonly ILogger<AcquisitionService> _logger;

    public AcquisitionService(
        IServiceScopeFactory scopeFactory,
        AcquisitionState state,
        AcquisitionRepository repository,
        IOptions<AcquisitionOptions> options,
        ILogger<AcquisitionService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _scopeFactory = scopeFactory;
        _state = state;
        _repository = repository;
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

        // The acquisition observes the period the current instant falls into,
        // not a window trailing behind it. Observing the same period again
        // replaces the previous observation instead of adding to it, so
        // acquiring more often costs nothing in correctness.
        ObservationPeriod period = ObservationPeriod.Containing(attemptedAt);

        AcquisitionWindow window = new(period.Start, period.End);

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

            IReadOnlyList<Device> devices = await adapter
                .GetDevicesAsync(window, cancellationToken)
                .ConfigureAwait(false);

            IReadOnlyList<Domain> domains = await adapter
                .GetDomainsAsync(window, cancellationToken)
                .ConfigureAwait(false);

            // Asked for only when the Data Source declares it can provide it.
            // This is what makes the declared capability load bearing rather
            // than merely descriptive.
            IReadOnlyList<DomainActivity> activities =
                dataSource.Capabilities.Contains(nameof(DomainActivity), StringComparer.Ordinal)
                    ? await adapter.GetDomainActivitiesAsync(window, cancellationToken).ConfigureAwait(false)
                    : [];

            _repository.Save(new StoredAcquisition
            {
                Period = period,
                ObservedAt = attemptedAt,
                DataSource = dataSource,
                Statistics = statistics,
                Devices = devices,
                Domains = domains,
                DomainActivities = activities,
            });

            _state.Update(new AcquisitionResult
            {
                AttemptedAt = attemptedAt,
                Succeeded = true,
                PeriodStart = period.Start,
                PeriodEnd = period.End,
                DataSource = dataSource,
                Statistics = statistics,
            });

            AcquisitionLog.Completed(
                _logger,
                adapter.Provider,
                period.Start,
                devices.Count,
                domains.Count,
                activities.Count);
        }
        catch (StorageException exception)
        {
            _state.Update(new AcquisitionResult
            {
                AttemptedAt = attemptedAt,
                Succeeded = false,
                Failure = exception.Message,
                PeriodStart = period.Start,
                PeriodEnd = period.End,
            });

            AcquisitionLog.Failed(_logger, exception.Message);
        }
        catch (AdapterException exception)
        {
            _state.Update(new AcquisitionResult
            {
                AttemptedAt = attemptedAt,
                Succeeded = false,
                Failure = exception.Message,
                PeriodStart = period.Start,
                PeriodEnd = period.End,
            });

            AcquisitionLog.Failed(_logger, exception.Message);
        }
    }
}
