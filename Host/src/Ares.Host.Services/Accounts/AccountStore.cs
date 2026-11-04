using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ares.Host.Services.Content;
using Ares.Host.Services.Persistence;
using Ares.Structs.Backend.Accounts;

namespace Ares.Host.Services.Accounts;

/// <summary>
/// The local account's persistent settings (display name, persistent player keys), stored in
/// MongoDB. Writes are atomic and check the version read by the operation.
/// </summary>
public sealed class AccountStore
{
    /// <summary>ETag of keys that still have their preset value.</summary>
    private const string InitialETag = "ares-host-initial";

    internal static JsonSerializerOptions SerializationOptions { get; } = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly HostDatabase _database;
    private long _version;
    private readonly TimeProvider _time;
    private readonly Lock _lock = new();

    /// <summary>Creates an account store backed by the Host database.</summary>
    public AccountStore(HostDatabase database, TimeProvider time)
    {
        _database = database;
        _time = time;
        _ = Read();
    }

    /// <summary>Display-name overrides, or null for the defaults.</summary>
    public AccountProfileOverrides Profile
    {
        get
        {
            lock (_lock)
            {
                return Read().Profile ?? AccountProfileOverrides.None;
            }
        }
    }

    /// <summary>
    /// Display-name rules: 3–32 characters, no surrounding whitespace, printable, and no
    /// <c>#</c>, tab or line breaks.
    /// </summary>
    public static bool IsValidDisplayName(string? name)
    {
        if (name is null || name != name.Trim())
        {
            return false;
        }

        int count = 0;
        foreach (Rune rune in name.EnumerateRunes())
        {
            count++;
            if (rune.Value is '#' or '\r' or '\n' or '\t' || !IsPrintable(rune))
            {
                return false;
            }
        }

        return count is >= 3 and <= 32;
    }

    /// <summary>Sets the display name and starts the 30-day rename cooldown.</summary>
    /// <returns>False when the name is invalid.</returns>
    public bool SetDisplayName(string? name)
    {
        if (!IsValidDisplayName(name))
        {
            return false;
        }

        lock (_lock)
        {
            AccountStateDocument state = Read();
            long cooldown = _time.GetUtcNow().ToUnixTimeSeconds() + (30 * 86400);
            Save(state with { Profile = new AccountProfileOverrides(name, cooldown) });
        }

        return true;
    }

    /// <summary>All persistent keys: presets first overridden by stored values, sorted by key.</summary>
    public IReadOnlyList<PersistentPlayerKey> GetKeys()
    {
        lock (_lock)
        {
            SortedDictionary<string, PersistentPlayerKey> keys = new(StringComparer.Ordinal);
            foreach (string name in BackendContent.CompletedFirstStartKeys)
            {
                keys[name] = new PersistentPlayerKey(name, "true", InitialETag);
            }

            foreach ((string name, StoredKey stored) in Read().PersistentPlayerKeys ?? AccountStateDocument.NoKeys)
            {
                keys[name] = new PersistentPlayerKey(name, stored.Value, stored.ETag);
            }

            return [.. keys.Values];
        }
    }

    /// <summary>Stores keys. Returns the stored entries, or an error code for invalid input.</summary>
    public (IReadOnlyList<PersistentPlayerKey>? Updated, string? Error) SetKeys(IReadOnlyList<PersistentPlayerKeyWrite?>? rows)
    {
        if (rows is null || rows.Count > 1024)
        {
            return (null, "invalid_local_keys");
        }

        foreach (PersistentPlayerKeyWrite? row in rows)
        {
            if (row?.Key is null || row.Value is null)
            {
                return (null, "invalid_local_key");
            }

            if (row.Key.Length is < 1 or > 256 || row.Value.Length > 8192)
            {
                return (null, "local_key_too_large");
            }
        }

        lock (_lock)
        {
            AccountStateDocument state = Read();
            Dictionary<string, StoredKey> stored = new(state.PersistentPlayerKeys ?? AccountStateDocument.NoKeys, StringComparer.Ordinal);
            List<PersistentPlayerKey> updated = [];
            long revision = state.Revision;
            foreach (PersistentPlayerKeyWrite? row in rows)
            {
                revision++;
                StoredKey entry = new(row!.Value!, "ares-host-key-" + revision.ToString(CultureInfo.InvariantCulture));
                stored[row.Key!] = entry;
                updated.Add(new PersistentPlayerKey(row.Key!, entry.Value, entry.ETag));
            }

            Save(state with { PersistentPlayerKeys = stored, Revision = revision });
            return (updated, null);
        }
    }

    // Printable: every Unicode category except "Other" and "Separator", plus the ASCII space.
    private static bool IsPrintable(Rune rune)
    {
        if (rune.Value == ' ')
        {
            return true;
        }

        UnicodeCategory category = Rune.GetUnicodeCategory(rune);
        return category is not (UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.Surrogate
            or UnicodeCategory.PrivateUse or UnicodeCategory.OtherNotAssigned or UnicodeCategory.SpaceSeparator
            or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator);
    }

    private AccountStateDocument Read()
    {
        (AccountStateDocument state, long version) = _database.Read<AccountStateDocument>(LocalPlayer.TenancyUserId, "account", SerializationOptions);
        _version = version;
        return Validate(state);
    }

    private static AccountStateDocument Validate(AccountStateDocument state)
    {
        if (state.Revision < 0 || state.Profile is { DisplayName: { } name } && !IsValidDisplayName(name)
            || state.Profile?.DisplayNameCooldownEndsAt is < 0
            || state.PersistentPlayerKeys?.Any(pair => pair.Key.Length is < 1 or > 256
                || pair.Value is not { Value.Length: <= 8192, ETag.Length: > 0 }) == true)
        {
            throw new InvalidDataException("Invalid account document");
        }

        return state;
    }

    private void Save(AccountStateDocument state)
    {
        _database.Save(LocalPlayer.TenancyUserId, "account", Validate(state), _version, SerializationOptions);
        _version = checked(_version + 1);
    }
}

/// <summary>Display-name overrides of the local account.</summary>
public sealed record AccountProfileOverrides(
    [property: JsonPropertyName("displayName"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? DisplayName,
    [property: JsonPropertyName("displayNameCooldownEndsAt"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? DisplayNameCooldownEndsAt)
{
    /// <summary>No overrides: the default name and a fresh cooldown.</summary>
    public static AccountProfileOverrides None { get; } = new(null, null);
}

/// <summary>A stored persistent player key.</summary>
public sealed record StoredKey(
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("etag")] string ETag);

/// <summary>The persistent account section of a player document.</summary>
public sealed record AccountStateDocument(
    [property: JsonPropertyName("profile")] AccountProfileOverrides? Profile,
    [property: JsonPropertyName("persistentPlayerKeys")] IReadOnlyDictionary<string, StoredKey>? PersistentPlayerKeys,
    [property: JsonPropertyName("revision")] long Revision)
{
    /// <summary>No stored keys.</summary>
    public static IReadOnlyDictionary<string, StoredKey> NoKeys { get; } = new Dictionary<string, StoredKey>();

    /// <summary>A new account.</summary>
    public static AccountStateDocument Empty { get; } = new(AccountProfileOverrides.None, NoKeys, 0);
}
