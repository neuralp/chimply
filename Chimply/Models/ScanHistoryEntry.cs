namespace Chimply.Models;

public class ScanHistoryEntry
{
    public string Subnet { get; init; } = string.Empty;

    /// <summary>Set for entries seeded from a local network interface; null for hand-typed subnets.</summary>
    public string? InterfaceName { get; init; }

    public List<ScanResult> Hosts { get; set; } = [];

    /// <summary>What the dropdown shows, e.g. "eth0 - 192.168.1.0/24".</summary>
    public string Display => string.IsNullOrEmpty(InterfaceName)
        ? Subnet
        : $"{InterfaceName} - {Subnet}";

    // AutoCompleteBox writes this into the TextBox on selection, so it stays the bare subnet.
    public override string ToString() => Subnet;
}
