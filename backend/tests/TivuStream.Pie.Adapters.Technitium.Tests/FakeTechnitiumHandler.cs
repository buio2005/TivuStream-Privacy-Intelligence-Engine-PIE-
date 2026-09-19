using System.Net;
using System.Text;

namespace TivuStream.Pie.Adapters.Technitium.Tests;

/// <summary>
/// Stands in for the Technitium server, answering with prepared bodies.
/// </summary>
/// <remarks>
/// Routes are matched on a fragment of path and query, first match wins, so
/// that two calls sharing a path and differing only in <c>statsType</c> can
/// be told apart.
/// </remarks>
internal sealed class FakeTechnitiumHandler : HttpMessageHandler
{
    private readonly List<(string Fragment, Func<string> Body, HttpStatusCode Status)> _routes = [];

    private readonly List<HttpRequestMessage> _requests = [];

    /// <summary>
    /// Requests received, in order.
    /// </summary>
    internal IReadOnlyList<HttpRequestMessage> Requests => _requests;

    /// <summary>
    /// Makes the server unreachable.
    /// </summary>
    internal bool Unreachable { get; set; }

    internal FakeTechnitiumHandler On(string fragment, string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _routes.Add((fragment, () => body, status));

        return this;
    }

    /// <summary>
    /// Registers a route whose body depends on how many times it was called.
    /// </summary>
    internal FakeTechnitiumHandler OnEachCall(string fragment, params string[] bodies)
    {
        int calls = 0;

        _routes.Add((fragment, () => bodies[Math.Min(calls++, bodies.Length - 1)], HttpStatusCode.OK));

        return this;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _requests.Add(request);

        if (Unreachable)
        {
            throw new HttpRequestException("No route to host.");
        }

        string target = request.RequestUri!.PathAndQuery;

        foreach ((string fragment, Func<string> body, HttpStatusCode status) in _routes)
        {
            if (target.Contains(fragment, StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(body(), Encoding.UTF8, "application/json"),
                });
            }
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
