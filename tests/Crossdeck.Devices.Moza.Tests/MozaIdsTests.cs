using Crossdeck.Devices.Moza;

namespace Crossdeck.Devices.Moza.Tests;

public sealed class MozaIdsTests
{
    [Theory]
    [InlineData(0x346E, 0x1305, true)]
    [InlineData(0x346E, 0x1306, true)]
    [InlineData(0x346E, 0x1307, false)]
    [InlineData(0x1234, 0x1305, false)]
    [InlineData(0x1234, 0x1306, false)]
    public void IsFcdDisplay_WithGivenVendorAndProduct_ReturnsWhetherItIsAnFcdDisplay(
        int vendorId,
        int productId,
        bool expected)
    {
        // Act
        var result = MozaIds.IsFcdDisplay(vendorId, productId);

        // Assert
        Assert.Equal(expected, result);
    }
}
