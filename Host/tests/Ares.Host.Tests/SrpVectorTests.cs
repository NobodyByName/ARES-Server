using System.Numerics;
using System.Security.Cryptography;
using Ares.Host.Services.Accounts;

namespace Ares.Host.Tests;

/// <summary>
/// SRP-6a known-answer vectors shared with the launcher's self-check: name "Alice", password
/// "correct horse", salt bytes 0..31, 1000 iterations, client secret 0x1234567, server secret 0x7654321.
/// </summary>
public sealed class SrpVectorTests
{
    private const string NameKey = "alice";
    private const string Password = "correct horse";
    private const int Iterations = 1000;
    private const string VerifierSha256 = "df79ae64ccc2bd988440948a5aaf011b27adc686990e276dbd204db534a2227b";
    private const string ClientProof = "90eb863b8091ef1feb92d3f15c942f463fcf61c8d6982bbfabb8dbfe158484d5";
    private const string ServerProof = "81429913cec81ec0bb009eb63d6ed60406baaa227694449559a0d2c63dfab10c";

    private const string ServerPublic =
        "dNkH5GPWHyCSmgMcK0f2RNEUYsVCrUBRgX7HBtDj69oAH9aZRoVDd4sWindjwHCvRRj8JmRaC9fKaQHvi1R2JGuIwRK5A+afSeZ5pgmP/WJzI17ot7aaP7YpeN0PO5Ld" +
        "0csNndiY6+fPuH7GHIdGpLhdhUCXwRpu0Rf1W4m9UUoubJRljt1GQqAGofRaDVkZ28dWw5L3DBUVlGid8fpr1opxEquZfggm5C5gterA6XSd+3Ygc5GAp2M8B4KI4jSm" +
        "vPK3SK/WelwITCMi1/G8TBTH3mekeNpQTX5zfNKiT1Cwvzv0DuuWeooqAbmMvh6v+F2OXgpwAylZfYQeyzqhfw==";

    private static readonly BigInteger s_clientSecret = 0x1234567;
    private static readonly BigInteger s_serverSecret = 0x7654321;
    private static readonly byte[] s_salt = [.. Enumerable.Range(0, 32).Select(index => (byte)index)];

    [Fact]
    public void Verifier_matches_the_shared_vector()
    {
        Assert.Equal(VerifierSha256, Convert.ToHexStringLower(SHA256.HashData(Srp.Verifier(Password, s_salt, Iterations))));
    }

    [Fact]
    public void Server_public_value_matches_the_shared_vector()
    {
        byte[] verifier = Srp.Verifier(Password, s_salt, Iterations);
        BigInteger value = ((Srp.K * Srp.Number(verifier)) + BigInteger.ModPow(Srp.G, s_serverSecret, Srp.N)) % Srp.N;
        Assert.Equal(ServerPublic, Convert.ToBase64String(Srp.Pad(value)));
    }

    [Fact]
    public void Server_accepts_the_shared_client_proof_and_answers_the_shared_server_proof()
    {
        byte[] verifier = Srp.Verifier(Password, s_salt, Iterations);
        byte[] clientPublic = Srp.Pad(BigInteger.ModPow(Srp.G, s_clientSecret, Srp.N));
        byte[]? serverProof = Srp.VerifyClient(NameKey, s_salt, verifier, clientPublic, s_serverSecret, Convert.FromBase64String(ServerPublic),
            Convert.FromHexString(ClientProof));
        Assert.NotNull(serverProof);
        Assert.Equal(ServerProof, Convert.ToHexStringLower(serverProof));
    }
}
