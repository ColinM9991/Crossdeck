using System.Threading.Channels;

namespace Crossdeck.Core;

public sealed class Signal
{
    private readonly Channel<bool> _channel;

    public Signal(
        bool singleConsumer = true,
        bool singleProducer = false)
    {
        _channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = singleConsumer,
            SingleWriter = singleProducer
        });
    }

    public IAsyncEnumerable<bool> WaitAsync(CancellationToken token) => _channel.Reader.ReadAllAsync(token);
    
    public void Set() => _channel.Writer.TryWrite(true);
    
    public async Task SetAsync(CancellationToken token) => await _channel.Writer.WriteAsync(true, token);
}