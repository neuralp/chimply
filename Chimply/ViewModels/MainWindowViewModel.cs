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
    private CancellationTokenSource? _cts;

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
        try
        {
            var addresses = CidrParser.Parse(SubnetInput);
            ProgressMax = addresses.Count;
        }
        catch (FormatException ex)
        {
            StatusText = $"Error: {ex.Message}";
            IsScanning = false;
            return;
        }

        var scannedCount = 0;
        var reportProgress = new Progress<ScanResult>(result =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                Results.Add(result);
                scannedCount++;
                Progress = scannedCount;

                if (result.Status == "Up")
                    HostsFound++;

                StatusText = $"Scanned {scannedCount}/{(int)ProgressMax} hosts...";
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
