namespace TivuStream.Pie.Adapters.Technitium.Responses;

/// <summary>
/// Payload of the DHCP leases call.
/// </summary>
/// <remarks>
/// Available only when the Data Source also acts as a DHCP server. It ties a
/// network address to a hardware address, which turns the identity of a
/// device from something that changes with the address into something stable.
/// </remarks>
internal sealed class DhcpLeasesResponse
{
    /// <summary>
    /// Assignments currently in force.
    /// </summary>
    public List<DhcpLease>? Leases { get; set; }
}

/// <summary>
/// Single address assignment.
/// </summary>
internal sealed class DhcpLease
{
    /// <summary>
    /// Network address assigned.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Hardware address of the device the assignment belongs to.
    /// </summary>
    public string? HardwareAddress { get; set; }

    /// <summary>
    /// Name the device declared, when it declared one.
    /// </summary>
    public string? HostName { get; set; }
}
