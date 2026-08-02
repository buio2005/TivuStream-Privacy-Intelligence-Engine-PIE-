using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TivuStream.Pie.Adapters.Technitium;

/// <summary>
/// Derives a stable identifier for a device.
/// </summary>
/// <remarks>
/// Technitium identifies clients only by network address. The project
/// requires an identifier of its own, stable across acquisitions, so the
/// identifier is derived from the address in a deterministic way.
/// <para>
/// The consequence is that the identity of a device coincides with the
/// identity of its address. A device whose address changes appears as a
/// different device, and an address reused by another device merges the two.
/// This limitation is inherent to identification by address and is stated in
/// the Technitium Integration Specification.
/// </para>
/// <para>
/// The derivation is not a security measure. It only needs to be stable and
/// free of collisions in practice.
/// </para>
/// </remarks>
internal static class DeviceIdentity
{
    private const string Namespace = "TivuStream.Pie.Adapters.Technitium.Device";

    /// <summary>
    /// Derives the identifier of a device from its network address.
    /// </summary>
    /// <param name="ipAddress">Network address of the device.</param>
    internal static Guid FromAddress(string ipAddress)
    {
        string seed = string.Create(
            CultureInfo.InvariantCulture,
            $"{Namespace}:{ipAddress}");

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));

        return new Guid(hash.AsSpan(0, 16));
    }
}
