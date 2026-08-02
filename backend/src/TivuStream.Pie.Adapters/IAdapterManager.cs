namespace TivuStream.Pie.Adapters;

/// <summary>
/// Manages the lifecycle of the registered Adapters.
/// </summary>
/// <remarks>
/// The Adapter Manager also orchestrates the Acquisition Flow: it is
/// triggered by the Scheduler, collects the data from the Adapters and hands
/// the Unified Data Model to the Core.
/// <para>
/// The method that performs the acquisition cycle is deliberately absent from
/// this contract. Its shape depends on the entry point of the Core, which is
/// not defined yet, and declaring it now would mean guessing at it.
/// </para>
/// </remarks>
public interface IAdapterManager
{
    /// <summary>
    /// Adapters currently registered.
    /// </summary>
    IReadOnlyList<IAdapter> Adapters { get; }

    /// <summary>
    /// Registers an Adapter.
    /// </summary>
    /// <param name="adapter">Adapter to register.</param>
    void Register(IAdapter adapter);
}
