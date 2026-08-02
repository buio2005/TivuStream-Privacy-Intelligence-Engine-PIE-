using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Model.Entities;

/// <summary>
/// Origin of the information processed by the system.
/// </summary>
/// <remarks>
/// Defined by the Data Model Specification.
/// </remarks>
public sealed record DataSource
{
    /// <summary>
    /// Unique identifier of the Data Source.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Name of the Data Source.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Product providing the data.
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// Version reported by the Data Source.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Operational status of the Data Source.
    /// </summary>
    public required DataSourceStatus Status { get; init; }

    /// <summary>
    /// Features offered by the Data Source.
    /// </summary>
    /// <remarks>
    /// The documentation does not define the admitted values.
    /// </remarks>
    public IReadOnlyList<string> Capabilities { get; init; } = [];

    /// <summary>
    /// Moment of the last successful acquisition.
    /// </summary>
    public required DateTimeOffset LastUpdate { get; init; }
}
