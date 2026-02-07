using System.Net;

namespace Chimply.Services;

public static class CidrParser
{
    public static List<IPAddress> Parse(string cidr)
    {
        var parts = cidr.Trim().Split('/');
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var networkAddress) || !int.TryParse(parts[1], out var prefixLength))
            throw new FormatException($"Invalid CIDR notation: {cidr}");

        if (prefixLength < 0 || prefixLength > 32)
            throw new FormatException($"Invalid prefix length: {prefixLength}");

        var networkBytes = networkAddress.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
            Array.Reverse(networkBytes);
        var networkUint = BitConverter.ToUInt32(networkBytes, 0);

        var hostBits = 32 - prefixLength;
        var hostCount = 1U << hostBits;
        var mask = hostCount - 1;
        var networkPart = networkUint & ~mask;

        var addresses = new List<IPAddress>();

        if (prefixLength >= 31)
        {
            // /31 and /32 — return all addresses (point-to-point or single host)
            for (uint i = 0; i < hostCount; i++)
            {
                var ipUint = networkPart | i;
                var bytes = BitConverter.GetBytes(ipUint);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                addresses.Add(new IPAddress(bytes));
            }
        }
        else
        {
            // Skip network address (first) and broadcast address (last)
            for (uint i = 1; i < hostCount - 1; i++)
            {
                var ipUint = networkPart | i;
                var bytes = BitConverter.GetBytes(ipUint);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                addresses.Add(new IPAddress(bytes));
            }
        }

        return addresses;
    }
}
