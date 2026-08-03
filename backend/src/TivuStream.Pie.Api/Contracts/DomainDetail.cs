using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Api.Contracts;

/// <summary>
/// A domain together with the devices that reached it.
/// </summary>
/// <remarks>
/// The API Specification requires the detail of a domain to carry category,
/// reputation, frequency and the devices involved.
/// <para>
/// Interactions appear one per combination of device, outcome and transport.
/// A device that reached the domain both directly and through a block shows
/// as two entries, because those are two different facts.
/// </para>
/// </remarks>
public sealed record DomainDetail
{
    /// <summary>
    /// The domain itself.
    /// </summary>
    public required Domain Domain { get; init; }

    /// <summary>
    /// Interactions recorded between devices and this domain.
    /// </summary>
    /// <remarks>
    /// Empty when the Data Source does not provide the correlation, which is
    /// not the same as no device having reached the domain.
    /// </remarks>
    public IReadOnlyList<DomainActivity> Activities { get; init; } = [];

    /// <summary>
    /// Indicates whether the correlation between devices and domains is
    /// available at all.
    /// </summary>
    public required bool ActivityAvailable { get; init; }
}
