using Crossdeck.Application.Devices;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Inputs;
using Crossdeck.Core.Devices.Models;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Application.Tests.Devices;

public sealed class DeviceRegistryTests
{
    [Fact]
    public void Constructors_VerifyGuardClauses()
    {
        // Arrange
        var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
        var assertion = new GuardClauseAssertion(fixture);

        // Act / Assert
        assertion.Verify(typeof(DeviceRegistry).GetConstructors());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenWatcherRaisesDeviceArrivedForMatchingFactory_CreatesDriverFromThatFactory(
        DeviceDescriptor descriptor,
        IDeviceWatcher watcher,
        IDeviceDriverFactory matchingFactory,
        IDeviceDriverFactory nonMatchingFactory,
        IInputSink inputSink,
        ILogger<DeviceRegistry> logger,
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

        var sut = new DeviceRegistry([watcher], [nonMatchingFactory, matchingFactory], inputSink, logger);

        // Act
        var task = sut.RunAsync(cts.Token);
        watcher.DeviceArrived += Raise.Event<EventHandler<DeviceArrivedEventArgs>>(watcher, new DeviceArrivedEventArgs(descriptor));

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        await matchingFactory.Received(1).CreateAsync(descriptor, Arg.Any<CancellationToken>());
        await nonMatchingFactory.DidNotReceive().CreateAsync(Arg.Any<DeviceDescriptor>(), Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenNoFactoryCanCreateDevice_DoesNotCreateAnyDriver(
        DeviceDescriptor descriptor,
        IDeviceWatcher watcher,
        IDeviceDriverFactory factory,
        IInputSink inputSink,
        ILogger<DeviceRegistry> logger)
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

        var sut = new DeviceRegistry([watcher], [factory], inputSink, logger);

        // Act
        var task = sut.RunAsync(cts.Token);
        watcher.DeviceArrived += Raise.Event<EventHandler<DeviceArrivedEventArgs>>(watcher, new DeviceArrivedEventArgs(descriptor));

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        await factory.DidNotReceive().CreateAsync(Arg.Any<DeviceDescriptor>(), Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenWatcherRaisesDeviceRemovedForRunningDriver_StopsAndDisposesDriver(
        DeviceDescriptor descriptor,
        DeviceIdentity identity,
        IDeviceWatcher watcher,
        IDeviceDriverFactory factory,
        IInputSink inputSink,
        ILogger<DeviceRegistry> logger,
        IDeviceDriver driver)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        descriptor = descriptor with { Identity = identity };

        watcher.RunAsync(Arg.Any<CancellationToken>())
            .Returns(ci => Task.Delay(Timeout.Infinite, ci.Arg<CancellationToken>()));

        driver.Identity.Returns(identity);
        driver.RunAsync(Arg.Any<CancellationToken>())
            .Returns(ci => Task.Delay(Timeout.Infinite, ci.Arg<CancellationToken>()));
        driver.DisposeAsync().Returns(_ =>
        {
            cts.Cancel();
            return ValueTask.CompletedTask;
        });

        factory.CanCreate(descriptor).Returns(true);
        factory.CreateAsync(descriptor, Arg.Any<CancellationToken>()).Returns(driver);

        var sut = new DeviceRegistry([watcher], [factory], inputSink, logger);

        // Act
        var task = sut.RunAsync(cts.Token);
        watcher.DeviceArrived += Raise.Event<EventHandler<DeviceArrivedEventArgs>>(watcher, new DeviceArrivedEventArgs(descriptor));
        watcher.DeviceRemoved += Raise.Event<EventHandler<DeviceRemovedEventArgs>>(watcher, new DeviceRemovedEventArgs(identity));

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        await driver.Received(1).DisposeAsync();
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenDriverFaults_RestartsDriverAfterRetryDelay(
        DeviceDescriptor descriptor,
        DeviceIdentity identity,
        IDeviceWatcher watcher,
        IDeviceDriverFactory factory,
        IInputSink inputSink,
        ILogger<DeviceRegistry> logger,
        IDeviceDriver driver)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var restarted = new TaskCompletionSource();
        var runCalls = 0;
        var addedIdentities = new List<DeviceIdentity>();
        descriptor = descriptor with { Identity = identity };

        watcher.RunAsync(Arg.Any<CancellationToken>())
            .Returns(ci => Task.Delay(Timeout.Infinite, ci.Arg<CancellationToken>()));

        driver.Identity.Returns(identity);
        driver.RunAsync(Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                runCalls++;
                if (runCalls == 1)
                    return Task.FromException(new InvalidOperationException());

                restarted.TrySetResult();
                return Task.Delay(Timeout.Infinite, call.Arg<CancellationToken>());
            });

        factory.CanCreate(descriptor).Returns(true);
        factory.CreateAsync(descriptor, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDeviceDriver?>(driver));

        var sut = new DeviceRegistry([watcher], [factory], inputSink, logger, TimeSpan.FromMilliseconds(100));
        sut.DriverAdded += (_, e) => addedIdentities.Add(e.Identity);

        // Act
        var task = sut.RunAsync(cts.Token);
        watcher.DeviceArrived += Raise.Event<EventHandler<DeviceArrivedEventArgs>>(watcher, new DeviceArrivedEventArgs(descriptor));
        await restarted.Task;
        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        await factory.Received(2).CreateAsync(descriptor, Arg.Any<CancellationToken>());
        await driver.Received(2).RunAsync(Arg.Any<CancellationToken>());
        Assert.Equal([identity, identity], addedIdentities);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenDriverFaults_DisposesFaultedDriver(
        DeviceDescriptor descriptor,
        DeviceIdentity identity,
        IDeviceWatcher watcher,
        IDeviceDriverFactory factory,
        IInputSink inputSink,
        ILogger<DeviceRegistry> logger,
        IDeviceDriver faultedDriver)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var faultedDriverDisposed = new TaskCompletionSource();
        var removedIdentities = new List<DeviceIdentity>();
        descriptor = descriptor with { Identity = identity };

        watcher.RunAsync(Arg.Any<CancellationToken>())
            .Returns(ci => Task.Delay(Timeout.Infinite, ci.Arg<CancellationToken>()));

        faultedDriver.Identity.Returns(identity);
        faultedDriver.RunAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException()));
        faultedDriver.DisposeAsync().Returns(_ =>
        {
            faultedDriverDisposed.TrySetResult();
            return ValueTask.CompletedTask;
        });

        factory.CanCreate(descriptor).Returns(true);
        factory.CreateAsync(descriptor, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDeviceDriver?>(faultedDriver));

        var sut = new DeviceRegistry([watcher], [factory], inputSink, logger, TimeSpan.FromMilliseconds(100));
        sut.DriverRemoved += (_, e) => removedIdentities.Add(e.Identity);

        // Act
        var task = sut.RunAsync(cts.Token);
        watcher.DeviceArrived += Raise.Event<EventHandler<DeviceArrivedEventArgs>>(watcher, new DeviceArrivedEventArgs(descriptor));
        await faultedDriverDisposed.Task;
        await cts.CancelAsync();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        await faultedDriver.Received(1).DisposeAsync();
        Assert.Equal([identity], removedIdentities);
    }

