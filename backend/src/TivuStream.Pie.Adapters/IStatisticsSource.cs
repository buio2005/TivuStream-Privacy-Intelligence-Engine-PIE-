using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Adapters;

/// <summary>
/// Adapter able to provide aggregated statistics of the network.
/// </summary>
/// <remarks>
/// Corresponds to the <c>Statistics</c> capability.
/// </remarks>
public interface IStatisticsSource : IAdapter
{
    /// <summary>
    /// Acquires the statistics observed during the given interval.
    /// </summary>
    /// <param name="window">Interval the acquisition refers to.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<Statistics> GetStatisticsAsync(AcquisitionWindow window, CancellationToken cancellationToken);
}
