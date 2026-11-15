using System.Text;
using Crossdeck.Devices.Moza.Channels;
using Crossdeck.Devices.Moza.Protocol;

namespace Crossdeck.Devices.Moza.Tests.Channels;

public sealed class ButtonEchoTests
{
    private const byte ClientVersionPackage = 0x03;
    private const byte ServerVersionPackage = 0x07;
    private const byte RegistrationPackage = 0x04;
    private const byte TokenPackage = 0x06;
    private const byte UpdatePackage = 0x05;
    private const long RetransmitStep = 6_000_000;

    [Fact]
    public void Poll_WhenPrologueIsPresent_SkipsItBeforeReadingPackages()
    {
        // Arrange
        var (sut, connection) = CreateSut();
        connection.Inbound.Append([0xFF]);
        connection.Inbound.Append(NetworkPackage.Encode(ClientVersionPackage, []));

        // Act
        sut.Poll();
        var packages = SentPackages(connection);

        // Assert
        var (id, _) = Assert.Single(packages);
        Assert.Equal(ServerVersionPackage, id);
    }

    [Fact]
    public void Poll_WhenClientVersionArrives_RespondsWithServerVersion()
    {
        // Arrange
        var (sut, connection) = CreateSut();
        connection.Inbound.Append(NetworkPackage.Encode(ClientVersionPackage, []));

        // Act
        sut.Poll();
        var packages = SentPackages(connection);

        // Assert
        var (id, payload) = Assert.Single(packages);
        Assert.Equal(ServerVersionPackage, id);
        Assert.Equal([0x00, 0x00, 0x00, 0x00], payload);
    }

    [Fact]
    public void Poll_WhenRegistrationArrives_StoresTheButtonIndexMapping()
    {
        // Arrange
        var (sut, connection) = CreateSut();
        connection.Inbound.Append(NetworkPackage.Encode(RegistrationPackage, RegistrationPayload(3, "/button/12")));
        connection.Inbound.Append(NetworkPackage.Encode(TokenPackage, [0x01, 0x02, 0x03, 0x04]));

        // Act
        sut.Poll();
        sut.OnButton(12, pressed: true);
        var packages = SentPackages(connection);

        // Assert
        var update = packages.Last(p => p.Id == UpdatePackage);
        Assert.Equal([3, 0x00, 0x01], update.Payload);
    }

    [Fact]
    public void Poll_WhenTokenArrives_MakesChannelLiveAndReplaysPressedButtons()
    {
        // Arrange
        var (sut, connection) = CreateSut();
        connection.Inbound.Append(NetworkPackage.Encode(RegistrationPackage, RegistrationPayload(3, "/button/12")));
        sut.OnButton(12, pressed: true);

        // Act
        connection.Inbound.Append(NetworkPackage.Encode(TokenPackage, [0x01, 0x02, 0x03, 0x04]));
        sut.Poll();
        var packages = SentPackages(connection);

        // Assert
        var tokenEcho = packages.Single(p => p.Id == TokenPackage);
        Assert.Equal([0x01, 0x02, 0x03, 0x04], tokenEcho.Payload);

        var update = packages.Single(p => p.Id == UpdatePackage);
        Assert.Equal([3, 0x00, 0x01], update.Payload);
    }

    [Fact]
    public void OnButton_WhenButtonIsNotRegistered_SendsNothing()
    {
        // Arrange
        var (sut, connection) = CreateSut();
        connection.Inbound.Append(NetworkPackage.Encode(TokenPackage, [0x01, 0x02, 0x03, 0x04]));
        sut.Poll();
        StreamHarness.Outgoing(connection, RetransmitStep);

        // Act
        sut.OnButton(99, pressed: true);
        var packages = SentPackages(connection);

        // Assert
        Assert.DoesNotContain(packages, p => p.Id == UpdatePackage);
    }

    [Fact]
    public void ReleaseAll_WhenButtonsAreDown_SendsReleaseUpdatesForEach()
    {
        // Arrange
        var (sut, connection) = CreateSut();
        connection.Inbound.Append(NetworkPackage.Encode(RegistrationPackage, RegistrationPayload(3, "/button/12")));
        connection.Inbound.Append(NetworkPackage.Encode(RegistrationPackage, RegistrationPayload(4, "/button/13")));
        connection.Inbound.Append(NetworkPackage.Encode(TokenPackage, [0x01, 0x02, 0x03, 0x04]));
        sut.Poll();
        sut.OnButton(12, pressed: true);
        sut.OnButton(13, pressed: true);
        StreamHarness.Outgoing(connection, RetransmitStep);

        // Act
        sut.ReleaseAll();
        var packages = SentPackages(connection);

        // Assert
        var releases = packages.Where(p => p.Id == UpdatePackage).OrderBy(p => p.Payload[0]).ToArray();
        Assert.Equal(2, releases.Length);
        Assert.Equal([3, 0x00, 0x00], releases[0].Payload);
        Assert.Equal([4, 0x00, 0x00], releases[1].Payload);
    }

    private static (ButtonEcho Sut, StreamConnection Connection) CreateSut()
    {
        var connection = StreamHarness.OpenedConnection(StreamMux.TelemetryService, 9060, 0);
        var sut = new ButtonEcho();
        sut.Attach(connection);
        return (sut, connection);
    }

    private static List<(byte Id, byte[] Payload)> SentPackages(StreamConnection connection)
    {
        var packages = new List<(byte, byte[])>();
        foreach (var wire in StreamHarness.Outgoing(connection, RetransmitStep))
        {
            var inner = StreamHarness.DecodeWire(wire).Inner;
            var queue = new ByteQueue();
            queue.Append(inner[6..^Wire.CrcSize]);
            if (!NetworkPackage.TryRead(queue, out var id, out var payload))
                throw new InvalidOperationException("package did not decode");
            packages.Add((id, payload));
        }

        return packages;
    }

    private static byte[] RegistrationPayload(byte index, string url) =>
        [index, .. Encoding.UTF8.GetBytes(url)];
}
