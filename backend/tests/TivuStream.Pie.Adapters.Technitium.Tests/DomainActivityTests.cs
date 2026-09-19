using TivuStream.Pie.Model.Entities;
using Xunit;

namespace TivuStream.Pie.Adapters.Technitium.Tests;

/// <summary>
/// Verifies the commitment that the browsing history of a device does not
/// travel beyond the Adapter.
/// </summary>
/// <remarks>
/// Technitium Integration Specification, Aggregation Responsibility: the
/// query log is aggregated while its pages are read, never held in full, and
/// the Core receives only consolidated activity.
/// </remarks>
public sealed class DomainActivityTests
{
    private const string FirstPage =
        """
        {
          "status": "ok",
          "response": {
            "pageNumber": 1, "totalPages": 2, "totalEntries": 5,
            "entries": [
              { "timestamp": "2026-09-01T10:20:00Z", "clientIpAddress": "10.0.0.5", "protocol": "Udp",
                "responseType": "Recursive",  "qname": "tracker.example" },
              { "timestamp": "2026-09-01T10:00:10Z", "clientIpAddress": "10.0.0.5", "protocol": "Udp",
                "responseType": "Cached",     "qname": "tracker.example" },
              { "timestamp": "2026-09-01T10:05:00Z", "clientIpAddress": "10.0.0.5", "protocol": "Udp",
                "responseType": "Blocked",    "qname": "tracker.example" }
            ]
          }
        }
        """;

    private const string SecondPage =
        """
        {
          "status": "ok",
          "response": {
            "pageNumber": 2, "totalPages": 2, "totalEntries": 5,
            "entries": [
              { "timestamp": "2026-09-01T10:40:00Z", "clientIpAddress": "10.0.0.5", "protocol": "Udp",
                "responseType": "Cached",     "qname": "tracker.example" },
              { "timestamp": "2026-09-01T10:30:00Z", "clientIpAddress": "10.0.0.6", "protocol": "Udp",
                "responseType": "Cached",     "qname": "tracker.example" }
            ]
          }
        }
        """;

    [Fact]
    public async Task Queries_are_consolidated_across_every_page_of_the_log()
    {
        FakeTechnitiumHandler server = ServerWithLogs();

        IReadOnlyList<DomainActivity> activities = await TestServer.AdapterFor(server)
            .GetDomainActivitiesAsync(TestServer.Window, CancellationToken.None);

        // Five logged queries leave the Adapter as three facts. What arrives
        // is how often, not when each one happened.
        Assert.Equal(3, activities.Count);
        Assert.Equal(5, activities.Sum(activity => activity.QueryCount));

        // Both pages were read, otherwise the second device would be missing.
        Assert.Equal(2, server.Requests.Count(request => request.RequestUri!.AbsolutePath == "/api/logs/query"));
    }

    [Fact]
    public async Task First_and_last_sighting_span_the_pages_whatever_the_order_of_arrival()
    {
        FakeTechnitiumHandler server = ServerWithLogs();

        IReadOnlyList<DomainActivity> activities = await TestServer.AdapterFor(server)
            .GetDomainActivitiesAsync(TestServer.Window, CancellationToken.None);

        DomainActivity direct = Assert.Single(
            activities,
            activity => activity.QueryCount == 3 && !activity.Blocked);

        Assert.Equal(new DateTimeOffset(2026, 9, 1, 10, 0, 10, TimeSpan.Zero), direct.FirstSeen);
        Assert.Equal(new DateTimeOffset(2026, 9, 1, 10, 40, 0, TimeSpan.Zero), direct.LastSeen);
    }

    [Fact]
    public async Task A_blocked_query_is_not_merged_with_one_that_was_answered()
    {
        FakeTechnitiumHandler server = ServerWithLogs();

        IReadOnlyList<DomainActivity> activities = await TestServer.AdapterFor(server)
            .GetDomainActivitiesAsync(TestServer.Window, CancellationToken.None);

        // A device that reached a domain both directly and through a block
        // produced two different facts. Merging them would state something
        // that did not happen.
        DomainActivity blocked = Assert.Single(activities, activity => activity.Blocked);

        Assert.Equal(1, blocked.QueryCount);
    }

