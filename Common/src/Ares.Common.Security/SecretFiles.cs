namespace Ares.Common.Security;

/// <summary>Reads operator-supplied secrets from files, so they never appear on a command line.</summary>
public static class SecretFiles
{
    /// <summary>Reads a binary signing key and checks its length.</summary>
    /// <exception cref="ArgumentException">The key is shorter than <paramref name="minimumBytes"/> or longer than <paramref name="maximumBytes"/>.</exception>
    public static byte[] ReadKey(string path, int minimumBytes = 32, int maximumBytes = 4096)
    {
        byte[] key = File.ReadAllBytes(path);
        return key.Length >= minimumBytes && key.Length <= maximumBytes
            ? key
            : throw new ArgumentException($"Identity key must be {minimumBytes}–{maximumBytes} bytes", nameof(path));
    }

    /// <summary>Reads an operator token (surrounding whitespace removed) and validates it.</summary>
    public static string ReadOperatorToken(string path)
    {
        return OperatorToken.Validate(File.ReadAllText(path).Trim());
    }
}
