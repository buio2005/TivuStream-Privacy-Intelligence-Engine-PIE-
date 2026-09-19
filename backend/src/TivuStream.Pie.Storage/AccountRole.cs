namespace TivuStream.Pie.Storage;

/// <summary>
/// What an account is allowed to read and do.
/// </summary>
/// <remarks>
/// Kept here rather than in the Unified Data Model: the model describes the
/// network being observed, not the people who observe it.
/// </remarks>
public enum AccountRole
{
    /// <summary>
    /// Reads every datum and manages the accounts.
    /// </summary>
    Administrator = 0,

    /// <summary>
    /// Reads the aggregated data, not what identifies a single device.
    /// </summary>
    Viewer = 1,
}
