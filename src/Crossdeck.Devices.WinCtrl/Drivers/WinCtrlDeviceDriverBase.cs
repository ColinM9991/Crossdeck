using Crossdeck.Core.Devices;
using Crossdeck.Core.Devices.Models;
using Crossdeck.Devices.Hid;

namespace Crossdeck.Devices.WinCtrl.Drivers;

public abstract class WinCtrlDeviceDriverBase : IDeviceDriver, IIndicatorOutput
{
    private readonly HidDeviceDriver _hid;

    protected WinCtrlDeviceDriverBase(HidDeviceDriver hid, WinCtrlOutputWriter writer)
    {
        _hid = hid;
        Writer = writer;
        _hid.InputReceived += (_, e) => InputReceived?.Invoke(this, e);
        Controls = [.. _hid.Controls];
    }

    public event EventHandler<DeviceInputEvent>? InputReceived;
    
    public DeviceIdentity Identity => _hid.Identity;
    
    protected WinCtrlOutputWriter Writer { get; }
    
    public IReadOnlyList<DeviceControl> Controls { get; }

    public virtual void Set(OutputId id, int value) => Writer.Set(id, value);

    public async Task RunAsync(CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Task[] tasks = [_hid.RunAsync(cts.Token), Writer.RunAsync(cts.Token)];
        await Task.WhenAny(tasks);
        await cts.CancelAsync();
        await Task.WhenAll(tasks);
    }

    public ValueTask DisposeAsync() => _hid.DisposeAsync();
}