using System.Buffers.Binary;
using Crossdeck.Devices.Moza.Protocol;
using Microsoft.Extensions.Logging;

namespace Crossdeck.Devices.Moza.Channels;

internal sealed class SettingsChannel(TimeProvider clock, ILogger logger)
{
    public const int Time = 0x02;
    public const int ProtocolVersion = 0x07;
    public const int CurrentFont = 0x15;
    public const int DisplayMode = 0x18;
    public const byte McduDisplayMode = 1;
    private const int PreambleHeaderLength = 8;

    private static readonly byte[] VersionPreamble = [0x07, 0x01, 0x00, 0x00, 0x00];
    private static readonly TimeSpan SyncWindow = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan WriteTimeout = TimeSpan.FromSeconds(5);

    private readonly Dictionary<int, byte[]> _values = [];

    private StreamConnection? _connection;
    private SettingsPhase _phase;
    private long _versionSentAt;
    private bool _sawFcdSetting;
    private (int Id, byte[] Data, long SentAt)? _pendingWrite;

    private bool _mcduRequested;
    private bool _mcduShown;
    private byte? _restoreDisplayMode;

    private bool Ready => _phase == SettingsPhase.Ready;

    private byte? DisplayModeNow => _values.TryGetValue(DisplayMode, out var v) && v.Length > 0 ? v[0] : null;

    public void Attach(StreamConnection connection)
    {
        _connection = connection;
        _phase = SettingsPhase.Preamble;
        _sawFcdSetting = false;
        _pendingWrite = null;
        _values.Clear();
    }

    public void Poll(long now)
    {
        if (_connection is null || _connection.Inbound.Length == 0)
            return;

        var inbound = _connection.Inbound;
        if (_phase == SettingsPhase.Preamble && !TryReadPreamble(inbound, now))
            return;

        ReadSettings(inbound);
        ApplyMcduPage();
    }

    public void OnTick(long now)
    {
        EndSyncWindowIfDue(now);
        ExpirePendingWrite(now);
        ApplyMcduPage();
    }

    public void ShowMcduPage()
    {
        _mcduRequested = true;
        ApplyMcduPage();
    }

    public void RestoreDisplayMode()
    {
        if (!_mcduShown || _restoreDisplayMode is not { } previous)
            return;

        Write(DisplayMode, [previous]);
        _mcduShown = false;
    }

    private void Write(int id, ReadOnlySpan<byte> data)
    {
        if (_connection is null)
        {
            logger.LogWarning("Dropped setting 0x{Id:X2} write: settings channel is not connected", id);
            return;
        }

        _connection.Send(SettingFrame.Encode(id, data));
        _pendingWrite = (id, data.ToArray(), clock.GetTimestamp());
    }

    private void ApplyMcduPage()
    {
        if (!_mcduRequested || _mcduShown || !Ready)
            return;

        _restoreDisplayMode ??= DisplayModeNow;
        if (_restoreDisplayMode is null)
            logger.LogWarning("displayMode was not reported during settings sync; it will not be restored on exit");

        Write(DisplayMode, [McduDisplayMode]);
        _mcduShown = true;
    }

    private void EndSyncWindowIfDue(long now)
    {
        if (_phase != SettingsPhase.Sync || clock.GetElapsedTime(_versionSentAt, now) < SyncWindow)
            return;

        if (_sawFcdSetting)
            _values.TryAdd(CurrentFont, [0x00, 0x00, 0x00, 0x00]);

        _phase = SettingsPhase.Ready;
    }

    private void ExpirePendingWrite(long now)
    {
        if (_pendingWrite is not { } pending || clock.GetElapsedTime(pending.SentAt, now) < WriteTimeout)
            return;

        logger.LogWarning("Setting 0x{Id:X2} write was not echoed within {Timeout}", pending.Id, WriteTimeout);
        _pendingWrite = null;
    }