    [Fact]
    public async Task Two_devices_reaching_the_same_domain_stay_two_facts()
    {
        FakeTechnitiumHandler server = ServerWithLogs();

        IReadOnlyList<DomainActivity> activities = await TestServer.AdapterFor(server)
            .GetDomainActivitiesAsync(TestServer.Window, CancellationToken.None);

        Assert.Equal(2, activities.Select(activity => activity.DeviceId).Distinct().Count());
    }

    [Fact]
    public async Task Without_the_logging_component_the_answer_is_a_refusal_and_not_an_empty_list()
    {
        FakeTechnitiumHandler server = new FakeTechnitiumHandler()
            .On("/api/apps/list", TestServer.Apps(queryLogsInstalled: false));

        // An empty list would read as "the network contacted nothing". The
        // truth is that nothing was looked at.
        await Assert.ThrowsAsync<AdapterException>(
            () => TestServer.AdapterFor(server).GetDomainActivitiesAsync(TestServer.Window, CancellationToken.None));
    }

    // ------------------------------------------------------------------
    // Identity of the device an activity belongs to
    // ------------------------------------------------------------------

    private const string OnePageForTwoDevices =
        """
        {
          "status": "ok",
          "response": {
            "pageNumber": 1, "totalPages": 1, "totalEntries": 2,
            "entries": [
              { "timestamp": "2026-09-01T10:10:00Z", "clientIpAddress": "10.0.0.5", "protocol": "Udp",
                "responseType": "Cached", "qname": "tracker.example" },
              { "timestamp": "2026-09-01T10:20:00Z", "clientIpAddress": "10.0.0.9", "protocol": "Udp",
                "responseType": "Cached", "qname": "tracker.example" }
            ]
          }
        }
        """;

    private const string TwoClients =
        """
        { "topClients": [ { "name": "10.0.0.5", "hits": 40 }, { "name": "10.0.0.9", "hits": 10 } ] }
        """;

    [Fact]
    public async Task An_activity_carries_the_identifier_of_its_device_when_the_identity_rests_on_hardware()
    {
        // 10.0.0.5 holds a lease, so its device is identified by hardware
        // address; 10.0.0.9 does not, so by network address. The activity of
        // each must carry the identifier of the device it belongs to, or the
        // two can never be put together.
        FakeTechnitiumHandler server = new FakeTechnitiumHandler()
            .On("/api/apps/list", TestServer.Apps(queryLogsInstalled: true))
            .On("/api/logs/query", OnePageForTwoDevices)
            .On("statsType=TopClients", TestServer.Ok(TwoClients))
            .On(
                "/api/dhcp/leases/list",
                TestServer.Ok(
                    """{ "leases": [ { "address": "10.0.0.5", "hardwareAddress": "AA-BB-CC-DD-EE-FF" } ] }"""));

        TechnitiumAdapter adapter = TestServer.AdapterFor(server);

        IReadOnlyList<Device> devices = await adapter.GetDevicesAsync(TestServer.Window, CancellationToken.None);
        IReadOnlyList<DomainActivity> activities = await adapter.GetDomainActivitiesAsync(TestServer.Window, CancellationToken.None);

        Assert.Equal(
            devices.Select(device => device.DeviceId).Order(),
            activities.Select(activity => activity.DeviceId).Order());
    }

    [Fact]
    public async Task An_activity_still_matches_its_device_when_the_source_offers_no_lease_information()
    {
        // No route for the leases: the source is not a DHCP server.
        FakeTechnitiumHandler server = new FakeTechnitiumHandler()
            .On("/api/apps/list", TestServer.Apps(queryLogsInstalled: true))
            .On("/api/logs/query", OnePageForTwoDevices)
            .On("statsType=TopClients", TestServer.Ok(TwoClients));

        TechnitiumAdapter adapter = TestServer.AdapterFor(server);

        IReadOnlyList<Device> devices = await adapter.GetDevicesAsync(TestServer.Window, CancellationToken.None);
        IReadOnlyList<DomainActivity> activities = await adapter.GetDomainActivitiesAsync(TestServer.Window, CancellationToken.None);

        Assert.Equal(
            devices.Select(device => device.DeviceId).Order(),
            activities.Select(activity => activity.DeviceId).Order());
    }

    private static FakeTechnitiumHandler ServerWithLogs()
    {
        return new FakeTechnitiumHandler()
            .On("/api/apps/list", TestServer.Apps(queryLogsInstalled: true))
            .OnEachCall("/api/logs/query", FirstPage, SecondPage);
    }
}
