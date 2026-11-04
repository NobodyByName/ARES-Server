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
public sealed record InventoryOperationReceipt(string RequestId, string Sha256);

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
    private long _version;
    private PlayerInventory _state;
    private InventoryProjection _projection;

    public InventoryStore(HostDatabase database)
    {
        _database = database;
        (_state, _version) = _database.Read<PlayerInventory>(LocalPlayer.TenancyUserId, "inventory", SerializationOptions);
        _projection = InventoryProjection.Create(_state);
    }

    private void Refresh()
    {
        (_state, _version) = _database.Read<PlayerInventory>(LocalPlayer.TenancyUserId, "inventory", SerializationOptions);
        _projection = InventoryProjection.Create(_state);
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
            IEnumerable<ItemDescription> definitions = InventoryCatalog.Definitions.Select(definition => InventoryCatalog.Definition(_state, definition));
            return BackendContent.GameData with
            {
                ItemDescriptions = new ItemDescriptionTable([.. BackendContent.GameData.ItemDescriptions.ItemDescriptions, .. definitions]),
            };
        }
    }

    public string DefinitionHash() => "ares-host-" + Convert.ToHexStringLower(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(GameData(), BackendJson.Options)));

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
                return existing == ticketId ? _database.ReadReservation(existing)!.Loadout
                    : throw new InvalidOperationException("The inventory is reserved for another ticket");
            }

            PlayerLoadout loadout = InventorySnapshots.Loadout(_projection.Snapshot);
            if (loadout.Items.Count > 0)
            {
                Save(_state with { RaidTicketId = ticketId }, new InventoryReservation(ticketId, loadout));
            }

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
        InventoryProjection projection = InventoryProjection.Create(candidate);
        if (JsonSerializer.SerializeToUtf8Bytes(candidate, SerializationOptions).Length > 1048576)
        {
            throw new InvalidDataException("Inventory save is too large");
        }

        _database.SaveInventory(candidate, _version, reservation, _state.RaidTicketId);
        _version = checked(_version + 1);
        _state = candidate;
        _projection = projection;
    }
}
