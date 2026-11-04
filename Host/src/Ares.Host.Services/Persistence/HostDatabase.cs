using System.Globalization;
using System.Text.Json;
using Ares.Common.Game.Inventory;
using Ares.Host.Services.Accounts;
using Ares.Host.Services.Content;
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
/// One operator-owned MongoDB database per Host. Each player is an ordinary BSON document;
/// account and inventory sections use optimistic concurrency for atomic updates.
/// </summary>
public sealed class HostDatabase : IDisposable
{
    private static readonly JsonWriterSettings s_json = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };
    private readonly MongoClient _client;
    private readonly IMongoCollection<BsonDocument> _players;
    private readonly IMongoCollection<BsonDocument> _reservations;

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
        _players = _client.GetDatabase(databaseName).GetCollection<BsonDocument>("players");
        _reservations = _client.GetDatabase(databaseName).GetCollection<BsonDocument>("raidReservations");
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

    /// <summary>Validates existing player metadata or creates the initial account and inventory sections.</summary>
    public void Initialize()
    {
        string id = PlayerId(LocalPlayer.TenancyUserId);
        BsonDocument? existing = Execute(() => _players.Find(new BsonDocument("_id", id)).FirstOrDefault());
        if (existing is not null)
        {
            ValidateEnvelope(existing);
            return;
        }

        BsonDocument document = new()
        {
            ["_id"] = id,
            ["accountVersion"] = 0L,
            ["inventoryVersion"] = 0L,
            ["account"] = Encode(AccountStateDocument.Empty, AccountStore.SerializationOptions),
            ["inventory"] = Encode(PlayerInventory.Empty, InventoryStore.SerializationOptions),
        };
        Execute(() =>
        {
            try
            {
                _players.InsertOne(document);
            }
            catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                ValidateEnvelope(_players.Find(new BsonDocument("_id", id)).Single());
            }

            return true;
        });
    }

    internal InventoryReservation? ReadReservation(string? ticketId)
    {
        if (ticketId is null)
        {
            return null;
        }

        BsonDocument record = Execute(() => _reservations.Find(new BsonDocument("_id", ticketId)).FirstOrDefault())
            ?? throw new InvalidDataException("Referenced raid reservation is missing");
        return new InventoryReservation(ticketId, ReservationLoadout(record));
    }

    private static PlayerLoadout ReservationLoadout(BsonDocument record)
    {
        if (record["playerId"] != PlayerId(LocalPlayer.TenancyUserId))
        {
            throw new InvalidDataException("Invalid raid reservation owner");
        }

        PlayerLoadoutState loadout = Decode<PlayerLoadoutState>(record["loadout"], InventoryStore.SerializationOptions);
        PlayerInventory inventory = new(record["inventoryRevision"].AsInt64, new PlayerStash(0, []), loadout);
        return InventorySnapshots.Loadout(InventoryProjection.Create(inventory).Snapshot);
    }

    internal void SaveInventory(PlayerInventory inventory, long version, InventoryReservation? reservation, string? previousTicket)
    {
        if (inventory.RaidTicketId == previousTicket)
        {
            Save(LocalPlayer.TenancyUserId, "inventory", inventory, version, InventoryStore.SerializationOptions);
            return;
        }

        Execute(() =>
        {
            using IClientSessionHandle session = _client.StartSession();
            return session.WithTransaction((transaction, cancellationToken) =>
            {
                string playerId = PlayerId(LocalPlayer.TenancyUserId);
                if (reservation is not null)
                {
                    StoreReservation(transaction, playerId, reservation, inventory, cancellationToken);
                }

                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", playerId)
                    & Builders<BsonDocument>.Filter.Eq("inventoryVersion", version);
                UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set("inventory", Encode(inventory, InventoryStore.SerializationOptions))
                    .Set("inventoryVersion", checked(version + 1));
                if (_players.UpdateOne(transaction, filter, update, cancellationToken: cancellationToken).MatchedCount != 1)
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

    private void StoreReservation(IClientSessionHandle session, string playerId, InventoryReservation reservation, PlayerInventory inventory, CancellationToken cancellationToken)
    {
        BsonDocument? existing = _reservations.Find(session, new BsonDocument("_id", reservation.TicketId)).FirstOrDefault(cancellationToken);
        if (existing is not null)
        {
            if (existing["playerId"] != playerId || !InventorySnapshots.Same(reservation.Loadout,
                    ReservationLoadout(existing)))
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
        BsonDocument document = Execute(() => _players.Find(new BsonDocument("_id", PlayerId(userId))).FirstOrDefault())
            ?? throw new InvalidDataException("Player document is missing; refusing to recreate existing player data");
        ValidateEnvelope(document);
        BsonValue value = document[section];
        return (Decode<T>(value, options), document[section + "Version"].AsInt64);
    }

    private static T Decode<T>(BsonValue value, JsonSerializerOptions options)
    {
        if (!value.IsBsonDocument)
        {
            throw new InvalidDataException("Invalid player document section");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(value.AsBsonDocument.ToJson(s_json), options)
                ?? throw new InvalidDataException("Player document section is empty");
        }
        catch (JsonException)
        {
            throw new InvalidDataException("Malformed player document section");
        }
    }

    internal void Save<T>(long userId, string section, T state, long expectedVersion, JsonSerializerOptions options)
    {
        BsonDocument encoded = Encode(state, options);
        FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("_id", PlayerId(userId))
            & Builders<BsonDocument>.Filter.Eq(section + "Version", expectedVersion);
        UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set(section, encoded)
            .Set(section + "Version", checked(expectedVersion + 1));
        if (Execute(() => _players.UpdateOne(filter, update)).MatchedCount != 1)
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
            JsonValueKind.Undefined => throw new InvalidDataException("Undefined player document value"),
            _ => throw new InvalidDataException("Unsupported player document value"),
        };
    }

    private static string PlayerId(long userId) => userId.ToString(CultureInfo.InvariantCulture);

    private static void ValidateEnvelope(BsonDocument document)
    {
        if (!document.TryGetValue("accountVersion", out BsonValue? account) || !account.IsInt64 || account.AsInt64 < 0
            || !document.TryGetValue("inventoryVersion", out BsonValue? inventory) || !inventory.IsInt64 || inventory.AsInt64 < 0
            || !document.Contains("account") || !document.Contains("inventory"))
        {
            throw new InvalidDataException("Unsupported or malformed Host database schema");
        }
    }
}
