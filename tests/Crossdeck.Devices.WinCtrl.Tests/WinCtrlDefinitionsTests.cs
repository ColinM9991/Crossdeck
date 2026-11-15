namespace Crossdeck.Devices.WinCtrl.Tests;

public sealed class WinCtrlDefinitionsTests
{
    [Theory]
    [InlineData(0xBB32, "mcdu")]
    [InlineData(0xBB31, "pfp-3n")]
    [InlineData(0xBB34, "pfp-4")]
    [InlineData(0xBB33, "pfp-7")]
    [InlineData(0xBF0F, "pap3")]
    [InlineData(0xBB10, "fcu")]
    [InlineData(0xBB60, "pdc-3n")]
    [InlineData(0xBB50, "pdc-3m")]
    public void ForPid_WhenPidIsKnownBase_ReturnsTheDefinition(int pid, string part)
    {
        // Act
        var definition = WinCtrlDefinitions.ForPid(pid);

        // Assert
        var resolved = Assert.IsType<WinCtrlDefinition>(definition);
        Assert.Equal(part, resolved.Part);
        Assert.Equal((ushort)pid, resolved.BasePid);
    }

    [Theory]
    [InlineData(0xBB36, 0xBB32)]
    [InlineData(0xBB3A, 0xBB32)]
    [InlineData(0xBB3E, 0xBB32)]
    [InlineData(0xBB35, 0xBB31)]
    [InlineData(0xBB3D, 0xBB31)]
    [InlineData(0xBB39, 0xBB31)]
    [InlineData(0xBB38, 0xBB34)]
    [InlineData(0xBB40, 0xBB34)]
    [InlineData(0xBB3C, 0xBB34)]
    [InlineData(0xBB37, 0xBB33)]
    [InlineData(0xBB3F, 0xBB33)]
    [InlineData(0xBB3B, 0xBB33)]
    [InlineData(0xBC1D, 0xBB10)]
    [InlineData(0xBC1E, 0xBB10)]
    [InlineData(0xBA01, 0xBB10)]
    [InlineData(0xBB61, 0xBB60)]
    [InlineData(0xBB62, 0xBB60)]
    [InlineData(0xBB51, 0xBB50)]
    [InlineData(0xBB52, 0xBB50)]
    public void ForPid_WhenPidIsAVariant_ReturnsTheBaseDefinition(int variantPid, int basePid)
    {
        // Act
        var definition = WinCtrlDefinitions.ForPid(variantPid);

        // Assert
        Assert.Same(WinCtrlDefinitions.ForPid(basePid), definition);
    }

    [Fact]
    public void ForPid_WhenPidIsUnknown_ReturnsNull()
    {
        // Act
        var definition = WinCtrlDefinitions.ForPid(0x1234);

        // Assert
        Assert.Null(definition);
    }
}
