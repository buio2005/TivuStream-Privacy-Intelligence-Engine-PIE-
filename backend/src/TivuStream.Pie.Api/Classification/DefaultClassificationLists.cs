using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Api.Classification;

/// <summary>
/// The classification lists an installation starts with.
/// </summary>
/// <remarks>
/// Stated by the Threat Intelligence Specification and reproduced here because
/// the composition root is what decides how an installation begins.
/// <para>
/// They all come from one source, the Block List Project, released into the
/// public domain. The single source is declared rather than hidden: among the
/// freely usable sources, few are split by category, and the few that are feed
/// one another. Adopting two of them would give the appearance of independent
/// opinions without being it, and an apparent agreement is worse than a
/// declared dependency.
/// </para>
/// <para>
/// Nothing is downloaded here. These are descriptions, and a description with
/// no file is a list that does not exist yet.
/// </para>
/// </remarks>
internal static class DefaultClassificationLists
{
    private const string Licence = "Unlicense";

    private const string BaseAddress = "https://blocklistproject.github.io/Lists/alt-version/";

    /// <summary>
    /// Returns the default lists.
    /// </summary>
    internal static IReadOnlyList<ClassificationList> All { get; } =
    [
        Describe("Block List Project - Advertising", "ads-nl.txt", ThreatCategory.Advertising),
        Describe("Block List Project - Tracking", "tracking-nl.txt", ThreatCategory.Tracking),
        Describe("Block List Project - Malware", "malware-nl.txt", ThreatCategory.Malware),
        Describe("Block List Project - Phishing", "phishing-nl.txt", ThreatCategory.Phishing),
        Describe("Block List Project - Cryptomining", "crypto-nl.txt", ThreatCategory.Cryptomining),
        Describe("Block List Project - Scam", "scam-nl.txt", ThreatCategory.Suspicious),
        Describe("Block List Project - Abuse", "abuse-nl.txt", ThreatCategory.Suspicious),
    ];

    private static ClassificationList Describe(string name, string file, ThreatCategory category)
    {
        return new ClassificationList
        {
            Name = name,
            SourceUrl = new Uri(BaseAddress + file),
            Category = category,
            Licence = Licence,

            // Never downloaded, and therefore carrying no age and no entries.
            UpdatedAt = null,
            EntryCount = 0,
            Enabled = true,
        };
    }
}
