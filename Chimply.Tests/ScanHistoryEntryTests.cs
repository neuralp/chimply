using Chimply.Models;

namespace Chimply.Tests;

public class ScanHistoryEntryTests
{
    [Fact]
    public void Display_WithInterfaceName_ReturnsLabelledSubnet()
    {
        var entry = new ScanHistoryEntry { Subnet = "192.168.1.0/24", InterfaceName = "eth0" };

        Assert.Equal("eth0 - 192.168.1.0/24", entry.Display);
    }

    [Fact]
    public void Display_WithoutInterfaceName_ReturnsSubnet()
    {
        var entry = new ScanHistoryEntry { Subnet = "192.168.1.1-50" };

        Assert.Equal("192.168.1.1-50", entry.Display);
    }

    [Fact]
    public void Display_WithEmptyInterfaceName_ReturnsSubnet()
    {
        var entry = new ScanHistoryEntry { Subnet = "10.0.0.0/24", InterfaceName = "" };

        Assert.Equal("10.0.0.0/24", entry.Display);
    }

    [Fact]
    public void ToString_WithInterfaceName_ReturnsBareSubnet()
    {
        // AutoCompleteBox fills the TextBox from ToString(), so a labelled entry must still
        // hand the scanner an unadorned subnet.
        var entry = new ScanHistoryEntry { Subnet = "192.168.1.0/24", InterfaceName = "eth0" };

        Assert.Equal("192.168.1.0/24", entry.ToString());
    }

    [Fact]
    public void ToString_WithoutInterfaceName_ReturnsBareSubnet()
    {
        var entry = new ScanHistoryEntry { Subnet = "192.168.1.1-50" };

        Assert.Equal("192.168.1.1-50", entry.ToString());
    }
}
