using Crossdeck.Application.Devices;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Inputs;
using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Application.Tests.Devices;

public sealed class DeviceRegistryTests
{
    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenWatcherRaisesDeviceArrivedForMatchingFactory_CreatesDriverFromThatFactory(
        DeviceDescriptor descriptor,
        IDeviceWatcher watcher,
        IDeviceDriverFactory matchingFactory,
        IDeviceDriverFactory nonMatchingFactory,
        IInputSink inputSink,
        IDeviceDriver driver)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        watcher.RunAsync(Arg.Any<CancellationToken>())
            .Returns(ci => Task.Delay(Timeout.Infinite, ci.Arg<CancellationToken>()));

        nonMatchingFactory.CanCreate(Arg.Any<DeviceDescriptor>()).Returns(false);
        matchingFactory.CanCreate(descriptor).Returns(true);
        matchingFactory.CreateAsync(descriptor, Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await cts.CancelAsync();
                return (IDeviceDriver?)driver;
            });
        driver.RunAsync(Arg.Any<CancellationToken>())
            .Returns(ci => Task.Delay(Timeout.Infinite, ci.Arg<CancellationToken>()));

        var sut = new DeviceRegistry([watcher], [nonMatchingFactory, matchingFactory], inputSink);

        // Act
        var task = sut.RunAsync(cts.Token);
        watcher.DeviceArrived += Raise.Event<EventHandler<DeviceArrivedEventArgs>>(watcher, new DeviceArrivedEventArgs(descriptor));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);

        // Assert
        await matchingFactory.Received(1).CreateAsync(descriptor, Arg.Any<CancellationToken>());
        await nonMatchingFactory.DidNotReceive().CreateAsync(Arg.Any<DeviceDescriptor>(), Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenNoFactoryCanCreateDevice_DoesNotCreateAnyDriver(
        DeviceDescriptor descriptor,
        IDeviceWatcher watcher,
        IDeviceDriverFactory factory,
        IInputSink inputSink)
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        watcher.RunAsync(Arg.Any<CancellationToken>())
            .Returns(ci => Task.Delay(Timeout.Infinite, ci.Arg<CancellationToken>()));
        factory.CanCreate(descriptor).Returns(callInfo =>
        {
            cts.Cancel();
            return false;
        });

        var sut = new DeviceRegistry([watcher], [factory], inputSink);

        // Act
        var task = sut.RunAsync(cts.Token);
        watcher.DeviceArrived += Raise.Event<EventHandler<DeviceArrivedEventArgs>>(watcher, new DeviceArrivedEventArgs(descriptor));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);

        // Assert
        await factory.DidNotReceive().CreateAsync(Arg.Any<DeviceDescriptor>(), Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenWatcherRaisesDeviceRemovedForRunningDriver_StopsAndDisposesDriver(
        DeviceDescriptor descriptor,
        DeviceIdentity identity,
        IDeviceWatcher watcher,
        IDeviceDriverFactory factory,
        IInputSink inputSink,
        IDeviceDriver driver)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        descriptor = descriptor with { Identity = identity };

        watcher.RunAsync(Arg.Any<CancellationToken>())
            .Returns(ci => Task.Delay(Timeout.Infinite, ci.Arg<CancellationToken>()));

        driver.Identity.Returns(identity);
        factory.CanCreate(descriptor).Returns(true);
        factory.CreateAsync(descriptor, Arg.Any<CancellationToken>()).Returns(driver);
        driver.RunAsync(Arg.Any<CancellationToken>())
            .Returns(ci => Task.Delay(Timeout.Infinite, ci.Arg<CancellationToken>()));
        driver.DisposeAsync().Returns(_ =>
        {
            cts.Cancel();
            return ValueTask.CompletedTask;
        });

        var sut = new DeviceRegistry([watcher], [factory], inputSink);

        // Act
        var task = sut.RunAsync(cts.Token);
        watcher.DeviceArrived += Raise.Event<EventHandler<DeviceArrivedEventArgs>>(watcher, new DeviceArrivedEventArgs(descriptor));
        watcher.DeviceRemoved += Raise.Event<EventHandler<DeviceRemovedEventArgs>>(watcher, new DeviceRemovedEventArgs(identity));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);

        // Assert
        await driver.Received(1).DisposeAsync();
    }
}
