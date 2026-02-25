using Chimply.Services;

namespace Chimply.Tests;

public class MacOuiLookupTests
{
    [Fact]
    public void Lookup_NullMac_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, MacOuiLookup.Lookup(null));
    }

    [Fact]
    public void Lookup_EmptyMac_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, MacOuiLookup.Lookup(""));
    }

    [Fact]
    public void Lookup_ShortMac_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, MacOuiLookup.Lookup("AA:BB"));
    }

    [Fact]
    public void Lookup_WhitespaceMac_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, MacOuiLookup.Lookup("   "));
    }

    [Fact]
    public void Lookup_UnknownOui_ReturnsEmpty()
    {
        // FF:FF:FF is unlikely to be a registered OUI
        Assert.Equal(string.Empty, MacOuiLookup.Lookup("FF:FF:FF:00:00:00"));
    }

    [Fact]
    public void Lookup_ValidMacFormat_DoesNotThrow()
    {
        // Just ensure it doesn't throw for a valid-format MAC
        var result = MacOuiLookup.Lookup("00:00:00:00:00:00");
        Assert.IsType<string>(result);
    }
}
