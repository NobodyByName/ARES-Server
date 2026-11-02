namespace Ares.Structs.Core.Serialization;

/// <summary>Malformed, truncated or out-of-range network data.</summary>
public class InvalidPacketException : FormatException
{
    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">What is wrong with the data.</param>
    public InvalidPacketException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and its cause.</summary>
    /// <param name="message">What is wrong with the data.</param>
    /// <param name="innerException">The cause.</param>
    public InvalidPacketException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
