using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Api.Contracts;

/// <summary>
/// Operational state of the system.
/// </summary>
/// <remarks>
/// The API Specification requires the report to cover Core, Adapter, Backend
/// and API.
/// <para>
/// The report states what has not been built yet rather than omitting it. A
/// missing section would read as a section with nothing to report, which is a
/// different statement.
/// </para>
/// </remarks>
public sealed record HealthReport
{
    /// <summary>
    /// State of the REST API layer.
    /// </summary>
    public required string Api { get; init; }

    /// <summary>
    /// State of the Core.
    /// </summary>
    public required string Core { get; init; }

    /// <summary>
    /// State of the Adapter layer.
    /// </summary>
    public required string Adapter { get; init; }

    /// <summary>
    /// Description of the Data Source at the time of the last acquisition.
    /// </summary>
    public DataSource? Backend { get; init; }

    /// <summary>
    /// Moment the last acquisition was attempted.
    /// </summary>
    public DateTimeOffset? LastAcquisitionAt { get; init; }

    /// <summary>
    /// Reason the last acquisition did not complete.
    /// </summary>
    public string? LastFailure { get; init; }
}
