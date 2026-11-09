using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Idioms;
using AutoFixture.Xunit2;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid.Models;
using Crossdeck.Testing;
using NSubstitute;
using Xunit;

namespace Crossdeck.Devices.Hid.Tests;

public sealed class HidDeviceDriverTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(HidDeviceDriver).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public void Identity_Always_ReturnsConstructorValue(
        [Frozen] DeviceIdentity identity,
        HidDeviceDriver sut)
    {
        // Act
        var result = sut.Identity;

        // Assert
        Assert.Equal(identity, result);
    }

    [Theory, AutoNSubstituteData]
    public void Controls_Always_ReturnsDecoderControls(
        IReadOnlyList<DeviceControl> controls,
        [Frozen] IHidReportDecoder decoder,
        HidDeviceDriver sut)
    {
        // Arrange
        decoder.Controls.Returns(controls);

        // Act
        var result = sut.Controls;

        // Assert
        Assert.Same(controls, result);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenButtonValueIsNonZero_RaisesPressedInputEvent(
        ButtonControl button,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder,
        HidDeviceDriver sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        transport.MaxInputReportLength.Returns(8);
        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await cts.CancelAsync();
                return 8;
            });

        decoder.When(d => d.Decode(Arg.Any<byte[]>(), Arg.Any<List<HidInputChange>>()))
            .Do(call => call.ArgAt<List<HidInputChange>>(1).Add(new HidInputChange(button, 1)));

        DeviceInputEvent? raised = null;
        sut.InputReceived += (_, e) => raised = e;

        // Act
        await sut.RunAsync(cts.Token);

        // Assert
        Assert.Equal(InputEventKind.Pressed, raised?.Kind);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenButtonValueIsZero_RaisesReleasedInputEvent(
        ButtonControl button,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder,
        HidDeviceDriver sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        transport.MaxInputReportLength.Returns(8);
        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await cts.CancelAsync();
                return 8;
            });

        decoder.When(d => d.Decode(Arg.Any<byte[]>(), Arg.Any<List<HidInputChange>>()))
            .Do(call => call.ArgAt<List<HidInputChange>>(1).Add(new HidInputChange(button, 0)));

        DeviceInputEvent? raised = null;
        sut.InputReceived += (_, e) => raised = e;

        // Act
        await sut.RunAsync(cts.Token);

        // Assert
        Assert.Equal(InputEventKind.Released, raised?.Kind);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenAxisControlChanges_RaisesAxisChangedInputEvent(
        AxisControl axis,
        double value,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder,
        HidDeviceDriver sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        transport.MaxInputReportLength.Returns(8);
        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await cts.CancelAsync();
                return 8;
            });

        decoder.When(d => d.Decode(Arg.Any<byte[]>(), Arg.Any<List<HidInputChange>>()))
            .Do(call => call.ArgAt<List<HidInputChange>>(1).Add(new HidInputChange(axis, value)));

        DeviceInputEvent? raised = null;
        sut.InputReceived += (_, e) => raised = e;

        // Act
        await sut.RunAsync(cts.Token);

        // Assert
        Assert.Equal(InputEventKind.AxisChanged, raised?.Kind);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenHatControlChanges_RaisesHatChangedInputEvent(
        HatControl hat,
        double value,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder,
        HidDeviceDriver sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        transport.MaxInputReportLength.Returns(8);
        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await cts.CancelAsync();
                return 8;
            });

        decoder.When(d => d.Decode(Arg.Any<byte[]>(), Arg.Any<List<HidInputChange>>()))
            .Do(call => call.ArgAt<List<HidInputChange>>(1).Add(new HidInputChange(hat, value)));

        DeviceInputEvent? raised = null;
        sut.InputReceived += (_, e) => raised = e;

        // Act
        await sut.RunAsync(cts.Token);

        // Assert
        Assert.Equal(InputEventKind.HatChanged, raised?.Kind);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenConsecutiveReportsAreIdentical_SuppressesDuplicateEvent(
        ButtonControl button,
        [Frozen] IHidTransport transport,
        [Frozen] IHidReportDecoder decoder,
        HidDeviceDriver sut)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var readCount = 0;

        transport.MaxInputReportLength.Returns(8);
        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                readCount++;
                if (readCount >= 2) await cts.CancelAsync();
                return 8;
            });

        decoder.When(d => d.Decode(Arg.Any<byte[]>(), Arg.Any<List<HidInputChange>>()))
            .Do(call => call.ArgAt<List<HidInputChange>>(1).Add(new HidInputChange(button, 1)));

        var raisedCount = 0;
        sut.InputReceived += (_, _) => raisedCount++;

        // Act
        await sut.RunAsync(cts.Token);

        // Assert
        Assert.Equal(1, raisedCount);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenReadReturnsZero_StopsWithoutRaisingAnyEvent(
        [Frozen] IHidTransport transport,
        HidDeviceDriver sut)
    {
        // Arrange
        transport.MaxInputReportLength.Returns(8);
        transport.ReadAsync(Arg.Any<Memory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));

        var raisedCount = 0;
        sut.InputReceived += (_, _) => raisedCount++;

        // Act
        await sut.RunAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, raisedCount);
    }

    [Theory, AutoNSubstituteData]
    public async Task DisposeAsync_Always_DisposesUnderlyingTransport(
        [Frozen] IHidTransport transport,
        HidDeviceDriver sut)
    {
        // Act
        await sut.DisposeAsync();

        // Assert
        await transport.Received(1).DisposeAsync();
    }
}
