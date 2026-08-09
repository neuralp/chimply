using System.Net;
using Chimply.Services;

namespace Chimply.Tests;

public class SubnetDetectorTests
{
    [Fact]
    public void DetectLocalSubnet_ReturnsNullOrValidCidr()
    {
        var result = SubnetDetector.DetectLocalSubnet();

        if (result != null)
        {
            Assert.Contains("/", result);
            var parts = result.Split('/');
            Assert.Equal(2, parts.Length);
            Assert.True(System.Net.IPAddress.TryParse(parts[0], out _),
                $"Network address '{parts[0]}' should be a valid IP");
            Assert.True(int.TryParse(parts[1], out var prefix),
                $"Prefix '{parts[1]}' should be a number");
            Assert.InRange(prefix, 0, 32);
        }
    }

    [Theory]
    [InlineData("192.168.1.77", 24, "192.168.1.0/24")]
    [InlineData("192.168.1.0", 24, "192.168.1.0/24")]
    [InlineData("10.5.3.9", 8, "10.0.0.0/8")]
    [InlineData("172.16.200.5", 16, "172.16.0.0/16")]
    [InlineData("192.168.1.130", 25, "192.168.1.128/25")]
    [InlineData("192.168.1.6", 30, "192.168.1.4/30")]
    [InlineData("192.168.1.7", 31, "192.168.1.6/31")]
    [InlineData("192.168.1.77", 32, "192.168.1.77/32")]
    [InlineData("192.168.1.77", 0, "0.0.0.0/0")]
    public void ComputeNetworkCidr_MasksHostBits(string address, int prefix, string expected)
    {
        var result = SubnetDetector.ComputeNetworkCidr(IPAddress.Parse(address), prefix);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("docker0", null)]
    [InlineData("veth1a2b3c", null)]
    [InlineData("virbr0", null)]
    [InlineData("br-4f2a91", null)]
    [InlineData("vEthernet (Default Switch)", null)]
    [InlineData("tun0", null)]
    [InlineData("tailscale0", null)]
    [InlineData("wg0", "WireGuard Tunnel")]
    [InlineData("Ethernet 3", "VMware Virtual Ethernet Adapter for VMnet8")]
    [InlineData("Ethernet 4", "Oracle VirtualBox Host-Only Ethernet Adapter")]
    [InlineData("Ethernet 5", "Hyper-V Virtual Ethernet Adapter")]
    public void IsVirtualAdapter_VirtualAdapters_ReturnsTrue(string name, string? description)
    {
        Assert.True(SubnetDetector.IsVirtualAdapter(name, description));
    }

    [Theory]
    [InlineData("eth0", null)]
    [InlineData("wlan0", null)]
    [InlineData("enp3s0", null)]
    [InlineData("Ethernet", "Realtek PCIe GbE Family Controller")]
    [InlineData("Wi-Fi", "Intel(R) Wi-Fi 6 AX201 160MHz")]
    public void IsVirtualAdapter_PhysicalAdapters_ReturnsFalse(string name, string? description)
    {
        Assert.False(SubnetDetector.IsVirtualAdapter(name, description));
    }

    [Fact]
    public void DetectLocalSubnets_ReturnsDistinctValidCidrs()
    {
        // Tolerates a machine with no usable interfaces; asserts shape when there are some.
        var subnets = SubnetDetector.DetectLocalSubnets();

        foreach (var subnet in subnets)
        {
            Assert.False(string.IsNullOrWhiteSpace(subnet.InterfaceName));

            // Shape only - actually parsing a /8 would materialize 16 million addresses.
            var parts = subnet.Cidr.Split('/');
            Assert.Equal(2, parts.Length);
            Assert.True(IPAddress.TryParse(parts[0], out _),
                $"Network address '{parts[0]}' should be a valid IP");
            Assert.True(int.TryParse(parts[1], out var prefix),
                $"Prefix '{parts[1]}' should be a number");
            Assert.InRange(prefix, 8, 31);
        }

        var distinct = subnets.Select(s => s.Cidr).Distinct().Count();
        Assert.Equal(subnets.Count, distinct);
    }

    [Fact]
    public void DetectLocalSubnet_MatchesFirstOfDetectLocalSubnets()
    {
        var subnets = SubnetDetector.DetectLocalSubnets();
        var single = SubnetDetector.DetectLocalSubnet();

        if (subnets.Count == 0)
            Assert.Null(single);
        else
            Assert.Equal(subnets[0].Cidr, single);
    }
}
