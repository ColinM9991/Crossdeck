using Crossdeck.Core.Devices.Display.Models;
using Crossdeck.Devices.Moza.Protocol;

namespace Crossdeck.Devices.Moza.Channels;

internal sealed class McduChannel(SettingsChannel settings)
{
    private const int MaxDeltaRows = 6;
    private const int ClientCapabilityLength = 5;

    private StreamConnection? _connection;
    private TextScreen? _sent;
    private uint _sequence;
    private bool _pageShown;

    public int? PageIndex { get; private set; }

    public void Attach(StreamConnection connection)
    {
        _connection = connection;
        _sent = null;
        _sequence = 0;
        PageIndex = null;
        _pageShown = false;
        connection.Send(McduFrameEncoder.InitConfig());
    }

    public void Poll()
    {
        if (_connection is null || _connection.Inbound.Length == 0)
            return;

        while (NetworkPackage.TryRead(_connection.Inbound, out var id, out var payload))
            HandlePackage(id, payload);
    }

    public void TrySend(TextScreen? latest)
    {
        if (latest is null || _connection is not { IsIdle: true } connection || PageIndex is null)
            return;

        ShowPageAfterFirstKeyframe();

        if (ReferenceEquals(latest, _sent))
            return;

        SendFrame(connection, latest);
        _sent = latest;
    }

    private void HandlePackage(byte id, byte[] payload)
    {
        if (id != McduFrameEncoder.ClientCapabilityPackage || payload.Length < ClientCapabilityLength)
            return;

        PageIndex = payload[4];
    }

    private void ShowPageAfterFirstKeyframe()
    {
        if (_sent is null || _pageShown)
            return;

        settings.ShowMcduPage();
        _pageShown = true;
    }

    private void SendFrame(StreamConnection connection, TextScreen latest)
    {
        var rows = _sent is null ? McduFrameEncoder.AllRows : McduFrameEncoder.ChangedRows(_sent, latest);
        if (rows.Count == 0)
            return;

        var keyframe = _sent is null || rows.Count > MaxDeltaRows;
        var frameRows = keyframe ? McduFrameEncoder.AllRows : rows;
        connection.Send(McduFrameEncoder.Encode(++_sequence, latest, frameRows, keyframe));
    }
}