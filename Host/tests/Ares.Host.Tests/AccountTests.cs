using System.Numerics;
using System.Security.Cryptography;
using Ares.Common.Logging;
using Ares.Host.Services;
using Ares.Host.Services.Accounts;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Ares.Host.Tests;

public sealed class AccountTests : IDisposable
{
    private readonly TestDatabase _database = new();
    private readonly ManualClock _clock = new();
    private readonly AccountDirectory _accounts;

    public AccountTests()
    {
        _accounts = new AccountDirectory(_database.Store, _clock);
    }

    public void Dispose() => _database.Dispose();

    [Fact]
    public void The_srp_group_is_a_2048_bit_safe_prime()
    {
        Assert.Equal(2048, (int)Srp.N.GetBitLength());
        Assert.True(IsProbablePrime(Srp.N));
        Assert.True(IsProbablePrime((Srp.N - 1) / 2));
    }

    [Theory]
    [InlineData("ab", "long-enough", AccountCreateResult.InvalidName)]
    [InlineData("has space", "long-enough", AccountCreateResult.InvalidName)]
    [InlineData("Ünicode", "long-enough", AccountCreateResult.InvalidName)]
    [InlineData("valid", "short", AccountCreateResult.InvalidPassword)]
    [InlineData("TESTER", "long-enough", AccountCreateResult.NameTaken)]
    [InlineData("New.Raider-1", "long-enough", AccountCreateResult.Created)]
    public void Creation_checks_names_passwords_and_case_insensitive_uniqueness(string name, string password, AccountCreateResult expected)
    {
        Assert.Equal(expected, _accounts.Create(name, password));
    }

    [Fact]
    public void Signing_in_with_the_right_password_issues_a_launch_key_for_that_account()
    {
        Assert.Equal(AccountCreateResult.Created, _accounts.Create("Alice", "correct horse"));
        SignInService signIn = new(_accounts, new JsonEventLog(TextWriter.Null), _clock);

        (ServiceReply reply, byte[] expectedServerProof) = SrpClient.SignIn(signIn, "ALICE", "correct horse");

        SignInFinishResponse finished = Assert.IsType<SignInFinishResponse>(reply.Body);
        Assert.Equal(200, reply.StatusCode);
        Assert.Equal(expectedServerProof, Convert.FromBase64String(finished.Proof));
        Assert.Equal("Alice", finished.Name);
        Assert.Equal(44, finished.LaunchKey.Length);
        Assert.Equal("Alice", _accounts.RedeemLaunchKey(finished.LaunchKey)?.Name);
        string stored = _database.Native.GetCollection<BsonDocument>("accounts").Find(FilterDefinition<BsonDocument>.Empty).ToList(TestContext.Current.CancellationToken).ToJson();
        Assert.DoesNotContain(finished.LaunchKey, stored, StringComparison.Ordinal);
    }

    [Fact]
    public void Wrong_passwords_unknown_names_and_reused_handshakes_are_rejected()
    {
        _accounts.Create("Alice", "correct horse");
        SignInService signIn = new(_accounts, new JsonEventLog(TextWriter.Null), _clock);

        Assert.Equal(401, SrpClient.SignIn(signIn, "Alice", "wrong horse").Reply.StatusCode);
        Assert.Equal(401, SrpClient.SignIn(signIn, "Nobody", "correct horse").Reply.StatusCode);
        Assert.Equal(400, signIn.Start(new SignInStartRequest("Alice", Convert.ToBase64String(new byte[Srp.ModulusBytes]))).StatusCode);

        SignInStartResponse started = (SignInStartResponse)signIn.Start(new SignInStartRequest("Alice", Convert.ToBase64String(Srp.Pad(2)))).Body;
        Assert.Equal(401, signIn.Finish(new SignInFinishRequest(started.Session, Convert.ToBase64String(new byte[32]))).StatusCode);
        Assert.Equal(401, signIn.Finish(new SignInFinishRequest(started.Session, Convert.ToBase64String(new byte[32]))).StatusCode);
    }

