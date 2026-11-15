using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices.Inputs;

/// <summary>
/// Exposes recent device input events as an asynchronous stream.
/// </summary>
public interface IInputMonitor
{
    /// <summary>
    /// The most recent device input events, as an asynchronous stream.
    /// </summary>
    IAsyncEnumerable<DeviceInputEvent> Recent { get; }
}