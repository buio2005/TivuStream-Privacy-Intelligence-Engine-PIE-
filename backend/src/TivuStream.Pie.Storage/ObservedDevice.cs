using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Storage;

/// <summary>
/// A device observed over an interval, aggregated across its periods.
/// </summary>
/// <remarks>
/// The Device entity of the Data Model also carries its activity and its
/// threats. A list does not: there they would travel as empty lists, read as
/// "no activity, no threat" when they mean "not part of this answer".
/// <para>
/// The description is taken from the most recent period of the interval in
/// which the device appears, the same rule applied to the classification of a
/// domain.
/// </para>
/// </remarks>
public sealed record ObservedDevice
{
    /// <summary>
    /// Identifier of the device.
    /// </summary>
    public required Guid DeviceId { get; init; }

    /// <summary>
    /// Host name, when the Data Source provides one.
    /// </summary>
    public string? Hostname { get; init; }

    /// <summary>
    /// Network address.
    /// </summary>
    public required string IpAddress { get; init; }

    /// <summary>
    /// Hardware address, when available.
    /// </summary>
    public string? MacAddress { get; init; }

    /// <summary>
    /// Manufacturer, when it can be determined.
    /// </summary>
    public string? Vendor { get; init; }

    /// <summary>
    /// Operating system, when it can be determined.
    /// </summary>
    public string? OperatingSystem { get; init; }

    /// <summary>
    /// What the identity rests upon.
    /// </summary>
    public required DeviceIdentityBasis IdentityBasis { get; init; }

    /// <summary>
    /// Always <see cref="DeviceStatus.Active"/>: a device appears in the
    /// interval because it produced traffic there.
    /// </summary>
    public DeviceStatus Status { get; init; } = DeviceStatus.Active;

    /// <summary>
    /// The earliest among the periods included.
    /// </summary>
    public required DateTimeOffset FirstSeen { get; init; }

    /// <summary>
    /// The most recent among the periods included.
    /// </summary>
    public required DateTimeOffset LastSeen { get; init; }

    /// <summary>
    /// The least precise quality among those aggregated.
    /// </summary>
    public required MeasurementQuality ObservationQuality { get; init; }
}
