namespace TivuStream.Pie.Adapters.Technitium;

/// <summary>
/// Settings required to reach a Technitium DNS Server instance.
/// </summary>
/// <remarks>
/// The instance is reached through its public HTTP API. No component of
/// Technitium is modified.
/// </remarks>
public sealed class TechnitiumOptions
{
    /// <summary>
    /// Identifier assigned to this Data Source when it was registered.
    /// </summary>
    /// <remarks>
    /// Assigned by the project and independent of the backend, so that the
    /// identity of a Data Source survives a change of address or of product.
    /// </remarks>
    public Guid DataSourceId { get; set; }

    /// <summary>
    /// Address of the web console of the instance.
    /// </summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// Non expiring API token used to authenticate every request.
    /// </summary>
    /// <remarks>
    /// The token should belong to a dedicated account holding the minimum
    /// permissions required, so that a compromised token does not inherit
    /// privileges the acquisition never needs.
    /// <para>
    /// The token stays confined to the Adapter and is never transmitted
    /// beyond it.
    /// </para>
    /// </remarks>
    public string? ApiToken { get; set; }
}
