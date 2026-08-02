using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Adapters;

/// <summary>
/// Adapter able to provide the domains observed on the network.
/// </summary>
/// <remarks>
/// Corresponds to the <c>Domain</c> capability.
/// <para>
/// Category and reputation are assigned by the Core and are not the
/// responsibility of an Adapter.
/// </para>
/// <para>
/// Some Data Sources return only the most frequently observed domains. When
/// the list is truncated, the values derived from it are approximate and the
/// system must present them as such.
/// </para>
/// </remarks>
public interface IDomainSource : IAdapter
{
    /// <summary>
    /// Acquires the domains observed during the given interval.
    /// </summary>
    /// <param name="window">Interval the acquisition refers to.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<IReadOnlyList<Domain>> GetDomainsAsync(AcquisitionWindow window, CancellationToken cancellationToken);
}
