using Chimply.Models;

namespace Chimply.Tests;

public class ScanResultTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var result = new ScanResult();

        Assert.Equal(string.Empty, result.IpAddress);
        Assert.Equal(string.Empty, result.Hostname);
        Assert.Equal(string.Empty, result.MacAddress);
        Assert.Equal(string.Empty, result.Manufacturer);
        Assert.Null(result.RoundTripTime);
        Assert.Equal("Unknown", result.Status);
        Assert.Equal(string.Empty, result.TimeSinceDiscovery);
        Assert.Equal("#9E9E9E", result.TimeSinceColor);
        Assert.Empty(result.PortEntries);
        Assert.Empty(result.OpenPorts);
        Assert.Null(result.StatusChangedAt);
    }

    [Fact]
    public void SetTimestamp_SetsStatusChangedAt()
    {
        var result = new ScanResult();

        result.SetTimestamp();

        Assert.NotNull(result.StatusChangedAt);
        Assert.True((DateTime.UtcNow - result.StatusChangedAt.Value).TotalSeconds < 2);
    }

    [Fact]
    public void SetTimestamp_UpdatesTimeDisplay()
    {
        var result = new ScanResult();

        result.SetTimestamp();

        Assert.Equal("just now", result.TimeSinceDiscovery);
        Assert.Equal("#F44336", result.TimeSinceColor); // red for < 60 seconds
    }

    [Fact]
    public void UpdateTimeDisplay_WithNoTimestamp_DoesNothing()
    {
        var result = new ScanResult();

        result.UpdateTimeDisplay();

        Assert.Equal(string.Empty, result.TimeSinceDiscovery);
    }

    [Fact]
    public void BuildPortEntries_CreatesPortEntriesFromOpenPorts()
    {
        var result = new ScanResult { IpAddress = "192.168.1.1" };
        result.OpenPorts = [22, 80, 443];

        result.BuildPortEntries();

        Assert.Equal(3, result.PortEntries.Count);
        Assert.Equal(22, result.PortEntries[0].Port);
        Assert.Equal("192.168.1.1", result.PortEntries[0].IpAddress);
        Assert.Equal(80, result.PortEntries[1].Port);
        Assert.Equal(443, result.PortEntries[2].Port);
    }

    [Fact]
    public void BuildPortEntries_EmptyPorts_CreatesEmptyCollection()
    {
        var result = new ScanResult { IpAddress = "192.168.1.1" };
        result.OpenPorts = [];

        result.BuildPortEntries();

        Assert.Empty(result.PortEntries);
    }
}
