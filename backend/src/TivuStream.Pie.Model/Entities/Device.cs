using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Model.Entities;

/// <summary>
/// Device identified by the system.
/// </summary>
/// <remarks>
/// Defined by the Data Model Specification. The associated collections
/// correspond to the objects that specification associates with a device.
/// </remarks>
public sealed record Device
{
    /// <summary>
    /// Unique identifier of the device.
    /// </summary>
    public required Guid DeviceId { get; init; }

    /// <summary>
    /// Host name of the device, when the Data Source provides one.
    /// </summary>
    public string? Hostname { get; init; }

    /// <summary>
    /// Network address of the device.
    /// </summary>
    public required string IpAddress { get; init; }

    /// <summary>
    /// Hardware address of the device, when available.
    /// </summary>
    public string? MacAddress { get; init; }

    /// <summary>
    /// Manufacturer of the device, when it can be determined.
    /// </summary>
    public string? Vendor { get; init; }

    /// <summary>
    /// Operating system of the device, when it can be determined.
    /// </summary>
    public string? OperatingSystem { get; init; }

    /// <summary>
    /// Moment the device was observed for the first time.
    /// </summary>
    public required DateTimeOffset FirstSeen { get; init; }

    /// <summary>
    /// Moment the device was observed most recently.
    /// </summary>
    public required DateTimeOffset LastSeen { get; init; }

    /// <summary>
    /// Activity status of the device within the observed interval.
    /// </summary>
    public required DeviceStatus Status { get; init; }

    /// <summary>
    /// Interactions between this device and the domains it contacted.
    /// </summary>
    public IReadOnlyList<DomainActivity> DomainActivities { get; init; } = [];

    /// <summary>
    /// Threats associated with this device.
    /// </summary>
    public IReadOnlyList<Threat> Threats { get; init; } = [];

    /// <summary>
    /// Alerts associated with this device.
    /// </summary>
    public IReadOnlyList<Alert> Alerts { get; init; } = [];

    /// <summary>
    /// Recommendations associated with this device.
    /// </summary>
    public IReadOnlyList<Recommendation> Recommendations { get; init; } = [];
}
