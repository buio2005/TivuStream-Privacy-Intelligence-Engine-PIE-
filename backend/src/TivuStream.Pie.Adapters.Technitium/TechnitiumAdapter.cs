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
public sealed class TechnitiumAdapter
    : IStatisticsSource, IDeviceSource, IDomainSource, IDomainActivitySource, ISourceConfigurationSource
{
    private const int TopListLimit = 1000;

    private const int LogEntriesPerPage = 1000;

    // The logging application keeps a limited number of records, well below
    // this bound. The limit exists so that a misconfigured retention cannot
    // turn a single acquisition into an unbounded read.
    private const int MaxLogPages = 100;

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

        QueryLogsApp? queryLogs = await FindQueryLogsAsync(cancellationToken).ConfigureAwait(false);

        return new DataSource
        {
            Id = _dataSourceId,
            Name = info.DnsServerDomain ?? Provider,
            Provider = Provider,
            Version = info.Version ?? string.Empty,
            Status = DataSourceStatus.Online,
            Capabilities = ResolveCapabilities(info, queryLogs is not null),
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

            // The source exposes only the most frequent domains. What was
            // counted is certain, what was left out is not: the value is a
            // lower bound and says so.
            UniqueDomains = stats.TopDomains?.Count ?? 0,
            UniqueDomainsQuality = MeasurementQuality.LowerBound,

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

        Dictionary<string, DhcpLease> leases = await ReadLeasesAsync(cancellationToken).ConfigureAwait(false);

        List<Device> devices = [];

        foreach (TopClient client in response.TopClients ?? [])
        {
            if (string.IsNullOrWhiteSpace(client.Name))
            {
                continue;
            }

            leases.TryGetValue(client.Name, out DhcpLease? lease);

            // An identity founded on the hardware address survives a change
            // of network address. One founded on the network address does
            // not, and says so.
            (Guid deviceId, DeviceIdentityBasis basis) = DeviceIdentity.Resolve(client.Name, lease);

            devices.Add(new Device
            {
                DeviceId = deviceId,
                IdentityBasis = basis,

                Hostname = FirstNonEmpty(client.Domain, lease?.HostName),
                IpAddress = client.Name,
                MacAddress = basis == DeviceIdentityBasis.HardwareAddress ? lease!.HardwareAddress : null,

                // Not exposed by the source. The Device Engine may enrich them.
                Vendor = null,
                OperatingSystem = null,

                // The source reports activity over an interval, not the
                // instants of first and last observation. The bounds of the
                // period are used and declared as such.
                FirstSeen = window.Start,
                LastSeen = window.End,
                ObservationQuality = MeasurementQuality.PeriodBounded,

                Status = DeviceStatus.Active,
            });
        }

        return devices;
    }

    /// <summary>
    /// Reads the address assignments, when the source provides them.
    /// </summary>
    /// <remarks>
    /// The source may not act as a DHCP server, or the account may not be
    /// allowed to read that section. Neither is a failure of the acquisition:
    /// the identity of devices simply rests on a weaker basis, which the
    /// devices themselves declare.
    /// </remarks>
    private async Task<Dictionary<string, DhcpLease>> ReadLeasesAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, DhcpLease> leases = new(StringComparer.OrdinalIgnoreCase);

        try
        {
            DhcpLeasesResponse response = await _client
                .GetAsync<DhcpLeasesResponse>("/api/dhcp/leases/list", cancellationToken)
                .ConfigureAwait(false);

            foreach (DhcpLease lease in response.Leases ?? [])
            {
                if (!string.IsNullOrWhiteSpace(lease.Address))
                {
                    leases[lease.Address] = lease;
                }
            }
        }
        catch (AdapterException)
        {
            // Left empty on purpose.
        }

        return leases;
    }

    private static string? FirstNonEmpty(string? preferred, string? fallback)
    {
        if (!string.IsNullOrWhiteSpace(preferred))
        {
            return preferred;
        }

        return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Domain>> GetDomainsAsync(AcquisitionWindow window, CancellationToken cancellationToken)
    {
        // The source keeps resolved and blocked domains in separate lists.
        // Both were observed, and the model draws no distinction by outcome,
        // so both are read. Leaving the blocked ones out would hide precisely
        // the domains a privacy tool exists to show.
        TopDomainsResponse resolved = await _client
            .GetAsync<TopDomainsResponse>(BuildTopUrl(window, "TopDomains"), cancellationToken)
            .ConfigureAwait(false);

        TopBlockedDomainsResponse blocked = await _client
            .GetAsync<TopBlockedDomainsResponse>(BuildTopUrl(window, "TopBlockedDomains"), cancellationToken)
            .ConfigureAwait(false);

        Dictionary<string, long> occurrences = new(StringComparer.OrdinalIgnoreCase);

        Collect(occurrences, resolved.TopDomains);
        Collect(occurrences, blocked.TopBlockedDomains);

        List<Domain> domains = new(occurrences.Count);

        foreach ((string name, long hits) in occurrences)
        {
            domains.Add(new Domain
            {
                Name = name,

                // Classification and reputation belong to the Threat Engine.
                // The Adapter states neither.
                Category = ThreatCategory.Unknown,
                Reputation = null,

                // The source reports activity over an interval, not the
                // instants of individual events. The bounds of the period are
                // used and declared as such rather than passed off as
                // observations.
                FirstSeen = window.Start,
                LastSeen = window.End,
                ObservationQuality = MeasurementQuality.PeriodBounded,

                Occurrences = hits,
            });
        }

        return domains;
    }

    private static void Collect(Dictionary<string, long> occurrences, List<TopDomain>? entries)
    {
        foreach (TopDomain entry in entries ?? [])
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            occurrences[entry.Name] = occurrences.TryGetValue(entry.Name, out long current)
                ? current + entry.Hits
                : entry.Hits;
        }
    }

    /// <inheritdoc />
    public async Task<SourceConfiguration> GetConfigurationAsync(CancellationToken cancellationToken)
    {
        SettingsResponse settings = await _client
            .GetAsync<SettingsResponse>("/api/settings/get", cancellationToken)
            .ConfigureAwait(false);

        List<string> transports = [];

        if (settings.EnableDnsOverTls)
        {
            transports.Add("Tls");
        }

        if (settings.EnableDnsOverHttps)
        {
            transports.Add("Https");
        }

        if (settings.EnableDnsOverQuic)
        {
            transports.Add("Quic");
        }

        return new SourceConfiguration
        {
            DnssecValidationEnabled = settings.DnssecValidation,
            EncryptedTransports = transports,
            QueryMinimisationEnabled = settings.QnameMinimization,
            ClientSubnetForwardingEnabled = settings.EDnsClientSubnet,
            FilteringEnabled = settings.EnableBlocking,
            FilterListCount = settings.BlockListUrls?.Count ?? 0,
            FilterListUpdateIntervalHours = settings.BlockListUpdateIntervalHours,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DomainActivity>> GetDomainActivitiesAsync(
        AcquisitionWindow window,
        CancellationToken cancellationToken)
    {
        QueryLogsApp? queryLogs = await FindQueryLogsAsync(cancellationToken).ConfigureAwait(false);

        if (queryLogs is null)
        {
            throw new AdapterException(
                "The Technitium instance does not provide query logs. Domain Activity is unavailable until the corresponding application is installed.");
        }

        // Read before the log, so that an activity is attributed to a device
        // on the same basis as the device itself. An address that held a
        // lease during the period but no longer does keeps the weaker basis.
        Dictionary<string, DhcpLease> leases = await ReadLeasesAsync(cancellationToken).ConfigureAwait(false);

        // Entries are grouped as they arrive, so that the individual queries
        // are never all held at once. They constitute the browsing history of
        // the devices on the network and must not travel further than this
        // method.
        Dictionary<ActivityKey, ActivityAccumulator> activities = [];

        for (int page = 1; page <= MaxLogPages; page++)
        {
            QueryLogsResponse logs = await _client
                .GetAsync<QueryLogsResponse>(BuildLogsUrl(queryLogs, window, page), cancellationToken)
                .ConfigureAwait(false);

            foreach (QueryLogEntry entry in logs.Entries ?? [])
            {
                Accumulate(activities, entry, leases);
            }

            if (page >= logs.TotalPages)
            {
                break;
            }
        }

        List<DomainActivity> result = new(activities.Count);

        foreach ((ActivityKey key, ActivityAccumulator accumulator) in activities)
        {
            result.Add(new DomainActivity
            {
                DeviceId = key.DeviceId,
                Domain = key.Domain,
                QueryCount = accumulator.QueryCount,
                Blocked = key.Blocked,
                Protocol = key.Protocol,
                FirstSeen = accumulator.FirstSeen,
                LastSeen = accumulator.LastSeen,
            });
        }

        return result;
    }

    private static void Accumulate(
        Dictionary<ActivityKey, ActivityAccumulator> activities,
        QueryLogEntry entry,
        Dictionary<string, DhcpLease> leases)
    {
        if (string.IsNullOrWhiteSpace(entry.ClientIpAddress) || string.IsNullOrWhiteSpace(entry.QName))
        {
            return;
        }

        leases.TryGetValue(entry.ClientIpAddress, out DhcpLease? lease);

        // Outcome and transport take part in the key rather than being
        // collapsed. A device that reached a domain both directly and through
        // a block produced two different facts, and merging them would state
        // something that did not happen.
        ActivityKey key = new(
            DeviceIdentity.Resolve(entry.ClientIpAddress, lease).Id,
            entry.QName,
            string.Equals(entry.ResponseType, "Blocked", StringComparison.OrdinalIgnoreCase),
            entry.Protocol ?? string.Empty);

        if (activities.TryGetValue(key, out ActivityAccumulator? existing))
        {
            existing.Add(entry.Timestamp);
        }
        else
        {
            activities[key] = new ActivityAccumulator(entry.Timestamp);
        }
    }

    private async Task<QueryLogsApp?> FindQueryLogsAsync(CancellationToken cancellationToken)
    {
        AppsResponse apps = await _client
            .GetAsync<AppsResponse>("/api/apps/list", cancellationToken)
            .ConfigureAwait(false);

        foreach (InstalledApp app in apps.Apps ?? [])
        {
            if (string.IsNullOrWhiteSpace(app.Name))
            {
                continue;
            }

            foreach (AppComponent component in app.DnsApps ?? [])
            {
                if (component.ClassPath?.Contains("QueryLogs", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // The log interrogation requires both the name of the
                    // application and the identifier of its component.
                    return new QueryLogsApp(app.Name, component.ClassPath);
                }
            }
        }

        return null;
    }

    private static string BuildLogsUrl(QueryLogsApp app, AcquisitionWindow window, int page)
    {
        return $"/api/logs/query?name={Uri.EscapeDataString(app.Name)}"
            + $"&classPath={Uri.EscapeDataString(app.ClassPath)}"
            + $"&pageNumber={page}&entriesPerPage={LogEntriesPerPage}&descendingOrder=false"
            + $"&start={Uri.EscapeDataString(TechnitiumClient.FormatInstant(window.Start))}"
            + $"&end={Uri.EscapeDataString(TechnitiumClient.FormatInstant(window.End))}";
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

    private static List<string> ResolveCapabilities(SessionInfo info, bool queryLogsAvailable)
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

        List<string> capabilities = [nameof(Statistics), nameof(Device), nameof(Domain)];

        // The configuration is readable only when the account behind the
        // token holds read access to the settings of the server. Declaring
        // the capability without it would promise a datum every acquisition
        // would then fail to obtain.
        bool canReadSettings =
            info.Permissions is not null
            && info.Permissions.TryGetValue("Settings", out SessionPermission? settings)
            && settings.CanView;

        if (canReadSettings)
        {
            capabilities.Add(nameof(SourceConfiguration));
        }

        // Declared only when the optional application is actually installed.
        // The interface is implemented in any case, but implementing it says
        // what this Adapter can do, not what this instance offers today.
        if (queryLogsAvailable)
        {
            capabilities.Add(nameof(DomainActivity));
        }

        return capabilities;
    }

    /// <summary>
    /// Application providing the query logs, as the interrogation needs it.
    /// </summary>
    /// <param name="Name">Name of the installed application.</param>
    /// <param name="ClassPath">Identifier of the component to interrogate.</param>
    private sealed record QueryLogsApp(string Name, string ClassPath);

    /// <summary>
    /// Identifies one interaction between a device and a domain.
    /// </summary>
    private readonly record struct ActivityKey(Guid DeviceId, string Domain, bool Blocked, string Protocol);

    /// <summary>
    /// Collects the entries belonging to a single interaction.
    /// </summary>
    private sealed class ActivityAccumulator
    {
        internal ActivityAccumulator(DateTimeOffset timestamp)
        {
            QueryCount = 1;
            FirstSeen = timestamp;
            LastSeen = timestamp;
        }

        internal long QueryCount { get; private set; }

        internal DateTimeOffset FirstSeen { get; private set; }

        internal DateTimeOffset LastSeen { get; private set; }

        internal void Add(DateTimeOffset timestamp)
        {
            QueryCount++;

            if (timestamp < FirstSeen)
            {
                FirstSeen = timestamp;
            }

            if (timestamp > LastSeen)
            {
                LastSeen = timestamp;
            }
        }
    }
}
