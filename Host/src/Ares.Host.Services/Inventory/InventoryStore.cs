using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ares.Common.Control;
using Ares.Common.Game.Inventory;
using Ares.Host.Services.Content;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Matchmaking;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Core;

namespace Ares.Host.Services.Inventory;

/// <summary>Items and definitions provisioned by the operator, with the inventory roots selected for raids.</summary>
public sealed record InventoryImport(IReadOnlyList<InventoryItem> Items, IReadOnlyList<ItemDescription> Definitions,
    IReadOnlyList<string> LoadoutRoots);

/// <summary>A loadout retained while its matchmaking reservation is outstanding.</summary>
public sealed record InventoryReservation(string TicketId, PlayerLoadout Loadout);

/// <summary>A bounded deduplication entry for a committed menu request.</summary>
public sealed record InventoryOperationReceipt(string RequestId, string Sha256);

/// <summary>The versioned inventory save in the Host data directory.</summary>
public sealed record InventoryDocument(int Format, int GameBuildId, InventorySnapshot Snapshot, IReadOnlyList<ItemDescription> Definitions,
    InventoryReservation? Reservation)
{
    /// <summary>Whether the local account's initial storage grant has already been considered.</summary>
    public bool StarterStorageGranted { get; init; }

    /// <summary>Whether the local account's standard player inventory has already been considered.</summary>
    public bool StarterLoadoutGranted { get; init; }

    /// <summary>Recently committed menu requests, retained across restarts.</summary>
    public IReadOnlyList<InventoryOperationReceipt> OperationReceipts { get; init; } = [];
}

/// <summary>
/// The local player's inventory. Imports and reservations are saved atomically to
/// <c>inventory-state.json</c>; malformed saves fail instead of resetting the player.
/// </summary>
public sealed partial class InventoryStore
{
    private const int Format = 1;
    private const int MaximumFileBytes = 1048576;
    private static readonly JsonSerializerOptions s_json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private readonly Lock _lock = new();
    private readonly string _path;
    private InventoryDocument _state;

    /// <summary>Loads the local player's save, or starts with an empty inventory.</summary>
    public InventoryStore(string dataDirectory)
    {
        _path = Path.Combine(dataDirectory, "inventory-state.json");
        _state = File.Exists(_path)
            ? Validate(ReadFile<InventoryDocument>(_path))
            : new InventoryDocument(Format, GameBuild.SteamBuildId, new InventorySnapshot(LocalPlayer.TenancyUserId, 0, [], []), [], null);
    }

    /// <summary>Provisions an empty local account once, preserving existing inventories and subsequent operator imports.</summary>
    public void EnsureStarterStorage(DateTimeOffset now)
    {
        lock (_lock)
        {
            if (_state.StarterStorageGranted)
            {
                return;
            }

            if (_state.Snapshot.Items.Count != 0 || _state.Reservation is not null)
            {
                Save(_state with { StarterStorageGranted = true });
                return;
            }

            long revision = checked(_state.Snapshot.Revision + 1);
            InventoryImport starter = StarterStorage.Create(revision, now);
            InventorySnapshot snapshot = new(LocalPlayer.TenancyUserId, revision, starter.Items, starter.LoadoutRoots);
            InventoryDocument candidate = new(Format, GameBuild.SteamBuildId, snapshot, starter.Definitions, null)
            {
                StarterStorageGranted = true,
            };
            Save(Validate(candidate));
        }
    }

    /// <summary>The current immutable snapshot.</summary>
    public InventorySnapshot Snapshot
    {
        get
        {
            lock (_lock)
            {
                return _state.Snapshot;
            }
        }
    }

    /// <summary>Adds the empty standard loadout to the starter stash, preserving all existing item identities.</summary>
    public void EnsureStarterLoadout(DateTimeOffset now)
    {
        lock (_lock)
        {
            if (_state.StarterLoadoutGranted || _state.Reservation is not null)
            {
                return;
            }

            if (_state.Snapshot.Items.All(item => item.GameAssetId != StorageItems.StashSlotAssetId)
                || _state.Snapshot.Items.Any(item => item.GameAssetId == LoadoutItems.FrameSlotAssetId))
            {
                Save(_state with { StarterLoadoutGranted = true });
                return;
            }

            long revision = checked(_state.Snapshot.Revision + 1);
            InventoryImport loadout = StarterLoadout.Create(revision, now);
            InventorySnapshot snapshot = _state.Snapshot with
            {
                Revision = revision,
                Items = [.. _state.Snapshot.Items, .. loadout.Items],
                LoadoutRoots = [.. _state.Snapshot.LoadoutRoots, .. loadout.LoadoutRoots],
            };
            Save(Validate(_state with
            {
                Snapshot = snapshot,
                Definitions = [.. _state.Definitions, .. loadout.Definitions.ExceptBy(_state.Definitions.Select(item => item.GameAssetId), item => item.GameAssetId)],
                StarterLoadoutGranted = true,
            }));
        }
    }

    /// <summary>The ticket with a reserved non-empty loadout, if any.</summary>
    public string? ReservedTicketId
    {
        get
        {
            lock (_lock)
            {
                return _state.Reservation?.TicketId;
            }
        }
    }

