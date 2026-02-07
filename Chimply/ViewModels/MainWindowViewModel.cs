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
    private readonly DispatcherTimer _timer;
    private CancellationTokenSource? _cts;

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

                    if (_resultsByIp.TryGetValue(result.IpAddress, out var existing))
                    {
                        // Update existing row in-place
                        existing.Hostname = result.Hostname;
                        existing.MacAddress = result.MacAddress;
                        existing.RoundTripTime = result.RoundTripTime;
                        existing.OpenPorts = result.OpenPorts;
                        existing.BuildPortEntries();

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
                    else
                    {
                        // First time seeing this host
                        result.Status = "New";
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
                if (!currentScanIps.Contains(entry.IpAddress) && entry.Status is "Up" or "New")
                {
                    entry.Status = "Down";
                    entry.RoundTripTime = null;
                    entry.OpenPorts = [];
                    entry.BuildPortEntries();
                    entry.SetTimestamp();
                }
            }

            StatusText = $"Scan complete. {HostsFound} host(s) up.";
        }
        catch (OperationCanceledException)
        {
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

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        _cts?.Cancel();
    }
}