    [Fact]
    public void Unknown_names_get_a_stable_stand_in_salt()
    {
        SignInService signIn = new(_accounts, new JsonEventLog(TextWriter.Null), _clock);
        string a = Convert.ToBase64String(Srp.Pad(2));

        SignInStartResponse first = (SignInStartResponse)signIn.Start(new SignInStartRequest("Nobody", a)).Body;
        SignInStartResponse second = (SignInStartResponse)signIn.Start(new SignInStartRequest("nobody", a)).Body;

        Assert.Equal(first.Salt, second.Salt);
        Assert.Equal(Srp.DefaultIterations, first.Iterations);
    }

    [Fact]
    public void Banned_accounts_cannot_sign_in_and_lose_their_launch_key()
    {
        _accounts.Create("Alice", "correct horse");
        SignInService signIn = new(_accounts, new JsonEventLog(TextWriter.Null), _clock);
        string key = ((SignInFinishResponse)SrpClient.SignIn(signIn, "Alice", "correct horse").Reply.Body).LaunchKey;

        Assert.True(_accounts.SetBanned("alice", banned: true)!.Record.Banned);

        Assert.Null(_accounts.RedeemLaunchKey(key));
        Assert.Equal(403, SrpClient.SignIn(signIn, "Alice", "correct horse").Reply.StatusCode);
        _accounts.SetBanned("Alice", banned: false);
        Assert.Equal(200, SrpClient.SignIn(signIn, "Alice", "correct horse").Reply.StatusCode);
    }

    [Fact]
    public void A_new_password_replaces_the_verifier_and_revokes_the_launch_key()
    {
        _accounts.Create("Alice", "correct horse");
        SignInService signIn = new(_accounts, new JsonEventLog(TextWriter.Null), _clock);
        string key = ((SignInFinishResponse)SrpClient.SignIn(signIn, "Alice", "correct horse").Reply.Body).LaunchKey;

        Assert.True(_accounts.SetPassword("Alice", "battery staple"));

        Assert.Null(_accounts.RedeemLaunchKey(key));
        Assert.Equal(401, SrpClient.SignIn(signIn, "Alice", "correct horse").Reply.StatusCode);
        Assert.Equal(200, SrpClient.SignIn(signIn, "Alice", "battery staple").Reply.StatusCode);
        Assert.False(_accounts.SetPassword("Nobody", "battery staple"));
    }

    [Fact]
    public void Launch_keys_wait_for_a_first_use_then_last_for_the_session_and_are_replaced_by_the_next_key()
    {
        string unused = _accounts.IssueLaunchKey(TestDatabase.UserId);
        _clock.Advance(AccountDirectory.LaunchKeyFirstUse);
        Assert.Null(_accounts.RedeemLaunchKey(unused));

        string key = _accounts.IssueLaunchKey(TestDatabase.UserId);
        _clock.Advance(AccountDirectory.LaunchKeyFirstUse - TimeSpan.FromSeconds(1));
        Assert.Equal(TestDatabase.UserId, _accounts.RedeemLaunchKey(key)?.UserId);
        _clock.Advance(AccountDirectory.LaunchKeySession - TimeSpan.FromSeconds(1));
        Assert.Equal(TestDatabase.UserId, _accounts.RedeemLaunchKey(key)?.UserId);
        _clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Null(_accounts.RedeemLaunchKey(key));

        string first = _accounts.IssueLaunchKey(TestDatabase.UserId);
        string second = _accounts.IssueLaunchKey(TestDatabase.UserId);
        Assert.Null(_accounts.RedeemLaunchKey(first));
        Assert.NotNull(_accounts.RedeemLaunchKey(second));
        Assert.Null(_accounts.RedeemLaunchKey(null));
        Assert.Null(_accounts.RedeemLaunchKey("short"));
    }

