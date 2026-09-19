using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;
using Xunit;

namespace TivuStream.Pie.Adapters.Technitium.Tests;

/// <summary>
/// Verifies how what the server says becomes the Unified Data Model.
/// </summary>
/// <remarks>
/// The mapping is where a value can be made to look better known than it is.
/// Each check states a qualification the Technitium Integration
/// Specification requires the Adapter to preserve.
/// </remarks>
public sealed class MappingTests
{
    // ------------------------------------------------------------------
    // Statistics
    // ------------------------------------------------------------------

    [Fact]
    public async Task A_name_that_does_not_exist_is_not_counted_as_a_failure_of_the_service()
    {
        Statistics statistics = await ReadStatistics(
            """
            {
              "stats": {
                "totalQueries": 1000, "totalNxDomain": 300,
                "totalServerFailure": 2, "totalRefused": 3, "totalDropped": 4
              }
            }
            """);

        // NXDOMAIN is a correct answer. Counting it would lower the score of
        // a healthy network.
        Assert.Equal(9, statistics.FailedQueries);
    }

    [Fact]
    public async Task The_count_of_unique_domains_is_declared_a_lower_bound()
    {
        Statistics statistics = await ReadStatistics(
            """
            {
              "stats": { "totalQueries": 1000 },
              "topDomains": [ { "name": "a.example", "hits": 5 }, { "name": "b.example", "hits": 1 } ]
            }
            """);

        // The source lists only its most frequent domains, so the real number
        // can only be higher.
        Assert.Equal(2, statistics.UniqueDomains);
        Assert.Equal(MeasurementQuality.LowerBound, statistics.UniqueDomainsQuality);
    }

    [Fact]
    public async Task Only_the_encrypted_transports_are_counted_as_encrypted()
    {
        Statistics statistics = await ReadStatistics(
            """
            {
              "stats": { "totalQueries": 1000 },
              "protocolTypeChartData": {
                "labels": [ "Udp", "Tls", "Https", "Quic", "Tcp" ],
                "datasets": [ { "data": [ 100, 10, 5, 1, 7 ] } ]
              }
            }
            """);

        Assert.Equal(16, statistics.EncryptedQueries);
    }

    // ------------------------------------------------------------------
    // Domains
    // ------------------------------------------------------------------

    [Fact]
    public async Task Blocked_domains_are_read_along_with_the_resolved_ones()
    {
        IReadOnlyList<Domain> domains = await ReadDomains();

        // Leaving the blocked ones out would hide precisely the domains a
        // privacy tool exists to show.
        Assert.Contains(domains, domain => domain.Name == "blocked.example" && domain.Occurrences == 4);
    }

    [Fact]
    public async Task A_domain_present_in_both_lists_is_the_sum_of_the_two()
    {
        IReadOnlyList<Domain> domains = await ReadDomains();

        Domain both = Assert.Single(domains, domain => domain.Name == "both.example");

        Assert.Equal(5, both.Occurrences);
    }

    [Fact]
    public async Task The_adapter_states_no_classification()
    {
        IReadOnlyList<Domain> domains = await ReadDomains();

        // Classification belongs to the Core. An Adapter that filled these in
        // would be passing an opinion off as an observation.
        Assert.All(domains, domain =>
        {
            Assert.Equal(ThreatCategory.Unknown, domain.Category);
            Assert.Null(domain.Reputation);
        });
    }

    [Fact]
    public async Task Sighting_times_are_the_bounds_of_the_period_and_declared_as_such()
    {
        IReadOnlyList<Domain> domains = await ReadDomains();

        // The source reports activity over an interval, not the instant of
        // each event.
        Assert.All(domains, domain =>
        {
            Assert.Equal(TestServer.Window.Start, domain.FirstSeen);
            Assert.Equal(TestServer.Window.End, domain.LastSeen);
            Assert.Equal(MeasurementQuality.PeriodBounded, domain.ObservationQuality);
        });
    }

    // ------------------------------------------------------------------
    // Devices
    // ------------------------------------------------------------------

    [Fact]
    public async Task A_device_with_a_lease_is_identified_by_its_hardware_address_and_says_so()
    {
        IReadOnlyList<Device> devices = await ReadDevices(leases: Lease("10.0.0.5", "AA-BB-CC-DD-EE-FF"));

        Device leased = Assert.Single(devices, device => device.IpAddress == "10.0.0.5");
        Device unleased = Assert.Single(devices, device => device.IpAddress == "10.0.0.9");

        Assert.Equal(DeviceIdentityBasis.HardwareAddress, leased.IdentityBasis);
        Assert.Equal(DeviceIdentityBasis.NetworkAddress, unleased.IdentityBasis);
    }

    [Fact]
    public async Task A_device_that_changes_address_keeps_its_identity_when_a_lease_ties_it_to_hardware()
    {
        IReadOnlyList<Device> before = await ReadDevices(leases: Lease("10.0.0.5", "AA-BB-CC-DD-EE-FF"));
        IReadOnlyList<Device> after = await ReadDevices(
            leases: Lease("10.0.0.77", "aa-bb-cc-dd-ee-ff"),
            leasedClient: "10.0.0.77");

        Guid idBefore = Assert.Single(before, device => device.IpAddress == "10.0.0.5").DeviceId;
        Guid idAfter = Assert.Single(after, device => device.IpAddress == "10.0.0.77").DeviceId;

        Assert.Equal(idBefore, idAfter);
    }

    [Fact]
    public async Task Without_any_lease_information_the_identity_rests_on_the_address_and_says_so()
    {
        // The source may not act as a DHCP server. That is not a failure of
        // the acquisition: the identity is simply on a weaker basis, and the
        // devices declare it.
        IReadOnlyList<Device> devices = await ReadDevices(leases: null);

        Assert.Equal(2, devices.Count);
        Assert.All(devices, device => Assert.Equal(DeviceIdentityBasis.NetworkAddress, device.IdentityBasis));
    }

