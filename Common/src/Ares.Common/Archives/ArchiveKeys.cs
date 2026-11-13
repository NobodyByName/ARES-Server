using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;

namespace Ares.Common.Archives;

/// <summary>The game's archive keys, supplied by the operator in a keys file.</summary>
/// <param name="PakKey">The 32-byte pak key.</param>
/// <param name="ContainerKey">The eight container key words.</param>
public sealed record ArchiveKeys(byte[] PakKey, IReadOnlyList<uint> ContainerKey)
{
    private const int PakKeyBytes = 32;
    private const int ContainerKeyWords = 8;
    private const string PakKeyName = "pak-key";
    private const string ContainerKeyName = "container-key";

    /// <summary>
    /// Parses a keys file: a <c>pak-key = …</c> line (64 hex digits) and a <c>container-key = …</c> line
    /// (eight hex words separated by commas or spaces). <c>0x</c> prefixes are optional; <c>#</c> starts a comment.
    /// </summary>
    /// <exception cref="FormatException">The text is not a valid keys file.</exception>
    public static ArchiveKeys Parse(string text)
    {
        string? pakKey = null;
        string? containerKey = null;
        foreach (string rawLine in text.Split('\n'))
        {
            string line = rawLine.Split('#')[0].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            int separator = line.IndexOf('=', StringComparison.Ordinal);
            if (separator < 0)
            {
                throw new FormatException($"Expected '{PakKeyName} = …' or '{ContainerKeyName} = …', got '{line}'");
            }

            string name = line[..separator].Trim();
            string value = line[(separator + 1)..].Trim();
            switch (name)
            {
                case PakKeyName when pakKey is null:
                    pakKey = value;
                    break;
                case ContainerKeyName when containerKey is null:
                    containerKey = value;
                    break;
                case PakKeyName or ContainerKeyName:
                    throw new FormatException($"'{name}' is given twice");
                default:
                    throw new FormatException($"Unknown key '{name}'");
            }
        }

        return new ArchiveKeys(
            ParsePakKey(pakKey ?? throw new FormatException($"'{PakKeyName}' is missing")),
            ParseContainerKey(containerKey ?? throw new FormatException($"'{ContainerKeyName}' is missing")));
    }

    /// <summary>SHA-256 (lowercase hex) of the pak key followed by the container key words, little-endian.</summary>
    public string Fingerprint()
    {
        byte[] data = new byte[PakKeyBytes + (ContainerKeyWords * sizeof(uint))];
        PakKey.CopyTo(data, 0);
        for (int i = 0; i < ContainerKeyWords; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(PakKeyBytes + (i * sizeof(uint))), ContainerKey[i]);
        }

        return Convert.ToHexStringLower(SHA256.HashData(data));
    }

    private static byte[] ParsePakKey(string value)
    {
        string hex = WithoutHexPrefix(value.Replace(" ", "", StringComparison.Ordinal));
        if (hex.Length != PakKeyBytes * 2 || !hex.All(char.IsAsciiHexDigit))
        {
            throw new FormatException($"'{PakKeyName}' must be {PakKeyBytes * 2} hex digits");
        }

        return Convert.FromHexString(hex);
    }

    private static uint[] ParseContainerKey(string value)
    {
        string[] words = value.Split([',', ' ', '\t', '[', ']', '{', '}'], StringSplitOptions.RemoveEmptyEntries);
        if (words.Length != ContainerKeyWords)
        {
            throw new FormatException($"'{ContainerKeyName}' must have {ContainerKeyWords} words");
        }

        return
        [
            .. words.Select(word => WithoutHexPrefix(word) is { Length: >= 1 and <= 8 } hex && hex.All(char.IsAsciiHexDigit)
                ? uint.Parse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)
                : throw new FormatException($"'{ContainerKeyName}' word '{word}' is not a 32-bit hex number")),
        ];
    }

    private static string WithoutHexPrefix(string value)
    {
        return value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? value[2..] : value;
    }
}
