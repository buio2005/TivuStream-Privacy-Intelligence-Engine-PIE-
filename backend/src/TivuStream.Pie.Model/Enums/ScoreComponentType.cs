namespace TivuStream.Pie.Model.Enums;

/// <summary>
/// Evaluation area contributing to the Network Privacy and Security Score.
/// </summary>
/// <remarks>
/// The values are the six areas defined by the NPSS Specification, listed in
/// the order of the Score Components section.
/// <para>
/// The weight of each area is part of the scoring algorithm and is therefore
/// not represented here.
/// </para>
/// </remarks>
public enum ScoreComponentType
{
    /// <summary>
    /// Configuration of the DNS service.
    /// </summary>
    DnsSecurity = 0,

    /// <summary>
    /// Level of privacy protection.
    /// </summary>
    PrivacyProtection = 1,

    /// <summary>
    /// Protection against known threats.
    /// </summary>
    ThreatProtection = 2,

    /// <summary>
    /// Behaviour of the devices on the network.
    /// </summary>
    DeviceHealth = 3,

    /// <summary>
    /// Quality of the overall configuration.
    /// </summary>
    Configuration = 4,

    /// <summary>
    /// General state of the network.
    /// </summary>
    NetworkIntegrity = 5,
}
