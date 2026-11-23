using Crossdeck.Application.Devices;
using Crossdeck.Application.Profiles;
using Crossdeck.Application.Profiles.Validation;
using Crossdeck.Application.Routing;
using Crossdeck.Application.Routing.Input;
using Crossdeck.Application.Session;
using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Inputs;
using Crossdeck.Core.Profiles;
using Crossdeck.Core.Profiles.Models;
using Crossdeck.Core.Routing;
using Crossdeck.Core.Session;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Crossdeck.Application.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Theory, AutoNSubstituteData]
    public void AddSimulationServices_Always_ResolvesSameSimSessionSupervisorInstanceForAllItsInterfaces(
        TimeProvider timeProvider)
    {
        // Arrange
        var services = new ServiceCollection();
        services.TryAddSingleton(timeProvider);
        services.AddSingleton<ILogger<SimSessionSupervisor>>(NullLogger<SimSessionSupervisor>.Instance);

        var provider = services.AddSimulationServices().BuildServiceProvider();

        // Act
        var concrete = provider.GetRequiredService<SimSessionSupervisor>();
        var activeSim = provider.GetRequiredService<IActiveSim>();
        var supervisor = provider.GetRequiredService<ISimSessionSupervisor>();

        // Assert
        Assert.Same(concrete, activeSim);
        Assert.Same(concrete, supervisor);
    }

    [Theory, AutoNSubstituteData]
    public void AddSimulationServices_Always_ResolvesSameDeviceRegistryInstanceForIDeviceRegistry(
        TimeProvider timeProvider)
    {
        // Arrange
        var services = new ServiceCollection();
        services.TryAddSingleton(timeProvider);
        services.AddSingleton<ILogger<DeviceRegistry>>(NullLogger<DeviceRegistry>.Instance);
        services.AddSingleton<ILogger<SimSessionSupervisor>>(NullLogger<SimSessionSupervisor>.Instance);
        services.AddSingleton<ILogger<InputDispatcher>>(NullLogger<InputDispatcher>.Instance);
        services.AddSingleton<ILogger<SubscriptionAggregator>>(NullLogger<SubscriptionAggregator>.Instance);

        var provider = services.AddSimulationServices().BuildServiceProvider();

        // Act
        var concrete = provider.GetRequiredService<DeviceRegistry>();
        var registry = provider.GetRequiredService<IDeviceRegistry>();

        // Assert
        Assert.Same(concrete, registry);
    }

    [Theory, AutoNSubstituteData]
    public void AddSimulationServices_Always_ResolvesSameInputDispatcherInstanceForAllItsInterfaces(
        TimeProvider timeProvider)
    {
        // Arrange
        var services = new ServiceCollection();
        services.TryAddSingleton(timeProvider);
        services.AddSingleton<ILogger<SimSessionSupervisor>>(NullLogger<SimSessionSupervisor>.Instance);
        services.AddSingleton<ILogger<InputDispatcher>>(NullLogger<InputDispatcher>.Instance);
        services.AddSingleton<ILogger<SubscriptionAggregator>>(NullLogger<SubscriptionAggregator>.Instance);

        var provider = services.AddSimulationServices().BuildServiceProvider();

        // Act
        var concrete = provider.GetRequiredService<InputDispatcher>();
        var inputSink = provider.GetRequiredService<IInputSink>();
        var inputMonitor = provider.GetRequiredService<IInputMonitor>();
        var dispatcher = provider.GetRequiredService<IInputDispatcher>();

        // Assert
        Assert.Same(concrete, inputSink);
        Assert.Same(concrete, inputMonitor);
        Assert.Same(concrete, dispatcher);
    }

    [Theory, AutoNSubstituteData]
    public void AddSimulationServices_Always_ResolvesTheProfileRootValidatorForIValidatorOfProfile(
        TimeProvider timeProvider)
    {
        // Arrange
        var services = new ServiceCollection();
        services.TryAddSingleton(timeProvider);

        var provider = services.AddSimulationServices().BuildServiceProvider();

        // Act
        var validator = provider.GetRequiredService<IValidator<Profile>>();

        // Assert
        Assert.IsType<ProfileRootValidator>(validator);
    }

    [Theory, AutoNSubstituteData]
    public void AddSimulationServices_Always_ResolvesTheProfileValidatorForIProfileValidator(
        TimeProvider timeProvider)
    {
        // Arrange
        var services = new ServiceCollection();
        services.TryAddSingleton(timeProvider);

        var provider = services.AddSimulationServices().BuildServiceProvider();

        // Act
        var validator = provider.GetRequiredService<IProfileValidator>();

        // Assert
        Assert.IsType<ProfileValidator>(validator);
    }
}
