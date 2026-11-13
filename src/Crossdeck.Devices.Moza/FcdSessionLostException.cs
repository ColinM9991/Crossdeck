namespace Crossdeck.Devices.Moza;

public sealed class FcdSessionLostException : Exception
{
    public FcdSessionLostException(string message)
        : base(message)
    {
    }

    public FcdSessionLostException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}