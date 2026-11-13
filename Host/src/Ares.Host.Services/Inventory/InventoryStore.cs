using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ares.Common.Game.Inventory;
using Ares.Host.Services.Content;
using Ares.Host.Services.Persistence;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Services.Inventory;

/// <summary>The selected client loadout for an outstanding raid handoff.</summary>
public sealed record InventoryReservation(string TicketId, PlayerLoadout Loadout);

/// <summary>A bounded receipt for an already committed inventory request.</summary>
public sealed record InventoryOperationReceipt(string RequestId, string Sha256)
{
    /// <summary>Removed structural nodes to repeat when a client retries after losing the response.</summary>
    public IReadOnlyList<string> DeletedItemIds { get; init; } = [];
}

/// <summary>Owns compact persistent inventory and projects the game's protocol tree on demand.</summary>
public sealed partial class InventoryStore
{
    internal static JsonSerializerOptions SerializationOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private readonly Lock _lock = new();
    private readonly HostDatabase _database;
    private readonly long _userId;
    private long _version;
    private PlayerInventory _state;
    private InventoryProjection _projection;

    /// <summary>Opens the inventory of player <paramref name="userId"/>.</summary>
    public InventoryStore(HostDatabase database, long userId)
    {
        _database = database;
        _userId = userId;
        (_state, _version) = _database.Read<PlayerInventory>(_userId, "inventory", SerializationOptions);
        _projection = InventoryProjection.Create(_userId, _state);
    }

    private void Refresh()
    {
        (_state, _version) = _database.Read<PlayerInventory>(_userId, "inventory", SerializationOptions);
        _projection = InventoryProjection.Create(_userId, _state);
    }

    /// <summary>Sets missing stash capacity once. Positive capacity survives emptying the inventory.</summary>
    public void InitializeLayout(DateTimeOffset now)
    {
        lock (_lock)
        {
            Refresh();
            PlayerInventory candidate = _state;
            if (candidate.Stash.SlotCount == 0)
            {
                IReadOnlyList<OwnedItem> contents = candidate.Stash.Items.Count == 0
                    ? [new OwnedItem(Guid.NewGuid().ToString("D"), ItemTechnicalNames.RubberDuck, 1,
                        StorageItems.InitialDurability, StorageItems.InitialMaxDurability, "ares-initial", now.ToUnixTimeMilliseconds())]
                    : candidate.Stash.Items;
                candidate = candidate with { Stash = new PlayerStash(StorageItems.StashSlotCount, contents) };
            }

            if (candidate != _state)
            {
                Save(candidate with { Revision = checked(_state.Revision + 1) });
            }
        }
    }

    /// <summary>The player's current owned-item state.</summary>
    public PlayerInventory Inventory
    {
        get
        {
            lock (_lock)
            {
                Refresh();
                return _state;
            }
        }
    }

    /// <summary>Current client-facing graph, generated with stable structural identities.</summary>
    public InventorySnapshot Snapshot
    {
        get
        {
            lock (_lock)
            {
                Refresh();
                return _projection.Snapshot;
            }
        }
    }

    public string? ReservedTicketId
    {
        get
        {
            lock (_lock)
            {
                Refresh();
                return _state.RaidTicketId;
            }
        }
    }

    /// <summary>Shared item definitions adapted to the purchased stash capacity.</summary>
    public GameDataResponse GameData()
    {
        lock (_lock)
        {
            Refresh();
            return GameData(_state);
        }
    }

    public string DefinitionHash() => DefinitionHashOf(GameData());

    private static GameDataResponse GameData(PlayerInventory state)
    {
        IEnumerable<ItemDescription> definitions = InventoryCatalog.Definitions.Select(definition => InventoryCatalog.Definition(state, definition));
        return BackendContent.GameData with
        {
            ItemDescriptions = new ItemDescriptionTable([.. BackendContent.GameData.ItemDescriptions.ItemDescriptions, .. definitions]),
        };
    }

