using System.Net;
using Xunit;

namespace TivuStream.Pie.Adapters.Technitium.Tests;

/// <summary>
/// Verifies how the Adapter reaches the server and reports its failures.
/// </summary>
/// <remarks>
/// Technitium Integration Specification: the token travels as a bearer
/// credential, and the outcome of a call is in its body, since a failed call
/// can arrive with HTTP 200.
/// </remarks>
public sealed class TransportTests
{
    private const string StatsPath = "/api/dashboard/stats/get";

    [Fact]
    public async Task Every_request_carries_the_token_as_a_bearer_credential()
    {
        FakeTechnitiumHandler server = new FakeTechnitiumHandler()
            .On(StatsPath, TestServer.Ok("""{ "stats": { "totalQueries": 10 } }"""))
            .On("/api/user/session/get", TestServer.Session());

        await TestServer.AdapterFor(server).GetStatisticsAsync(TestServer.Window, CancellationToken.None);

        Assert.NotEmpty(server.Requests);

        Assert.All(server.Requests, request =>
        {
            Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
            Assert.Equal(TestServer.Token, request.Headers.Authorization?.Parameter);
        });
    }

    [Fact]
    public async Task A_failure_reported_with_http_200_is_still_a_failure()
    {
        FakeTechnitiumHandler server = new FakeTechnitiumHandler()
            .On(StatsPath, """{ "status": "error", "errorMessage": "Invalid token." }""");

        AdapterException failure = await Assert.ThrowsAsync<AdapterException>(
            () => TestServer.AdapterFor(server).GetStatisticsAsync(TestServer.Window, CancellationToken.None));

        Assert.Contains("Invalid token.", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Diagnostic_details_of_the_server_do_not_travel_beyond_the_adapter()
    {
        FakeTechnitiumHandler server = new FakeTechnitiumHandler()
            .On(
                StatsPath,
                """
                {
                  "status": "error",
                  "errorMessage": "Something failed.",
                  "stackTrace": "at Technitium.Internal.Frame()",
                  "innerErrorMessage": "inner detail of the server"
                }
                """);

        AdapterException failure = await Assert.ThrowsAsync<AdapterException>(
            () => TestServer.AdapterFor(server).GetStatisticsAsync(TestServer.Window, CancellationToken.None));

        Assert.DoesNotContain("Technitium.Internal", failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("inner detail", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_token_never_appears_in_a_failure_message()
    {
        FakeTechnitiumHandler server = new FakeTechnitiumHandler()
            .On(StatsPath, "{}", HttpStatusCode.InternalServerError);

        AdapterException failure = await Assert.ThrowsAsync<AdapterException>(
            () => TestServer.AdapterFor(server).GetStatisticsAsync(TestServer.Window, CancellationToken.None));

        Assert.DoesNotContain(TestServer.Token, failure.Message, StringComparison.Ordinal);
        Assert.Contains("500", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unreachable_server_is_reported_in_the_vocabulary_of_the_project()
    {
        FakeTechnitiumHandler server = new() { Unreachable = true };

        AdapterException failure = await Assert.ThrowsAsync<AdapterException>(
            () => TestServer.AdapterFor(server).GetStatisticsAsync(TestServer.Window, CancellationToken.None));

        Assert.Equal(AdapterFailure.Unreachable, failure.Failure);
    }

    [Fact]
    public async Task A_token_the_server_does_not_know_is_reported_as_refused_credentials()
    {
        // Technitium answers an unknown token with a status of its own, and
        // an HTTP status of 200.
        FakeTechnitiumHandler server = new FakeTechnitiumHandler()
            .On("/api/user/session/get", """{ "status": "invalid-token", "errorMessage": "Invalid token or session expired." }""");

        AdapterException failure = await Assert.ThrowsAsync<AdapterException>(
            () => TestServer.AdapterFor(server).DescribeAsync(CancellationToken.None));

        Assert.Equal(AdapterFailure.CredentialsRefused, failure.Failure);
        Assert.DoesNotContain(TestServer.Token, failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_answer_of_401_is_reported_as_refused_credentials()
    {
        FakeTechnitiumHandler server = new FakeTechnitiumHandler()
            .On("/api/user/session/get", "{}", System.Net.HttpStatusCode.Unauthorized);

        AdapterException failure = await Assert.ThrowsAsync<AdapterException>(
            () => TestServer.AdapterFor(server).DescribeAsync(CancellationToken.None));

        Assert.Equal(AdapterFailure.CredentialsRefused, failure.Failure);
    }

    [Fact]
    public async Task An_answer_that_cannot_be_read_is_a_failure_and_not_an_empty_result()
    {
        FakeTechnitiumHandler server = new FakeTechnitiumHandler()
            .On(StatsPath, "<html>a proxy error page</html>");

        await Assert.ThrowsAsync<AdapterException>(
            () => TestServer.AdapterFor(server).GetStatisticsAsync(TestServer.Window, CancellationToken.None));
    }
}
