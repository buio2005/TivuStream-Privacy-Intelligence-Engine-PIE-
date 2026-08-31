using TivuStream.Pie.Core;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Storage;

namespace TivuStream.Pie.Api.Classification;

/// <summary>
/// Holds the classification engine the rest of the application uses.
/// </summary>
/// <remarks>
/// Reading the lists means opening files that together hold hundreds of
/// thousands of names. It happens when the lists change, not when a domain is
/// classified.
/// <para>
/// This class joins what Storage keeps with what the Core decides, which is
/// the composition root's job. The Core does not open files, and Storage does
/// not classify.
/// </para>
/// </remarks>
internal sealed class ClassificationProvider
{
    private readonly ClassificationListRepository _repository;
    private readonly ClassificationListStore _store;
    private readonly ILogger<ClassificationProvider> _logger;
    private readonly Lock _gate = new();

    private ClassificationEngine _engine = new([]);

    /// <summary>
    /// Creates the provider.
    /// </summary>
    /// <param name="repository">Source of the list descriptions.</param>
    /// <param name="store">Source of the list contents.</param>
    /// <param name="logger">Recipient of what happened while loading.</param>
    public ClassificationProvider(
        ClassificationListRepository repository,
        ClassificationListStore store,
        ILogger<ClassificationProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// The engine built from the lists loaded most recently.
    /// </summary>
    /// <remarks>
    /// Before the first load, and when no list is available, the engine
    /// answers that it does not know. It never fails and never guesses.
    /// </remarks>
    public ClassificationEngine Engine
    {
        get
        {
            lock (_gate)
            {
                return _engine;
            }
        }
    }

    /// <summary>
    /// Puts the default lists in place, without touching those already held.
    /// </summary>
    public void EnsureDefaults()
    {
        int added = DefaultClassificationLists.All.Count(_repository.AddIfAbsent);

        if (added > 0)
        {
            ClassificationLog.DefaultsAdded(_logger, added);
        }
    }

    /// <summary>
    /// Reads every enabled list and builds the engine again.
    /// </summary>
    public void Reload()
    {
        List<LoadedClassificationList> loaded = [];
        int entries = 0;

        foreach (ClassificationList descriptor in _repository.GetAll())
        {
            if (!descriptor.Enabled)
            {
                continue;
            }

            ClassificationListContent content = _store.Read(descriptor.Name);

            if (!content.Present)
            {
                // Not a list that has aged: a list that does not exist yet.
                ClassificationLog.NeverDownloaded(_logger, descriptor.Name);
                continue;
            }

            if (content.Entries.Count == 0 && content.UnreadableLines > 0)
            {
                // A file was found and understood by nobody. Left silent, this
                // would show as a network where nothing was found, because
                // nothing was looked at.
                ClassificationLog.NotUnderstood(_logger, descriptor.Name, content.UnreadableLines);
                continue;
            }

            loaded.Add(new LoadedClassificationList
            {
                Descriptor = descriptor with { EntryCount = content.Entries.Count },
                Entries = content.Entries,
            });

            entries += content.Entries.Count;
        }

        lock (_gate)
        {
            _engine = new ClassificationEngine(loaded);
        }

        if (loaded.Count == 0)
        {
            ClassificationLog.NothingToClassifyWith(_logger);
        }
        else
        {
            ClassificationLog.Loaded(_logger, loaded.Count, entries);
        }
    }
}
