namespace TivuStream.Pie.Model.Enums;

/// <summary>
/// What the identity of a device rests upon.
/// </summary>
/// <remarks>
/// Attributing behaviour to a device is the strongest statement the system
/// makes. How solid that statement is must be visible rather than implied.
/// </remarks>
public enum DeviceIdentityBasis
{
    /// <summary>
    /// Identity derived from the network address.
    /// </summary>
    /// <remarks>
    /// A device that changes address appears as a different device, and an
    /// address reassigned to another device merges two identities.
    /// </remarks>
    NetworkAddress = 0,

    /// <summary>
    /// Identity derived from the hardware address.
    /// </summary>
    /// <remarks>
    /// Stable across changes of network address.
    /// </remarks>
    HardwareAddress = 1,
}
