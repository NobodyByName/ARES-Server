namespace Ares.Common.Maps;

/// <summary>Why a map-collision file could not be read.</summary>
public enum MapCollisionFileError
{
    /// <summary>The data does not start with the map-collision magic.</summary>
    NotAMapCollisionFile,

    /// <summary>The file uses another format version; it must be written again by the current map tool.</summary>
    UnsupportedFormatVersion,

    /// <summary>The file was written for another game build; it must be written again from the current game.</summary>
    GameBuildMismatch,

    /// <summary>The file ends early.</summary>
    Truncated,

    /// <summary>The file's content is inconsistent.</summary>
    Malformed,
}

/// <summary>A map-collision file could not be read; <see cref="Error"/> tells why.</summary>
public sealed class MapCollisionFormatException : IOException
{
    /// <summary>Creates the exception.</summary>
    public MapCollisionFormatException(MapCollisionFileError error, string message)
        : base(message)
    {
        Error = error;
    }

    /// <summary>Why the file could not be read.</summary>
    public MapCollisionFileError Error { get; }
}
