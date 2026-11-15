using Crossdeck.Core.Devices.Models;

namespace Crossdeck.Core.Devices;

/// <summary>
/// Sets the state of indicator outputs on a device.
/// </summary>
public interface IIndicatorOutput
{
    /// <summary>
    /// Sets the value of an indicator.
    /// </summary>
    /// <param name="id">The identity of the indicator to set.</param>
    /// <param name="value">The value to apply to the indicator.</param>
    void Set(OutputId id, int value);
}