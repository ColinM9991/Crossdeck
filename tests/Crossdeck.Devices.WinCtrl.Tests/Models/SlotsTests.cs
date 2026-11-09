using Crossdeck.Devices.WinCtrl.Models;

namespace Crossdeck.Devices.WinCtrl.Tests.Models;

public sealed class SlotsTests
{
    [Fact]
    public void Pap_WithoutDot_FirstBarIsNull()
    {
        // Act
        var slot = Slots.Pap(group: 2, bit: 0x10);

        // Assert
        Assert.Null(slot.Bars[0]);
    }

    [Fact]
    public void Pap_WithDot_FirstBarUsesTheGivenGroupAndBit()
    {
        // Act
        var slot = Slots.Pap(group: 2, bit: 0x10, hasDot: true);

        // Assert
        Assert.Equal(new BitAddr(2, 0x10), slot.Bars[0]);
    }

    [Fact]
    public void Pap_Always_ComputesByteAsIndexTimesFourPlusGroup()
    {
        // Act
        var slot = Slots.Pap(group: 2, bit: 0x10, hasDot: true);

        // Assert
        Assert.Equal(new BitAddr(1 * 4 + 2, 0x10), slot.Bars[1]);
        Assert.Equal(new BitAddr(7 * 4 + 2, 0x10), slot.Bars[7]);
    }

    [Fact]
    public void Fcu_WithoutDot_FirstBarIsNull()
    {
        // Act
        var slot = Slots.Fcu(packetByte: 25);

        // Assert
        Assert.Null(slot.Bars[0]);
    }

    [Fact]
    public void Fcu_Always_MapsSegmentIndexThroughFcuBitPermutation()
    {
        // Act
        var slot = Slots.Fcu(packetByte: 25, hasDot: true);

        // Assert
        Assert.Equal(new BitAddr(0, 1 << 0), slot.Bars[0]);
        Assert.Equal(new BitAddr(0, 1 << 2), slot.Bars[1]);
        Assert.Equal(new BitAddr(0, 1 << 3), slot.Bars[2]);
    }

    [Fact]
    public void Efis_Always_MapsSegmentIndexThroughEfisBitPermutation()
    {
        // Act
        var slot = Slots.Efis(packetByte: 25, hasDot: true);

        // Assert
        Assert.Equal(new BitAddr(0, 1 << 7), slot.Bars[0]);
        Assert.Equal(new BitAddr(0, 1 << 1), slot.Bars[1]);
        Assert.Equal(new BitAddr(0, 1 << 0), slot.Bars[2]);
    }

    [Fact]
    public void FcuShifted_Always_FirstBarIsNull()
    {
        // Act
        var slot = Slots.FcuShifted(start: 28, i: 0);

        // Assert
        Assert.Null(slot.Bars[0]);
    }

    [Fact]
    public void FcuShifted_WhenFcuBitIsBelowFour_StaysOnTheSameByteAndShiftsMaskUp()
    {
        // Act
        var slot = Slots.FcuShifted(start: 28, i: 0);

        // Assert: FcuBit[1] == 2, which is < 4
        Assert.Equal(new BitAddr(28 + 0 - 25, 1 << (2 + 4)), slot.Bars[1]);
    }

    [Fact]
    public void FcuShifted_WhenFcuBitIsFourOrAbove_AdvancesTheByteAndShiftsMaskDown()
    {
        // Act
        var slot = Slots.FcuShifted(start: 28, i: 0);

        // Assert: FcuBit[4] == 4, which is >= 4
        Assert.Equal(new BitAddr(28 + 0 + 1 - 25, 1 << (4 - 4)), slot.Bars[4]);
    }
}
