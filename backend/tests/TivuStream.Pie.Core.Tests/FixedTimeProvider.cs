namespace TivuStream.Pie.Core.Tests;

/// <summary>
/// Time provider that always reports the same instant.
/// </summary>
/// <remarks>
/// Written here rather than taken from a package: it is a few lines, and a
/// dependency added for that alone would not be justified.
/// </remarks>
internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _instant;

    internal FixedTimeProvider(DateTimeOffset instant)
    {
        _instant = instant;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _instant;
    }
}
