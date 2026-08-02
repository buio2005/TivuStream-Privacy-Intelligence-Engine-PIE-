namespace TivuStream.Pie.Model.Enums;

/// <summary>
/// Activity status of a device within the observed interval.
/// </summary>
/// <remarks>
/// The status describes presence, not health: a device is active when it
/// produced traffic during the interval that was acquired.
/// <para>
/// Assessing the behaviour of a device, as opposed to its mere presence,
/// belongs to the Device Engine.
/// </para>
/// </remarks>
public enum DeviceStatus
{
    /// <summary>
    /// The device produced no traffic during the observed interval.
    /// </summary>
    Inactive = 0,

    /// <summary>
    /// The device produced traffic during the observed interval.
    /// </summary>
    Active = 1,
}
