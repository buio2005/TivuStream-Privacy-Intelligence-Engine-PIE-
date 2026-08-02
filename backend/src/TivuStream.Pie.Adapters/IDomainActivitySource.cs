using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Adapters;

/// <summary>
/// Adapter able to correlate devices and domains.
/// </summary>
/// <remarks>
/// Corresponds to the <c>DomainActivity</c> capability, the only one whose
/// availability depends on the configuration of the Data Source.
/// <para>
/// Some Data Sources provide the correlation natively. Others expose it only
/// after an optional component has been enabled. An Adapter implementing this
/// interface must still declare the capability only when the data is
/// effectively obtainable.
/// </para>
/// <para>
/// The Adapter returns activity already aggregated by device and domain. The
/// individual query never leaves the Adapter: it constitutes the browsing
/// history of the devices on the network, and the project does not retain it.
/// </para>
/// </remarks>
public interface IDomainActivitySource : IAdapter
{
    /// <summary>
    /// Acquires the interactions between devices and domains observed during
    /// the given interval.
    /// </summary>
    /// <param name="window">Interval the acquisition refers to.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<IReadOnlyList<DomainActivity>> GetDomainActivitiesAsync(AcquisitionWindow window, CancellationToken cancellationToken);
}
