namespace Ares.Common.GameData;

/// <summary>A game data directory cannot be used. The message tells the operator why and what to do.</summary>
public sealed class GameDataException : Exception
{
    /// <summary>Creates the exception.</summary>
    public GameDataException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with the failure that caused it.</summary>
    public GameDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
