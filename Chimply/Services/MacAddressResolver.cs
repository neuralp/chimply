using System.Net;
using System.Runtime.InteropServices;

namespace Chimply.Services;

public static class MacAddressResolver
{
    [DllImport("iphlpapi.dll", ExactSpelling = true)]
    private static extern int SendARP(uint destIp, uint srcIp, byte[] macAddr, ref int macAddrLen);

    public static string? Resolve(IPAddress ipAddress)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return ResolveWindows(ipAddress);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return ResolveLinux(ipAddress);

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveWindows(IPAddress ipAddress)
    {
        var addressBytes = ipAddress.GetAddressBytes();
        var destIp = BitConverter.ToUInt32(addressBytes, 0);
        var macAddr = new byte[6];
        var macAddrLen = macAddr.Length;

        var result = SendARP(destIp, 0, macAddr, ref macAddrLen);
        if (result != 0)
            return null;

        return BitConverter.ToString(macAddr, 0, macAddrLen).Replace('-', ':');
    }

    private static string? ResolveLinux(IPAddress ipAddress)
    {
        var targetIp = ipAddress.ToString();
        if (!File.Exists("/proc/net/arp"))
            return null;

        foreach (var line in File.ReadLines("/proc/net/arp").Skip(1))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 4 && parts[0] == targetIp)
            {
                var mac = parts[3];
                if (mac != "00:00:00:00:00:00")
                    return mac.ToUpperInvariant();
            }
        }

        return null;
    }
}
