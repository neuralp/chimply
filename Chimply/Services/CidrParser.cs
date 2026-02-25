using System.Net;

namespace Chimply.Services;

public static class CidrParser
{
    public static List<IPAddress> Parse(string input)
    {
        var trimmed = input.Trim();

        // Check for range format (contains '-' but not '/')
        if (trimmed.Contains('-') && !trimmed.Contains('/'))
            return ParseRange(trimmed);

        return ParseCidr(trimmed);
    }

    private static List<IPAddress> ParseRange(string input)
    {
        var dashIndex = input.IndexOf('-');
        var startStr = input[..dashIndex];
        var endStr = input[(dashIndex + 1)..];

        if (!IPAddress.TryParse(startStr, out var startAddress))
            throw new FormatException($"Invalid start IP address: {startStr}");

        var startBytes = startAddress.GetAddressBytes();
        if (BitConverter.IsLittleEndian)
            Array.Reverse(startBytes);
        var startUint = BitConverter.ToUInt32(startBytes, 0);

        uint endUint;
        if (!endStr.Contains('.') && uint.TryParse(endStr, out var lastOctet))
        {
            // Short range: 192.168.1.1-50
            if (lastOctet > 255)
                throw new FormatException($"Invalid end octet: {endStr}");
            endUint = (startUint & 0xFFFFFF00) | lastOctet;
        }
        else if (IPAddress.TryParse(endStr, out var endAddress))
        {
            // Full IP range: 192.168.1.1-192.168.1.50
            var endBytes = endAddress.GetAddressBytes();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(endBytes);
            endUint = BitConverter.ToUInt32(endBytes, 0);
        }
        else
        {
            throw new FormatException($"Invalid range end: {endStr}");
        }

        if (endUint < startUint)
            throw new FormatException("End address must be greater than or equal to start address");

        var addresses = new List<IPAddress>();
        for (var ip = startUint; ip <= endUint; ip++)
        {
            var bytes = BitConverter.GetBytes(ip);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            addresses.Add(new IPAddress(bytes));
        }

        return addresses;
    }

    private static List<IPAddress> ParseCidr(string cidr)
    {
        var parts = cidr.Split('/');
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
