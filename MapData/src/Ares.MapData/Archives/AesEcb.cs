using System.Security.Cryptography;

namespace Ares.MapData.Archives;

/// <summary>AES-256 in ECB mode without padding, as the containers use it.</summary>
internal static class AesEcb
{
    /// <summary>Bytes in an AES block.</summary>
    public const int BlockSize = 16;

    public static byte[] Decrypt(ReadOnlySpan<byte> data, byte[] key)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        return aes.DecryptEcb(data, PaddingMode.None);
    }

    public static byte[] Encrypt(ReadOnlySpan<byte> data, byte[] key)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        return aes.EncryptEcb(data, PaddingMode.None);
    }
}
