using System.ComponentModel;
using System.IO;
using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Chimply.Models;
using Chimply.ViewModels;

namespace Chimply.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm != null)
            _vm.PropertyChanged -= OnViewModelPropertyChanged;
        _vm = DataContext as MainWindowViewModel;
        if (_vm != null)
            _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsScanning) && _vm?.IsScanning == true)
            SubnetBox.IsDropDownOpen = false;
    }

    private async void OnExportCsvClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || vm.Results.Count == 0)
            return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export CSV",
            DefaultExtension = "csv",
            SuggestedFileName = "chimply-scan",
            FileTypeChoices =
            [
                new FilePickerFileType("CSV Files") { Patterns = ["*.csv"] }
            ]
        });

        if (file is null)
            return;

        var sb = new StringBuilder();
        sb.AppendLine("IP Address,Hostname,RTT (ms),MAC Address,Manufacturer,Open Ports,Status,Last Change");

        foreach (var r in vm.Results)
        {
            sb.Append(CsvField(r.IpAddress)).Append(',');
            sb.Append(CsvField(r.Hostname)).Append(',');
            sb.Append(r.RoundTripTime?.ToString() ?? "").Append(',');
            sb.Append(CsvField(r.MacAddress)).Append(',');
            sb.Append(CsvField(r.Manufacturer)).Append(',');
            sb.Append(CsvField(string.Join(" ", r.OpenPorts))).Append(',');
            sb.Append(CsvField(r.Status)).Append(',');
            sb.AppendLine(CsvField(r.TimeSinceDiscovery));
        }

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(sb.ToString());
    }

    private static string CsvField(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private async void OnCopyIpClick(object? sender, RoutedEventArgs e)
    {
        if (HostGrid.SelectedItem is ScanResult result && Clipboard is { } clipboard)
            await clipboard.SetTextAsync(result.IpAddress);
    }

    private async void OnCopyHostnameClick(object? sender, RoutedEventArgs e)
    {
        if (HostGrid.SelectedItem is ScanResult result && Clipboard is { } clipboard)
        {
            var hostname = result.Hostname;
            var dotIndex = hostname.IndexOf('.');
            if (dotIndex > 0)
                hostname = hostname[..dotIndex];
            await clipboard.SetTextAsync(hostname);
        }
    }

    private async void OnCopyMacClick(object? sender, RoutedEventArgs e)
    {
        if (HostGrid.SelectedItem is ScanResult result && Clipboard is { } clipboard)
            await clipboard.SetTextAsync(result.MacAddress);
    }

    private void OnSubnetHistoryClick(object? sender, RoutedEventArgs e)
    {
        SubnetBox.IsDropDownOpen = true;
    }

    private void OnSubnetSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count > 0
            && e.AddedItems[0] is ScanHistoryEntry entry
            && DataContext is MainWindowViewModel vm)
        {
            vm.RestoreHistoryEntry(entry);
        }
    }

    private async void OnConfigClick(object? sender, RoutedEventArgs e)
    {
        await new ConfigDialog().ShowDialog(this);
    }

    private async void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        await new AboutDialog().ShowDialog(this);
    }
}
