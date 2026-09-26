using Microsoft.Extensions.DependencyInjection;
using TivuStream.Pie.Adapters.Technitium;
using Xunit;

namespace TivuStream.Pie.Api.Tests;

/// <summary>
/// Verifies how the engine reaches its Data Source.
/// </summary>
/// <remarks>
/// The answers of the Data Source carry the domains the network contacted, and
/// those never leave the device. A proxy configured in the system, often left
/// behind by a VPN client, may forward them elsewhere; one configured but not
/// running makes a reachable source look unreachable. The source is reached
/// directly.
/// </remarks>
public sealed class DataSourceConnectionTests : IDisposable
{
    private readonly PieApplication _app = new();

    public void Dispose()
    {
        _app.Dispose();
    }

    [Fact]
    public void The_data_source_is_reached_without_the_proxy_of_the_system()
    {
        Assert.False(PrimaryHandler(typeof(TechnitiumAdapter).Name).UseProxy);
    }

    [Fact]
    public void Classification_lists_keep_the_proxy_of_the_system()
    {
        // They come from the internet anyway and carry nothing about the
        // network. Where a proxy is required to go out, it keeps working.
        Assert.True(PrimaryHandler("ClassificationUpdateService").UseProxy);
    }

    private SocketsHttpHandler PrimaryHandler(string client)
    {
        HttpMessageHandler handler = _app.Services
            .GetRequiredService<IHttpMessageHandlerFactory>()
            .CreateHandler(client);

        while (handler is DelegatingHandler delegating)
        {
            handler = delegating.InnerHandler!;
        }

        return Assert.IsType<SocketsHttpHandler>(handler);
    }
}