    [Theory, AutoNSubstituteData]
    public async Task RunAsync_WhenDriverFaultsWhileShuttingDown_DoesNotRestartDriver(
        DeviceDescriptor descriptor,
        DeviceIdentity identity,
        IDeviceWatcher watcher,
        IDeviceDriverFactory factory,
        IInputSink inputSink,
        ILogger<DeviceRegistry> logger,
        IDeviceDriver faultedDriver)
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var faultedDriverDisposed = new TaskCompletionSource();
        descriptor = descriptor with { Identity = identity };

        watcher.RunAsync(Arg.Any<CancellationToken>())
            .Returns(ci => Task.Delay(Timeout.Infinite, ci.Arg<CancellationToken>()));

        faultedDriver.Identity.Returns(identity);
        faultedDriver.RunAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                return Task.FromException(new InvalidOperationException());
            });
        faultedDriver.DisposeAsync().Returns(_ =>
        {
            faultedDriverDisposed.TrySetResult();
            return ValueTask.CompletedTask;
        });

        factory.CanCreate(descriptor).Returns(true);
        factory.CreateAsync(descriptor, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IDeviceDriver?>(faultedDriver));

        var sut = new DeviceRegistry([watcher], [factory], inputSink, logger, TimeSpan.FromMilliseconds(100));

        // Act
        var task = sut.RunAsync(cts.Token);
        watcher.DeviceArrived += Raise.Event<EventHandler<DeviceArrivedEventArgs>>(watcher, new DeviceArrivedEventArgs(descriptor));
        await faultedDriverDisposed.Task;

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        await factory.Received(1).CreateAsync(descriptor, Arg.Any<CancellationToken>());
    }
}
