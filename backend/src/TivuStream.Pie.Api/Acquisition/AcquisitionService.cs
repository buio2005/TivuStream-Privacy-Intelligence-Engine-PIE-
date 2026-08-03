using Microsoft.Extensions.Options;
using TivuStream.Pie.Adapters;
using TivuStream.Pie.Adapters.Technitium;
using TivuStream.Pie.Core;
using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;
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
    // Continuity is judged over the last day, as the specification states.
    private const int ContinuityWindowHours = 24;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AcquisitionState _state;
    private readonly AcquisitionRepository _repository;
    private readonly ScoreRepository _scoreRepository;
    private readonly NpssEngine _engine;
    private readonly AcquisitionOptions _options;
    private readonly ILogger<AcquisitionService> _logger;

    public AcquisitionService(
        IServiceScopeFactory scopeFactory,
        AcquisitionState state,
        AcquisitionRepository repository,
        ScoreRepository scoreRepository,
        NpssEngine engine,
        IOptions<AcquisitionOptions> options,
        ILogger<AcquisitionService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _scopeFactory = scopeFactory;
        _state = state;
        _repository = repository;
        _scoreRepository = scoreRepository;
        _engine = engine;
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

    /// <summary>
    /// Counts the periods observed and the periods that could reasonably have
    /// been observed.
    /// </summary>
    /// <remarks>
    /// Nothing is expected of the system before its first observation. A
    /// freshly installed instance would otherwise be penalised for not having
    /// been watching the network before it existed, which says nothing about
    /// the network.
    /// </remarks>
    private (int Observed, int Expected) MeasureContinuity(ObservationPeriod current)
    {
        DateTimeOffset windowStart = current.Start.AddHours(-(ContinuityWindowHours - 1));

        DateTimeOffset? firstObservation = _repository.GetFirstPeriodStart();

        DateTimeOffset from = firstObservation is null || firstObservation < windowStart
            ? windowStart
            : firstObservation.Value;

        int expected = (int)(current.Start - from).TotalHours + 1;

        return (_repository.CountPeriodsSince(from), Math.Max(1, expected));
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
            SourceConfiguration? configuration =
                dataSource.Capabilities.Contains(nameof(SourceConfiguration), StringComparer.Ordinal)
                    ? await adapter.GetConfigurationAsync(cancellationToken).ConfigureAwait(false)
                    : null;

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
                Configuration = configuration,
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

            // The Core is invoked once the observation has been recorded. It
            // receives what it needs and knows neither where the data came
            // from nor where its result will go.
            Npss? previous = _scoreRepository.GetLatest();

            (int observedPeriods, int expectedPeriods) = MeasureContinuity(period);

            Npss score = _engine.Evaluate(new NpssEvaluationInput
            {
                Statistics = statistics,
                Configuration = configuration,
                SourceReachable = dataSource.Status == DataSourceStatus.Online,
                ObservedPeriods = observedPeriods,
                ExpectedPeriods = expectedPeriods,
                PreviousOverallScore = previous?.OverallScore,
                PreviousCoverage = previous?.Coverage,
            });

            _scoreRepository.Save(dataSource.Id, period, score);

            AcquisitionLog.Scored(_logger, score.OverallScore, score.Coverage);

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
