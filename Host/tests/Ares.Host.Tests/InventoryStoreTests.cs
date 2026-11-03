using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Ares.Common.Game.Inventory;
using Ares.Host.Services.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Tests;

public sealed class InventoryStoreTests : IDisposable
{
    private const string Ticket = "0b7f6c3e-9d0a-4c1b-8e2f-3a4b5c6d7e8f";
    private const string OtherTicket = "1b7f6c3e-9d0a-4c1b-8e2f-3a4b5c6d7e8f";
    private const long AssetId = 5000000001;
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "ares-inventory-test-" + Guid.NewGuid().ToString("N"));

    private static InventoryImport Provision() => new(
        [new InventoryItem("carried", AssetId, 2, [], 0, 0, "", "test", 0), new InventoryItem("stash", AssetId, 3, [], 0, 0, "", "test", 0)],
        [new ItemDescription(AssetId, "test", 10, true, false, [], [])], ["carried"]);

    [Fact]
    public void Standard_player_inventory_is_added_once_without_replacing_the_stash()
    {
        InventoryStore store = new(_directory);
        store.EnsureStarterStorage(DateTimeOffset.UnixEpoch);
        InventoryItem[] original = [.. store.Snapshot.Items];
        store.EnsureStarterLoadout(DateTimeOffset.UnixEpoch.AddMinutes(1));
        InventorySnapshot snapshot = store.Snapshot;
        Assert.True(InventorySnapshots.Same(new PlayerLoadout(snapshot.UserId, snapshot.Revision, original),
            new PlayerLoadout(snapshot.UserId, snapshot.Revision, [.. snapshot.Items.Take(original.Length)])));
        InventoryItem root = Assert.Single(snapshot.Items, item => item.GameAssetId == LoadoutItems.FrameSlotAssetId);
        Assert.Equal(root.InstanceId, Assert.Single(snapshot.LoadoutRoots));
        InventoryItem backpack = Assert.Single(snapshot.Items, item => item.GameAssetId == LoadoutItems.BackpackAssetId);
        Assert.Equal(10, backpack.Slots.Count);
        Assert.All(backpack.Slots, id => Assert.Empty(snapshot.Items.Single(item => item.InstanceId == id).Slots));
        Assert.DoesNotContain(InventorySnapshots.Loadout(snapshot).Items, item => item.GameAssetId == StorageItems.RubberDuckAssetId);

        InventoryStore restarted = new(_directory);
        restarted.EnsureStarterLoadout(DateTimeOffset.UnixEpoch.AddDays(1));
        Assert.Equal(snapshot.Revision, restarted.Snapshot.Revision);
        Assert.Equal(snapshot.Items.Select(item => item.InstanceId), restarted.Snapshot.Items.Select(item => item.InstanceId));
        Assert.Equal(store.DefinitionHash(), restarted.DefinitionHash());
        restarted.Import(new InventoryImport([], [], []), snapshot.Revision);
        restarted.EnsureStarterLoadout(DateTimeOffset.UnixEpoch.AddDays(2));
        Assert.Empty(restarted.Snapshot.Items);
    }

    [Fact]
    public void Starter_storage_contains_one_duck_and_is_persisted_once()
    {
        InventoryStore store = new(_directory);
        store.EnsureStarterStorage(DateTimeOffset.UnixEpoch);
        InventorySnapshot first = store.Snapshot;
        InventoryItem duck = Assert.Single(first.Items, item => item.GameAssetId == StorageItems.RubberDuckAssetId);
        Assert.Equal(1, duck.Amount);
        InventoryItem root = Assert.Single(first.Items, item => item.GameAssetId == StorageItems.StashSlotAssetId);
        InventoryItem container = Assert.Single(first.Items, item => item.InstanceId == Assert.Single(root.Slots));
        Assert.Equal(StorageItems.StashContainerAssetId, container.GameAssetId);
        Assert.Equal(StorageItems.StashSlotCount, container.Slots.Count);
        Assert.Equal(StorageItems.StashSlotCount, first.Items.Count(item => item.GameAssetId == StorageItems.ContentSlotAssetId));
        InventoryItem occupied = Assert.Single(first.Items, item => item.Slots.Contains(duck.InstanceId));
        Assert.Equal(StorageItems.ContentSlotAssetId, occupied.GameAssetId);
        Assert.Empty(first.LoadoutRoots);

        InventoryStore restarted = new(_directory);
        restarted.EnsureStarterStorage(DateTimeOffset.UnixEpoch.AddDays(1));
        Assert.Equal(first.Revision, restarted.Snapshot.Revision);
        Assert.Equal(first.Items.Select(item => item.InstanceId), restarted.Snapshot.Items.Select(item => item.InstanceId));
        Assert.Equal(store.DefinitionHash(), restarted.DefinitionHash());
    }

    [Fact]
    public void Starter_grants_preserve_existing_items_and_do_not_repeat_after_an_operator_clear()
    {
        InventoryStore existing = new(_directory);
        existing.Import(Provision(), 0);
        existing.EnsureStarterStorage(DateTimeOffset.UnixEpoch);
        Assert.Equal(2, existing.Snapshot.Items.Count);
        Assert.DoesNotContain(existing.Snapshot.Items, item => item.GameAssetId == StorageItems.RubberDuckAssetId);
        existing.Import(new InventoryImport([], [], []), existing.Snapshot.Revision);
        InventoryStore restarted = new(_directory);
        restarted.EnsureStarterStorage(DateTimeOffset.UnixEpoch);
        Assert.Empty(restarted.Snapshot.Items);
    }

    [Fact]
    public void An_existing_empty_save_receives_storage_and_invalid_slot_layouts_are_rejected()
    {
        InventoryStore empty = new(_directory);
        empty.Import(new InventoryImport([], [], []), 0);
        InventoryStore store = new(_directory);
        store.EnsureStarterStorage(DateTimeOffset.UnixEpoch);
        InventoryItem[] broken = [.. store.Snapshot.Items.Select(item => item.GameAssetId == StorageItems.StashContainerAssetId
            ? item with { Slots = [.. item.Slots.Skip(1)] }
            : item)];
        Assert.Throws<InvalidDataException>(() => store.Import(new InventoryImport(broken, StorageItems.Definitions, []), store.Snapshot.Revision));
        Assert.Single(store.Snapshot.Items, item => item.GameAssetId == StorageItems.RubberDuckAssetId);
    }

    [Fact]
    public void Imports_and_reservations_survive_restarts_without_granting_items_again()
    {
        InventoryStore first = new(_directory);
        Assert.Empty(first.Snapshot.Items);
        first.Import(Provision(), 0);
        PlayerLoadout reserved = first.Reserve(Ticket);

        InventoryStore restarted = new(_directory);
        Assert.Equal(1, restarted.Snapshot.Revision);
        Assert.Equal(2, restarted.Snapshot.Items.Count);
        Assert.Equal("carried", Assert.Single(restarted.Reserve(Ticket).Items).InstanceId);
        Assert.True(InventorySnapshots.Same(reserved, restarted.Reserve(Ticket)));
        Assert.Throws<InvalidOperationException>(() => restarted.Reserve(OtherTicket));
        Assert.Throws<InvalidOperationException>(() => restarted.Import(Provision(), 1));

        restarted.Release(OtherTicket);
        Assert.Equal(Ticket, restarted.ReservedTicketId);
        restarted.Release(Ticket);
        restarted.Import(Provision(), 1);
        Assert.Equal(2, new InventoryStore(_directory).Snapshot.Revision);
        Assert.Equal(2, new InventoryStore(_directory).Snapshot.Items.Count);
    }

    [Fact]
    public void Invalid_or_stale_imports_leave_the_saved_inventory_unchanged()
    {
        InventoryStore store = new(_directory);
        InventoryImport valid = Provision();
        store.Import(valid, 0);
        byte[] saved = File.ReadAllBytes(Path.Combine(_directory, "inventory-state.json"));
        Assert.Throws<InvalidOperationException>(() => store.Import(valid, 0));
        Assert.Throws<InvalidDataException>(() => store.Import(valid with { Items = [valid.Items[0] with { Amount = 11 }] }, 1));
        Assert.Throws<InvalidDataException>(() => store.Import(valid with { Definitions = [] }, 1));
        Assert.Throws<InvalidDataException>(() => store.Import(valid with { Items = [valid.Items[0] with { LockedInRound = Ticket }] }, 1));
        Assert.Equal(saved, File.ReadAllBytes(Path.Combine(_directory, "inventory-state.json")));
    }

    [Fact]
    public void A_failed_save_does_not_publish_the_new_state()
    {
        Directory.CreateDirectory(Path.Combine(_directory, "inventory-state.json"));
        InventoryStore store = new(_directory);
        Assert.ThrowsAny<IOException>(() => store.Import(Provision(), 0));
        Assert.Equal(0, store.Snapshot.Revision);
        Assert.Empty(store.Snapshot.Items);
        Assert.Empty(Directory.GetFiles(_directory));
    }

    [Fact]
    public void An_oversized_selected_loadout_is_rejected_before_reservation()
    {
        InventoryStore store = new(_directory);
        InventoryItem[] items = [.. Enumerable.Range(0, 256).Select(index => new InventoryItem(
            index.ToString("D3", CultureInfo.InvariantCulture) + new string('x', 125), AssetId, 1, [], 0, 0, "", new string('é', 128), 0))];
        InventoryImport provision = Provision() with { Items = items, LoadoutRoots = [.. items.Select(item => item.InstanceId)] };
        Assert.Throws<InvalidDataException>(() => store.Import(provision, 0));
        Assert.Equal(0, store.Snapshot.Revision);
        store.Import(provision with { LoadoutRoots = [] }, 0);
        Assert.Empty(store.Reserve(Ticket).Items);
    }

    [Fact]
    public void Malformed_or_foreign_saves_fail_instead_of_resetting_the_player()
    {
        InventoryStore store = new(_directory);
        store.Import(Provision(), 0);
        string path = Path.Combine(_directory, "inventory-state.json");
        string text = File.ReadAllText(path);
        File.WriteAllText(path, text.Replace("\"format\": 1", "\"format\": 99", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => new InventoryStore(_directory));
        JsonObject foreignBuild = JsonNode.Parse(text)!.AsObject();
        foreignBuild["gameBuildId"] = 0;
        File.WriteAllText(path, foreignBuild.ToJsonString());
        Assert.Throws<InvalidDataException>(() => new InventoryStore(_directory));
        File.WriteAllText(path, "null");
        Assert.Throws<InvalidDataException>(() => new InventoryStore(_directory));
        File.WriteAllText(path, "{\"format\":1}");
        Assert.Throws<JsonException>(() => new InventoryStore(_directory));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