    /// <summary>The bootstrap tables plus the definitions of the persisted inventory.</summary>
    public GameDataResponse GameData()
    {
        lock (_lock)
        {
            return BackendContent.GameData with
            {
                ItemDescriptions = new ItemDescriptionTable([.. BackendContent.GameData.ItemDescriptions.ItemDescriptions, .. _state.Definitions]),
            };
        }
    }

    /// <summary>A stable manifest hash that changes when the served definition tables change.</summary>
    public string DefinitionHash() => "ares-host-" + Convert.ToHexStringLower(SHA256.HashData(
        JsonSerializer.SerializeToUtf8Bytes(GameData(), BackendJson.Options)));

    /// <summary>Imports a bounded operator file without granting anything on subsequent sign-ins.</summary>
    public void ImportFile(string path, long expectedRevision) => Import(ReadFile<InventoryImport>(path), expectedRevision);

    /// <summary>Replaces an idle inventory if its revision still matches the operator's snapshot.</summary>
    public void Import(InventoryImport inventory, long expectedRevision)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        lock (_lock)
        {
            if (_state.Reservation is not null)
            {
                throw new InvalidOperationException("The inventory is reserved for a raid");
            }

            if (_state.Snapshot.Revision != expectedRevision)
            {
                throw new InvalidOperationException("The inventory revision changed");
            }

            InventorySnapshot snapshot = new(LocalPlayer.TenancyUserId, checked(expectedRevision + 1), inventory.Items, inventory.LoadoutRoots);
            InventoryDocument candidate = Validate(new InventoryDocument(Format, GameBuild.SteamBuildId, snapshot, inventory.Definitions, null)
            {
                StarterStorageGranted = _state.StarterStorageGranted,
                StarterLoadoutGranted = _state.StarterLoadoutGranted,
            });
            Save(candidate);
        }
    }

    /// <summary>Freezes the selected item trees for a ticket; retries return the same saved snapshot.</summary>
    public PlayerLoadout Reserve(string ticketId)
    {
        if (!Guid.TryParseExact(ticketId, "D", out Guid ticket) || ticket.ToString("D") != ticketId)
        {
            throw new ArgumentException("A reservation needs a canonical ticket id", nameof(ticketId));
        }

        lock (_lock)
        {
            if (_state.Reservation is { } reservation)
            {
                return reservation.TicketId == ticketId
                    ? reservation.Loadout
                    : throw new InvalidOperationException("The inventory is reserved for another ticket");
            }

            PlayerLoadout loadout = InventorySnapshots.Loadout(_state.Snapshot);
            if (loadout.Items.Count > 0)
            {
                Save(_state with { Reservation = new InventoryReservation(ticketId, loadout) });
            }

            return loadout;
        }
    }

    /// <summary>
    /// Releases an allocation confirmed lost by Raid. Carried items are unchanged: raid inventory
    /// mutations and result settlement are not supported yet.
    /// </summary>
    public void Release(string ticketId)
    {
        lock (_lock)
        {
            if (_state.Reservation?.TicketId == ticketId)
            {
                Save(_state with { Reservation = null });
            }
        }
    }

    private static InventoryDocument Validate(InventoryDocument document)
    {
        if (document.Format != Format || document.GameBuildId != GameBuild.SteamBuildId
            || document.Snapshot is not { UserId: LocalPlayer.TenancyUserId }
            || document.Definitions is not { Count: <= InventorySnapshots.MaximumItems }
            || document.OperationReceipts is not { Count: <= MaximumReceipts }
            || document.OperationReceipts.Any(receipt => receipt is not { Sha256: { Length: 64 } hash } || !ValidRequestId(receipt.RequestId)
                || hash.Any(character => !char.IsAsciiHexDigitLower(character)))
            || document.OperationReceipts.Select(receipt => receipt.RequestId).Distinct(StringComparer.Ordinal).Count() != document.OperationReceipts.Count)
        {
            throw new InvalidDataException("Unsupported inventory save");
        }

        InventorySnapshot snapshot = InventorySnapshots.Copy(document.Snapshot);
        Dictionary<long, ItemDescription> definitions = BackendContent.GameData.ItemDescriptions.ItemDescriptions.ToDictionary(item => item.GameAssetId);
        List<ItemDescription> copies = [];
        foreach (ItemDescription definition in document.Definitions)
        {
            if (definition is not { GameAssetId: not 0, ItemType.Length: <= 128, MaxAmount: > 0, Tags.Count: <= 128, GroupIds.Count: <= 128 }
                || string.IsNullOrWhiteSpace(definition.ItemType) || definition.Tags.Any(tag => tag is not { Length: <= 256 })
                || !definitions.TryAdd(definition.GameAssetId, definition))
            {
                throw new InvalidDataException("Invalid or duplicate inventory definition");
            }

            copies.Add(definition with
            {
                Tags = Array.AsReadOnly([.. definition.Tags]),
                GroupIds = Array.AsReadOnly([.. definition.GroupIds]),
                SlotConfig = FreezeSlots(definition.SlotConfig),
            });
        }

        HashSet<long> unique = [];
        Dictionary<string, InventoryItem> byId = snapshot.Items.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
        foreach (InventoryItem item in snapshot.Items)
        {
            if (!definitions.TryGetValue(item.GameAssetId, out ItemDescription? definition) || item.Amount > definition.MaxAmount
                || (!definition.Stackable && item.Amount != 1) || (definition.Unique && !unique.Add(item.GameAssetId))
                || item.LockedInRound.Length != 0)
            {
                throw new InvalidDataException("Inventory item does not match an available definition");
            }

            bool validSlots = definition.SlotConfig switch
            {
                NoItemSlots => item.Slots.Count == 0,
                SingleItemSlot slot => item.Slots.Count <= 1 && (!slot.IsRequired || item.Slots.Count == 1)
                    && item.Slots.All(id => slot.AllowedSlotAssetIds.Contains(byId[id].GameAssetId)),
                ItemSlotArray slots => item.Slots.Count == slots.NumberOfSlots
                    && item.Slots.All(id => byId[id].GameAssetId == slots.SlotAssetId),
                StructuralItemSlots slots => item.Slots.Select(id => byId[id].GameAssetId).SequenceEqual(slots.SlotAssetIds),
                null => true,
                _ => false,
            };
            if (!validSlots)
            {
                throw new InvalidDataException("Inventory children do not match their slot configuration");
            }
        }

        InventoryReservation? reservation = document.Reservation;
        PlayerLoadout selected = InventorySnapshots.Loadout(snapshot);
        AllocationRequest transfer = new(Guid.Empty.ToString("D"), ScenarioNames.TheDam, [snapshot.UserId], [selected]);
        if (JsonSerializer.SerializeToUtf8Bytes(transfer, AllocationApi.JsonOptions).Length > AllocationApi.MaximumBodyBytes)
        {
            throw new InvalidDataException("Selected loadout is too large for the allocation API");
        }

        InventoryDocument validated = document with
        {
            Snapshot = snapshot,
            Definitions = copies.AsReadOnly(),
            OperationReceipts = Array.AsReadOnly([.. document.OperationReceipts]),
        };
        if (reservation is null)
        {
            return validated;
        }

        if (!Guid.TryParseExact(reservation.TicketId, "D", out Guid ticket) || ticket.ToString("D") != reservation.TicketId
            || !InventorySnapshots.Same(selected, InventorySnapshots.Copy(reservation.Loadout)))
        {
            throw new InvalidDataException("Inventory reservation does not match its save");
        }

        return validated with { Reservation = reservation with { Loadout = InventorySnapshots.Copy(reservation.Loadout) } };
    }

    private static ItemSlotConfiguration? FreezeSlots(ItemSlotConfiguration? configuration)
    {
        return configuration switch
        {
            NoItemSlots or null => configuration,
            SingleItemSlot { AllowedSlotAssetIds.Count: <= 2048 } slot when slot.AllowedSlotAssetIds.All(id => id != 0)
                && slot.AllowedSlotAssetIds.Distinct().Count() == slot.AllowedSlotAssetIds.Count
                && (slot.DefaultSlotAssetId == 0 || slot.AllowedSlotAssetIds.Contains(slot.DefaultSlotAssetId))
                => slot with { AllowedSlotAssetIds = Array.AsReadOnly([.. slot.AllowedSlotAssetIds]) },
            ItemSlotArray { SlotAssetId: not 0, NumberOfSlots: >= 0 and <= InventorySnapshots.MaximumItems } => configuration,
            StructuralItemSlots { SlotAssetIds.Count: <= InventorySnapshots.MaximumItems } slots when slots.SlotAssetIds.All(id => id != 0)
                && slots.SlotAssetIds.Distinct().Count() == slots.SlotAssetIds.Count
                => new StructuralItemSlots(Array.AsReadOnly([.. slots.SlotAssetIds])),
            _ => throw new InvalidDataException("Invalid inventory slot configuration"),
        };
    }

    private static T ReadFile<T>(string path)
    {
        using FileStream stream = File.OpenRead(path);
        if (stream.Length > MaximumFileBytes)
        {
            throw new InvalidDataException("Inventory file is too large");
        }

        return JsonSerializer.Deserialize<T>(stream, s_json) ?? throw new InvalidDataException("Inventory file is empty");
    }

    private void Save(InventoryDocument candidate)
    {
        string directory = Path.GetDirectoryName(_path)!;
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(directory);
        }
        else
        {
            Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        byte[] data = JsonSerializer.SerializeToUtf8Bytes(candidate, s_json);
        if (data.Length > MaximumFileBytes)
        {
            throw new InvalidDataException("Inventory save is too large");
        }

        string temporary = Path.Combine(directory, ".inventory-" + Guid.NewGuid().ToString("N"));
        try
        {
            FileStreamOptions options = new() { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
            if (!OperatingSystem.IsWindows())
            {
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            }

            using (FileStream stream = new(temporary, options))
            {
                stream.Write(data);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporary, _path, overwrite: true);
            _state = candidate;
        }
        finally
        {
            File.Delete(temporary);
        }
    }
}
