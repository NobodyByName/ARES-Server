using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ares.Host.Services.Content;
using Ares.Host.Services.Persistence;
using Ares.Structs.Backend.Accounts;

namespace Ares.Host.Services.Accounts;

/// <summary>
/// One account's persistent game settings (the game's persistent player keys), stored in
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
    private readonly long _userId;
    private long _version;
    private long _transientRevision;
    private Dictionary<string, StoredKey> _transientKeys = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();

    /// <summary>Opens the settings of player <paramref name="userId"/>.</summary>
    public AccountStore(HostDatabase database, long userId)
    {
        _database = database;
        _userId = userId;
        _ = Read();
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

            foreach ((string name, StoredKey entry) in _transientKeys)
            {
                keys[name] = new PersistentPlayerKey(name, entry.Value, entry.ETag);
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
            bool durable = rows.Any(row => !IsTransientKey(row!.Key!));
            AccountStateDocument state = durable ? Read() : AccountStateDocument.Empty;
            Dictionary<string, StoredKey> stored = new(state.PersistentPlayerKeys ?? AccountStateDocument.NoKeys, StringComparer.Ordinal);
            Dictionary<string, StoredKey> transient = new(_transientKeys, StringComparer.Ordinal);
            List<PersistentPlayerKey> updated = [];
            long revision = state.Revision;
            long sessionRevision = _transientRevision;
            foreach (PersistentPlayerKeyWrite? row in rows)
            {
                bool ephemeral = IsTransientKey(row!.Key!);
                string etag;
                if (ephemeral)
                {
                    sessionRevision = checked(sessionRevision + 1);
                    etag = "ares-host-session-key-" + sessionRevision.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    revision = checked(revision + 1);
                    etag = "ares-host-key-" + revision.ToString(CultureInfo.InvariantCulture);
                }

                StoredKey entry = new(row.Value!, etag);
                (ephemeral ? transient : stored)[row.Key!] = entry;
                updated.Add(new PersistentPlayerKey(row.Key!, entry.Value, entry.ETag));
            }

            if (durable)
            {
                Save(new AccountStateDocument(stored, revision));
            }

            _transientKeys = transient;
            _transientRevision = sessionRevision;
            return (updated, null);
        }
    }

    private AccountStateDocument Read()
    {
        (AccountStateDocument state, long version) = _database.Read<AccountStateDocument>(_userId, "settings", SerializationOptions);
        _version = version;
        return Validate(state);
    }

    private static AccountStateDocument Validate(AccountStateDocument state)
    {
        if (state.Revision < 0
            || state.PersistentPlayerKeys?.Any(pair => pair.Key.Length is < 1 or > 256
                || IsTransientKey(pair.Key) || pair.Value is not { Value.Length: <= 8192, ETag.Length: > 0 }) == true)
        {
            throw new InvalidDataException("Invalid account document");
        }

        return state;
    }

    private static bool IsTransientKey(string key) => key is MatchmakingPreferenceKeys.SearchSettings or MatchmakingPreferenceKeys.MapSelection;

    private void Save(AccountStateDocument state)
    {
        _database.Save(_userId, "settings", Validate(state), _version, SerializationOptions);
        _version = checked(_version + 1);
    }
}

/// <summary>A stored persistent player key.</summary>
public sealed record StoredKey(
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("etag")] string ETag);

/// <summary>The game-settings section of an account document.</summary>
public sealed record AccountStateDocument(
    [property: JsonPropertyName("persistentPlayerKeys")] IReadOnlyDictionary<string, StoredKey>? PersistentPlayerKeys,
    [property: JsonPropertyName("revision")] long Revision)
{
    /// <summary>The initial persistent-key collection.</summary>
    public static IReadOnlyDictionary<string, StoredKey> NoKeys { get; } = new Dictionary<string, StoredKey>();

    /// <summary>A new account.</summary>
    public static AccountStateDocument Empty { get; } = new(NoKeys, 0);
}
