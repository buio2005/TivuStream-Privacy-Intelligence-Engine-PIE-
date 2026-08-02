using TivuStream.Pie.Model.Entities;

namespace TivuStream.Pie.Adapters;

/// <summary>
/// Component that converts the data produced by a Data Source into the
/// Unified Data Model.
/// </summary>
/// <remarks>
/// An Adapter communicates with exactly one Data Source and performs no
/// analysis. Credentials for the Data Source stay confined within it.
/// <para>
/// This interface carries only the identity of the Adapter. The ability to
/// provide a given kind of data is expressed by the capability interfaces,
/// which an Adapter implements only when it can actually serve them.
/// </para>
/// </remarks>
public interface IAdapter
{
    /// <summary>
    /// Product this Adapter integrates.
    /// </summary>
    /// <remarks>
    /// Available without contacting the Data Source, so that an Adapter can
    /// be registered, logged and reported on even while it is unreachable.
    /// </remarks>
    string Provider { get; }

    /// <summary>
    /// Describes the Data Source as it is at this moment.
    /// </summary>
    /// <remarks>
    /// The returned description carries the version, the operational status
    /// and the capabilities that are effectively available.
    /// <para>
    /// Capabilities are determined at runtime and may be narrower than the
    /// capability interfaces this Adapter implements: a Data Source can offer
    /// a kind of data only once an optional component has been enabled on it.
    /// An Adapter must never declare a capability whose interface it does not
    /// implement.
    /// </para>
    /// </remarks>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<DataSource> DescribeAsync(CancellationToken cancellationToken);
}
