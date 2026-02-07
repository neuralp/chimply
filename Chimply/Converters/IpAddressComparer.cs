using System.Collections;
using System.Net;
using Chimply.Models;

namespace Chimply.Converters;

public class IpAddressComparer : IComparer
{
    public static readonly IpAddressComparer Instance = new();

    public int Compare(object? x, object? y)
    {
        if (x is ScanResult a && y is ScanResult b)
            return ToUint(a.IpAddress).CompareTo(ToUint(b.IpAddress));
        return 0;
    }

    private static uint ToUint(string ip)
    {
        if (!IPAddress.TryParse(ip, out var addr)) return 0;
        var b = addr.GetAddressBytes();
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }
}
