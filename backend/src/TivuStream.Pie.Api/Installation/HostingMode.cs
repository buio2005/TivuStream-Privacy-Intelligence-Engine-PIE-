using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace TivuStream.Pie.Api.Installation;

/// <summary>
/// Whether PIE runs as a system service or was started by hand.
/// </summary>
/// <remarks>
/// A service has no window: what it writes to the standard output goes to the
/// system log, or nowhere. What is meant for the eyes of the person starting
/// it has to take another way (Installation Specification, First
/// Administrator Under A Service).
/// </remarks>
internal sealed record HostingMode(bool AsService)
{
    internal const string ServiceName = "TivuStreamPIE";

    internal static HostingMode Detect()
    {
        return new HostingMode(WindowsServiceHelpers.IsWindowsService() || SystemdHelpers.IsSystemdService());
    }
}
