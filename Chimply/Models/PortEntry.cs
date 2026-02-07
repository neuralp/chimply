using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;

namespace Chimply.Models;

public partial class PortEntry
{
    public int Port { get; }
    public string IpAddress { get; }

    public PortEntry(int port, string ipAddress)
    {
        Port = port;
        IpAddress = ipAddress;
    }

    [RelayCommand]
    private void Open()
    {
        var url = Port switch
        {
            21 => $"ftp://{IpAddress}",
            22 => $"ssh://{IpAddress}",
            80 => $"http://{IpAddress}",
            443 => $"https://{IpAddress}",
            _ => null
        };

        if (url != null)
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
