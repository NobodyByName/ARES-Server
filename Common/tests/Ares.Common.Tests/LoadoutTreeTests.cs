using Ares.Common.Game.Content;
using Ares.Common.Game.Inventory;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game;

namespace Ares.Common.Tests;

/// <summary>Real-data tier: the loadout tree of the default ARES content over the loaded game data.</summary>
public sealed class LoadoutTreeTests
{
    private static readonly LoadoutContent s_loadout = AresContent.Defaults.Loadout;
    private static readonly Lazy<LoadoutDefinitions> s_definitions = new(() => AresContent.Defaults.ItemDefinitions(RealGameData.Data));

    private static LoadoutDefinitions Definitions => s_definitions.Value;

    private static GameDataSet Data => RealGameData.Data;

    private static long RubberDuck => Data.Items.ByTechnicalName[ItemTechnicalNames.RubberDuck].AssetId;

    [RealDataTheory]
    [InlineData(null)]
    [InlineData(GameNames.Items.CombatMk1)]
    [InlineData(GameNames.Items.LootingMk1)]
    [InlineData(GameNames.Items.TacticalMk1)]
    public void Typed_slots_survive_the_tree_handed_to_a_raid(string? augmentName)
    {
        long? augment = augmentName is null ? null : Data.Items.ByTechnicalName[augmentName].AssetId;
        LoadoutContents contents = Contents(augment);
        LoadoutTree tree = LoadoutTree.Build(Definitions, contents, Structure);
        PlayerLoadout loadout = InventorySnapshots.Loadout(new InventorySnapshot(1, 0, tree.Items, tree.Roots));

        Assert.Equivalent(contents, LoadoutTree.Parse(Definitions, loadout), strict: true);
        // Every frame stays in the menu tree, but only the selected one is handed to the raid.
        Assert.Equal(Data.Menu.Augments.FrameAssetIds.Count + 1, tree.Items.Count(item => item.GameAssetId == s_loadout.StandardFrameAssetId
            || Data.Menu.Augments.FrameAssetIds.Values.Contains(item.GameAssetId)));
        Assert.Single(loadout.Items, item => item.GameAssetId == contents.FrameAssetId(Definitions));
        // The Ferro carries one empty node per mod slot.
        InventoryItem ferro = Assert.Single(loadout.Items, item => item.GameAssetId == Data.Names.FerroI.AssetId);
        Assert.Equal(((StructuralItemSlots)Definitions.ByAsset[Data.Names.FerroI.AssetId].SlotConfig!).SlotAssetIds,
            ferro.Slots.Select(slot => loadout.Items.Single(item => item.InstanceId == slot).GameAssetId));
        LoadoutSlot armor = tree.Slots.Values.Single(slot => slot.Branch == s_loadout.Branches.Shield && slot.FrameAssetId == contents.FrameAssetId(Definitions));
        Assert.Contains(Data.Names.LightShield.AssetId, armor.AllowedAssetIds);
        Assert.Single(tree.Slots.Values, slot => slot.Branch == Data.Names.AugmentSlotAssetId && slot.FrameAssetId is null);
    }

    [RealDataFact]
    public void An_empty_handoff_has_no_augment_and_no_branches()
    {
        LoadoutContents parsed = LoadoutTree.Parse(Definitions, new PlayerLoadout(1, 0, []));
        Assert.Null(parsed.Augment);
        Assert.Empty(parsed.Branches);
        Assert.Equal(s_loadout.StandardFrameAssetId, parsed.FrameAssetId(Definitions));
    }

    [RealDataFact]
    public void Items_must_fit_their_slots_and_the_tree_must_keep_its_roots()
    {
        Dictionary<long, IReadOnlyList<InventoryItem?>> misplaced = new() { [s_loadout.Branches.MainWeapon] = [Item(RubberDuck, 1)] };
        Assert.Throws<InvalidDataException>(() => LoadoutTree.Build(Definitions, new LoadoutContents(null, misplaced), Structure));

        LoadoutTree tree = LoadoutTree.Build(Definitions, Contents(null), Structure);
        PlayerLoadout loadout = InventorySnapshots.Loadout(new InventorySnapshot(1, 0, tree.Items, [tree.Roots[0]]));
        Assert.NotNull(LoadoutTree.Parse(Definitions, loadout));
        PlayerLoadout augmentOnly = InventorySnapshots.Loadout(new InventorySnapshot(1, 0, tree.Items, [tree.Roots[1]]));
        Assert.Throws<InvalidDataException>(() => LoadoutTree.Parse(Definitions, augmentOnly));
    }

    // A loadout filling a slot of every kind: augment, firearm, armor, backpack, quick-use and safe pocket.
    private static LoadoutContents Contents(long? augment)
    {
        long frame = Definitions.FrameAssetId(augment);
        long[] frameBranches = [.. ((StructuralItemSlots)Definitions.ByAsset[s_loadout.StandardFrameAssetId].SlotConfig!).SlotAssetIds];
        Dictionary<long, IReadOnlyList<InventoryItem?>> branches = frameBranches.ToDictionary(branch => branch,
            IReadOnlyList<InventoryItem?> (branch) => new InventoryItem?[Definitions.Container(frame, branch).NumberOfSlots]);
        Place(s_loadout.Branches.MainWeapon, 0, Item(Data.Names.FerroI.AssetId, 1));
        Place(s_loadout.Branches.Shield, 0, Item(Data.Names.LightShield.AssetId, 1));
        Place(s_loadout.Branches.Backpack, 1, Item(Data.Names.HeavyAmmo.AssetId, 40));
        Place(s_loadout.Branches.Backpack, 3, Item(RubberDuck, 2));
        Place(s_loadout.Branches.QuickUse, 0, Item(Data.Names.SnapHook.AssetId, 1));
        Place(s_loadout.Branches.SafePockets, 0, Item(RubberDuck, 1));
        return new LoadoutContents(augment is { } equipped ? Item(equipped, 1) : null, branches);

        void Place(long branch, int index, InventoryItem item) => ((InventoryItem?[])branches[branch])[index] = item;
    }

    private static InventoryItem Item(long asset, long amount) => new(Guid.NewGuid().ToString("D"), asset, amount, [], 0.5, 1, "", "v1", 3);

    private static InventoryItem Structure(long asset, string path, IReadOnlyList<string> children) => new(path, asset, 1, children, 1, 1, "", "structure", 0);
}
