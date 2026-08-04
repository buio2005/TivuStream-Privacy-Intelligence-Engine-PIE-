using System.Globalization;

namespace TivuStream.Pie.Storage;

/// <summary>
/// Reads and writes the files holding the domains of the classification lists.
/// </summary>
/// <remarks>
/// The file keeps the list in the form it was downloaded in. Nothing is
/// normalised on the way in, so that what the person opens is what the source
/// published.
/// </remarks>
public sealed class ClassificationListStore
{
    private readonly string _directoryPath;

    /// <summary>
    /// Creates the store.
    /// </summary>
    /// <param name="options">Settings governing where data is kept.</param>
    public ClassificationListStore(StorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _directoryPath = options.ListDirectoryPath;
    }

    /// <summary>
    /// Indicates whether a list has ever been downloaded.
    /// </summary>
    /// <param name="listName">Name of the list.</param>
    /// <returns><c>true</c> when the file exists.</returns>
    public bool Exists(string listName)
    {
        return File.Exists(PathOf(listName));
    }

    /// <summary>
    /// Reads the domains a list contains.
    /// </summary>
    /// <remarks>
    /// Lines that carry no domain are skipped rather than guessed at. Comment
    /// lines, blank lines and entries the reader does not recognise are
    /// counted, so that a list understood in part is never reported as a list
    /// understood in full.
    /// </remarks>
    /// <param name="listName">Name of the list.</param>
    /// <returns>What was read, and what was skipped.</returns>
    public ClassificationListContent Read(string listName)
    {
        string path = PathOf(listName);

        if (!File.Exists(path))
        {
            return ClassificationListContent.Absent;
        }

        HashSet<string> entries = new(StringComparer.Ordinal);
        int skipped = 0;

        foreach (string line in File.ReadLines(path))
        {
            string? entry = ParseEntry(line);

            if (entry is null)
            {
                skipped++;
                continue;
            }

            entries.Add(entry);
        }

        return new ClassificationListContent
        {
            Present = true,
            Entries = entries,
            UnreadableLines = skipped,
        };
    }

    /// <summary>
    /// Replaces the file of a list with newly downloaded content.
    /// </summary>
    /// <remarks>
    /// The content is written beside the file and then moved over it, so that
    /// an interrupted update leaves the previous list intact rather than a
    /// truncated one. A list that could not be updated is less useful than a
    /// recent one and more useful than none.
    /// </remarks>
    /// <param name="listName">Name of the list.</param>
    /// <param name="content">Content as published by the source.</param>
    public void Replace(string listName, string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        string path = PathOf(listName);
        string temporaryPath = path + ".incoming";

        Directory.CreateDirectory(_directoryPath);

        try
        {
            File.WriteAllText(temporaryPath, content);
            File.Move(temporaryPath, path, overwrite: true);
        }
        catch (IOException exception)
        {
            throw new StorageException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The file of list '{listName}' could not be replaced."),
                exception);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    /// <summary>
    /// Removes the file of a list.
    /// </summary>
    /// <param name="listName">Name of the list.</param>
    public void Remove(string listName)
    {
        string path = PathOf(listName);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Extracts the domain a line carries, when it carries one.
    /// </summary>
    /// <remarks>
    /// Two shapes are understood: one domain per line, and the hosts shape
    /// where an address precedes the domain. Anything else returns nothing and
    /// is counted as unread.
    /// </remarks>
    private static string? ParseEntry(string line)
    {
        ReadOnlySpan<char> trimmed = line.AsSpan().Trim();

        if (trimmed.IsEmpty || trimmed[0] is '#' or '!' or ';')
        {
            return null;
        }

        int comment = trimmed.IndexOfAny('#', '!');

        if (comment >= 0)
        {
            trimmed = trimmed[..comment].Trim();
        }

        if (trimmed.IsEmpty)
        {
            return null;
        }

        Span<Range> parts = stackalloc Range[3];
        int count = trimmed.SplitAny(parts, " \t", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (count is 0)
        {
            return null;
        }

        // In the hosts shape the address comes first and the name second.
        // A line with more than two fields is not one this reader claims to
        // understand.
        ReadOnlySpan<char> candidate = count switch
        {
            1 => trimmed[parts[0]],
            2 => trimmed[parts[1]],
            _ => [],
        };

        return IsDomainName(candidate)
            ? candidate.ToString().TrimEnd('.').ToLowerInvariant()
            : null;
    }

    private static bool IsDomainName(ReadOnlySpan<char> candidate)
    {
        if (candidate.IsEmpty || candidate.Length > 253 || !candidate.Contains('.'))
        {
            return false;
        }

        foreach (char character in candidate)
        {
            bool admitted = char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_';

            if (!admitted)
            {
                return false;
            }
        }

        return true;
    }

    private string PathOf(string listName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(listName);

        if (listName.AsSpan().IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new StorageException(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"The name '{listName}' cannot be used as a file name."));
        }

        return Path.Combine(_directoryPath, listName + ".txt");
    }
}
