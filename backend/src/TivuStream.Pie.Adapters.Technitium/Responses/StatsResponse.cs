namespace TivuStream.Pie.Adapters.Technitium.Responses;

/// <summary>
/// Payload of the dashboard statistics call.
/// </summary>
internal sealed class StatsResponse
{
    /// <summary>
    /// Aggregated counters of the observed interval.
    /// </summary>
    public StatsCounters? Stats { get; set; }

    /// <summary>
    /// Distribution of the queries across transport protocols.
    /// </summary>
    public ChartData? ProtocolTypeChartData { get; set; }

    /// <summary>
    /// Most frequently observed domains within the interval.
    /// </summary>
    public List<TopDomain>? TopDomains { get; set; }
}

/// <summary>
/// Aggregated counters reported by the server.
/// </summary>
internal sealed class StatsCounters
{
    /// <summary>
    /// Total number of queries.
    /// </summary>
    public long TotalQueries { get; set; }

    /// <summary>
    /// Number of queries the server could not resolve.
    /// </summary>
    public long TotalServerFailure { get; set; }

    /// <summary>
    /// Number of queries answered with a non existing domain.
    /// </summary>
    public long TotalNxDomain { get; set; }

    /// <summary>
    /// Number of queries the server refused to answer.
    /// </summary>
    public long TotalRefused { get; set; }

    /// <summary>
    /// Number of queries answered from cache.
    /// </summary>
    public long TotalCached { get; set; }

    /// <summary>
    /// Number of queries blocked by the filtering rules.
    /// </summary>
    public long TotalBlocked { get; set; }

    /// <summary>
    /// Number of queries dropped without an answer.
    /// </summary>
    public long TotalDropped { get; set; }

    /// <summary>
    /// Number of distinct clients observed.
    /// </summary>
    public int TotalClients { get; set; }
}

/// <summary>
/// Chart data, made of labels and of the corresponding series.
/// </summary>
internal sealed class ChartData
{
    /// <summary>
    /// Labels of the chart.
    /// </summary>
    public List<string>? Labels { get; set; }

    /// <summary>
    /// Series of the chart.
    /// </summary>
    public List<ChartDataset>? Datasets { get; set; }
}

/// <summary>
/// Single series of a chart.
/// </summary>
internal sealed class ChartDataset
{
    /// <summary>
    /// Values of the series, in the order of the labels.
    /// </summary>
    public List<long>? Data { get; set; }
}
