using Microsoft.Extensions.Options;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Storage;

namespace TivuStream.Pie.Api.Classification;

/// <summary>
/// Keeps the classification lists current.
/// </summary>
/// <remarks>
/// What travels outwards is a request for a list. The domains observed on the
/// network are never part of it, for any purpose. This is the only moment PIE
/// reaches anywhere other than its own Data Source, and it reaches for public
/// files that say nothing about the person running it.
/// </remarks>
internal sealed class ClassificationUpdateService : BackgroundService
{
    /// <summary>
    /// Largest list accepted.
    /// </summary>
    /// <remarks>
    /// The addresses can be chosen by the person, so the size of what arrives
    /// is not something the project controls. A cap keeps a mistaken address
    /// from filling the disk of a machine that is often small.
    /// </remarks>
    private const long MaximumListBytes = 64L * 1024 * 1024;

    private readonly IHttpClientFactory _clientFactory;
    private readonly ClassificationListRepository _repository;
    private readonly ClassificationListStore _store;
    private readonly ClassificationProvider _provider;
    private readonly ClassificationOptions _options;
    private readonly TimeProvider _time;
    private readonly ILogger<ClassificationUpdateService> _logger;

    public ClassificationUpdateService(
        IHttpClientFactory clientFactory,
        ClassificationListRepository repository,
        ClassificationListStore store,
        ClassificationProvider provider,
        IOptions<ClassificationOptions> options,
        TimeProvider time,
        ILogger<ClassificationUpdateService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _clientFactory = clientFactory;
        _repository = repository;
        _store = store;
        _provider = provider;
        _options = options.Value;
        _time = time;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Checked often, acted upon rarely: a list is fetched only when what
        // is held has grown older than the interval.
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(15));

        do
        {
            await UpdateDueListsAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task UpdateDueListsAsync(CancellationToken cancellationToken)
    {
        TimeSpan interval = TimeSpan.FromHours(Math.Max(1, _options.UpdateIntervalHours));
        DateTimeOffset now = _time.GetUtcNow();

        bool anythingChanged = false;

        foreach (ClassificationList list in _repository.GetAll())
        {
            if (!list.Enabled)
            {
                continue;
            }

            bool due = list.UpdatedAt is not { } updatedAt
                || now - updatedAt >= interval
                || !_store.Exists(list.Name);

            if (!due)
            {
                continue;
            }

            anythingChanged |= await UpdateAsync(list, now, cancellationToken).ConfigureAwait(false);
        }

        if (anythingChanged)
        {
            _provider.Reload();
        }
    }

    private async Task<bool> UpdateAsync(
        ClassificationList list,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            using HttpClient client = _clientFactory.CreateClient(nameof(ClassificationUpdateService));

            using HttpResponseMessage response = await client
                .GetAsync(list.SourceUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength > MaximumListBytes)
            {
                ClassificationLog.TooLarge(_logger, list.Name);
                return false;
            }

            string content = await response.Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            _store.Replace(list.Name, content);

            ClassificationListContent stored = _store.Read(list.Name);

            // The instant is recorded only now, after a file exists and has
            // been read. An age written before the content arrived would
            // declare a freshness the list does not have.
            _repository.Save(list with
            {
                UpdatedAt = now,
                EntryCount = stored.Entries.Count,
            });

            ClassificationLog.Updated(
                _logger,
                list.Name,
                stored.Entries.Count,
                stored.UnreadableLines);

            return true;
        }
        catch (HttpRequestException exception)
        {
            ReportFailure(list, exception.Message);
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            ReportFailure(list, "the source did not answer in time");
            return false;
        }
        catch (StorageException exception)
        {
            ReportFailure(list, exception.Message);
            return false;
        }
    }

    /// <summary>
    /// Records that a list could not be updated, without invalidating it.
    /// </summary>
    /// <remarks>
    /// The stored file stays in use and its declared age keeps growing. A list
    /// that cannot be updated is less useful than a recent one and more useful
    /// than none.
    /// </remarks>
    private void ReportFailure(ClassificationList list, string reason)
    {
        if (_store.Exists(list.Name))
        {
            ClassificationLog.UpdateFailedButHeld(_logger, list.Name, list.UpdatedAt, reason);
        }
        else
        {
            ClassificationLog.UpdateFailedAndAbsent(_logger, list.Name, reason);
        }
    }
}
