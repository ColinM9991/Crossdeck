using Crossdeck.Devices.Hid.Models;

namespace Crossdeck.Devices.Hid.Tests.Models;

public sealed class HidUsageTests
{
    [Theory, AutoNSubstituteData]
    public void FromRaw_WithRawValue_SplitsIntoPageAndId(ushort page, ushort id)
    {
        // Arrange
        var raw = ((uint)page << 16) | id;

        // Act
        var usage = HidUsage.FromRaw(raw);

        // Assert
        Assert.Equal(page, usage.Page);
        Assert.Equal(id, usage.Id);
    }

    [Theory, AutoNSubstituteData]
    public void Raw_AfterFromRaw_ReturnsOriginalRawValue(ushort page, ushort id)
    {
        // Arrange
        var raw = ((uint)page << 16) | id;
        var usage = HidUsage.FromRaw(raw);

        // Act
        var result = usage.Raw;

        // Assert
        Assert.Equal(raw, result);
    }

    [Theory, AutoNSubstituteData]
    public void ImplicitConversion_FromRawUInt_ProducesEquivalentHidUsage(ushort page, ushort id)
    {
        // Arrange
        var raw = ((uint)page << 16) | id;

        // Act
        HidUsage usage = raw;

        // Assert
        Assert.Equal(HidUsage.FromRaw(raw), usage);
    }

    [Theory, AutoNSubstituteData]
    public void ToString_Always_FormatsAsPageAndIdHex(ushort page, ushort id)
    {
        // Arrange
        var usage = new HidUsage(page, id);

        // Act
        var result = usage.ToString();

        // Assert
        Assert.Equal($"0x{page:X4}_{id:X4}", result);
    }
}
