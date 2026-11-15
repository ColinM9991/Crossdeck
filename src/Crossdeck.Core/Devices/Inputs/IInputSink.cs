using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices.Inputs;

/// <summary>
/// Receives device input events.
/// </summary>
public interface IInputSink
{
    /// <summary>
    /// Posts an input event for dispatch.
    /// </summary>
    /// <param name="inputEvent">The input event to post.</param>
    void Post(DeviceInputEvent inputEvent);
}