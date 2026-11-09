using System.Globalization;
using System.Text.Json;
using Ares.Common.Game.Inventory;
using Ares.Host.Services.Accounts;
using Ares.Host.Services.Inventory;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;

namespace Ares.Host.Services.Persistence;

/// <summary>A stale database update; callers must reload before trying the operation again.</summary>
public sealed class PersistenceConflictException() : InvalidOperationException("Player data changed; reload before retrying");

/// <summary>A database failure identified by its operation and exception category.</summary>
public sealed class PersistenceException(string operation) : InvalidOperationException($"MongoDB operation failed ({operation}); check database availability, permissions and schema");

/// <summary>
/// One operator-owned MongoDB database per Host. Each account is one document: its sign-in fields
/// plus the game's settings and inventory sections, which use optimistic concurrency for atomic updates.
/// </summary>
public sealed class HostDatabase : IDisposable
{
    private static readonly JsonWriterSettings s_json = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };

    // Sign-in fields only; the game sections are read and written on their own.
    private static readonly ProjectionDefinition<BsonDocument, BsonDocument> s_accountFields =
        Builders<BsonDocument>.Projection.Exclude("settings").Exclude("inventory").Exclude("settingsVersion").Exclude("inventoryVersion");
    private readonly MongoClient _client;
    private readonly IMongoCollection<BsonDocument> _reservations;
    private readonly IMongoCollection<BsonDocument> _accounts;

    /// <summary>Connects to the configured MongoDB service.</summary>
    public HostDatabase(string connectionString, string databaseName)
    {
        ValidateConfiguration(connectionString, databaseName);
        MongoClientSettings settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
        settings.ConnectTimeout = TimeSpan.FromSeconds(5);
        settings.SocketTimeout = TimeSpan.FromSeconds(10);
        settings.ReadPreference = ReadPreference.Primary;
        settings.WriteConcern = WriteConcern.WMajority.With(journal: true, wTimeout: TimeSpan.FromSeconds(10));
        _client = new MongoClient(settings);
        _reservations = _client.GetDatabase(databaseName).GetCollection<BsonDocument>("raidReservations");
        _accounts = _client.GetDatabase(databaseName).GetCollection<BsonDocument>("accounts");
    }

    /// <summary>Checks connection URI syntax and database naming rules.</summary>
    public static void ValidateConfiguration(string connectionString, string databaseName)
    {
        try
        {
            _ = MongoUrl.Create(connectionString);
        }
        catch (Exception error) when (error is ArgumentException or FormatException or MongoConfigurationException)
        {
            throw new ArgumentException("Invalid MongoDB connection URI");
        }

        if (databaseName.Length is < 1 or > 63 || databaseName.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('_' or '-'))
            || databaseName is "admin" or "local" or "config")
        {
            throw new ArgumentException("MongoDB database name must be 1-63 ASCII letters, digits, underscores or hyphens, and cannot be admin, local or config");
        }
    }

    /// <summary>Creates the indexes the account table relies on.</summary>
    public void Initialize()
    {
        Execute(() => _accounts.Indexes.CreateMany(
        [
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("nameKey"), new CreateIndexOptions { Unique = true }),
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("launchKey.hash"), new CreateIndexOptions { Sparse = true }),
        ]));
    }

    /// <summary>Stores a new account with empty game settings and inventory.</summary>
    /// <returns>False when the user id or the account name is already taken.</returns>
    internal bool InsertAccount(long userId, AccountRecord account)
    {
        BsonDocument document = Encode(account, AccountDirectory.SerializationOptions);
        document.InsertAt(0, new BsonElement("_id", PlayerId(userId)));
        document.AddRange(new BsonDocument
        {
            ["settingsVersion"] = 0L,
            ["inventoryVersion"] = 0L,
            ["settings"] = Encode(AccountStateDocument.Empty, AccountStore.SerializationOptions),
            ["inventory"] = Encode(PlayerInventory.Empty, InventoryStore.SerializationOptions),
        });
        return Execute(() =>
        {
            try
            {
                _accounts.InsertOne(document);
                return true;
            }
            catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        });
    }

    /// <summary>Removes an account with its game data and raid reservations.</summary>
    internal bool DeleteAccount(long userId)
    {
        string id = PlayerId(userId);
        return Execute(() =>
        {
            using IClientSessionHandle session = _client.StartSession();
            return session.WithTransaction((transaction, cancellationToken) =>
            {
                bool deleted = _accounts.DeleteOne(transaction, new BsonDocument("_id", id), cancellationToken: cancellationToken).DeletedCount == 1;
                _reservations.DeleteMany(transaction, new BsonDocument("playerId", id), cancellationToken: cancellationToken);
                return deleted;
            });
        });
    }

    /// <summary>The account with <paramref name="nameKey"/>, or null.</summary>
    internal (long UserId, AccountRecord Account)? FindAccountByName(string nameKey) => FindAccount(new BsonDocument("nameKey", nameKey));

    /// <summary>The account with <paramref name="userId"/>, or null.</summary>
    internal AccountRecord? FindAccount(long userId) => FindAccount(new BsonDocument("_id", PlayerId(userId)))?.Account;

    /// <summary>The account holding the launch key with <paramref name="hash"/>, or null.</summary>
    internal (long UserId, AccountRecord Account)? FindAccountByLaunchKey(string hash) => FindAccount(new BsonDocument("launchKey.hash", hash));

    /// <summary>Every account, ordered by name.</summary>
    internal IReadOnlyList<(long UserId, AccountRecord Account)> ListAccounts()
    {
        List<BsonDocument> records = Execute(() => _accounts.Find(FilterDefinition<BsonDocument>.Empty).Project(s_accountFields)
            .Sort(new BsonDocument("nameKey", 1)).ToList());
        return [.. records.Select(DecodeAccount)];
    }

    /// <summary>Replaces one account field.</summary>
    /// <returns>False when the account does not exist.</returns>
    internal bool UpdateAccount<T>(long userId, string field, T value)
    {
        UpdateDefinition<BsonDocument> update = value is null
            ? Builders<BsonDocument>.Update.Unset(field)
            : Builders<BsonDocument>.Update.Set(field, EncodeValue(JsonSerializer.SerializeToElement(value, AccountDirectory.SerializationOptions)));
        return Execute(() => _accounts.UpdateOne(new BsonDocument("_id", PlayerId(userId)), update)).MatchedCount == 1;
    }

    /// <summary>Sets an account's name and lookup key.</summary>
    /// <returns>False when another account has the name.</returns>
    internal bool RenameAccount(long userId, string name, string nameKey)
    {
        UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set("name", name).Set("nameKey", nameKey);
        return Execute(() =>
        {
            try
            {
                return _accounts.UpdateOne(new BsonDocument("_id", PlayerId(userId)), update).MatchedCount == 1;
            }
            catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return false;
            }
        });
    }

    /// <summary>Players that hold a raid reservation.</summary>
    internal IReadOnlyList<long> ReservationOwners()
    {
        List<string> owners = Execute(() => _reservations.Distinct<string>("playerId", FilterDefinition<BsonDocument>.Empty).ToList());
        return [.. owners.Select(owner => long.Parse(owner, NumberStyles.None, CultureInfo.InvariantCulture))];
    }

    private (long UserId, AccountRecord Account)? FindAccount(BsonDocument filter)
    {
        BsonDocument? record = Execute(() => _accounts.Find(filter).Project(s_accountFields).FirstOrDefault());
        return record is null ? null : DecodeAccount(record);
    }

    private static (long UserId, AccountRecord Account) DecodeAccount(BsonDocument record)
    {
        if (!record.TryGetValue("_id", out BsonValue? id) || !id.IsString
            || !long.TryParse(id.AsString, NumberStyles.None, CultureInfo.InvariantCulture, out long userId))
        {
            throw new InvalidDataException("Invalid account id");
        }

        BsonDocument fields = record.DeepClone().AsBsonDocument;
        fields.Remove("_id");
        return (userId, AccountDirectory.Validate(Decode<AccountRecord>(fields, AccountDirectory.SerializationOptions)));
    }

    internal InventoryReservation? ReadReservation(long userId, string? ticketId)
    {
        if (ticketId is null)
        {
            return null;
        }

        BsonDocument record = Execute(() => _reservations.Find(new BsonDocument("_id", ticketId)).FirstOrDefault())
            ?? throw new InvalidDataException("Referenced raid reservation is missing");
        return new InventoryReservation(ticketId, ReservationLoadout(userId, record));
    }

    private static PlayerLoadout ReservationLoadout(long userId, BsonDocument record)
    {
        if (record["playerId"] != PlayerId(userId))
        {
            throw new InvalidDataException("Invalid raid reservation owner");
        }

        PlayerLoadoutState loadout = Decode<PlayerLoadoutState>(record["loadout"], InventoryStore.SerializationOptions);
        PlayerInventory inventory = new(record["inventoryRevision"].AsInt64, new PlayerStash(0, []), loadout);
        return InventorySnapshots.Loadout(InventoryProjection.Create(userId, inventory).Snapshot);
    }

    internal void SaveInventory(long userId, PlayerInventory inventory, long version, InventoryReservation? reservation, string? previousTicket)
    {
        if (inventory.RaidTicketId == previousTicket)
        {
            Save(userId, "inventory", inventory, version, InventoryStore.SerializationOptions);
            return;
        }

        Execute(() =>
        {
            using IClientSessionHandle session = _client.StartSession();
            return session.WithTransaction((transaction, cancellationToken) =>
            {
                string playerId = PlayerId(userId);
                if (reservation is not null)
                {
                    StoreReservation(transaction, userId, reservation, inventory, cancellationToken);
                }

                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", playerId)
                    & Builders<BsonDocument>.Filter.Eq("inventoryVersion", version);
                UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set("inventory", Encode(inventory, InventoryStore.SerializationOptions))
                    .Set("inventoryVersion", checked(version + 1));
                if (_accounts.UpdateOne(transaction, filter, update, cancellationToken: cancellationToken).MatchedCount != 1)
                {
                    throw new PersistenceConflictException();
                }

                if (previousTicket is not null)
                {
                    _reservations.DeleteOne(transaction, new BsonDocument { ["_id"] = previousTicket, ["playerId"] = playerId }, cancellationToken: cancellationToken);
                }

                return true;
            });
        });
    }

    private void StoreReservation(IClientSessionHandle session, long userId, InventoryReservation reservation, PlayerInventory inventory, CancellationToken cancellationToken)
    {
        string playerId = PlayerId(userId);
        BsonDocument? existing = _reservations.Find(session, new BsonDocument("_id", reservation.TicketId)).FirstOrDefault(cancellationToken);
        if (existing is not null)
        {
            if (existing["playerId"] != playerId || !InventorySnapshots.Same(reservation.Loadout,
                    ReservationLoadout(userId, existing)))
            {
                throw new PersistenceConflictException();
            }

            return;
        }

        BsonDocument document = new()
        {
            ["_id"] = reservation.TicketId,
            ["playerId"] = playerId,
            ["inventoryRevision"] = inventory.Revision,
            ["loadout"] = Encode(inventory.Loadout, InventoryStore.SerializationOptions),
        };

        _reservations.InsertOne(session, document, cancellationToken: cancellationToken);
    }

    internal (T State, long Version) Read<T>(long userId, string section, JsonSerializerOptions options)
    {
        BsonDocument document = Execute(() => _accounts.Find(new BsonDocument("_id", PlayerId(userId))).FirstOrDefault())
            ?? throw new InvalidDataException("Account document is missing");
        ValidateEnvelope(document);
        BsonValue value = document[section];
        return (Decode<T>(value, options), document[section + "Version"].AsInt64);
    }

    private static T Decode<T>(BsonValue value, JsonSerializerOptions options)
    {
        if (!value.IsBsonDocument)
        {
            throw new InvalidDataException("Invalid account document section");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(value.AsBsonDocument.ToJson(s_json), options)
                ?? throw new InvalidDataException("Account document section is empty");
        }
        catch (JsonException)
        {
            throw new InvalidDataException("Malformed account document section");
        }
    }

    internal void Save<T>(long userId, string section, T state, long expectedVersion, JsonSerializerOptions options)
    {
        BsonDocument encoded = Encode(state, options);
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", PlayerId(userId))
            & Builders<BsonDocument>.Filter.Eq(section + "Version", expectedVersion);
        UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set(section, encoded)
            .Set(section + "Version", checked(expectedVersion + 1));
        if (Execute(() => _accounts.UpdateOne(filter, update)).MatchedCount != 1)
        {
            throw new PersistenceConflictException();
        }
    }

    /// <summary>Releases this Host's MongoDB client connections.</summary>
    public void Dispose() => _client.Dispose();

    private static T Execute<T>(Func<T> operation)
    {
        try
        {
            return operation();
        }
        catch (Exception error) when (error is MongoException or TimeoutException)
        {
            throw new PersistenceException(error.GetType().Name);
        }
    }

    // Encode JSON values by their declared types, preserving literal player-key names
    // such as "$date" and the full precision of integral IDs.
    private static BsonDocument Encode<T>(T value, JsonSerializerOptions options) => EncodeValue(JsonSerializer.SerializeToElement(value, options)).AsBsonDocument;

    private static BsonValue EncodeValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Object => new BsonDocument(value.EnumerateObject().Select(property => new BsonElement(property.Name, EncodeValue(property.Value)))),
            JsonValueKind.Array => new BsonArray(value.EnumerateArray().Select(EncodeValue)),
            JsonValueKind.String => new BsonString(value.GetString()!),
            JsonValueKind.Number when value.TryGetInt64(out long integer) => new BsonInt64(integer),
            JsonValueKind.Number => new BsonDouble(value.GetDouble()),
            JsonValueKind.True => BsonBoolean.True,
            JsonValueKind.False => BsonBoolean.False,
            JsonValueKind.Null => BsonNull.Value,
            JsonValueKind.Undefined => throw new InvalidDataException("Undefined account document value"),
            _ => throw new InvalidDataException("Unsupported account document value"),
        };
    }

    private static string PlayerId(long userId) => userId.ToString(CultureInfo.InvariantCulture);

    private static void ValidateEnvelope(BsonDocument document)
    {
        if (!document.TryGetValue("settingsVersion", out BsonValue? settings) || !settings.IsInt64 || settings.AsInt64 < 0
            || !document.TryGetValue("inventoryVersion", out BsonValue? inventory) || !inventory.IsInt64 || inventory.AsInt64 < 0
            || !document.Contains("settings") || !document.Contains("inventory"))
        {
            throw new InvalidDataException("Unsupported or malformed Host database schema");
        }
    }
}
