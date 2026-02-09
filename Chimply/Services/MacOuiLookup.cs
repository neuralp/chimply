using System.Reflection;

namespace Chimply.Services;

/// <summary>
/// Maps MAC address OUI prefixes (first 3 octets) to manufacturer names.
/// Loaded from the embedded Resource/mac.csv file at startup.
/// To update: replace Resource/mac.csv with a newer version from https://maclookup.app/downloads/csv-database
/// </summary>
public static class MacOuiLookup
{
    private static readonly Dictionary<string, string> Entries = LoadEntries();

    private static Dictionary<string, string> LoadEntries()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream("Chimply.Resource.mac.csv");
        if (stream == null) return dict;

        using var reader = new StreamReader(stream);

        // Skip header line
        reader.ReadLine();

        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Format: Mac Prefix,Vendor Name,Private,Block Type,Last Update
            // Vendor name may be quoted and contain commas
            var oui = ParseField(line, 0);
            var vendor = ParseField(line, 1);

            if (!string.IsNullOrEmpty(oui) && !string.IsNullOrEmpty(vendor))
                dict.TryAdd(oui, vendor);
        }

        return dict;
    }

    private static string ParseField(string line, int fieldIndex)
    {
        var pos = 0;
        for (var i = 0; i < fieldIndex; i++)
        {
            if (pos >= line.Length) return string.Empty;

            if (line[pos] == '"')
            {
                // Skip to closing quote, then past the comma
                var close = line.IndexOf('"', pos + 1);
                pos = close < 0 ? line.Length : close + 2;
            }
            else
            {
                var comma = line.IndexOf(',', pos);
                pos = comma < 0 ? line.Length : comma + 1;
            }
        }

        if (pos >= line.Length) return string.Empty;

        if (line[pos] == '"')
        {
            var close = line.IndexOf('"', pos + 1);
            return close < 0 ? line[(pos + 1)..] : line[(pos + 1)..close];
        }

        var end = line.IndexOf(',', pos);
        return end < 0 ? line[pos..] : line[pos..end];
    }

    /// <summary>
    /// Looks up the manufacturer for a given MAC address.
    /// </summary>
    /// <param name="macAddress">MAC address in "AA:BB:CC:DD:EE:FF" format.</param>
    /// <returns>Manufacturer name, or empty string if not found.</returns>
    public static string Lookup(string? macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress) || macAddress.Length < 8)
            return string.Empty;

        var oui = macAddress[..8].ToUpperInvariant();
        return Entries.GetValueOrDefault(oui, string.Empty);
    }
}
