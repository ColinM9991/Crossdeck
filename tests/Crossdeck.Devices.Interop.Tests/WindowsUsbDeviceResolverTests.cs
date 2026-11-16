using System.Runtime.Versioning;

namespace Crossdeck.Devices.Interop.Tests;

[SupportedOSPlatform("windows")]
public sealed class WindowsUsbDeviceResolverTests
{
    [Theory]
    [AutoNSubstituteData]
    public void Resolve_WithUnresolvableHidPath_ReturnsNull(string hidPath)
    {
        // Arrange
        var sut = new WindowsUsbDeviceResolver();

        // Act
        var result = sut.Resolve(hidPath);

        // Assert
        Assert.Null(result);
    }
}
