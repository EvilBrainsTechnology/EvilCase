using System.Net;
using System.Net.Sockets;

namespace EvilBrains.EvilCase.Host;

/// <summary>
/// IPv6 is keyed on its /64: the full address lets one customer spread over a block. An IPv4-mapped address
/// is unwrapped first, or it would be keyed as a /64.
/// </summary>
internal static class RateLimitPartitionKey
{
    private const int Ipv6PrefixBits = 64;

    public static string ForAddress(IPAddress? address)
    {
        if (address is null)
            return "unknown";

        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        var addressBytes = address.GetAddressBytes();

        if (address.AddressFamily != AddressFamily.InterNetworkV6)
            return "v4|" + Convert.ToHexString(addressBytes);

        var prefixBytes = addressBytes.AsSpan(0, Ipv6PrefixBits / 8);

        return "v6|" + Convert.ToHexString(prefixBytes);
    }
}
