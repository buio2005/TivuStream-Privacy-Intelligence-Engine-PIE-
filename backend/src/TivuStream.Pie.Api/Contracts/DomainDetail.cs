using TivuStream.Pie.Model;
using TivuStream.Pie.Model.Entities;
using TivuStream.Pie.Storage;

namespace TivuStream.Pie.Api.Contracts;

/// <summary>
/// What the list of activities of a domain means.
/// </summary>
/// <remarks>
/// An empty list because the reader may not see it, and an empty list because
/// no device reached the domain, are different statements. Presented alike,
/// the first would read as the second.
/// </remarks>
public enum ActivityAccess
{
    /// <summary>
    /// The Data Source offers the activity and whoever asks may read it. The
    /// list is the activity.
    /// </summary>
    Available,

    /// <summary>
    /// The Data Source does not offer the activity. The list is empty and
    /// means nothing.
    /// </summary>
    Unavailable,

    /// <summary>
    /// The Data Source offers it, but the role of whoever asks does not cover
    /// it. The list is empty and means nothing.
    /// </summary>
    Withheld,
}

/// <summary>
/// A domain over the window of the list, together with the devices that
/// reached it.
/// </summary>
/// <remarks>
/// The API Specification requires the detail of a domain to carry category,
/// reputation, frequency and the devices involved, over the same window as
/// the list and declaring it.
/// <para>
/// Interactions appear one per combination of device, outcome and transport.
/// A device that reached the domain both directly and through a block shows
/// as two entries, because those are two different facts.
/// </para>
/// </remarks>
public sealed record DomainDetail
{
    /// <summary>
    /// Interval actually covered.
    /// </summary>
    public required ObservationPeriod? Period { get; init; }

    /// <summary>
    /// Hourly periods that exist within the window.
    /// </summary>
    public required int PeriodsObserved { get; init; }

    /// <summary>
    /// Hourly periods the window asked for.
    /// </summary>
    public required int PeriodsRequested { get; init; }

    /// <summary>
    /// The domain, aggregated as in the list, so that the two agree.
    /// </summary>
    public required Domain Domain { get; init; }

    /// <summary>
    /// Activity of devices towards this domain over the window.
    /// </summary>
    /// <remarks>
    /// Meaningful only when <see cref="ActivityAccess"/> is
    /// <see cref="Contracts.ActivityAccess.Available"/>.
    /// </remarks>
    public IReadOnlyList<ObservedActivity> Activities { get; init; } = [];

    /// <summary>
    /// What <see cref="Activities"/> means.
    /// </summary>
    public required ActivityAccess ActivityAccess { get; init; }
}
