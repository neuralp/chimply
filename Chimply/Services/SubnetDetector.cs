using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Chimply.Services;

public static class SubnetDetector
{
    public static string? DetectLocalSubnet()
    {
        try
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up
                             && ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderByDescending(ni => ni.GetIPProperties().GatewayAddresses.Count);

            foreach (var ni in interfaces)
            {
                var unicast = ni.GetIPProperties().UnicastAddresses
                    .FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

                if (unicast == null) continue;

                var ipBytes = unicast.Address.GetAddressBytes();
                var prefix = unicast.PrefixLength;

                // Compute network address by masking
                var networkBytes = new byte[4];
                for (var i = 0; i < 4; i++)
                {
                    var bits = Math.Min(8, Math.Max(0, prefix - i * 8));
                    var mask = (byte)(0xFF << (8 - bits));
                    networkBytes[i] = (byte)(ipBytes[i] & mask);
                }

                return $"{new IPAddress(networkBytes)}/{prefix}";
            }
        }
        catch
        {
            // Fall through to null
        }

        return null;
    }
}
