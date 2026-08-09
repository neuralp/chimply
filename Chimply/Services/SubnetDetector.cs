using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Chimply.Services;

/// <summary>A local network interface and the subnet it is attached to.</summary>
public readonly record struct InterfaceSubnet(string InterfaceName, string Cidr);

public static class SubnetDetector
{
    // Substrings that identify container/hypervisor/VPN adapters, matched against
    // both the interface name and its description.
    private static readonly string[] VirtualAdapterMarkers =
    [
        "docker", "veth", "virbr", "br-", "vethernet", "vmware",
        "virtualbox", "vboxnet", "hyper-v", "tap", "tun",
        "tailscale", "wireguard", "zerotier"
    ];

    /// <summary>
    /// Returns the subnet of every usable local interface, most likely default route first.
    /// Loopback, tunnel and virtual adapters are excluded, and duplicate subnets are collapsed.
    /// </summary>
    public static IReadOnlyList<InterfaceSubnet> DetectLocalSubnets()
    {
        var subnets = new List<InterfaceSubnet>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up
                             && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback
                             && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel
                             && !IsVirtualAdapter(ni.Name, ni.Description))
                .OrderByDescending(ni => ni.GetIPProperties().GatewayAddresses.Count);

            foreach (var ni in interfaces)
            {
                var unicasts = ni.GetIPProperties().UnicastAddresses
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

                foreach (var unicast in unicasts)
                {
                    var prefix = unicast.PrefixLength;

                    // A prefix of 0 is reported by some adapters and would yield 0.0.0.0/0,
                    // i.e. a four-billion-address scan. A /32 is a point-to-point host route
                    // (VPNs such as Tailscale use one) whose only address is this machine.
                    if (prefix is < 8 or > 31) continue;

                    // Skip APIPA - a link-local address means DHCP never answered.
                    if (unicast.Address.GetAddressBytes() is [169, 254, _, _]) continue;

                    var cidr = ComputeNetworkCidr(unicast.Address, prefix);

                    // Two adapters on one subnet must not produce two history entries.
                    if (seen.Add(cidr))
                        subnets.Add(new InterfaceSubnet(ni.Name, cidr));
                }
            }
        }
        catch
        {
            // Return whatever was gathered before the network stack objected.
        }

        return subnets;
    }

    /// <summary>The subnet of the most likely default-route interface, or null if none was found.</summary>
    public static string? DetectLocalSubnet()
    {
        var subnets = DetectLocalSubnets();
        return subnets.Count > 0 ? subnets[0].Cidr : null;
    }

    /// <summary>Masks off the host bits of <paramref name="address"/> and renders it as CIDR.</summary>
    public static string ComputeNetworkCidr(IPAddress address, int prefixLength)
    {
        var ipBytes = address.GetAddressBytes();

        var networkBytes = new byte[4];
        for (var i = 0; i < 4; i++)
        {
            var bits = Math.Min(8, Math.Max(0, prefixLength - i * 8));
            var mask = (byte)(0xFF << (8 - bits));
            networkBytes[i] = (byte)(ipBytes[i] & mask);
        }

        return $"{new IPAddress(networkBytes)}/{prefixLength}";
    }

    /// <summary>True for container, hypervisor and VPN adapters, which are rarely worth scanning.</summary>
    public static bool IsVirtualAdapter(string name, string? description) =>
        VirtualAdapterMarkers.Any(marker =>
            name.Contains(marker, StringComparison.OrdinalIgnoreCase)
            || description?.Contains(marker, StringComparison.OrdinalIgnoreCase) == true);
}
