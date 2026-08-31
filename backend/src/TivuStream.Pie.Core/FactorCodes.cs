namespace TivuStream.Pie.Core;

/// <summary>
/// Codes of the factors that explain a score.
/// </summary>
/// <remarks>
/// Listed in the NPSS Specification together with the values each carries.
/// <para>
/// A published code never changes meaning. Altering the sense of an existing
/// code would change the sentences shown by interfaces already written, with
/// nothing to signal it. A factor whose meaning changes receives a new code.
/// </para>
/// </remarks>
public static class FactorCodes
{
    // Shared.

    /// <summary>The source does not provide its own configuration.</summary>
    public const string ConfigurationUnavailable = nameof(ConfigurationUnavailable);

    /// <summary>No classification list is available.</summary>
    public const string ClassificationUnavailable = nameof(ClassificationUnavailable);

    /// <summary>Queries below the minimum needed to judge. Values: queries, minimum.</summary>
    public const string ObservationInsufficient = nameof(ObservationInsufficient);

    /// <summary>The source does not report the activity per domain.</summary>
    public const string DomainActivityUnavailable = nameof(DomainActivityUnavailable);

    // DNS Security.

    /// <summary>DNSSEC validation is on.</summary>
    public const string DnssecValidationEnabled = nameof(DnssecValidationEnabled);

    /// <summary>DNSSEC validation is off.</summary>
    public const string DnssecValidationDisabled = nameof(DnssecValidationDisabled);

    /// <summary>Encrypted transports are enabled. Values: transports.</summary>
    public const string EncryptedTransportsAvailable = nameof(EncryptedTransportsAvailable);

    /// <summary>No encrypted transport is enabled.</summary>
    public const string EncryptedTransportsAbsent = nameof(EncryptedTransportsAbsent);

    /// <summary>Query name minimisation is on.</summary>
    public const string QueryMinimisationEnabled = nameof(QueryMinimisationEnabled);

    /// <summary>Query name minimisation is off.</summary>
    public const string QueryMinimisationDisabled = nameof(QueryMinimisationDisabled);

    /// <summary>Client subnet forwarding is on.</summary>
    public const string ClientSubnetForwardingEnabled = nameof(ClientSubnetForwardingEnabled);

    /// <summary>Client subnet forwarding is off.</summary>
    public const string ClientSubnetForwardingDisabled = nameof(ClientSubnetForwardingDisabled);

    /// <summary>Share of queries received over an encrypted transport. Values: share.</summary>
    public const string EncryptedQueryShare = nameof(EncryptedQueryShare);

    /// <summary>Share of queries the service could not satisfy. Values: share.</summary>
    public const string FailedQueryShare = nameof(FailedQueryShare);

    /// <summary>No traffic was observed in the period.</summary>
    public const string NoTrafficObserved = nameof(NoTrafficObserved);

    // Privacy Protection.

    /// <summary>No known tracking was observed.</summary>
    public const string TrackingExposureNone = nameof(TrackingExposureNone);

    /// <summary>Share of queries towards privacy categories. Values: share.</summary>
    public const string TrackingExposureMeasured = nameof(TrackingExposureMeasured);

    /// <summary>Share of those queries that was blocked. Values: share, queries.</summary>
    public const string TrackingBlockingMeasured = nameof(TrackingBlockingMeasured);

    /// <summary>There was nothing to block.</summary>
    public const string TrackingBlockingUntested = nameof(TrackingBlockingUntested);

    // Threat Protection.

    /// <summary>No known threat and no suspicious domain was observed.</summary>
    public const string ThreatExposureNone = nameof(ThreatExposureNone);

    /// <summary>Only suspicious domains, none confirmed. Values: suspicious.</summary>
    public const string ThreatExposureSuspiciousOnly = nameof(ThreatExposureSuspiciousOnly);

    /// <summary>Confirmed and suspicious domains observed. Values: confirmed, suspicious.</summary>
    public const string ThreatExposureMeasured = nameof(ThreatExposureMeasured);

    /// <summary>Share of the risky queries that was blocked. Values: share, queries.</summary>
    public const string ThreatBlockingMeasured = nameof(ThreatBlockingMeasured);

    /// <summary>There was nothing to block.</summary>
    public const string ThreatBlockingUntested = nameof(ThreatBlockingUntested);

    // Device Health.

    /// <summary>The Device Engine and the Alert Engine do not exist yet.</summary>
    public const string EnginesNotImplemented = nameof(EnginesNotImplemented);

    // Configuration.

    /// <summary>The source answered.</summary>
    public const string SourceReachable = nameof(SourceReachable);

    /// <summary>The source could not be reached.</summary>
    public const string SourceUnreachable = nameof(SourceUnreachable);

    /// <summary>Domain filtering is on.</summary>
    public const string FilteringEnabled = nameof(FilteringEnabled);

    /// <summary>Domain filtering is off.</summary>
    public const string FilteringDisabled = nameof(FilteringDisabled);

    /// <summary>Filter lists are configured. Values: count.</summary>
    public const string FilterListsConfigured = nameof(FilterListsConfigured);

    /// <summary>No filter list is configured, so filtering has no effect.</summary>
    public const string FilterListsAbsent = nameof(FilterListsAbsent);

    // Network Integrity.

    /// <summary>Periods observed against those expected. Values: observed, expected.</summary>
    public const string ObservationContinuity = nameof(ObservationContinuity);

    /// <summary>No period was expected, so continuity cannot be judged.</summary>
    public const string ObservationContinuityUnknown = nameof(ObservationContinuityUnknown);

    /// <summary>Acquisition attempts are not recorded yet.</summary>
    public const string AcquisitionReliabilityUnknown = nameof(AcquisitionReliabilityUnknown);
}