    [Fact]
    public void Renaming_keeps_the_password_and_frees_the_old_name()
    {
        _accounts.Create("Alice", "correct horse");
        _accounts.Create("Bob", "battery staple");
        SignInService signIn = new(_accounts, new JsonEventLog(TextWriter.Null), _clock);

        Assert.Equal(AccountRenameResult.NameTaken, _accounts.Rename("Alice", "BOB").Result);
        Assert.Equal(AccountRenameResult.InvalidName, _accounts.Rename("Alice", "a b").Result);
        Assert.Equal(AccountRenameResult.NotFound, _accounts.Rename("Nobody", "Carol").Result);
        Assert.Equal("ALICE", _accounts.Rename("Alice", "ALICE").Account?.Name);
        Assert.Equal("Carol", _accounts.Rename("alice", "Carol").Account?.Name);

        Assert.Equal(200, SrpClient.SignIn(signIn, "carol", "correct horse").Reply.StatusCode);
        Assert.Equal(401, SrpClient.SignIn(signIn, "Alice", "correct horse").Reply.StatusCode);
        Assert.Equal(AccountCreateResult.Created, _accounts.Create("Alice", "another-password"));
    }

    [Fact]
    public void Deleting_an_account_removes_its_player_data()
    {
        _accounts.Create("Alice", "correct horse");
        long userId = _accounts.Find("alice")!.UserId;

        Assert.Equal(userId, _accounts.Delete("ALICE")?.UserId);

        Assert.Null(_accounts.Find(userId));
        Assert.Null(_accounts.Delete("Alice"));
        Assert.Single(_database.Accounts.Find(FilterDefinition<BsonDocument>.Empty).ToList(TestContext.Current.CancellationToken));
    }

    private static bool IsProbablePrime(BigInteger value)
    {
        BigInteger d = value - 1;
        int r = 0;
        while (d.IsEven)
        {
            d >>= 1;
            r++;
        }

        foreach (int witness in new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37 })
        {
            BigInteger x = BigInteger.ModPow(witness, d, value);
            if (x == 1 || x == value - 1)
            {
                continue;
            }

            bool composite = true;
            for (int round = 1; round < r && composite; round++)
            {
                x = BigInteger.ModPow(x, 2, value);
                composite = x != value - 1;
            }

            if (composite)
            {
                return false;
            }
        }

        return true;
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta) => _now += delta;
    }
}

/// <summary>The client half of the SRP-6a sign-in, as the launcher performs it.</summary>
internal static class SrpClient
{
    /// <summary>Runs both sign-in steps; returns the finish reply and the server proof the client expects.</summary>
    internal static (ServiceReply Reply, byte[] ServerProof) SignIn(SignInService service, string name, string password)
    {
        BigInteger secret = Srp.Number(RandomNumberGenerator.GetBytes(32));
        byte[] clientPublic = Srp.Pad(BigInteger.ModPow(Srp.G, secret, Srp.N));
        ServiceReply started = service.Start(new SignInStartRequest(name, Convert.ToBase64String(clientPublic)));
        if (started.Body is not SignInStartResponse challenge)
        {
            return (started, []);
        }

        (byte[] proof, byte[] serverProof) = Prove(name, password, secret, clientPublic, challenge);
        return (service.Finish(new SignInFinishRequest(challenge.Session, Convert.ToBase64String(proof))), serverProof);
    }

    /// <summary>The client proof M1 and the expected server proof M2 for a challenge.</summary>
    internal static (byte[] Proof, byte[] ServerProof) Prove(string name, string password, BigInteger secret, byte[] clientPublic, SignInStartResponse challenge)
    {
        string nameKey = AccountDirectory.NameKey(name);
        byte[] salt = Convert.FromBase64String(challenge.Salt);
        byte[] serverPublic = Convert.FromBase64String(challenge.ServerPublic);
        BigInteger x = Srp.PrivateKey(password, salt, challenge.Iterations);
        BigInteger u = Srp.Number(Srp.Hash(clientPublic, serverPublic));
        BigInteger b = Srp.Number(serverPublic);
        BigInteger basis = ((b - (Srp.K * BigInteger.ModPow(Srp.G, x, Srp.N))) % Srp.N + Srp.N) % Srp.N;
        byte[] key = Srp.Hash(Srp.Pad(BigInteger.ModPow(basis, secret + (u * x), Srp.N)));
        byte[] proof = Srp.ClientProof(nameKey, salt, clientPublic, serverPublic, key);
        return (proof, Srp.Hash(clientPublic, proof, key));
    }
}
