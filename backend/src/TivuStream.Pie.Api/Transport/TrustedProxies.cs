using System.Net;

namespace TivuStream.Pie.Api.Transport;

/// <summary>
/// The proxies whose forwarding headers PIE believes.
/// </summary>
internal static class TrustedProxies
{
    /// <summary>
    /// Reads <c>Transport:TrustedProxies</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">An entry is not an address.</exception>
    internal static IPAddress[] Parse(IEnumerable<string> configured)
    {
        List<IPAddress> proxies = [];

        foreach (string entry in configured.Select(entry => entry.Trim()).Where(entry => entry.Length > 0))
        {
            if (!IPAddress.TryParse(entry, out IPAddress? address))
            {
                // A name would have to be resolved, and whoever controls the
                // resolver would choose whom PIE believes.
                throw new InvalidOperationException(
                    $"'Transport:TrustedProxies' lists addresses, not names: '{entry}' is not an address.");
            }

            proxies.Add(address);
        }

        return [.. proxies];
    }
}
