using Chimply.Converters;
using Chimply.Models;

namespace Chimply.Tests;

public class IpAddressComparerTests
{
    private readonly IpAddressComparer _comparer = IpAddressComparer.Instance;

    [Fact]
    public void Compare_LowerIpFirst_ReturnsNegative()
    {
        var a = new ScanResult { IpAddress = "192.168.1.1" };
        var b = new ScanResult { IpAddress = "192.168.1.10" };

        Assert.True(_comparer.Compare(a, b) < 0);
    }

    [Fact]
    public void Compare_HigherIpFirst_ReturnsPositive()
    {
        var a = new ScanResult { IpAddress = "192.168.1.10" };
        var b = new ScanResult { IpAddress = "192.168.1.2" };

        Assert.True(_comparer.Compare(a, b) > 0);
    }

    [Fact]
    public void Compare_EqualIps_ReturnsZero()
    {
        var a = new ScanResult { IpAddress = "192.168.1.1" };
        var b = new ScanResult { IpAddress = "192.168.1.1" };

        Assert.Equal(0, _comparer.Compare(a, b));
    }

    [Fact]
    public void Compare_DifferentSubnets_ComparesCorrectly()
    {
        var a = new ScanResult { IpAddress = "10.0.0.1" };
        var b = new ScanResult { IpAddress = "192.168.1.1" };

        Assert.True(_comparer.Compare(a, b) < 0);
    }

    [Fact]
    public void Compare_NonScanResultObjects_ReturnsZero()
    {
        Assert.Equal(0, _comparer.Compare("not a scan result", 42));
    }

    [Fact]
    public void Compare_NullObjects_ReturnsZero()
    {
        Assert.Equal(0, _comparer.Compare(null, null));
    }

    [Fact]
    public void Compare_NumericSortingNotLexicographic()
    {
        // Lexicographic: "192.168.1.9" > "192.168.1.10" (because '9' > '1')
        // Numeric:       "192.168.1.9" < "192.168.1.10"
        var a = new ScanResult { IpAddress = "192.168.1.9" };
        var b = new ScanResult { IpAddress = "192.168.1.10" };

        Assert.True(_comparer.Compare(a, b) < 0);
    }
}
