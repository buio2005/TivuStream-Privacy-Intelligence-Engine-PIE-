using TivuStream.Pie.Adapters.Technitium.Responses;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Adapters.Technitium;

/// <summary>
/// Adapter for Technitium DNS Server.
/// </summary>
/// <remarks>
/// Covers the base level of integration, available on any installation with
/// no additional component: statistics, devices and domains.
/// <para>
/// Correlating devices and domains requires an optional component of
/// Technitium and is therefore not served by this Adapter, which does not
/// implement the corresponding capability interface.
/// </para>
/// </remarks>
public sealed class TechnitiumAdapter : IStatisticsSource, IDeviceSource, IDomainSource
{
    private const int TopListLimit = 1000;

    private static readonly string[] EncryptedProtocols = ["Tls", "Https", "Quic"];

    private readonly TechnitiumClient _client;
    private readonly Guid _dataSourceId;

    /// <summary>
    /// Creates the Adapter.
    /// </summary>
    /// <param name="httpClient">Client used to reach the instance.</param>
    /// <param name="options">Settings of the instance.</param>
    public TechnitiumAdapter(HttpClient httpClient, TechnitiumOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);

        _client = new TechnitiumClient(httpClient, options);
        _dataSourceId = options.DataSourceId;
    }

    /// <inheritdoc />
    public string Provider => "Technitium DNS Server";

    /// <inheritdoc />
    public async Task<DataSource> DescribeAsync(CancellationToken cancellationToken)
    {
        SessionResponse session = await _client
            .GetRootAsync<SessionResponse>("/api/user/session/get", cancellationToken)
            .ConfigureAwait(false);

        SessionInfo info = session.Info
            ?? throw new AdapterException("The Technitium instance did not describe itself.");

        return new DataSource
        {
            Id = _dataSourceId,
            Name = info.DnsServerDomain ?? Provider,
            Provider = Provider,
            Version = info.Version ?? string.Empty,
            Status = DataSourceStatus.Online,
            Capabilities = ResolveCapabilities(info),
            LastUpdate = DateTimeOffset.UtcNow,
        };
    }

    /// <inheritdoc />
    public async Task<Statistics> GetStatisticsAsync(AcquisitionWindow window, CancellationToken cancellationToken)
    {
        StatsResponse stats = await _client
            .GetAsync<StatsResponse>(BuildStatsUrl(window), cancellationToken)
            .ConfigureAwait(false);

        SessionResponse session = await _client
            .GetRootAsync<SessionResponse>("/api/user/session/get", cancellationToken)
            .ConfigureAwait(false);

        StatsCounters counters = stats.Stats
            ?? throw new AdapterException("The Technitium instance returned no statistics.");

        return new Statistics
        {
            TotalQueries = counters.TotalQueries,
            BlockedQueries = counters.TotalBlocked,
            CachedQueries = counters.TotalCached,

            // A non existing domain is a valid answer, not a failure of the
            // service, and is therefore left out of this counter.
            FailedQueries = counters.TotalServerFailure + counters.TotalRefused + counters.TotalDropped,

            // The source exposes only the most frequent domains, so this
            // value is an approximation and must be presented as such.
            UniqueDomains = stats.TopDomains?.Count ?? 0,

            ActiveDevices = counters.TotalClients,
            EncryptedQueries = SumEncryptedQueries(stats.ProtocolTypeChartData),
            DnssecEnabled = session.Info?.DnssecValidation ?? false,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Device>> GetDevicesAsync(AcquisitionWindow window, CancellationToken cancellationToken)
    {
        TopClientsResponse response = await _client
            .GetAsync<TopClientsResponse>(BuildTopUrl(window, "TopClients"), cancellationToken)
            .ConfigureAwait(false);

        List<Device> devices = [];

        foreach (TopClient client in response.TopClients ?? [])
        {
            if (string.IsNullOrWhiteSpace(client.Name))
            {
                continue;
            }

            devices.Add(new Device
            {
                DeviceId = DeviceIdentity.FromAddress(client.Name),
                Hostname = string.IsNullOrWhiteSpace(client.Domain) ? null : client.Domain,
                IpAddress = client.Name,

                // Not exposed by the source. The Device Engine may enrich them.
                MacAddress = null,
                Vendor = null,
                OperatingSystem = null,

                // The source reports activity over an interval, not the
                // instants of first and last observation. The bounds of the
                // acquired interval are therefore used.
                FirstSeen = window.Start,
                LastSeen = window.End,

                Status = DeviceStatus.Active,
            });
        }

        return devices;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Domain>> GetDomainsAsync(AcquisitionWindow window, CancellationToken cancellationToken)
    {
        TopDomainsResponse response = await _client
            .GetAsync<TopDomainsResponse>(BuildTopUrl(window, "TopDomains"), cancellationToken)
            .ConfigureAwait(false);

        List<Domain> domains = [];

        foreach (TopDomain domain in response.TopDomains ?? [])
        {
            if (string.IsNullOrWhiteSpace(domain.Name))
            {
                continue;
            }

            domains.Add(new Domain
            {
                Name = domain.Name,

                // Classification and reputation belong to the Threat Engine.
                // The Adapter states neither.
                Category = ThreatCategory.Unknown,
                Reputation = null,

                FirstSeen = window.Start,
                LastSeen = window.End,
                Occurrences = domain.Hits,
            });
        }

        return domains;
    }

    private static string BuildStatsUrl(AcquisitionWindow window)
    {
        return "/api/dashboard/stats/get?type=Custom&utc=true"
            + $"&start={Uri.EscapeDataString(TechnitiumClient.FormatInstant(window.Start))}"
            + $"&end={Uri.EscapeDataString(TechnitiumClient.FormatInstant(window.End))}";
    }

    private static string BuildTopUrl(AcquisitionWindow window, string statsType)
    {
        return $"/api/dashboard/stats/getTop?type=Custom&statsType={statsType}&limit={TopListLimit}"
            + $"&start={Uri.EscapeDataString(TechnitiumClient.FormatInstant(window.Start))}"
            + $"&end={Uri.EscapeDataString(TechnitiumClient.FormatInstant(window.End))}";
    }

    private static long SumEncryptedQueries(ChartData? protocols)
    {
        if (protocols?.Labels is null || protocols.Datasets is null || protocols.Datasets.Count == 0)
        {
            return 0;
        }

        List<long>? values = protocols.Datasets[0].Data;

        if (values is null)
        {
            return 0;
        }

        long total = 0;

        for (int index = 0; index < protocols.Labels.Count && index < values.Count; index++)
        {
            if (EncryptedProtocols.Contains(protocols.Labels[index], StringComparer.OrdinalIgnoreCase))
            {
                total += values[index];
            }
        }

        return total;
    }

    private static IReadOnlyList<string> ResolveCapabilities(SessionInfo info)
    {
        // A capability is declared only when the account behind the token can
        // actually read the data it depends on.
        bool canReadDashboard =
            info.Permissions is not null
            && info.Permissions.TryGetValue("Dashboard", out SessionPermission? dashboard)
            && dashboard.CanView;

        if (!canReadDashboard)
        {
            return [];
        }

        return [nameof(Statistics), nameof(Device), nameof(Domain)];
    }
}
