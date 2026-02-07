using CommunityToolkit.Mvvm.ComponentModel;

namespace Chimply.Models;

public partial class ScanResult : ObservableObject
{
    [ObservableProperty] private string _ipAddress = string.Empty;
    [ObservableProperty] private string _hostname = string.Empty;
    [ObservableProperty] private string _macAddress = string.Empty;
    [ObservableProperty] private long? _roundTripTime;
    [ObservableProperty] private string _status = "Unknown";
}
