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
    private readonly IReadOnlyList<LoadedClassificationList> _lists;

    /// <summary>
    /// Creates the engine over the lists it is to consult.
    /// </summary>
    /// <remarks>
    /// Lists that are not enabled are ignored. A list that was never
    /// downloaded declares no update instant and contains no entries, so it
    /// takes no part in any classification.
    /// </remarks>
    /// <param name="lists">Lists held by the installation.</param>
    public ClassificationEngine(IReadOnlyList<LoadedClassificationList> lists)
    {
        ArgumentNullException.ThrowIfNull(lists);

        _lists = [.. lists.Where(list => list.Descriptor.Enabled)];
    }

    /// <summary>
    /// Classifies a domain name.
    /// </summary>
    /// <remarks>
    /// The comparison walks from the full name towards the parent, dropping
    /// one label at a time, and stops at the first match.
    /// <para>
    /// A name found in full is stated with high confidence. A name reached
    /// through a parent is an inference, and is stated with medium confidence:
    /// the list asserts something about the parent, and the engine extends the
    /// assertion to the name observed.
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
