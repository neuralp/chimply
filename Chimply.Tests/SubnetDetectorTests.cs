using Chimply.Services;

namespace Chimply.Tests;

public class SubnetDetectorTests
{
    [Fact]
    public void DetectLocalSubnet_ReturnsNullOrValidCidr()
    {
        var result = SubnetDetector.DetectLocalSubnet();

        if (result != null)
        {
            Assert.Contains("/", result);
            var parts = result.Split('/');
            Assert.Equal(2, parts.Length);
            Assert.True(System.Net.IPAddress.TryParse(parts[0], out _),
                $"Network address '{parts[0]}' should be a valid IP");
            Assert.True(int.TryParse(parts[1], out var prefix),
                $"Prefix '{parts[1]}' should be a number");
            Assert.InRange(prefix, 0, 32);
        }
    }
}
