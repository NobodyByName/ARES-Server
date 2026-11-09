using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Ares.Common.Logging;

namespace Ares.Host.Services.Accounts;

/// <summary>Body of <c>POST /ares/v1/sign-in/start</c>: the account name and the client's SRP value A (base64).</summary>
public sealed record SignInStartRequest(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("a")] string? ClientPublic);

/// <summary>Reply of <c>POST /ares/v1/sign-in/start</c>: the handshake id, salt and PBKDF2 iterations, and the server's SRP value B.</summary>
public sealed record SignInStartResponse(
    [property: JsonPropertyName("session")] string Session,
    [property: JsonPropertyName("salt")] string Salt,
    [property: JsonPropertyName("iterations")] int Iterations,
    [property: JsonPropertyName("b")] string ServerPublic);

/// <summary>Body of <c>POST /ares/v1/sign-in/finish</c>: the client proof M1 (base64).</summary>
public sealed record SignInFinishRequest(
    [property: JsonPropertyName("session")] string? Session,
    [property: JsonPropertyName("proof")] string? Proof);

/// <summary>Reply of <c>POST /ares/v1/sign-in/finish</c>: the server proof M2 (base64), the account name and a fresh launch key.</summary>
public sealed record SignInFinishResponse(
    [property: JsonPropertyName("proof")] string Proof,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("launch_key")] string LaunchKey,
    [property: JsonPropertyName("launch_key_expires_in")] int LaunchKeyExpiresIn);

/// <summary>
/// Launcher sign-in: an SRP-6a handshake proves the password without sending it, then the Host
/// issues a launch key that the launcher writes into the profile's game configuration. Protocol 1;
/// ARES-Launcher keeps its own copy of this contract.
/// </summary>
public sealed class SignInService(AccountDirectory accounts, JsonEventLog log, TimeProvider time)
{
    /// <summary>Path of the first step.</summary>
    public const string StartPath = "/ares/v1/sign-in/start";

    /// <summary>Path of the second step.</summary>
    public const string FinishPath = "/ares/v1/sign-in/finish";

    /// <summary>Most handshakes waiting for their second step.</summary>
    private const int MaximumPending = 1024;

    /// <summary>Time allowed between the two steps.</summary>
    private static readonly TimeSpan s_handshakeLifetime = TimeSpan.FromMinutes(1);

    /// <summary>Per-run key for the stand-in salt of unknown names, so replies do not reveal which names exist.</summary>
    private static readonly byte[] s_unknownNameKey = RandomNumberGenerator.GetBytes(32);

    private readonly Dictionary<string, Handshake> _pending = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();

    /// <summary>Starts a handshake. Unknown names get a stand-in that can never finish.</summary>
    public ServiceReply Start(SignInStartRequest request)
    {
        if (!AccountDirectory.IsValidName(request.Name) || Decode(request.ClientPublic) is not { Length: Srp.ModulusBytes } clientPublic
            || Srp.Number(clientPublic) % Srp.N == 0)
        {
            return ServiceReply.Error(400, "invalid_sign_in");
        }

        string nameKey = AccountDirectory.NameKey(request.Name!);
        Account? account = accounts.Find(nameKey);
        byte[] salt;
        byte[] verifier;
        int iterations;
        if (account is null)
        {
            salt = HMACSHA256.HashData(s_unknownNameKey, Encoding.UTF8.GetBytes(nameKey));
            verifier = Srp.Pad(BigInteger.ModPow(Srp.G, Srp.Number(RandomNumberGenerator.GetBytes(32)), Srp.N));
            iterations = Srp.DefaultIterations;
        }
        else
        {
            salt = Convert.FromBase64String(account.Record.Srp.Salt);
            verifier = Convert.FromBase64String(account.Record.Srp.Verifier);
            iterations = account.Record.Srp.Iterations;
        }

        (BigInteger secret, byte[] serverPublic) = Srp.ServerChallenge(verifier);
        string session = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
        DateTimeOffset now = time.GetUtcNow();
        lock (_lock)
        {
            foreach (string expired in _pending.Where(pair => pair.Value.ExpiresAt <= now).Select(pair => pair.Key).ToList())
            {
                _pending.Remove(expired);
            }

            if (_pending.Count >= MaximumPending)
            {
                return ServiceReply.Error(503, "sign_in_busy");
            }

            _pending.Add(session, new Handshake(account?.UserId, nameKey, salt, verifier, clientPublic, secret, serverPublic, now + s_handshakeLifetime));
        }

        return ServiceReply.Ok(new SignInStartResponse(session, Convert.ToBase64String(salt), iterations, Convert.ToBase64String(serverPublic)));
    }

    /// <summary>Checks the client's proof and issues a launch key. Each handshake allows one attempt.</summary>
    public ServiceReply Finish(SignInFinishRequest request)
    {
        Handshake? handshake;
        lock (_lock)
        {
            if (request.Session is null || !_pending.Remove(request.Session, out handshake) || handshake.ExpiresAt <= time.GetUtcNow())
            {
                return ServiceReply.Error(401, "sign_in_expired");
            }
        }

        byte[]? serverProof = Decode(request.Proof) is { } proof
            ? Srp.VerifyClient(handshake.NameKey, handshake.Salt, handshake.Verifier, handshake.ClientPublic, handshake.Secret, handshake.ServerPublic, proof)
            : null;
        if (serverProof is null || handshake.UserId is not { } userId || accounts.Find(userId) is not { } account)
        {
            log.Write("launcher_sign_in", new JsonObject { ["name"] = handshake.NameKey, ["result"] = "rejected" });
            return ServiceReply.Error(401, "invalid_credentials");
        }

        if (account.Record.Banned)
        {
            log.Write("launcher_sign_in", new JsonObject { ["name"] = account.Name, ["result"] = "banned" });
            return ServiceReply.Error(403, "account_banned");
        }

        string launchKey = accounts.IssueLaunchKey(userId);
        log.Write("launcher_sign_in", new JsonObject { ["name"] = account.Name, ["result"] = "signed_in" });
        return ServiceReply.Ok(new SignInFinishResponse(Convert.ToBase64String(serverProof), account.Name, launchKey,
            (int)AccountDirectory.LaunchKeyFirstUse.TotalSeconds));
    }

    private static byte[]? Decode(string? base64)
    {
        if (base64 is null || base64.Length > 1024)
        {
            return null;
        }

        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private sealed record Handshake(long? UserId, string NameKey, byte[] Salt, byte[] Verifier, byte[] ClientPublic, BigInteger Secret,
        byte[] ServerPublic, DateTimeOffset ExpiresAt);
}
