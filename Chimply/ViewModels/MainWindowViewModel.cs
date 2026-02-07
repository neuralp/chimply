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
    private readonly HashSet<string> _previouslySeenIps = [];
    private CancellationTokenSource? _cts;
    private readonly DispatcherTimer _timer;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCommand))]
    private string _subnetInput = "192.168.1.0/24";

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
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) =>
        {
            foreach (var result in Results)
                result.UpdateTimeSinceDiscovery();
        };
        _timer.Start();
    }

    private bool CanScan() => !IsScanning && !string.IsNullOrWhiteSpace(SubnetInput);
    private bool CanStop() => IsScanning;

    [RelayCommand(CanExecute = nameof(CanScan))]
    private async Task ScanAsync()
    {
        Results.Clear();
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
                    result.DiscoveredAt = DateTime.UtcNow;
                    result.UpdateTimeSinceDiscovery();
                    currentScanIps.Add(result.IpAddress);

                    if (_previouslySeenIps.Contains(result.IpAddress))
                        result.Status = "Up";
                    else
                        result.Status = "New";

                    Results.Add(result);
                    HostsFound++;
                }

                StatusText = $"Scanned {scannedCount}/{totalHosts} hosts...";
            });
        });

        try
        {
            await Task.Run(() => _scanner.ScanAsync(SubnetInput, reportProgress, _cts.Token));
            StatusText = $"Scan complete. {HostsFound} host(s) found.";
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
            // Remember all IPs seen this scan for next time
            foreach (var ip in currentScanIps)
                _previouslySeenIps.Add(ip);

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
