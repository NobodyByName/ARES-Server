using Ares.Common.Game.Inventory;
using Ares.Structs.Backend.Progression;

namespace Ares.Common.Tests;

public sealed class LoadoutTreeTests
{
    private static readonly long[] s_branches = [.. ((StructuralItemSlots)LoadoutDefinitions.ByAsset[LoadoutItems.StandardFrameAssetId].SlotConfig!).SlotAssetIds];

    [Theory]
    [InlineData(null)]
    [InlineData(AugmentItems.CombatMk1AssetId)]
    [InlineData(AugmentItems.LootingMk1AssetId)]
    [InlineData(AugmentItems.TacticalMk1AssetId)]
    public void Typed_slots_survive_the_tree_handed_to_a_raid(long? augment)
    {
        LoadoutContents contents = Contents(augment);
        LoadoutTree tree = LoadoutTree.Build(contents, Structure);
        PlayerLoadout loadout = InventorySnapshots.Loadout(new InventorySnapshot(1, 0, tree.Items, tree.Roots));

        Assert.Equivalent(contents, LoadoutTree.Parse(loadout), strict: true);
        // Every frame stays in the menu tree, but only the selected one is handed to the raid.
        Assert.Equal(AugmentItems.FrameAssetIds.Count + 1, tree.Items.Count(item => item.GameAssetId == LoadoutItems.StandardFrameAssetId
            || AugmentItems.FrameAssetIds.Values.Contains(item.GameAssetId)));
        Assert.Single(loadout.Items, item => item.GameAssetId == contents.FrameAssetId);
        // The Ferro carries one empty node per mod slot.
        InventoryItem ferro = Assert.Single(loadout.Items, item => item.GameAssetId == WeaponItems.FerroIAssetId);
        Assert.Equal(((StructuralItemSlots)LoadoutDefinitions.ByAsset[WeaponItems.FerroIAssetId].SlotConfig!).SlotAssetIds,
            ferro.Slots.Select(slot => loadout.Items.Single(item => item.InstanceId == slot).GameAssetId));
        LoadoutSlot armor = tree.Slots.Values.Single(slot => slot is { Branch: LoadoutItems.ArmorSlotAssetId } && slot.FrameAssetId == contents.FrameAssetId);
        Assert.Contains(ShieldItems.LightShieldAssetId, armor.AllowedAssetIds);
        Assert.Single(tree.Slots.Values, slot => slot is { Branch: AugmentItems.AugmentSlotAssetId, FrameAssetId: null });
    }

    [Fact]
    public void An_empty_handoff_has_no_augment_and_no_branches()
    {
        LoadoutContents parsed = LoadoutTree.Parse(new PlayerLoadout(1, 0, []));
        Assert.Null(parsed.Augment);
        Assert.Empty(parsed.Branches);
        Assert.Equal(LoadoutItems.StandardFrameAssetId, parsed.FrameAssetId);
    }

    [Fact]
    public void Items_must_fit_their_slots_and_the_tree_must_keep_its_roots()
    {
        Dictionary<long, IReadOnlyList<InventoryItem?>> misplaced = new() { [LoadoutItems.Firearm1SlotAssetId] = [Item(StorageItems.RubberDuckAssetId, 1)] };
        Assert.Throws<InvalidDataException>(() => LoadoutTree.Build(new LoadoutContents(null, misplaced), Structure));

        LoadoutTree tree = LoadoutTree.Build(Contents(null), Structure);
        PlayerLoadout loadout = InventorySnapshots.Loadout(new InventorySnapshot(1, 0, tree.Items, [tree.Roots[0]]));
        Assert.NotNull(LoadoutTree.Parse(loadout));
        PlayerLoadout augmentOnly = InventorySnapshots.Loadout(new InventorySnapshot(1, 0, tree.Items, [tree.Roots[1]]));
        Assert.Throws<InvalidDataException>(() => LoadoutTree.Parse(augmentOnly));
    }

    // A loadout filling a slot of every kind: augment, firearm, armor, backpack, quick-use and safe pocket.
    private static LoadoutContents Contents(long? augment)
    {
        long frame = LoadoutDefinitions.FrameAssetId(augment);
        Dictionary<long, IReadOnlyList<InventoryItem?>> branches = s_branches.ToDictionary(branch => branch,
            IReadOnlyList<InventoryItem?> (branch) => new InventoryItem?[LoadoutDefinitions.Container(frame, branch).NumberOfSlots]);
        Place(LoadoutItems.Firearm1SlotAssetId, 0, Item(WeaponItems.FerroIAssetId, 1));
        Place(LoadoutItems.ArmorSlotAssetId, 0, Item(ShieldItems.LightShieldAssetId, 1));
        Place(LoadoutItems.BackpackSlotAssetId, 1, Item(WeaponItems.HeavyAmmoAssetId, 40));
        Place(LoadoutItems.BackpackSlotAssetId, 3, Item(StorageItems.RubberDuckAssetId, 2));
        Place(LoadoutItems.BeltSlotAssetId, 0, Item(GadgetItems.SnapHookAssetId, 1));
        Place(LoadoutItems.SafePocketSlotAssetId, 0, Item(StorageItems.RubberDuckAssetId, 1));
        return new LoadoutContents(augment is { } equipped ? Item(equipped, 1) : null, branches);

        void Place(long branch, int index, InventoryItem item) => ((InventoryItem?[])branches[branch])[index] = item;
    }

    private static InventoryItem Item(long asset, long amount) => new(Guid.NewGuid().ToString("D"), asset, amount, [], 0.5, 1, "", "v1", 3);

    private static InventoryItem Structure(long asset, string path, IReadOnlyList<string> children) => new(path, asset, 1, children, 1, 1, "", "structure", 0);
}
