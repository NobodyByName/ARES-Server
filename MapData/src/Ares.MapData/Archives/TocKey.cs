namespace Ares.MapData.Archives;

/// <summary>
/// The 32-byte key of the containers' tables of contents (<c>.utoc</c>), supplied by the operator in a file of its own.
/// </summary>
internal sealed class TocKey
{
    private const int KeyBytes = 32;

    private TocKey(byte[] bytes)
    {
        Bytes = bytes;
    }

    public byte[] Bytes { get; }

    /// <summary>
    /// Parses a TOC key file: the key as 64 hex digits with an optional <c>0x</c> prefix. Whitespace and line breaks
    /// inside the key are ignored; <c>#</c> starts a comment. Error messages never contain the file's text.
    /// </summary>
    /// <exception cref="FormatException">The text is not a single 64-digit hex key.</exception>
    public static TocKey Parse(string text)
    {
        string hex = string.Concat(text.Split('\n').Select(line => line.Split('#')[0]).SelectMany(line => line.Where(c => !char.IsWhiteSpace(c))));
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hex = hex[2..];
        }

        if (hex.Length != KeyBytes * 2 || !hex.All(char.IsAsciiHexDigit))
        {
            throw new FormatException($"expected one key of {KeyBytes * 2} hex digits");
        }

        return new TocKey(Convert.FromHexString(hex));
    }
}

/// <summary>A table of contents did not decrypt to a valid table with the given <see cref="TocKey"/>.</summary>
internal sealed class WrongTocKeyException(string message) : Exception(message);
