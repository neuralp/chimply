namespace Chimply.Models;

public class ScanHistoryEntry
{
    public string Subnet { get; init; } = string.Empty;
    public List<ScanResult> Hosts { get; set; } = [];

    public override string ToString() => Subnet;
}
