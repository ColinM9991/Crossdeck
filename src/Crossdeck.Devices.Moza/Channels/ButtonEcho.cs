using System.Text;
using System.Text.RegularExpressions;
using Crossdeck.Devices.Moza.Protocol;

namespace Crossdeck.Devices.Moza.Channels;

internal sealed partial class ButtonEcho
{
    private const byte RawPrologue = 0xFF;
    private const byte ClientVersionPackage = 0x03;
    private const byte RegistrationPackage = 0x04;
    private const byte UpdatePackage = 0x05;
    private const byte TokenPackage = 0x06;
    private const byte ServerVersionPackage = 0x07;
    private const byte BoolType = 0x00;

    private readonly Dictionary<int, byte> _indexByButton = [];
    private readonly HashSet<int> _down = [];

    private StreamConnection? _connection;
    private bool _prologueSeen;
    private bool _live;

    public void Attach(StreamConnection connection)
    {
        _connection = connection;
        _indexByButton.Clear();
        _prologueSeen = false;
        _live = false;
    }

    public void Poll()
    {
        if (_connection is null || _connection.Inbound.Length == 0)
            return;

        var inbound = _connection.Inbound;
        SkipPrologue(inbound);

        while (NetworkPackage.TryRead(inbound, out var id, out var payload))
            HandlePackage(id, payload);
    }

    public void OnButton(int button, bool pressed)
    {
        var changed = pressed ? _down.Add(button) : _down.Remove(button);
        if (changed && _live)
            Send(button, pressed);
    }

    public void ReleaseAll()
    {
        foreach (var button in _down.ToArray())
            OnButton(button, pressed: false);
    }

    private void SkipPrologue(ByteQueue inbound)
    {
        if (_prologueSeen)
            return;

        if (inbound.Peek() == RawPrologue)
            inbound.Consume(1);

        _prologueSeen = true;
    }

    private void HandlePackage(byte id, byte[] payload)
    {
        switch (id)
        {
            case ClientVersionPackage:
                _connection!.Send(NetworkPackage.Encode(ServerVersionPackage, [0x00, 0x00, 0x00, 0x00]));
                break;
            case RegistrationPackage when payload.Length > 0:
                OnRegistration(payload[0], ReadUrl(payload));
                break;
            case TokenPackage when payload.Length == 4:
                OnToken(payload);
                break;
        }
    }

    private void OnRegistration(byte index, string? url)
    {
        foreach (var button in _indexByButton.Where(x => x.Value == index).Select(x => x.Key).ToList())
            _indexByButton.Remove(button);

        if (url is not null && ButtonUrl().Match(url) is { Success: true } match)
            _indexByButton[int.Parse(match.Groups[1].Value)] = index;
    }

    private static string? ReadUrl(byte[] payload) =>
        payload.Length > 1 ? Encoding.UTF8.GetString(payload, 1, payload.Length - 1) : null;

    private void OnToken(ReadOnlySpan<byte> token)
    {
        _connection!.Send(NetworkPackage.Encode(TokenPackage, token));
        if (_live)
            return;

        _live = true;
        foreach (var button in _down)
            Send(button, pressed: true);
    }

    private void Send(int button, bool pressed)
    {
        if (_indexByButton.TryGetValue(button, out var index))
            _connection!.Send(NetworkPackage.Encode(UpdatePackage, [index, BoolType, pressed ? (byte)1 : (byte)0]));
    }

    [GeneratedRegex(@"/button/(0|[1-9][0-9]{0,2})\z")]
    private static partial Regex ButtonUrl();
}