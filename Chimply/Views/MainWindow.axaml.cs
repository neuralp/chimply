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
    public MainWindow()
    {
        InitializeComponent();
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

    private async void OnConfigClick(object? sender, RoutedEventArgs e)
    {
        await new ConfigDialog().ShowDialog(this);
    }

    private async void OnAboutClick(object? sender, RoutedEventArgs e)
    {
        await new AboutDialog().ShowDialog(this);
    }
}
