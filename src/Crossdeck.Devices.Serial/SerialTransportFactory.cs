using System.IO.Ports;

namespace Crossdeck.Devices.Serial;

internal sealed class SerialTransportFactory : ISerialTransportFactory
{
    public ISerialTransport Create(SerialSettings serialSettings)
    {
        var serialPort = new SerialPort(serialSettings.PortName, serialSettings.BaudRate)
        {
            DtrEnable = serialSettings.DtrEnabled,
            RtsEnable = serialSettings.RtsEnabled,
            ReadTimeout = serialSettings.ReadTimeout,
            WriteTimeout = serialSettings.WriteTimeout
        };

        try
        {
            serialPort.Open();

            return new SerialTransport(serialPort);
        }
        catch
        {
            serialPort.Dispose();

            throw;
        }
        finally
        {
            if (serialPort.IsOpen)
                serialPort.Close();
        }
    }
}