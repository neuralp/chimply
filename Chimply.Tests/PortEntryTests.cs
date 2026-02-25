using Chimply.Models;

namespace Chimply.Tests;

public class PortEntryTests
{
    [Theory]
    [InlineData(21, "10.0.0.1")]
    [InlineData(22, "192.168.1.1")]
    [InlineData(80, "172.16.0.1")]
    [InlineData(443, "10.10.10.10")]
    public void Constructor_SetsProperties(int port, string ip)
    {
        var entry = new PortEntry(port, ip);

        Assert.Equal(port, entry.Port);
        Assert.Equal(ip, entry.IpAddress);
    }

    [Fact]
    public void OpenCommand_IsNotNull()
    {
        var entry = new PortEntry(80, "192.168.1.1");

        Assert.NotNull(entry.OpenCommand);
    }
}