    private bool TryReadPreamble(ByteQueue inbound, long now)
    {
        if (inbound.Length < PreambleHeaderLength)
            return false;

        var span = inbound.Span;
        ValidatePreamble(span);

        var mcuIdLength = span[6];
        var settingCount = span[7];
        var total = PreambleHeaderLength + mcuIdLength + settingCount;
        if (inbound.Length < total)
            return false;

        inbound.Consume(total);

        SendTimeAndVersion();
        _versionSentAt = now;
        _phase = SettingsPhase.Sync;
        return true;
    }

    private static void ValidatePreamble(ReadOnlySpan<byte> span)
    {
        if (span[..VersionPreamble.Length].SequenceEqual(VersionPreamble) && span[VersionPreamble.Length] == 0x00)
            return;

        throw new FcdSessionLostException(
            $"Unexpected settings preamble {Convert.ToHexString(span[..PreambleHeaderLength])}.");
    }

    private void SendTimeAndVersion()
    {
        var utcNow = clock.GetUtcNow();
        Span<byte> time = stackalloc byte[12];
        BinaryPrimitives.WriteUInt64LittleEndian(time, (ulong)utcNow.ToUnixTimeSeconds());
        BinaryPrimitives.WriteInt32LittleEndian(time[8..],
            (int)clock.LocalTimeZone.GetUtcOffset(utcNow).TotalSeconds);
        _connection!.Send(SettingFrame.Encode(Time, time));

        Span<byte> version = stackalloc byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(version, 3);
        BinaryPrimitives.WriteInt32LittleEndian(version[4..], 0);
        _connection.Send(SettingFrame.Encode(ProtocolVersion, version));
    }

    private void ReadSettings(ByteQueue inbound)
    {
        while (inbound.Length > 0)
        {
            if (!ReadNext(inbound))
                return;
        }
    }

    private bool ReadNext(ByteQueue inbound)
    {
        var first = inbound.Peek();
        if (first == SettingFrame.Header)
            return ReadSettingFrame(inbound);

        if (LegacySize(first) is { } size)
            return ReadLegacyValue(inbound, first, size);

        SkipUnrecognised(inbound);
        return true;
    }

    private bool ReadSettingFrame(ByteQueue inbound)
    {
        switch (SettingFrame.TryRead(inbound, out var id, out var data))
        {
            case SettingReadResult.Incomplete:
                return false;
            case SettingReadResult.BadCrc:
                logger.LogWarning("Dropped settings frame with bad CRC");
                break;
            case SettingReadResult.Frame:
                Cache(id, data);
                break;
        }

        return true;
    }

    private bool ReadLegacyValue(ByteQueue inbound, byte id, int size)
    {
        if (inbound.Length < 1 + size)
            return false;

        Cache(id, inbound.Span.Slice(1, size).ToArray());
        inbound.Consume(1 + size);
        return true;
    }

    private void SkipUnrecognised(ByteQueue inbound)
    {
        var next = inbound.Span.IndexOf(SettingFrame.Header);
        var skipped = next < 0 ? inbound.Length : next;
        logger.LogWarning("Skipped {Count} unrecognised bytes on the settings channel", skipped);
        inbound.Consume(skipped);
    }

    private void Cache(int id, byte[] data)
    {
        _values[id] = data;
        if (id is >= 0x13 and <= 0x1C or 0x21)
            _sawFcdSetting = true;

        ConfirmPendingWrite(id, data);
    }

    private void ConfirmPendingWrite(int id, byte[] data)
    {
        if (_pendingWrite is not { } pending || pending.Id != id || !pending.Data.AsSpan().SequenceEqual(data))
            return;

        _pendingWrite = null;
    }

    private static int? LegacySize(byte id) => id switch
    {
        0x01 => 4,
        0x05 => 1,
        0x04 => 8,
        0x06 => 1,
        _ => null
    };

    private enum SettingsPhase
    {
        Preamble,
        Sync,
        Ready
    }
}