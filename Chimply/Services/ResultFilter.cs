using Chimply.Models;

namespace Chimply.Services;

/// <summary>
/// Decides whether a scan result should stay visible under the grid's text filter.
/// Only IP address, hostname, MAC address, manufacturer and open ports are searched -
/// RTT, status and last-change are deliberately excluded.
/// </summary>
public static class ResultFilter
{
    /// <summary>True when every whitespace-separated term appears in at least one searched field.</summary>
    public static bool Matches(ScanResult result, string? filterText)
    {
        if (string.IsNullOrWhiteSpace(filterText))
            return true;

        var terms = filterText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        // Terms never contain whitespace, so a term can never span two ports.
        return terms.All(term =>
            Contains(result.IpAddress, term)
            || Contains(result.Hostname, term)
            || Contains(result.MacAddress, term)
            || Contains(result.Manufacturer, term)
            || result.OpenPorts.Any(port => Contains(port.ToString(), term)));
    }

    private static bool Contains(string field, string term) =>
        field.Contains(term, StringComparison.OrdinalIgnoreCase);
}
