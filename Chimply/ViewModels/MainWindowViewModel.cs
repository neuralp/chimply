using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Chimply.Models;
using Chimply.Services;

namespace Chimply.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INetworkScanner _scanner = new NetworkScanner();
    private readonly Dictionary<string, ScanResult> _resultsByIp = new();
    private readonly Dictionary<string, string> _macToIp = new();
    private readonly DispatcherTimer _timer;
    private CancellationTokenSource? _cts;
    private ScanHistoryEntry? _activeEntry;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    private string _subnetInput = SubnetDetector.DetectLocalSubnet() ?? "192.168.1.0/24";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isScanning;

    [ObservableProperty] private double _progress;
    [ObservableProperty] private double _progressMax = 1;
    [ObservableProperty] private string _statusText = "Ready";
    [ObservableProperty] private int _hostsFound;

    public ObservableCollection<ScanResult> Results { get; } = [];
    public ObservableCollection<ScanHistoryEntry> ScanHistory { get; } = [];

    public MainWindowViewModel()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _timer.Tick += (_, _) =>
        {
            foreach (var result in Results)
                result.UpdateTimeDisplay();
        };
        _timer.Start();
    }

    private bool CanScan() => !IsScanning && !string.IsNullOrWhiteSpace(SubnetInput);
    private bool CanStop() => IsScanning;

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        HostsFound = 0;
        Progress = 0;
        IsScanning = true;
        StatusText = "Scanning...";

        _cts = new CancellationTokenSource();

        // Calculate total hosts for progress bar
        int totalHosts;
        try
        {
            var addresses = CidrParser.Parse(SubnetInput);
            totalHosts = addresses.Count;
            ProgressMax = totalHosts;
        }
        catch (FormatException ex)
        {
            StatusText = $"Error: {ex.Message}";
            IsScanning = false;
            return;
        }

        var subnet = SubnetInput.Trim();

        // Switching subnets: snapshot current results into the previous entry and reset
        if (_activeEntry != null && _activeEntry.Subnet != subnet)
        {
            _activeEntry.Hosts = Results.ToList();
            Results.Clear();
            _resultsByIp.Clear();
            _macToIp.Clear();
        }

        // Find or create the history entry for this subnet (most-recent first)
        var historyEntry = ScanHistory.FirstOrDefault(e => e.Subnet == subnet);
        if (historyEntry != null)
        {
            var idx = ScanHistory.IndexOf(historyEntry);
            if (idx > 0)
                ScanHistory.Move(idx, 0);
        }
        else
        {
            historyEntry = new ScanHistoryEntry { Subnet = subnet };
            ScanHistory.Insert(0, historyEntry);
        }
        _activeEntry = historyEntry;

        var currentScanIps = new HashSet<string>();
        var scannedCount = 0;
        var reportProgress = new Progress<ScanResult>(result =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                scannedCount++;
                Progress = scannedCount;

                if (result.Status == "Up")
                {
                    currentScanIps.Add(result.IpAddress);

                    // Detect MAC address moving to a different IP
                    var ipChanged = false;
                    if (!string.IsNullOrEmpty(result.MacAddress) &&
                        _macToIp.TryGetValue(result.MacAddress, out var previousIp) &&
                        previousIp != result.IpAddress)
                    {
                        ipChanged = true;
                        // Mark the old IP entry as Down
                        if (_resultsByIp.TryGetValue(previousIp, out var oldEntry))
                        {
                            oldEntry.Status = "Down";
                            oldEntry.RoundTripTime = null;
                            oldEntry.OpenPorts = [];
                            oldEntry.BuildPortEntries();
                            oldEntry.SetTimestamp();
                        }
                    }

                    // Track MAC → IP mapping
                    if (!string.IsNullOrEmpty(result.MacAddress))
                        _macToIp[result.MacAddress] = result.IpAddress;

                    if (_resultsByIp.TryGetValue(result.IpAddress, out var existing))
                    {
                        // Update existing row in-place
                        existing.Hostname = result.Hostname;
                        existing.MacAddress = result.MacAddress;
                        existing.Manufacturer = result.Manufacturer;
                        existing.RoundTripTime = result.RoundTripTime;
                        existing.OpenPorts = result.OpenPorts;
                        existing.BuildPortEntries();

                        if (ipChanged)
                        {
                            existing.Status = "Upd IP";
                            existing.SetTimestamp();
                        }
                        else
                        {
                            var previousStatus = existing.Status;
                            if (previousStatus == "New")
                            {
                                // New → Up: no timestamp update
                                existing.Status = "Up";
                            }
                            else if (previousStatus == "Down")
                            {
                                // Down → Up: update timestamp
                                existing.Status = "Up";
                                existing.SetTimestamp();
                            }
                            // Up → Up: no change
                        }
                    }
                    else
                    {
                        // First time seeing this host
                        result.Status = ipChanged ? "Upd IP" : "New";
                        result.SetTimestamp();
                        result.BuildPortEntries();
                        Results.Add(result);
                        _resultsByIp[result.IpAddress] = result;
                    }

                    HostsFound++;
                }

                StatusText = $"Scanned {scannedCount}/{totalHosts} hosts...";
            });
        });

        try
        {
            await Task.Run(() => _scanner.ScanAsync(SubnetInput, reportProgress, _cts.Token));

            // Mark hosts not seen in this scan as Down
            foreach (var entry in _resultsByIp.Values)
            {
                if (!currentScanIps.Contains(entry.IpAddress) && entry.Status is "Up" or "New" or "Upd IP")
                {
                    entry.Status = "Down";
                    entry.RoundTripTime = null;
                    entry.OpenPorts = [];
                    entry.BuildPortEntries();
                    entry.SetTimestamp();
                }
            }

            _activeEntry.Hosts = Results.ToList();
            StatusText = $"Scan complete. {HostsFound} host(s) up.";
        }
        catch (OperationCanceledException)
        {
            _activeEntry.Hosts = Results.ToList();
            StatusText = $"Scan cancelled. {HostsFound} host(s) found so far.";
        }
        catch (FormatException ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public void RestoreHistoryEntry(ScanHistoryEntry entry)
    {
        SubnetInput = entry.Subnet;
        Results.Clear();
        _resultsByIp.Clear();
        _macToIp.Clear();

        foreach (var host in entry.Hosts)
        {
            Results.Add(host);
            _resultsByIp[host.IpAddress] = host;
            if (!string.IsNullOrEmpty(host.MacAddress))
                _macToIp[host.MacAddress] = host.IpAddress;
        }

        _activeEntry = entry;
        HostsFound = entry.Hosts.Count(h => h.Status is "Up" or "New" or "Upd IP");
        StatusText = entry.Hosts.Count > 0
            ? $"Restored {entry.Hosts.Count} host(s) from history."
            : "Ready";
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        _cts?.Cancel();
    }

    [RelayCommand]
    private void Clear()
    {
        Results.Clear();
        _resultsByIp.Clear();
        _macToIp.Clear();
        HostsFound = 0;
        StatusText = "Ready";
    }
}
