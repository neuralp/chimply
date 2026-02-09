using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Chimply.Models;

public partial class ScanResult : ObservableObject
{
    [ObservableProperty] private string _ipAddress = string.Empty;
    [ObservableProperty] private string _hostname = string.Empty;
    [ObservableProperty] private string _macAddress = string.Empty;
    [ObservableProperty] private string _manufacturer = string.Empty;
    [ObservableProperty] private long? _roundTripTime;
    [ObservableProperty] private string _status = "Unknown";
    [ObservableProperty] private string _timeSinceDiscovery = string.Empty;
    [ObservableProperty] private string _timeSinceColor = "#9E9E9E";
    [ObservableProperty] private ObservableCollection<PortEntry> _portEntries = [];

    public List<int> OpenPorts { get; set; } = [];

    public DateTime? StatusChangedAt { get; private set; }

    public void SetTimestamp()
    {
        StatusChangedAt = DateTime.UtcNow;
        UpdateTimeDisplay();
    }

    public void UpdateTimeDisplay()
    {
        if (StatusChangedAt is not { } ts) return;

        var elapsed = DateTime.UtcNow - ts;
        TimeSinceDiscovery = elapsed.TotalSeconds < 5 ? "just now"
            : elapsed.TotalSeconds < 60 ? $"{(int)elapsed.TotalSeconds} seconds ago"
            : elapsed.TotalMinutes < 2 ? "1 minute ago"
            : elapsed.TotalMinutes < 60 ? $"{(int)elapsed.TotalMinutes} minutes ago"
            : elapsed.TotalHours < 2 ? "1 hour ago"
            : $"{(int)elapsed.TotalHours} hours ago";

        TimeSinceColor = elapsed.TotalSeconds < 60 ? "#F44336"    // red
            : elapsed.TotalMinutes < 5 ? "#FF9800"                // orange
            : elapsed.TotalMinutes < 15 ? "#FFEB3B"               // yellow
            : elapsed.TotalMinutes < 60 ? "#4CAF50"               // green
            : "#64B5F6";                                           // blue
    }

    public void BuildPortEntries()
    {
        PortEntries = new ObservableCollection<PortEntry>(
            OpenPorts.Select(p => new PortEntry(p, IpAddress)));
    }
}
