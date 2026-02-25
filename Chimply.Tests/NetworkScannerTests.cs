using Chimply.Models;
using Chimply.Services;

namespace Chimply.Tests;

public class NetworkScannerTests
{
    [Fact]
    public void NetworkScanner_ImplementsINetworkScanner()
    {
        var scanner = new NetworkScanner();
        Assert.IsAssignableFrom<INetworkScanner>(scanner);
    }

    [Fact]
    public async Task ScanAsync_WithCancellation_ThrowsOperationCanceled()
    {
        var scanner = new NetworkScanner();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scanner.ScanAsync("192.168.1.0/30", new Progress<ScanResult>(), cts.Token));
    }

    [Fact]
    public async Task ScanAsync_InvalidCidr_ThrowsFormatException()
    {
        var scanner = new NetworkScanner();

        await Assert.ThrowsAsync<FormatException>(() =>
            scanner.ScanAsync("invalid", new Progress<ScanResult>(), CancellationToken.None));
    }

    [Fact]
    public async Task ScanAsync_SingleHost_ReportsProgress()
    {
        var scanner = new NetworkScanner();
        var results = new List<ScanResult>();
        var progress = new Progress<ScanResult>(r => results.Add(r));

        // Scan localhost - should always respond
        await scanner.ScanAsync("127.0.0.1/32", progress, CancellationToken.None);

        // Give Progress<T> callbacks time to execute (they're posted to the sync context)
        await Task.Delay(500);

        Assert.Single(results);
        Assert.Equal("127.0.0.1", results[0].IpAddress);
    }
}
