using System.Net;
using Chimply.Services;

namespace Chimply.Tests;

public class CidrParserTests
{
    [Fact]
    public void Parse_Cidr24_Returns254Addresses()
    {
        var result = CidrParser.Parse("192.168.1.0/24");

        Assert.Equal(254, result.Count);
        Assert.Equal("192.168.1.1", result[0].ToString());
        Assert.Equal("192.168.1.254", result[^1].ToString());
    }

    [Fact]
    public void Parse_Cidr24_ExcludesNetworkAndBroadcast()
    {
        var result = CidrParser.Parse("10.0.0.0/24");

        Assert.DoesNotContain(result, ip => ip.ToString() == "10.0.0.0");
        Assert.DoesNotContain(result, ip => ip.ToString() == "10.0.0.255");
    }

    [Fact]
    public void Parse_Cidr32_ReturnsSingleAddress()
    {
        var result = CidrParser.Parse("192.168.1.5/32");

        Assert.Single(result);
        Assert.Equal("192.168.1.5", result[0].ToString());
    }

    [Fact]
    public void Parse_Cidr31_ReturnsTwoAddresses()
    {
        var result = CidrParser.Parse("192.168.1.0/31");

        Assert.Equal(2, result.Count);
        Assert.Equal("192.168.1.0", result[0].ToString());
        Assert.Equal("192.168.1.1", result[1].ToString());
    }

    [Fact]
    public void Parse_Cidr30_ReturnsTwoUsableAddresses()
    {
        var result = CidrParser.Parse("192.168.1.0/30");

        Assert.Equal(2, result.Count);
        Assert.Equal("192.168.1.1", result[0].ToString());
        Assert.Equal("192.168.1.2", result[^1].ToString());
    }

    [Fact]
    public void Parse_FullRange_ReturnsCorrectAddresses()
    {
        var result = CidrParser.Parse("192.168.1.1-192.168.1.5");

        Assert.Equal(5, result.Count);
        Assert.Equal("192.168.1.1", result[0].ToString());
        Assert.Equal("192.168.1.5", result[^1].ToString());
    }

    [Fact]
    public void Parse_ShortRange_SingleNumber_ReturnsCorrectAddresses()
    {
        var result = CidrParser.Parse("192.168.1.10-20");

        Assert.Equal(11, result.Count);
        Assert.Equal("192.168.1.10", result[0].ToString());
        Assert.Equal("192.168.1.20", result[^1].ToString());
    }

    [Fact]
    public void Parse_FullRange_SingleAddress()
    {
        var result = CidrParser.Parse("192.168.1.5-192.168.1.5");

        Assert.Single(result);
        Assert.Equal("192.168.1.5", result[0].ToString());
    }

    [Fact]
    public void Parse_InvalidCidr_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CidrParser.Parse("not-an-ip/24"));
    }

    [Fact]
    public void Parse_InvalidPrefixLength_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CidrParser.Parse("192.168.1.0/33"));
    }

    [Fact]
    public void Parse_NegativePrefixLength_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CidrParser.Parse("192.168.1.0/-1"));
    }

    [Fact]
    public void Parse_RangeEndBeforeStart_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CidrParser.Parse("192.168.1.50-192.168.1.10"));
    }

    [Fact]
    public void Parse_ShortRangeInvalidOctet_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CidrParser.Parse("192.168.1.1-256"));
    }

    [Fact]
    public void Parse_InvalidRangeEnd_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CidrParser.Parse("192.168.1.1-abc"));
    }

    [Fact]
    public void Parse_TrimsWhitespace()
    {
        var result = CidrParser.Parse("  192.168.1.0/32  ");

        Assert.Single(result);
        Assert.Equal("192.168.1.0", result[0].ToString());
    }

    [Fact]
    public void Parse_Cidr16_ReturnsLargeRange()
    {
        var result = CidrParser.Parse("10.0.0.0/16");

        // /16 = 65536 total, minus network and broadcast = 65534
        Assert.Equal(65534, result.Count);
        Assert.Equal("10.0.0.1", result[0].ToString());
        Assert.Equal("10.0.255.254", result[^1].ToString());
    }
}
