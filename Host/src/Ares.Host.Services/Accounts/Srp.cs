using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Ares.Host.Services.Accounts;

/// <summary>
/// SRP-6a password verification (RFC 5054 2048-bit group, SHA-256). The password never crosses
/// the network and the Host stores only a salt and a verifier. The private value <c>x</c> is
/// hardened with PBKDF2-SHA512: <c>x = H(salt | PBKDF2(password, salt, iterations))</c>. It leaves
/// out the account name (the salt is unique per account), so an operator can rename an account
/// without its password; the proofs still bind the name.
/// ARES-Launcher keeps its own copy of the client side.
/// </summary>
public static class Srp
{
    /// <summary>PBKDF2-SHA512 iterations for new verifiers.</summary>
    public const int DefaultIterations = 210000;

    /// <summary>Salt length in bytes.</summary>
    public const int SaltBytes = 32;

    /// <summary>Length of N, A, B and S in bytes.</summary>
    public const int ModulusBytes = 256;

    /// <summary>The RFC 5054 2048-bit safe prime N.</summary>
    public static readonly BigInteger N = BigInteger.Parse(
        "0AC6BDB41324A9A9BF166DE5E1389582FAF72B6651987EE07FC3192943DB56050A37329CBB4A099ED8193E0757767A13DD52312AB4B03310D" +
        "CD7F48A9DA04FD50E8083969EDB767B0CF6095179A163AB3661A05FBD5FAAAE82918A9962F0B93B855F97993EC975EEAA80D740ADBF4FF747359D0" +
        "41D5C33EA71D281E446B14773BCA97B43A23FB801676BD207A436C6481F1D2B9078717461A5B9D32E688F87748544523B524B0D57D5EA77A2775D2EC" +
        "FA032CFBDBF52FB3786160279004E57AE6AF874E7303CE53299CCC041C7BC308D82A5698F3A8D0C38271AE35F8E9DBFBB694B5C803D89F7AE435DE23" +
        "6D525F54759B65E372FCD68EF20FA7111F9E4AFF73", NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    /// <summary>The generator g.</summary>
    public static readonly BigInteger G = 2;

    /// <summary>The multiplier <c>k = H(N | PAD(g))</c>.</summary>
    public static readonly BigInteger K = Number(Hash(Pad(N), Pad(G)));

    /// <summary>The verifier <c>v = g^x mod N</c> for a password, padded to <see cref="ModulusBytes"/>.</summary>
    public static byte[] Verifier(string password, byte[] salt, int iterations)
    {
        return Pad(BigInteger.ModPow(G, PrivateKey(password, salt, iterations), N));
    }

    /// <summary>The private value <c>x</c>.</summary>
    public static BigInteger PrivateKey(string password, byte[] salt, int iterations)
    {
        byte[] stretched = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA512, 64);
        return Number(Hash(salt, stretched));
    }

    /// <summary>A fresh server secret <c>b</c> and public value <c>B = k*v + g^b mod N</c>.</summary>
    public static (BigInteger Secret, byte[] Public) ServerChallenge(byte[] verifier)
    {
        BigInteger secret = Number(RandomNumberGenerator.GetBytes(32));
        BigInteger value = ((K * Number(verifier)) + BigInteger.ModPow(G, secret, N)) % N;
        return (secret, Pad(value));
    }

    /// <summary>
    /// Checks the client's proof <c>M1</c>. Returns the server proof <c>M2</c>, or null when the
    /// client value or the proof is invalid.
    /// </summary>
    public static byte[]? VerifyClient(string nameKey, byte[] salt, byte[] verifier, byte[] clientPublic, BigInteger serverSecret, byte[] serverPublic,
        byte[] clientProof)
    {
        BigInteger a = Number(clientPublic);
        if (clientPublic.Length != ModulusBytes || a % N == 0)
        {
            return null;
        }

        BigInteger u = Number(Hash(Pad(a), serverPublic));
        if (u == 0)
        {
            return null;
        }

        BigInteger shared = BigInteger.ModPow(a * BigInteger.ModPow(Number(verifier), u, N) % N, serverSecret, N);
        byte[] key = Hash(Pad(shared));
        byte[] expected = ClientProof(nameKey, salt, clientPublic, serverPublic, key);
        return CryptographicOperations.FixedTimeEquals(expected, clientProof) ? Hash(clientPublic, expected, key) : null;
    }

    /// <summary><c>M1 = H(H(N) xor H(g) | H(I) | s | A | B | K)</c>.</summary>
    public static byte[] ClientProof(string nameKey, byte[] salt, byte[] clientPublic, byte[] serverPublic, byte[] key)
    {
        byte[] group = Hash(Pad(N));
        byte[] generator = Hash(Pad(G));
        for (int index = 0; index < group.Length; index++)
        {
            group[index] ^= generator[index];
        }

        return Hash(group, Hash(Encoding.UTF8.GetBytes(nameKey)), salt, clientPublic, serverPublic, key);
    }

    /// <summary>A big-endian unsigned value padded to <see cref="ModulusBytes"/>.</summary>
    public static byte[] Pad(BigInteger value)
    {
        byte[] bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        byte[] padded = new byte[ModulusBytes];
        bytes.CopyTo(padded, ModulusBytes - bytes.Length);
        return padded;
    }

    /// <summary>A big-endian unsigned value.</summary>
    public static BigInteger Number(ReadOnlySpan<byte> bytes) => new(bytes, isUnsigned: true, isBigEndian: true);

    /// <summary>SHA-256 of the concatenated parts.</summary>
    public static byte[] Hash(params ReadOnlySpan<byte[]> parts)
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (byte[] part in parts)
        {
            hash.AppendData(part);
        }

        return hash.GetHashAndReset();
    }
}
