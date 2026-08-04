using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;
using Xunit;

namespace TivuStream.Pie.Core.Tests;

/// <summary>
/// Verifies the rules the Threat Intelligence Specification states.
/// </summary>
/// <remarks>
/// Each test refers to a commitment made to the person using the tool, not to
/// an implementation detail.
/// </remarks>
public sealed class ClassificationEngineTests
{
    private static readonly DateTimeOffset ListUpdatedAt =
        new(2026, 7, 29, 6, 0, 0, TimeSpan.Zero);

    // ------------------------------------------------------------------
    // A name found in full is an assertion; a name reached through a
    // parent is an inference, and the two are not stated alike
    // ------------------------------------------------------------------

    [Fact]
    public void A_name_present_in_full_is_stated_with_high_confidence()
    {
        DomainClassification result = Engine().Classify("tracker.example.com");

        Assert.Equal(ThreatCategory.Tracking, result.Category);
        Assert.Equal(ConfidenceLevel.High, result.Confidence);
    }

    [Fact]
    public void A_name_reached_through_a_parent_is_stated_with_medium_confidence()
    {
        // The list asserts something about the parent. Extending it to the
        // observed name is reasonable and remains an inference.
        DomainClassification result = Engine().Classify("pixel.ads.example.net");

        Assert.Equal(ThreatCategory.Advertising, result.Category);
        Assert.Equal(ConfidenceLevel.Medium, result.Confidence);
    }

    [Fact]
    public void The_search_stops_at_the_nearest_match()
    {
        // Both "inner.example.org" and "example.org" are listed, under
        // different categories. The closer statement is the more specific one.
        DomainClassification result = Engine().Classify("inner.example.org");

        Assert.Equal(ThreatCategory.Analytics, result.Category);
    }

    // ------------------------------------------------------------------
    // Unknown means not classified, never harmless
    // ------------------------------------------------------------------

    [Fact]
    public void A_name_in_no_list_is_unclassified_and_carries_no_provenance()
    {
        DomainClassification result = Engine().Classify("something.unlisted.test");

        Assert.Equal(ThreatCategory.Unknown, result.Category);

        // No confidence and no source: there is no list to attribute a
        // statement to, and inventing one would be a false record.
        Assert.Null(result.Confidence);
        Assert.Null(result.SourceName);
        Assert.Null(result.SourceUpdatedAt);
    }

    [Fact]
    public void A_parent_is_not_classified_by_one_of_its_children()
    {
        // "tracker.example.com" is listed. "example.com" is not, and must not
        // inherit the category of a name below it.
        Assert.Equal(ThreatCategory.Unknown, Engine().Classify("example.com").Category);
    }

    // ------------------------------------------------------------------
    // Every classification declares where it comes from and how old it is
    // ------------------------------------------------------------------

    [Fact]
    public void A_classification_declares_the_list_it_comes_from()
    {
        DomainClassification result = Engine().Classify("tracker.example.com");

        Assert.Equal("Trackers", result.SourceName);
        Assert.Equal(ListUpdatedAt, result.SourceUpdatedAt);
    }

    [Fact]
    public void A_list_never_updated_produces_a_classification_with_no_age()
    {
        LoadedClassificationList neverUpdated = List(
            "Fresh Install",
            ThreatCategory.Malware,
            updatedAt: null,
            "bad.example.com");

        DomainClassification result = new ClassificationEngine([neverUpdated])
            .Classify("bad.example.com");

        // The absence is passed on rather than replaced with the moment the
        // classification was produced, which would claim an update that never
        // happened.
        Assert.Equal(ThreatCategory.Malware, result.Category);
        Assert.Null(result.SourceUpdatedAt);
    }

    // ------------------------------------------------------------------
    // A list left out of the classification classifies nothing
    // ------------------------------------------------------------------

    [Fact]
    public void A_disabled_list_takes_no_part_in_the_classification()
    {
        LoadedClassificationList enabled = List(
            "Disabled",
            ThreatCategory.Malware,
            ListUpdatedAt,
            "bad.example.com");

        LoadedClassificationList disabled = enabled with
        {
            Descriptor = enabled.Descriptor with { Enabled = false },
        };

        Assert.Equal(
            ThreatCategory.Unknown,
            new ClassificationEngine([disabled]).Classify("bad.example.com").Category);
    }

