namespace Crossdeck.Devices.Interop.Tests;

public sealed class NullUsbDeviceResolverTests
{
    [Theory]
    [AutoNSubstituteData]
    public void Resolve_WithHidPath_ReturnsNull(string hidPath)
    {
        // Arrange
        var sut = new NullUsbDeviceResolver();

        // Act
        var result = sut.Resolve(hidPath);

        // Assert
        Assert.Null(result);
    }
}
