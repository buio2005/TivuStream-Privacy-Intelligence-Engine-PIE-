namespace TivuStream.Pie.Model.Enums;

/// <summary>
/// Classification assigned to an observed domain or to a detected threat.
/// </summary>
/// <remarks>
/// The values are the Primary Categories defined by the Threat Intelligence
/// Specification.
/// <para>
/// <see cref="Unknown"/> is placed first so that it becomes the default value.
/// The specification designates it as the category assigned when a domain
/// cannot be classified, which makes it the correct fallback.
/// </para>
/// </remarks>
public enum ThreatCategory
{
    /// <summary>
    /// The domain could not be classified.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Domains associated with the distribution of malicious software.
    /// </summary>
    Malware = 1,

    /// <summary>
    /// Domains designed to steal credentials or personal data.
    /// </summary>
    Phishing = 2,

    /// <summary>
    /// Domains used to monitor user activity.
    /// </summary>
    Tracking = 3,

    /// <summary>
    /// Domains used to deliver advertising content.
    /// </summary>
    Advertising = 4,

    /// <summary>
    /// Domains used to collect statistics and usage data.
    /// </summary>
    Analytics = 5,

    /// <summary>
    /// Domains associated with cryptocurrency mining activity.
    /// </summary>
    Cryptomining = 6,

    /// <summary>
    /// Domains showing anomalous behaviour or uncertain reputation.
    /// </summary>
    Suspicious = 7,

    /// <summary>
    /// Domains belonging to social platforms.
    /// </summary>
    Social = 8,

    /// <summary>
    /// Domains dedicated to the delivery of media content.
    /// </summary>
    Streaming = 9,

    /// <summary>
    /// Cloud services and distributed infrastructure.
    /// </summary>
    Cloud = 10,

    /// <summary>
    /// Services dedicated to artificial intelligence.
    /// </summary>
    AiServices = 11,
}
