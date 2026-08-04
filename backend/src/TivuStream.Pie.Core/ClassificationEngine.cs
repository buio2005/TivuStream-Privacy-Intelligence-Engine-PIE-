using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Core;

/// <summary>
/// Assigns a category to a domain using the lists held locally.
/// </summary>
/// <remarks>
/// Matching happens on the device alone. The domains observed on the network
/// are never sent anywhere, for any purpose, including asking a reputation
/// service whether they are dangerous.
/// <para>
/// The rules are stated by the Threat Intelligence Specification.
/// </para>
/// </remarks>
public sealed class ClassificationEngine
{
    /// <summary>
    /// Order the categories are judged in, from the gravest downwards.
    /// </summary>
    /// <remarks>
    /// This is a declared editorial judgement, like the weights of the score.
    /// It does not derive from a measurement and does not claim to.
    /// <para>
    /// Security comes before privacy: a domain that both tracks and
    /// distributes malware is presented as a threat rather than a nuisance.
    /// The descriptive categories express no judgement and yield to any
    /// category that does.
    /// </para>
    /// </remarks>
    private static readonly ThreatCategory[] SeverityOrder =
    [
        ThreatCategory.Malware,
        ThreatCategory.Phishing,
        ThreatCategory.Cryptomining,
        ThreatCategory.Suspicious,
        ThreatCategory.Tracking,
        ThreatCategory.Analytics,
        ThreatCategory.Advertising,
        ThreatCategory.Social,
        ThreatCategory.Streaming,
        ThreatCategory.Cloud,
        ThreatCategory.AiServices,
        ThreatCategory.Unknown,
    ];

    private readonly IReadOnlyList<LoadedClassificationList> _lists;

    /// <summary>
    /// Creates the engine over the lists it is to consult.
    /// </summary>
    /// <remarks>
    /// Lists that are not enabled are ignored. A list that was never
    /// downloaded declares no update instant and contains no entries, so it
    /// takes no part in any classification.
    /// <para>
    /// The lists are ordered once, here, by declared severity and then by how
    /// recently each was updated. Consulting them in that order means the
    /// first match found at a given level is already the one the rules
    /// require, and that the same set of lists always produces the same
    /// answer.
    /// </para>
    /// </remarks>
    /// <param name="lists">Lists held by the installation.</param>
    public ClassificationEngine(IReadOnlyList<LoadedClassificationList> lists)
    {
        ArgumentNullException.ThrowIfNull(lists);

        _lists =
        [
            .. lists
                .Where(list => list.Descriptor.Enabled)
                .OrderBy(list => SeverityOf(list.Descriptor.Category))

                // A list that was never updated yields to one that was.
                .ThenByDescending(list => list.Descriptor.UpdatedAt ?? DateTimeOffset.MinValue)

                // Nothing else distinguishes them, and an arbitrary order
                // would make the same installation answer differently after a
                // reordering of the code.
                .ThenBy(list => list.Descriptor.Name, StringComparer.Ordinal),
        ];
    }

    /// <summary>
    /// Classifies a domain name.
    /// </summary>
    /// <remarks>
    /// The comparison walks from the full name towards the parent, dropping
    /// one label at a time, and stops at the first level that matches.
    /// <para>
    /// A name found in full is stated with high confidence. A name reached
    /// through a parent is an inference, and is stated with medium confidence:
    /// the list asserts something about the parent, and the engine extends the
    /// assertion to the name observed.
    /// </para>
    /// <para>
    /// The nearest name wins whatever its category. Specificity is a stronger
    /// signal than severity: letting a grave category matched on the parent
    /// override a direct match would replace a statement with an inference.
    /// </para>
    /// </remarks>
    /// <param name="name">Domain name to classify.</param>
    /// <returns>
    /// What is known about the domain. A name present in no list is returned
    /// as unclassified, which is not the same as harmless.
    /// </returns>
    public DomainClassification Classify(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string candidate = name.Trim().TrimEnd('.').ToLowerInvariant();

        bool isFullName = true;

        while (candidate.Length > 0)
        {
            foreach (LoadedClassificationList list in _lists)
            {
                if (!list.Entries.Contains(candidate))
                {
                    continue;
                }

                return new DomainClassification
                {
                    Category = list.Descriptor.Category,
                    Confidence = isFullName ? ConfidenceLevel.High : ConfidenceLevel.Medium,
                    SourceName = list.Descriptor.Name,
                    SourceUpdatedAt = list.Descriptor.UpdatedAt,
                };
            }

            int separator = candidate.IndexOf('.', StringComparison.Ordinal);

            if (separator < 0)
            {
                break;
            }

            candidate = candidate[(separator + 1)..];
            isFullName = false;
        }

        return DomainClassification.Unclassified;
    }

    /// <summary>
    /// Position of a category in the declared order of severity.
    /// </summary>
    private static int SeverityOf(ThreatCategory category)
    {
        int position = Array.IndexOf(SeverityOrder, category);

        // A category absent from the order would otherwise be treated as the
        // gravest of all. Failing here is better than classifying by accident.
        return position >= 0
            ? position
            : throw new ArgumentOutOfRangeException(
                nameof(category),
                category,
                "The category is missing from the declared order of severity.");
    }

    /// <summary>
    /// Returns the domain with its classification applied.
    /// </summary>
    /// <param name="domain">Domain to classify.</param>
    /// <returns>The same domain, carrying the category and its provenance.</returns>
    public Domain Classify(Domain domain)
    {
        ArgumentNullException.ThrowIfNull(domain);

        DomainClassification classification = Classify(domain.Name);

        return domain with
        {
            Category = classification.Category,
            CategoryConfidence = classification.Confidence,
            CategorySource = classification.SourceName,
            CategorySourceUpdatedAt = classification.SourceUpdatedAt,
        };
    }
}
