using TivuStream.Pie.Model.Enums;

namespace TivuStream.Pie.Storage;

/// <summary>
/// Who a device is, as far as the observed interval tells.
/// </summary>
/// <remarks>
/// An identifier alone says nothing to whoever reads it. The description is
/// taken from the most recent period of the interval in which the device
/// appears, the same rule applied to the classification of a domain.
/// <para>
/// A device can take part in activity without appearing among the devices of
/// any period of the interval: the two are read from different reports of the
/// Data Source. Its description is then absent, all of it, rather than
/// guessed.
/// </para>
/// </remarks>
public sealed record DeviceIdentification
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
    /// Network address, absent when the device is not described in the
    /// interval.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// What the identity rests upon, absent when the device is not described
    /// in the interval.
    /// </summary>
    public DeviceIdentityBasis? IdentityBasis { get; init; }
}

/// <summary>
/// Activity of one device towards one domain, aggregated over an interval.
/// </summary>
/// <remarks>
/// One per combination of device, outcome and transport. A device that
/// reached the domain both directly and through a block shows twice, because
/// those are two different facts.
/// </remarks>
public sealed record ObservedActivity
{
    /// <summary>
    /// The device that produced the activity.
    /// </summary>
    public required DeviceIdentification Device { get; init; }

    /// <summary>
    /// Queries summed over the periods included.
    /// </summary>
    public required long QueryCount { get; init; }

    /// <summary>
    /// Whether the queries were blocked by the Data Source.
    /// </summary>
    public required bool Blocked { get; init; }

    /// <summary>
    /// Transport used for the queries, as the Data Source names it.
    /// </summary>
    public required string Protocol { get; init; }

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
