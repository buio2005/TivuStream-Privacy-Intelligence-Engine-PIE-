using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Adapters;

/// <summary>
/// Adapter able to identify the devices present on the network.
/// </summary>
/// <remarks>
/// Corresponds to the <c>Device</c> capability.
/// <para>
/// The devices returned carry no analysis: threats, alerts and
/// recommendations are produced by the Core and are not the responsibility of
/// an Adapter.
/// </para>
/// </remarks>
public interface IDeviceSource : IAdapter
{
    /// <summary>
    /// Acquires the devices observed during the given interval.
    /// </summary>
    /// <param name="window">Interval the acquisition refers to.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<IReadOnlyList<Device>> GetDevicesAsync(AcquisitionWindow window, CancellationToken cancellationToken);
}
