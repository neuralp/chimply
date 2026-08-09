using Chimply.Models;
using Chimply.Services;

namespace Chimply.Tests;

public class ResultFilterTests
{
    private static ScanResult Dell() => new()
    {
        IpAddress = "10.2.1.9",
        Hostname = "prox.neural.local",
        MacAddress = "44:A8:42:28:41:3A",
        Manufacturer = "Dell Inc.",
        RoundTripTime = 26,
        Status = "Up",
        OpenPorts = [22]
    };

    private static ScanResult Idrac() => new()
    {
        IpAddress = "10.2.1.119",
        Hostname = "idrac-9JGLD42.neural.local",
        MacAddress = "44:A8:42:28:41:3B",
        Manufacturer = "Dell Inc.",
        Status = "Up",
        OpenPorts = [22, 80, 443]
    };

    private static ScanResult Cisco() => new()
    {
        IpAddress = "10.2.1.1",
        Hostname = "",
        MacAddress = "00:A2:EE:95:0D:3B",
        Manufacturer = "Cisco Systems, Inc",
        Status = "Up",
        OpenPorts = [80, 443]
    };

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Matches_EmptyOrWhitespaceFilter_ReturnsTrue(string? filter)
    {
        Assert.True(ResultFilter.Matches(Dell(), filter));
    }

    [Fact]
    public void Matches_PartialIpAddress_ReturnsTrue()
    {
        Assert.True(ResultFilter.Matches(Dell(), "10.2.1"));
    }

    [Fact]
    public void Matches_Hostname_ReturnsTrue()
    {
        Assert.True(ResultFilter.Matches(Dell(), "prox"));
    }

    [Fact]
    public void Matches_MacAddressFragment_IsCaseInsensitive()
    {
        Assert.True(ResultFilter.Matches(Dell(), "44:a8"));
    }

    [Fact]
    public void Matches_Manufacturer_IsCaseInsensitive()
    {
        Assert.True(ResultFilter.Matches(Dell(), "dell"));
    }

    [Fact]
    public void Matches_PortNumber_ReturnsTrue()
    {
        Assert.True(ResultFilter.Matches(Idrac(), "443"));
    }

    [Fact]
    public void Matches_PortNotOpen_ReturnsFalse()
    {
        Assert.False(ResultFilter.Matches(Dell(), "443"));
    }

    [Fact]
    public void Matches_AllTermsPresent_ReturnsTrue()
    {
        // "dell" hits Manufacturer, "443" hits Open Ports.
        Assert.True(ResultFilter.Matches(Idrac(), "dell 443"));
    }

    [Fact]
    public void Matches_OneTermMissing_ReturnsFalse()
    {
        // The Dell row has no 443, so the AND rule rejects it.
        Assert.False(ResultFilter.Matches(Dell(), "dell 443"));
    }

    [Fact]
    public void Matches_TermsAcrossDifferentFields_ReturnsTrue()
    {
        Assert.True(ResultFilter.Matches(Dell(), "dell 10.2.1.9"));
    }

    [Fact]
    public void Matches_MultiWordManufacturer_ReturnsTrue()
    {
        Assert.True(ResultFilter.Matches(Cisco(), "cisco systems"));
    }

    [Fact]
    public void Matches_ExtraWhitespaceBetweenTerms_IsIgnored()
    {
        Assert.True(ResultFilter.Matches(Idrac(), "  dell   443  "));
    }

    [Fact]
    public void Matches_UnrelatedTerm_ReturnsFalse()
    {
        Assert.False(ResultFilter.Matches(Dell(), "netgear"));
    }

    [Fact]
    public void Matches_StatusText_IsNotSearched()
    {
        // Status is deliberately excluded, so filtering on it must not match everything.
        Assert.False(ResultFilter.Matches(Dell(), "Up"));
    }

    [Fact]
    public void Matches_RoundTripTime_IsNotSearched()
    {
        // RTT of 26 must not be matched by "26" - no other field contains it.
        var result = Dell();
        result.RoundTripTime = 7777;

        Assert.False(ResultFilter.Matches(result, "7777"));
    }

    [Fact]
    public void Matches_EmptyFields_DoesNotThrow()
    {
        var empty = new ScanResult();

        Assert.True(ResultFilter.Matches(empty, ""));
        Assert.False(ResultFilter.Matches(empty, "anything"));
    }
}
