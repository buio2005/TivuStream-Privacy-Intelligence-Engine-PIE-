namespace TivuStream.Pie.Model.Entities;

/// <summary>
/// Settings of the Data Source that bear on privacy and security.
/// </summary>
/// <remarks>
/// Defined by the Data Model Specification.
/// <para>
/// The features a backend offers are data to be analysed. They are distinct
/// from capabilities, which declare which entities the source is able to
/// provide.
/// </para>
/// <para>
/// The properties are expressed in terms independent of any backend.
/// </para>
/// </remarks>
public sealed record SourceConfiguration
{
    /// <summary>
    /// Indicates whether the source validates DNSSEC signatures.
    /// </summary>
    public required bool DnssecValidationEnabled { get; init; }

    /// <summary>
    /// Encrypted transports the source accepts queries on.
    /// </summary>
    public IReadOnlyList<string> EncryptedTransports { get; init; } = [];

    /// <summary>
    /// Indicates whether the source minimises the name sent to authoritative
    /// servers.
    /// </summary>
    public required bool QueryMinimisationEnabled { get; init; }

    /// <summary>
    /// Indicates whether the source forwards the subnet of the client to
    /// external servers.
    /// </summary>
    /// <remarks>
    /// This feature improves the accuracy of geographically aware answers by
    /// telling external servers which part of the network a query came from.
    /// It reduces privacy, so the score rewards its absence.
    /// </remarks>
    public required bool ClientSubnetForwardingEnabled { get; init; }

    /// <summary>
    /// Indicates whether the source filters unwanted domains.
    /// </summary>
    public required bool FilteringEnabled { get; init; }

    /// <summary>
    /// Number of filter lists the source is configured with.
    /// </summary>
    /// <remarks>
    /// Filtering enabled with no list configured has no effect, which is why
    /// the two are recorded separately.
    /// </remarks>
    public required int FilterListCount { get; init; }

    /// <summary>
    /// Hours between two updates of the filter lists.
    /// </summary>
    public required int FilterListUpdateIntervalHours { get; init; }
}