    private static string DefinitionHashOf(GameDataResponse data) =>
        "ares-host-" + Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(data, BackendJson.Options)));

    public void ImportFile(string path, long expectedRevision)
    {
        using FileStream stream = File.OpenRead(path);
        if (stream.Length > 1048576)
        {
            throw new InvalidDataException("Inventory import is too large");
        }

        Import(JsonSerializer.Deserialize<PlayerInventory>(stream, SerializationOptions)
            ?? throw new InvalidDataException("Inventory import is empty"), expectedRevision);
    }

    /// <summary>Adds one supported item stack to the idle player's stash from the local operator console.</summary>
    public void AddItem(string technicalName, DateTimeOffset now, long quantity = 1)
    {
        if (!ItemTechnicalNames.AssetIds.ContainsKey(technicalName))
        {
            throw new InvalidDataException("Unknown inventory item technical name");
        }

        ItemDescription definition = InventoryCatalog.ByAsset[InventoryCatalog.AssetId(technicalName)];
        if (quantity <= 0 || quantity > definition.MaxAmount || !definition.Stackable && quantity != 1)
        {
            throw new InvalidDataException("Invalid item quantity");
        }

        lock (_lock)
        {
            Refresh();
            if (_state.RaidTicketId is not null)
            {
                throw new InvalidOperationException("The inventory is reserved for a raid");
            }

            OwnedItem item = new(Guid.NewGuid().ToString("D"), technicalName, quantity,
                StorageItems.InitialDurability, StorageItems.InitialMaxDurability, "ares-operator", now.ToUnixTimeMilliseconds());
            Save(_state with
            {
                Revision = checked(_state.Revision + 1),
                Stash = _state.Stash with { Items = [.. _state.Stash.Items, item] },
            });
        }
    }

    public void Import(PlayerInventory inventory, long expectedRevision)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        lock (_lock)
        {
            Refresh();
            if (_state.RaidTicketId is not null)
            {
                throw new InvalidOperationException("The inventory is reserved for a raid");
            }

            if (_state.Revision != expectedRevision)
            {
                throw new InvalidOperationException("The inventory revision changed");
            }

            if (inventory.RaidTicketId is not null)
            {
                throw new InvalidDataException("Inventory import cannot create a raid reservation");
            }

            Save(inventory with { Revision = checked(expectedRevision + 1) });
        }
    }

    public PlayerLoadout Reserve(string ticketId)
    {
        if (!Guid.TryParseExact(ticketId, "D", out Guid ticket) || ticket.ToString("D") != ticketId)
        {
            throw new ArgumentException("A reservation needs a canonical ticket id", nameof(ticketId));
        }

        lock (_lock)
        {
            Refresh();
            if (_state.RaidTicketId is { } existing)
            {
                return existing == ticketId ? _database.ReadReservation(_userId, existing)!.Loadout
                    : throw new InvalidOperationException("The inventory is reserved for another ticket");
            }

            PlayerLoadout loadout = InventorySnapshots.Loadout(_projection.Snapshot);
            Save(_state with { RaidTicketId = ticketId }, new InventoryReservation(ticketId, loadout));
            return loadout;
        }
    }

    /// <summary>Releases an outstanding handoff after Raid reports it lost.</summary>
    public void Release(string ticketId)
    {
        lock (_lock)
        {
            Refresh();
            if (_state.RaidTicketId == ticketId)
            {
                Save(_state with { RaidTicketId = null });
            }
        }
    }

    private void Save(PlayerInventory candidate, InventoryReservation? reservation = null)
    {
        InventoryProjection projection = InventoryProjection.Create(_userId, candidate);
        if (JsonSerializer.SerializeToUtf8Bytes(candidate, SerializationOptions).Length > 1048576)
        {
            throw new InvalidDataException("Inventory save is too large");
        }

        _database.SaveInventory(_userId, candidate, _version, reservation, _state.RaidTicketId);
        _version = checked(_version + 1);
        _state = candidate;
        _projection = projection;
    }
}