    [Fact]
    public void An_installation_with_no_list_classifies_nothing_and_fails_at_nothing()
    {
        // Before any list is downloaded the engine still answers, and answers
        // that it does not know.
        Assert.Equal(
            ThreatCategory.Unknown,
            new ClassificationEngine([]).Classify("tracker.example.com").Category);
    }

    // ------------------------------------------------------------------
    // Comparison of names
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("TRACKER.EXAMPLE.COM")]
    [InlineData("Tracker.Example.Com")]
    [InlineData("tracker.example.com.")]
    [InlineData("  tracker.example.com  ")]
    public void The_same_name_written_differently_is_the_same_name(string written)
    {
        // Case and the trailing root label are not distinctions a network
        // makes, and must not become distinctions the classification makes.
        Assert.Equal(ThreatCategory.Tracking, Engine().Classify(written).Category);
    }

    // ------------------------------------------------------------------
    // Applying the classification to a domain
    // ------------------------------------------------------------------

    [Fact]
    public void Classifying_a_domain_leaves_what_was_observed_untouched()
    {
        Domain observed = new()
        {
            Name = "tracker.example.com",
            Category = ThreatCategory.Unknown,
            FirstSeen = new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.Zero),
            LastSeen = new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.Zero),
            ObservationQuality = MeasurementQuality.PeriodBounded,
            Occurrences = 42,
        };

        Domain classified = Engine().Classify(observed);

        Assert.Equal(ThreatCategory.Tracking, classified.Category);
        Assert.Equal(ConfidenceLevel.High, classified.CategoryConfidence);
        Assert.Equal("Trackers", classified.CategorySource);
        Assert.Equal(ListUpdatedAt, classified.CategorySourceUpdatedAt);

        // The engine judges; it does not rewrite the observation.
        Assert.Equal(observed.Occurrences, classified.Occurrences);
        Assert.Equal(observed.FirstSeen, classified.FirstSeen);
        Assert.Equal(observed.LastSeen, classified.LastSeen);
        Assert.Equal(observed.ObservationQuality, classified.ObservationQuality);
    }

    [Fact]
    public void An_unclassified_domain_carries_no_provenance_after_classification()
    {
        Domain observed = new()
        {
            Name = "something.unlisted.test",
            Category = ThreatCategory.Unknown,
            FirstSeen = new DateTimeOffset(2026, 8, 4, 9, 0, 0, TimeSpan.Zero),
            LastSeen = new DateTimeOffset(2026, 8, 4, 10, 0, 0, TimeSpan.Zero),
            Occurrences = 1,
        };

        Domain classified = Engine().Classify(observed);

        Assert.Equal(ThreatCategory.Unknown, classified.Category);
        Assert.Null(classified.CategoryConfidence);
        Assert.Null(classified.CategorySource);
        Assert.Null(classified.CategorySourceUpdatedAt);
    }

    // ------------------------------------------------------------------
    // Fixtures
    // ------------------------------------------------------------------

    private static ClassificationEngine Engine()
    {
        return new ClassificationEngine(
        [
            List("Trackers", ThreatCategory.Tracking, ListUpdatedAt, "tracker.example.com"),
            List("Advertising", ThreatCategory.Advertising, ListUpdatedAt, "ads.example.net"),
            List("Analytics", ThreatCategory.Analytics, ListUpdatedAt, "inner.example.org"),
            List("Suspicious", ThreatCategory.Suspicious, ListUpdatedAt, "example.org"),
        ]);
    }

    private static LoadedClassificationList List(
        string name,
        ThreatCategory category,
        DateTimeOffset? updatedAt,
        params string[] entries)
    {
        return new LoadedClassificationList
        {
            Descriptor = Descriptor(name, category, updatedAt) with { EntryCount = entries.Length },
            Entries = new HashSet<string>(entries, StringComparer.Ordinal),
        };
    }

    private static ClassificationList Descriptor(
        string name,
        ThreatCategory category,
        DateTimeOffset? updatedAt)
    {
        return new ClassificationList
        {
            Name = name,
            SourceUrl = new Uri("https://lists.invalid/" + name),
            Category = category,
            Licence = "CC0-1.0",
            UpdatedAt = updatedAt,
            EntryCount = 0,
            Enabled = true,
        };
    }
}
