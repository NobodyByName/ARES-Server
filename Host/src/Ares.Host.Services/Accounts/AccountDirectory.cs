using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ares.Host.Services.Persistence;

namespace Ares.Host.Services.Accounts;

/// <summary>Result of <see cref="AccountDirectory.Create"/>.</summary>
public enum AccountCreateResult
{
    /// <summary>The account was stored.</summary>
    Created,

    /// <summary>The name breaks <see cref="AccountDirectory.IsValidName"/>.</summary>
    InvalidName,

    /// <summary>The password breaks <see cref="AccountDirectory.IsValidPassword"/>.</summary>
    InvalidPassword,

    /// <summary>Another account has the same name (ignoring case).</summary>
    NameTaken,
}

/// <summary>Result of <see cref="AccountDirectory.Rename"/>.</summary>
public enum AccountRenameResult
{
    /// <summary>The account has its new name.</summary>
    Renamed,

    /// <summary>No account has the old name.</summary>
    NotFound,

    /// <summary>The new name breaks <see cref="AccountDirectory.IsValidName"/>.</summary>
    InvalidName,

    /// <summary>Another account has the new name (ignoring case).</summary>
    NameTaken,
}

/// <summary>An account and its user id.</summary>
public sealed record Account(long UserId, AccountRecord Record)
{
    /// <summary>The account name as created.</summary>
    public string Name => Record.Name;
}

/// <summary>
/// The operator-managed account table: names, SRP verifiers, bans and launch keys. Accounts are
/// created on the Host console; players only sign in.
/// </summary>
public sealed class AccountDirectory(HostDatabase database, TimeProvider time)
{
    /// <summary>Shortest accepted password.</summary>
    public const int MinimumPasswordLength = 8;

    /// <summary>Longest accepted password.</summary>
    public const int MaximumPasswordLength = 128;

    /// <summary>How long a new launch key waits for the game to sign in.</summary>
    public static readonly TimeSpan LaunchKeyFirstUse = TimeSpan.FromMinutes(15);

    /// <summary>How long a used launch key keeps signing in the same game session.</summary>
    public static readonly TimeSpan LaunchKeySession = TimeSpan.FromHours(24);

