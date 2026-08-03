using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Adapters;

/// <summary>
/// Adapter able to report the settings of its Data Source.
/// </summary>
/// <remarks>
/// Corresponds to the <c>SourceConfiguration</c> capability.
/// <para>
/// The configuration describes the present state of the source and is not
/// bound to an observation period, so no interval is requested.
/// </para>
/// </remarks>
public interface ISourceConfigurationSource : IAdapter
{
    /// <summary>
    /// Acquires the settings of the Data Source.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<SourceConfiguration> GetConfigurationAsync(CancellationToken cancellationToken);
}
