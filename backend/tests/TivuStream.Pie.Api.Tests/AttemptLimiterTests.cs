using System.Net;
using TivuStream.Pie.Api.Authentication;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies how the counters of failed attempts grow, block and are forgotten.
/// </summary>
/// <remarks>
/// Authentication Specification, Attempts and V8: five failures per source,
/// ten per account; a delay of thirty seconds that doubles up to fifteen
/// minutes; a success clears the account's counter; a counter is forgotten
/// fifteen minutes after its delay has ended (D11).
/// </remarks>
public sealed class AttemptLimiterTests
{
    private static readonly IPAddress Source = IPAddress.Parse("192.168.1.20");

    private readonly TestClock _clock = new(PieApplication.Now);
    private readonly AttemptLimiter _limiter;

    public AttemptLimiterTests()
    {
        _limiter = new AttemptLimiter(_clock);
    }

    [Fact]
    public void A_source_is_let_through_until_its_fifth_failure_and_then_waits_thirty_seconds()
    {
        Fail(4, Source, "maria");

        Assert.Null(_limiter.RetryAfter(Source, "maria"));

        Fail(1, Source, "maria");

        Assert.Equal(TimeSpan.FromSeconds(30), _limiter.RetryAfter(Source, "someone-else"));

        _clock.Advance(TimeSpan.FromSeconds(30));

        Assert.Null(_limiter.RetryAfter(Source, "maria"));
    }

    [Fact]
    public void The_delay_doubles_at_every_further_failure_and_stops_at_fifteen_minutes()
    {
        List<TimeSpan> delays = [];

        Fail(4, Source, account: null);

        for (int failure = 0; failure < 8; failure++)
        {
            Fail(1, Source, account: null);

            TimeSpan delay = _limiter.RetryAfter(Source, account: null)!.Value;

            delays.Add(delay);
            _clock.Advance(delay);
        }

        Assert.Equal(
            [30, 60, 120, 240, 480, 900, 900, 900],
            delays.Select(delay => (int)delay.TotalSeconds));
    }

    [Fact]
    public void An_account_waits_after_ten_failures_from_different_sources()
    {
        for (int index = 1; index <= 9; index++)
        {
            _limiter.RecordFailure(IPAddress.Parse($"10.0.0.{index}"), "maria");
        }

        Assert.Null(_limiter.RetryAfter(IPAddress.Parse("10.0.0.99"), "maria"));

        _limiter.RecordFailure(IPAddress.Parse("10.0.0.10"), "maria");

        // From anywhere, and only for that name.
        Assert.Equal(TimeSpan.FromSeconds(30), _limiter.RetryAfter(IPAddress.Parse("10.0.0.99"), "maria"));
        Assert.Null(_limiter.RetryAfter(IPAddress.Parse("10.0.0.99"), "giulia"));
    }

    [Fact]
    public void A_success_clears_the_account_and_leaves_the_source()
    {
        Fail(4, Source, "maria");

        _limiter.RecordSuccess("maria");

        // The account starts over; the source does not, or anyone holding an
        // account could clear the way for guessing the others.
        Fail(1, Source, "giulia");

        Assert.NotNull(_limiter.RetryAfter(Source, account: null));
    }

    [Fact]
    public void A_counter_is_forgotten_fifteen_minutes_after_its_delay_has_ended_and_not_before()
    {
        Fail(5, Source, account: null);

        // Thirty seconds of delay, then almost fifteen minutes of quiet.
        _clock.Advance(TimeSpan.FromSeconds(30) + TimeSpan.FromMinutes(15) - TimeSpan.FromSeconds(1));

        Fail(1, Source, account: null);

        // Still remembered: the sixth failure doubles the delay.
        Assert.Equal(TimeSpan.FromSeconds(60), _limiter.RetryAfter(Source, account: null));

        _clock.Advance(TimeSpan.FromSeconds(60) + TimeSpan.FromMinutes(15));

        Fail(1, Source, account: null);

        // Forgotten: one failure is again just one.
        Assert.Null(_limiter.RetryAfter(Source, account: null));
    }

    [Fact]
    public void The_longest_delay_does_not_end_with_an_empty_counter()
    {
        Fail(10, Source, account: null);

        TimeSpan delay = _limiter.RetryAfter(Source, account: null)!.Value;

        Assert.Equal(AttemptLimiter.MaximumDelay, delay);

        _clock.Advance(delay);

        // Counting the quiet from the last failure would forget everything at
        // this very moment.
        Fail(1, Source, account: null);

        Assert.Equal(AttemptLimiter.MaximumDelay, _limiter.RetryAfter(Source, account: null));
    }

    [Theory]
    [InlineData("2001:db8:1:2::10", "2001:db8:1:2:ffff::1", true)]
    [InlineData("2001:db8:1:2::10", "2001:db8:1:3::10", false)]
    [InlineData("192.168.1.20", "::ffff:192.168.1.20", true)]
    [InlineData("192.168.1.20", "192.168.1.21", false)]
    public void Addresses_are_counted_as_what_one_device_holds(string first, string second, bool together)
    {
        Assert.Equal(
            together,
            AttemptLimiter.SourceKey(IPAddress.Parse(first)) == AttemptLimiter.SourceKey(IPAddress.Parse(second)));
    }

    [Fact]
    public void Connections_whose_address_is_unknown_share_one_counter()
    {
        Fail(5, source: null, account: null);

        Assert.NotNull(_limiter.RetryAfter(source: null, account: null));
        Assert.Null(_limiter.RetryAfter(Source, account: null));
    }

    private void Fail(int times, IPAddress? source, string? account)
    {
        for (int index = 0; index < times; index++)
        {
            _limiter.RecordFailure(source, account);
        }
    }
}