    // ------------------------------------------------------------------
    // Capabilities
    // ------------------------------------------------------------------

    [Fact]
    public async Task The_base_level_is_declared_when_the_account_can_read_the_dashboard()
    {
        DataSource source = await Describe(dashboard: true, settings: false, queryLogs: false);

        Assert.Equal(["Statistics", "Device", "Domain"], source.Capabilities);
    }

    [Fact]
    public async Task Configuration_is_declared_only_when_the_account_can_read_the_settings()
    {
        DataSource without = await Describe(dashboard: true, settings: false, queryLogs: false);
        DataSource with = await Describe(dashboard: true, settings: true, queryLogs: false);

        Assert.DoesNotContain("SourceConfiguration", without.Capabilities);
        Assert.Contains("SourceConfiguration", with.Capabilities);
    }

    [Fact]
    public async Task Domain_activity_is_declared_only_when_the_logging_component_is_installed()
    {
        DataSource without = await Describe(dashboard: true, settings: true, queryLogs: false);
        DataSource with = await Describe(dashboard: true, settings: true, queryLogs: true);

        // The Adapter can always do it; what the capability says is whether
        // this instance offers it today.
        Assert.DoesNotContain("DomainActivity", without.Capabilities);
        Assert.Contains("DomainActivity", with.Capabilities);
    }

    [Fact]
    public async Task No_capability_is_declared_when_the_account_cannot_read_the_dashboard()
    {
        DataSource source = await Describe(dashboard: false, settings: true, queryLogs: true);

        Assert.Empty(source.Capabilities);
    }

    // ------------------------------------------------------------------
    // Configuration
    // ------------------------------------------------------------------

    [Fact]
    public async Task Configuration_reports_only_what_bears_on_privacy_and_security()
    {
        FakeTechnitiumHandler server = new FakeTechnitiumHandler()
            .On(
                "/api/settings/get",
                TestServer.Ok(
                    """
                    {
                      "dnssecValidation": true,
                      "enableDnsOverTls": true, "enableDnsOverHttps": false, "enableDnsOverQuic": true,
                      "qnameMinimization": true, "eDnsClientSubnet": false,
                      "enableBlocking": true,
                      "blockListUrls": [ "https://lists.test/a.txt", "https://lists.test/b.txt" ],
                      "blockListUpdateIntervalHours": 24
                    }
                    """));

        SourceConfiguration configuration = await TestServer.AdapterFor(server)
            .GetConfigurationAsync(CancellationToken.None);

        Assert.True(configuration.DnssecValidationEnabled);
        Assert.Equal(["Tls", "Quic"], configuration.EncryptedTransports);
        Assert.True(configuration.QueryMinimisationEnabled);
        Assert.False(configuration.ClientSubnetForwardingEnabled);
        Assert.True(configuration.FilteringEnabled);
        Assert.Equal(2, configuration.FilterListCount);
        Assert.Equal(24, configuration.FilterListUpdateIntervalHours);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static async Task<Statistics> ReadStatistics(string payload)
    {
        FakeTechnitiumHandler server = new FakeTechnitiumHandler()
            .On("/api/dashboard/stats/get?", TestServer.Ok(payload))
            .On("/api/user/session/get", TestServer.Session());

        return await TestServer.AdapterFor(server).GetStatisticsAsync(TestServer.Window, CancellationToken.None);
    }

    private static async Task<IReadOnlyList<Domain>> ReadDomains()
    {
        FakeTechnitiumHandler server = new FakeTechnitiumHandler()
            .On(
                "statsType=TopDomains",
                TestServer.Ok(
                    """
                    { "topDomains": [ { "name": "resolved.example", "hits": 7 }, { "name": "both.example", "hits": 2 } ] }
                    """))
            .On(
                "statsType=TopBlockedDomains",
                TestServer.Ok(
                    """
                    { "topBlockedDomains": [ { "name": "blocked.example", "hits": 4 }, { "name": "both.example", "hits": 3 } ] }
                    """));

        return await TestServer.AdapterFor(server).GetDomainsAsync(TestServer.Window, CancellationToken.None);
    }

    private static async Task<IReadOnlyList<Device>> ReadDevices(string? leases, string leasedClient = "10.0.0.5")
    {
        FakeTechnitiumHandler server = new FakeTechnitiumHandler()
            .On(
                "statsType=TopClients",
                TestServer.Ok(
                    $$"""
                    {
                      "topClients": [
                        { "name": "{{leasedClient}}", "domain": "tv.lan", "hits": 40 },
                        { "name": "10.0.0.9", "hits": 10 }
                      ]
                    }
                    """));

        // With no route registered the fake answers 404, which is how an
        // instance that is not a DHCP server presents itself.
        if (leases is not null)
        {
            server.On("/api/dhcp/leases/list", leases);
        }

        return await TestServer.AdapterFor(server).GetDevicesAsync(TestServer.Window, CancellationToken.None);
    }

    private static string Lease(string address, string hardwareAddress)
    {
        return TestServer.Ok(
            $$"""{ "leases": [ { "address": "{{address}}", "hardwareAddress": "{{hardwareAddress}}", "hostName": "tv" } ] }""");
    }

    private static async Task<DataSource> Describe(bool dashboard, bool settings, bool queryLogs)
    {
        FakeTechnitiumHandler server = new FakeTechnitiumHandler()
            .On("/api/user/session/get", TestServer.Session(dashboard, settings))
            .On("/api/apps/list", TestServer.Apps(queryLogs));

        return await TestServer.AdapterFor(server).DescribeAsync(CancellationToken.None);
    }
}