    internal static JsonSerializerOptions SerializationOptions { get; } = new()
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
    };

    /// <summary>Account names: 3–32 ASCII letters, digits, <c>_</c>, <c>-</c> or <c>.</c>.</summary>
    public static bool IsValidName(string? name)
    {
        return name is { Length: >= 3 and <= 32 } && name.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.');
    }

    /// <summary>Passwords: <see cref="MinimumPasswordLength"/> to <see cref="MaximumPasswordLength"/> characters, no line breaks.</summary>
    public static bool IsValidPassword(string? password)
    {
        return password is { Length: >= MinimumPasswordLength and <= MaximumPasswordLength } && !password.Any(char.IsControl);
    }

    /// <summary>The case-insensitive lookup key of a valid name; also the SRP identity.</summary>
    public static string NameKey(string name) => name.ToLowerInvariant();

    /// <summary>Creates an account with a fresh SRP verifier.</summary>
    public AccountCreateResult Create(string name, string password)
    {
        if (!IsValidName(name))
        {
            return AccountCreateResult.InvalidName;
        }

        if (!IsValidPassword(password))
        {
            return AccountCreateResult.InvalidPassword;
        }

        string key = NameKey(name);
        if (database.FindAccountByName(key) is not null)
        {
            return AccountCreateResult.NameTaken;
        }

        AccountRecord record = new(name, key, RandomUserId(), time.GetUtcNow().ToUnixTimeSeconds(), Banned: false, NewVerifier(password), LaunchKey: null);
        return database.InsertAccount(RandomUserId(), record) ? AccountCreateResult.Created : AccountCreateResult.NameTaken;
    }

    /// <summary>Deletes an account and all its player data.</summary>
    /// <returns>The deleted account, or null when none has that name.</returns>
    public Account? Delete(string name)
    {
        Account? account = Find(name);
        return account is not null && database.DeleteAccount(account.UserId) ? account : null;
    }

    /// <summary>
    /// Renames an account; the name is also its in-game display name. The password stays valid
    /// and the player sees the new name from its next profile request or match.
    /// </summary>
    public (AccountRenameResult Result, Account? Account) Rename(string name, string newName)
    {
        if (Find(name) is not { } account)
        {
            return (AccountRenameResult.NotFound, null);
        }

        if (!IsValidName(newName))
        {
            return (AccountRenameResult.InvalidName, null);
        }

        AccountRecord renamed = account.Record with { Name = newName, NameKey = NameKey(newName) };
        if (database.FindAccountByName(renamed.NameKey) is { } other && other.UserId != account.UserId)
        {
            return (AccountRenameResult.NameTaken, null);
        }

        return database.RenameAccount(account.UserId, renamed.Name, renamed.NameKey)
            ? (AccountRenameResult.Renamed, account with { Record = renamed })
            : (AccountRenameResult.NameTaken, null);
    }

    /// <summary>Replaces the password and revokes the launch key.</summary>
    /// <returns>False when the account is missing or the password is invalid.</returns>
    public bool SetPassword(string name, string password)
    {
        if (!IsValidPassword(password) || Find(name) is not { } account)
        {
            return false;
        }

        database.UpdateAccount(account.UserId, "srp", NewVerifier(password));
        return database.UpdateAccount<LaunchKeyRecord?>(account.UserId, "launchKey", null);
    }

    /// <summary>Bans or unbans an account; a ban revokes the launch key.</summary>
    /// <returns>The account, or null when none has that name.</returns>
    public Account? SetBanned(string name, bool banned)
    {
        if (Find(name) is not { } account || !database.UpdateAccount(account.UserId, "banned", banned))
        {
            return null;
        }

        if (banned)
        {
            database.UpdateAccount<LaunchKeyRecord?>(account.UserId, "launchKey", null);
        }

        return account with { Record = account.Record with { Banned = banned } };
    }

    /// <summary>Every account, ordered by name.</summary>
    public IReadOnlyList<Account> List() => [.. database.ListAccounts().Select(entry => new Account(entry.UserId, entry.Account))];

    /// <summary>The account with <paramref name="name"/> (ignoring case), or null.</summary>
    public Account? Find(string name)
    {
        return IsValidName(name) && database.FindAccountByName(NameKey(name)) is { } entry ? new Account(entry.UserId, entry.Account) : null;
    }

    /// <summary>The account with <paramref name="userId"/>, or null.</summary>
    public Account? Find(long userId) => database.FindAccount(userId) is { } record ? new Account(userId, record) : null;

    /// <summary>Issues a fresh launch key for one game start, replacing the previous one.</summary>
    /// <returns>The key: base64 of 32 random bytes. Only its hash is stored.</returns>
    public string IssueLaunchKey(long userId)
    {
        string key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        database.UpdateAccount<LaunchKeyRecord?>(userId, "launchKey", new LaunchKeyRecord(HashLaunchKey(key), time.GetUtcNow().ToUnixTimeSeconds(), FirstUsedAt: null));
        return key;
    }

    /// <summary>
    /// The unbanned account owning a live launch key, or null. A key first has
    /// <see cref="LaunchKeyFirstUse"/> to be used; from its first use it signs in for
    /// <see cref="LaunchKeySession"/>.
    /// </summary>
    public Account? RedeemLaunchKey(string? key)
    {
        if (key is not { Length: 44 }
            || database.FindAccountByLaunchKey(HashLaunchKey(key)) is not { Account: { Banned: false, LaunchKey: { } launch } } entry)
        {
            return null;
        }

        long now = time.GetUtcNow().ToUnixTimeSeconds();
        long expiresAt = launch.FirstUsedAt is { } used
            ? used + (long)LaunchKeySession.TotalSeconds
            : launch.IssuedAt + (long)LaunchKeyFirstUse.TotalSeconds;
        if (now >= expiresAt)
        {
            return null;
        }

        if (launch.FirstUsedAt is null)
        {
            database.UpdateAccount(entry.UserId, "launchKey", launch with { FirstUsedAt = now });
        }

        return new Account(entry.UserId, entry.Account);
    }

    internal static AccountRecord Validate(AccountRecord record)
    {
        if (!IsValidName(record.Name) || record.NameKey != NameKey(record.Name) || record.EmbarkUserId <= 0 || record.CreatedAt < 0
            || record.Srp is not { Salt.Length: > 0, Verifier.Length: > 0, Iterations: > 0 }
            || record.LaunchKey is { Hash.Length: 0 } or { IssuedAt: < 0 })
        {
            throw new InvalidDataException("Invalid account record");
        }

        return record;
    }

    private static SrpVerifierRecord NewVerifier(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(Srp.SaltBytes);
        byte[] verifier = Srp.Verifier(password, salt, Srp.DefaultIterations);
        return new SrpVerifierRecord(Convert.ToBase64String(salt), Convert.ToBase64String(verifier), Srp.DefaultIterations);
    }

    private static string HashLaunchKey(string key) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

    // Random 19-digit ids, like the game's own user ids.
    private static long RandomUserId() => 1000000000000000000 + (long)(BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8)) % 8000000000000000000);
}

/// <summary>A stored account.</summary>
/// <param name="Name">The name as created.</param>
/// <param name="NameKey">Lower-case name: the unique lookup key and SRP identity.</param>
/// <param name="EmbarkUserId">The Embark account id reported in identity tokens and profiles.</param>
/// <param name="CreatedAt">Unix seconds.</param>
/// <param name="Banned">A banned account cannot sign in.</param>
/// <param name="Srp">Password verifier.</param>
/// <param name="LaunchKey">The current launch key, if any.</param>
public sealed record AccountRecord(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("nameKey")] string NameKey,
    [property: JsonPropertyName("embarkUserId")] long EmbarkUserId,
    [property: JsonPropertyName("createdAt")] long CreatedAt,
    [property: JsonPropertyName("banned")] bool Banned,
    [property: JsonPropertyName("srp")] SrpVerifierRecord Srp,
    [property: JsonPropertyName("launchKey"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] LaunchKeyRecord? LaunchKey = null);

/// <summary>An SRP-6a salt and verifier (base64) and the PBKDF2 iterations they used.</summary>
public sealed record SrpVerifierRecord(
    [property: JsonPropertyName("salt")] string Salt,
    [property: JsonPropertyName("verifier")] string Verifier,
    [property: JsonPropertyName("iterations")] int Iterations);

/// <summary>A launch key's SHA-256 (hex), issue time and first use (Unix seconds).</summary>
public sealed record LaunchKeyRecord(
    [property: JsonPropertyName("hash")] string Hash,
    [property: JsonPropertyName("issuedAt")] long IssuedAt,
    [property: JsonPropertyName("firstUsedAt"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? FirstUsedAt = null);
