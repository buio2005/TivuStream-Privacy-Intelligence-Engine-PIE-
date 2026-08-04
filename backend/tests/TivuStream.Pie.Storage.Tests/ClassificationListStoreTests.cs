using Xunit;

namespace TivuStream.Pie.Storage.Tests;

/// <summary>
/// Verifies that a list file is read as what it says.
/// </summary>
/// <remarks>
/// A reader that understands nothing does not fail: it returns an empty list,
/// and the tool then reports a network where nothing was found because nothing
/// was looked at. These checks exist so that the failure cannot pass for good
/// news.
/// </remarks>
public sealed class ClassificationListStoreTests : IDisposable
{
    private readonly string _directory =
        Path.Combine(Path.GetTempPath(), "pie-lists-" + Guid.NewGuid().ToString("N"));

    // ------------------------------------------------------------------
    // Shapes the reader claims to understand
    // ------------------------------------------------------------------

    [Fact]
    public void One_domain_per_line_is_read()
    {
        ClassificationListContent content = Read(
            """
            tracker.example.com
            ads.example.net
            """);

        Assert.True(content.Present);
        Assert.Equal(2, content.Entries.Count);
        Assert.Contains("tracker.example.com", content.Entries);
        Assert.Contains("ads.example.net", content.Entries);
    }

    [Fact]
    public void The_hosts_shape_is_read_as_the_name_and_not_the_address()
    {
        ClassificationListContent content = Read(
            """
            0.0.0.0 tracker.example.com
            127.0.0.1	ads.example.net
            """);

        Assert.Equal(2, content.Entries.Count);
        Assert.Contains("tracker.example.com", content.Entries);
        Assert.Contains("ads.example.net", content.Entries);

        // Recording the address instead of the name would produce a list that
        // matches nothing, silently.
        Assert.DoesNotContain("0.0.0.0", content.Entries);
    }

    [Theory]
    [InlineData("TRACKER.Example.COM")]
    [InlineData("tracker.example.com.")]
    [InlineData("   tracker.example.com   ")]
    public void A_name_is_stored_in_one_form_however_it_was_written(string written)
    {
        Assert.Contains("tracker.example.com", Read(written).Entries);
    }

    [Fact]
    public void A_trailing_comment_does_not_become_part_of_the_name()
    {
        ClassificationListContent content = Read("tracker.example.com # known tracker");

        Assert.Contains("tracker.example.com", content.Entries);
        Assert.Single(content.Entries);
    }

    [Fact]
    public void The_same_name_listed_twice_is_one_entry()
    {
        ClassificationListContent content = Read(
            """
            tracker.example.com
            0.0.0.0 tracker.example.com
            """);

        Assert.Single(content.Entries);
    }

    // ------------------------------------------------------------------
    // What the reader does not understand is counted, never guessed
    // ------------------------------------------------------------------

    [Fact]
    public void Comments_and_blank_lines_are_skipped_and_counted()
    {
        ClassificationListContent content = Read(
            """
            # Title of the list
            ! another comment convention

            tracker.example.com
            """);

        Assert.Single(content.Entries);
        Assert.Equal(3, content.UnreadableLines);
    }

    [Fact]
    public void A_shape_the_reader_does_not_claim_to_understand_is_counted_not_guessed()
    {
        // Adblock syntax is not among the shapes supported. Extracting
        // something plausible from it would classify by guesswork.
        ClassificationListContent content = Read(
            """
            ||tracker.example.com^$third-party
            tracker.example.com
            """);

        Assert.Single(content.Entries);
        Assert.Equal(1, content.UnreadableLines);
    }

    [Fact]
    public void A_file_understood_by_nobody_is_reported_as_empty_and_fully_unread()
    {
        // This is the case the checks exist for: the reader returns nothing,
        // and says plainly that it understood nothing, rather than presenting
        // an empty list as a list.
        ClassificationListContent content = Read(
            """
            [Adblock Plus 2.0]
            ||one.example.com^
            ||two.example.com^
            """);

        Assert.Empty(content.Entries);
        Assert.Equal(3, content.UnreadableLines);
    }

    // ------------------------------------------------------------------
    // A list never downloaded is not a list that has aged
    // ------------------------------------------------------------------

    [Fact]
    public void A_list_with_no_file_is_reported_as_absent()
    {
        ClassificationListContent content = Store().Read("Never Downloaded");

        Assert.False(content.Present);
        Assert.Empty(content.Entries);
    }

    [Fact]
    public void A_list_with_no_file_does_not_exist()
    {
        Assert.False(Store().Exists("Never Downloaded"));
    }

    // ------------------------------------------------------------------
    // Replacing a file
    // ------------------------------------------------------------------

    [Fact]
    public void Replacing_a_list_leaves_no_earlier_entry_behind()
    {
        ClassificationListStore store = Store();

        store.Replace("Trackers", "old.example.com");
        store.Replace("Trackers", "new.example.com");

        ClassificationListContent content = store.Read("Trackers");

        Assert.Contains("new.example.com", content.Entries);
        Assert.DoesNotContain("old.example.com", content.Entries);
    }

    [Fact]
    public void Replacing_a_list_leaves_no_working_file_behind()
    {
        ClassificationListStore store = Store();

        store.Replace("Trackers", "tracker.example.com");

        // The update writes beside the file and moves it over. What remains
        // must be the list and nothing else, or a later reading would find a
        // file the store does not know about.
        string[] files = Directory.GetFiles(_directory);

        Assert.Single(files);
        Assert.EndsWith("Trackers.txt", files[0], StringComparison.Ordinal);
    }

    [Fact]
    public void A_list_can_be_removed()
    {
        ClassificationListStore store = Store();

        store.Replace("Trackers", "tracker.example.com");
        store.Remove("Trackers");

        Assert.False(store.Exists("Trackers"));
    }

    [Fact]
    public void Removing_a_list_that_was_never_downloaded_is_not_an_error()
    {
        Store().Remove("Never Downloaded");
    }

    // ------------------------------------------------------------------
    // A name is not a path
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("../escape")]
    [InlineData("sub/list")]
    public void A_name_that_would_reach_outside_the_directory_is_refused(string name)
    {
        // List names can be chosen by the person. A name is a name, and must
        // not be able to address a file elsewhere on the machine.
        Assert.Throws<StorageException>(() => Store().Exists(name));
    }

    // ------------------------------------------------------------------
    // Fixtures
    // ------------------------------------------------------------------

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private ClassificationListStore Store()
    {
        return new ClassificationListStore(new StorageOptions { ListDirectoryPath = _directory });
    }

    private ClassificationListContent Read(string fileContent)
    {
        ClassificationListStore store = Store();

        store.Replace("Trackers", fileContent);

        return store.Read("Trackers");
    }
}
