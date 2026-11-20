using Ares.Common.Game.Loot;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Raid.World.Loot;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.Loot;

/// <summary>
/// Contents profiles on made-up game data: how the profiles of a container's tag chain and class merge, and how the
/// merged profile shapes table pool, type pool and loose item socket rolls. The last test uses the real game data.
/// </summary>
public sealed class LootProfileRollsTests
{
    private const string Tag = SyntheticGameData.ContainerTag;
    private const string LooseTag = SyntheticGameData.LooseItemTag;
    private const string SupplyTag = LooseTag + ".Supply";
    private const string RaiderClass = "/Game/Synthetic/BP_Raider.BP_Raider_C";
    private const string Table = "StaticValueLootTable_Synth";
    private const string Salvage = "Item.Drop.Type.Salvage";
    private const string Fabric = "DA_Item_Synth_Fabric";
    private const string Wires = "DA_Item_Synth_Wires";
    private const string GunParts = "DA_Item_Synth_GunParts";
    private const string Gem = "DA_Item_Synth_Gem";
    private const string Seeds = "DA_Item_Synth_Seeds";
    private const string Ammo = "DA_Item_Synth_Ammo";
    private const string Bandage = "DA_Item_Synth_Bandage";
    private const int Rolls = 400;

    private static readonly GameDataSet s_data = Data();

    [Fact]
    public void Profiles_merge_down_the_tag_chain_then_the_class_and_the_nearest_wins()
    {
        LootTypeEntry root = Entry("Item.Drop.Type.Ammo", 1);
        LootProfile[] profiles =
        [
            Profile("Class", containerClass: SyntheticGameData.ContainerClass, stacks: new LootRange(3, 3), first: new LootFirstStackTier(null)),
            Profile("Category", "socket.lootcontainer.SYNTHETIC", weights: [Weight(Salvage, 3), Weight(Wires, 0.5, item: true)], tiers: new Dictionary<string, double> { ["Rare"] = 2 },
                add: [Add(Seeds, 4, item: true)], extra: new LootProfileExtraStack(0, null)),
            Profile("Top", "Socket.LootContainer", only: [Match(Salvage)], weights: [Weight(Salvage, 2), Weight(Fabric, 5, item: true)], stacks: new LootRange(1, 1),
                tiers: new Dictionary<string, double> { ["Rare"] = 0.5, ["Common"] = 4 }, add: [Add(Seeds, 1, item: true), Add("Item.Drop.Type.Ammo", 2)],
                first: new LootFirstStackTier("Rare"), extra: new LootProfileExtraStack(0.5, [root])),
            Profile("Elsewhere", "Socket.LootContainer.Synthetic.Other", stacks: new LootRange(7, 7)),
        ];

        // A tag below a profile's tag, spelled with another case, still finds the chain; the class comes last.
        MergedLootProfile merged = MergedLootProfile.Resolve(profiles, "Socket.LootCOntainer.Synthetic.Shelf", SyntheticGameData.ContainerClass);
        Assert.Equal(["Top", "Category", "Class"], merged.Chain.Select(profile => profile.Name));
        Assert.Equal(new LootRange(3, 3), merged.Stacks);
        Assert.Equal(new LootFirstStackTier(null), merged.FirstStackTier);
        Assert.Equal([Match(Salvage)], merged.Only!);
        Assert.Equal((2, 4), (merged.Tiers["Rare"], merged.Tiers["Common"]));
        Assert.Equal(1, merged.TierMultiplier("Other"));

        // Weights and adds are keyed by match: the nearest one wins, other keys accumulate, nearest profile first.
        Assert.Equal([Weight(Salvage, 3), Weight(Wires, 0.5, item: true), Weight(Fabric, 5, item: true)], merged.Weights);
        Assert.Equal([Add(Seeds, 4, item: true), Add("Item.Drop.Type.Ammo", 2)], merged.Add);
        Assert.Equal(15, merged.Multiplier(Item(Fabric)));
        Assert.Equal(1.5, merged.Multiplier(Item(Wires)));
        Assert.Equal(1, merged.Multiplier(Item(Gem)));

        // Each part of the extra stack comes from the nearest profile setting it.
        Assert.True(merged.SetsExtraStack);
        Assert.Equal(0, merged.ExtraStackChance);
        Assert.Same(root, Assert.Single(merged.ExtraStackEntries!));

        // A child can set a key back to 1; without a class or below no profile, the chain is shorter or empty.
        MergedLootProfile reset = MergedLootProfile.Resolve([.. profiles, Profile("Reset", Tag + ".Shelf", weights: [Weight(Salvage, 1)])], Tag + ".Shelf", "");
        Assert.Equal(5, reset.Multiplier(Item(Fabric)));
        Assert.Equal(["Top", "Category", "Reset"], reset.Chain.Select(profile => profile.Name));
        MergedLootProfile none = MergedLootProfile.Resolve(profiles, "Socket.Loot.Synthetic", null);
        Assert.Empty(none.Chain);
        Assert.Equal((null, null, false, 1.0), (none.Only, none.Stacks, none.SetsExtraStack, none.Multiplier(Item(Fabric))));
    }

    [Fact]
    public void Without_a_profile_for_them_rolls_are_the_same_as_without_profiles()
    {
        LootContentRolls plain = Containers();
        LootContentRolls elsewhere = Containers(Profile("Other tag", Tag + ".Other", only: [Match(Gem, item: true)]), Profile("Empty", Tag),
            Profile("Other class", containerClass: SyntheticGameData.ContainerClass, stacks: new LootRange(5, 5)));
        LooseItemRolls loosePlain = Loose();
        LooseItemRolls looseElsewhere = Loose(Profile("Other loose", LooseTag + ".Other", only: [Match(Gem, item: true)]), Profile("Empty loose", LooseTag));

        Random a = new(3);
        Random b = new(3);
        for (int roll = 0; roll < Rolls; roll++)
        {
            Assert.Equal(plain.Roll(Tag, "", 8, PointOfInterestLootValue.High, a).Stacks, elsewhere.Roll(Tag, "", 8, PointOfInterestLootValue.High, b).Stacks);
            Assert.Equal(plain.Roll(null, RaiderClass, 8, PointOfInterestLootValue.Unknown, a).Stacks, elsewhere.Roll(null, RaiderClass, 8, PointOfInterestLootValue.Unknown, b).Stacks);
            Assert.Equal(loosePlain.Roll(SupplyTag, PointOfInterestLootValue.Unknown, a), looseElsewhere.Roll(SupplyTag, PointOfInterestLootValue.Unknown, b));
        }
    }

    [Fact]
    public void Only_keeps_matching_items_and_a_zero_multiplier_removes_an_item()
    {
        LootContentRolls rolls = Containers(Profile("Salvage", Tag, only: [Match(Salvage)], weights: [Weight(GunParts, 0, item: true)]));

        HashSet<string> seen = [.. Draws(rolls, Tag).Select(stack => stack.Item.TechnicalName)];
        Assert.Equal([Fabric, Wires], seen.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void An_item_within_its_tier_is_drawn_by_its_multiplier()
    {
        // One stack, always of the first stack's tier (Common: fabric, wires, gun parts).
        LootContentRolls even = Containers(Profile("One", Tag, stacks: new LootRange(1, 1)));
        LootContentRolls weighted = Containers(Profile("Fabric", Tag, stacks: new LootRange(1, 1), weights: [Weight(Fabric, 8, item: true)]));

        double evenShare = Draws(even, Tag).Count(stack => stack.Item.TechnicalName == Fabric) / (double)Rolls;
        double weightedShare = Draws(weighted, Tag).Count(stack => stack.Item.TechnicalName == Fabric) / (double)Rolls;
        Assert.InRange(evenShare, 0.25, 0.42);
        Assert.InRange(weightedShare, 0.72, 0.88);
    }

    [Fact]
    public void An_added_item_bypasses_placement_lists_with_its_amount_and_tier()
    {
        // Seeds has no placement for the container; added in the Common tier, it is the only Common candidate left.
        LootContentRolls rolls = Containers(Profile("Seeds", Tag, only: [Match(Seeds, item: true), Match(Gem, item: true)], add: [Add(Seeds, 3, item: true, tier: "Common")],
            stacks: new LootRange(1, 1)));
        Assert.All(Draws(rolls, Tag), stack => Assert.Equal((Seeds, 3, 1.0), (stack.Item.TechnicalName, stack.Amount, stack.Durability)));

        // A drop tag adds every item with it; the items keep their own tier by default.
        LootContentRolls byTag = Containers(Profile("Nature", Tag, only: [Match("Item.Drop.Type.Nature")], add: [Add("Item.Drop.Type.Nature", 2)]));
        Assert.All(Draws(byTag, Tag), stack => Assert.Equal((Seeds, 2), (stack.Item.TechnicalName, stack.Amount)));
        Assert.Equal("Other", byTag.TierOf(Item(Seeds)).Name);

        // When several adds bring an item, the first (nearest profile first, then listed order) sets amount and tier.
        LootProfile parent = Profile("Parent", "Socket.LootContainer", only: [Match(Seeds, item: true)], add: [Add("Item.Drop.Type.Nature", 2)]);
        Assert.All(Draws(Containers(parent, Profile("Child", Tag, add: [Add(Seeds, 3, item: true)])), Tag), stack => Assert.Equal(3, stack.Amount));
        Assert.All(Draws(Containers(Profile("Both", Tag, only: [Match(Seeds, item: true)], add: [Add("Item.Drop.Type.Nature", 2), Add(Seeds, 3, item: true)])), Tag),
            stack => Assert.Equal(2, stack.Amount));
    }

    [Fact]
    public void Root_excluded_drop_tags_keep_out_drop_tag_adds_but_not_items_added_by_name()
    {
        string[] excluded = ["Item.Drop.Type.Nature"];
        LootProfile byTag = Profile("Nature", Tag, only: [Match(Seeds, item: true)], add: [Add("Item.Drop.Type.Nature", 1)]);
        LootProfile byName = Profile("Seeds", Tag, only: [Match(Seeds, item: true)], add: [Add(Seeds, 1, item: true)]);

        Assert.Empty(Draws(Containers(excluded, byTag), Tag));
        Assert.All(Draws(Containers(excluded, byName), Tag), stack => Assert.Equal(Seeds, stack.Item.TechnicalName));
        Assert.Equal(Rolls, Draws(Containers(excluded, byName), Tag).Count);
        Assert.Null(Loose(excluded, byTag with { Tag = LooseTag }).Roll(LooseTag, PointOfInterestLootValue.Unknown, new Random(1)));
        Assert.Equal(Seeds, Loose(excluded, byName with { Tag = LooseTag }).Roll(LooseTag, PointOfInterestLootValue.Unknown, new Random(1))?.Item.TechnicalName);
    }

    [Fact]
    public void A_null_first_stack_tier_turns_forcing_off_and_a_zero_tier_multiplier_removes_the_tier()
    {
        LootContentRolls inherited = Containers(Profile("One", Tag, stacks: new LootRange(1, 1)));
        LootContentRolls off = Containers(Profile("One", Tag, stacks: new LootRange(1, 1), first: new LootFirstStackTier(null)));
        LootContentRolls rare = Containers(Profile("One", Tag, stacks: new LootRange(1, 1), first: new LootFirstStackTier("Rare")));
        LootContentRolls noRare = Containers(Profile("Three", Tag, stacks: new LootRange(3, 3), first: new LootFirstStackTier(null), tiers: new Dictionary<string, double> { ["Rare"] = 0 }));

        Assert.DoesNotContain(Draws(inherited, Tag), stack => stack.Item.TechnicalName == Gem);
        Assert.InRange(Draws(off, Tag).Count(stack => stack.Item.TechnicalName == Gem), Rolls / 5, Rolls * 4 / 5);
        Assert.All(Draws(rare, Tag), stack => Assert.Equal(Gem, stack.Item.TechnicalName));
        Assert.DoesNotContain(Draws(noRare, Tag), stack => stack.Item.TechnicalName == Gem);
    }

    [Fact]
    public void When_only_tiers_weighing_zero_remain_nothing_more_is_drawn()
    {
        Dictionary<string, double> none = new() { ["Common"] = 0, ["Rare"] = 0, ["Other"] = 0 };
        LootContentRolls containers = Containers(Profile("None", Tag, stacks: new LootRange(3, 3), first: new LootFirstStackTier(null), tiers: none));
        LooseItemRolls loose = Loose(Profile("None", LooseTag, tiers: none));

        Assert.Empty(Draws(containers, Tag));
        Assert.Empty(LooseDraws(loose, LooseTag, new Random(1)));
    }

    [Fact]
    public void Profile_stacks_override_the_pool_and_stay_capped_by_the_slots()
    {
        LootContentRolls rolls = Containers(Profile("Three", Tag, stacks: new LootRange(3, 3)));
        Random random = new(5);

        Assert.Equal(3, rolls.Roll(Tag, "", 8, PointOfInterestLootValue.Unknown, random).Stacks.Count);
        Assert.Equal(2, rolls.Roll(Tag, "", 2, PointOfInterestLootValue.Unknown, random).Stacks.Count);
        Assert.Equal(2, Containers().Roll(Tag, "", 8, PointOfInterestLootValue.Unknown, random).Stacks.Count);
        Assert.Empty(Containers(Profile("None", Tag, stacks: new LootRange(0, 0))).Roll(Tag, "", 8, PointOfInterestLootValue.Unknown, random).Stacks);
    }

    [Fact]
    public void A_profile_extra_stack_applies_on_a_pool_without_the_flag()
    {
        // The pool has no extra stack; the root extra stack (ammo, chance 1) applies once a profile sets one.
        Assert.DoesNotContain(Draws(Containers(), Tag), stack => stack.Item.TechnicalName == Ammo);
        List<LootStack> root = Draws(Containers(Profile("Extra", Tag, extra: new LootProfileExtraStack(null, null))), Tag);
        Assert.Equal(Rolls, root.Count(stack => stack.Item.TechnicalName == Ammo && stack.Amount == 5));

        List<LootStack> own = Draws(Containers(Profile("Extra", Tag, extra: new LootProfileExtraStack(null, [Entry("Item.Drop.Type.Consumable.Healing", 2)]))), Tag);
        Assert.Equal(Rolls, own.Count(stack => stack.Item.TechnicalName == Bandage && stack.Amount == 2));
        Assert.DoesNotContain(own, stack => stack.Item.TechnicalName == Ammo);

        // Chance 0 turns it off; only and zero multipliers filter its items too.
        Assert.DoesNotContain(Draws(Containers(Profile("Extra", Tag, extra: new LootProfileExtraStack(0, null))), Tag), stack => stack.Item.TechnicalName == Ammo);
        Assert.DoesNotContain(Draws(Containers(Profile("Extra", Tag, extra: new LootProfileExtraStack(1, null), weights: [Weight(Ammo, 0, item: true)])), Tag),
            stack => stack.Item.TechnicalName == Ammo);
    }

    [Fact]
    public void Type_pools_take_the_profile_filters_adds_stacks_and_extra_stack()
    {
        LootContentRolls rolls = Containers(Profile("Raider", containerClass: RaiderClass, weights: [Weight(GunParts, 0, item: true)], add: [Add(Seeds, 4, item: true)],
            stacks: new LootRange(3, 3), extra: new LootProfileExtraStack(1, null), tiers: new Dictionary<string, double> { ["Common"] = 0 }));
        Random random = new(8);

        for (int roll = 0; roll < Rolls; roll++)
        {
            LootContents contents = rolls.Roll(null, RaiderClass, 8, PointOfInterestLootValue.Unknown, random);
            Assert.Equal("Raider", contents.Pool);

            // Fabric and wires from the salvage entry (gun parts weighed 0, tiers ignored), seeds from the add, then the extra stack.
            Assert.Equal([Fabric, Seeds, Wires], contents.Stacks.Take(3).Select(stack => stack.Item.TechnicalName).Order(StringComparer.Ordinal));
            Assert.Equal(4, contents.Stacks.Single(stack => stack.Item.TechnicalName == Seeds).Amount);
            Assert.Equal(Ammo, contents.Stacks[3].Item.TechnicalName);
        }

        // Without a profile the type pool has no extra stack.
        Assert.All(Enumerable.Range(0, Rolls).Select(_ => Containers().Roll(null, RaiderClass, 8, PointOfInterestLootValue.Unknown, random)),
            contents => Assert.DoesNotContain(contents.Stacks, stack => stack.Item.TechnicalName == Ammo));
    }

    [Fact]
    public void Loose_sockets_take_the_profile_filters_adds_and_tier_multipliers()
    {
        Random random = new(4);

        // The supply pool's bandages are gone once a parent profile weighs healing items 0.
        Assert.Contains(LooseDraws(Loose(), SupplyTag, random), stack => stack.Item.TechnicalName == Bandage);
        Assert.DoesNotContain(LooseDraws(Loose(Profile("No healing", "Socket.Loot", weights: [Weight("Item.Drop.Type.Consumable.Healing", 0)])), SupplyTag, random),
            stack => stack.Item.TechnicalName == Bandage);

        // An added item needs no placement and brings its amount; a socket only an add fills can hold something.
        LooseItemRolls seeds = Loose(Profile("Seeds", "socket.loot", only: [Match(Seeds, item: true)], add: [Add(Seeds, 2, item: true)]));
        Assert.All(LooseDraws(seeds, "Socket.Loot.Elsewhere", random), stack => Assert.Equal((Seeds, 2, 1.0), (stack.Item.TechnicalName, stack.Amount, stack.Durability)));
        Assert.True(seeds.CanHold("Socket.Loot.Elsewhere"));
        Assert.False(Loose().CanHold("Socket.Loot.Elsewhere"));

        // A tier multiplier of 0 removes the tier's items.
        Assert.Contains(LooseDraws(Loose(), LooseTag, random), stack => stack.Item.TechnicalName == Gem);
        Assert.DoesNotContain(LooseDraws(Loose(Profile("No rare", LooseTag, tiers: new Dictionary<string, double> { ["Rare"] = 0 })), LooseTag, random),
            stack => stack.Item.TechnicalName == Gem);
    }

    [Fact]
    public void A_profile_on_a_correctly_spelled_tag_covers_the_games_misspelled_medical_sockets()
    {
        // Real data: the game spells some medical socket tags Socket.LootCOntainer.Medical.
        LootContentSettings shipped = ShippedContent.Value.LootContents(TestGameData.Data);
        LootProfile medical = Profile("Medical", "Socket.LootContainer.Medical", only: [Match("DA_Item_AdrenalinShot", item: true)], add: [Add("DA_Item_AdrenalinShot", 1, item: true)]);
        LootContentRolls rolls = new(TestGameData.Items.Definitions, TestGameData.Loot, shipped with { Profiles = [medical] });
        Random random = new(6);

        for (int roll = 0; roll < 50; roll++)
        {
            LootContents contents = rolls.Roll("Socket.LootCOntainer.Medical.RsrCartShelf.Drawer01", "", 8, PointOfInterestLootValue.Unknown, random);
            Assert.Equal("Medical", contents.Pool);
            Assert.All(contents.Stacks, stack => Assert.Equal(("DA_Item_AdrenalinShot", 1), (stack.Item.TechnicalName, stack.Amount)));
        }

        Assert.Equal(["Medical"], rolls.Profile("Socket.LootCOntainer.Medical.RsrCartShelf.Drawer01", "").Chain.Select(profile => profile.Name));
    }

    // Stacks of many seeded rolls of a container tag.
    private static List<LootStack> Draws(LootContentRolls rolls, string tag)
    {
        Random random = new(1);
        return [.. Enumerable.Range(0, Rolls).SelectMany(_ => rolls.Roll(tag, "", 8, PointOfInterestLootValue.Unknown, random).Stacks)];
    }

    private static List<LootStack> LooseDraws(LooseItemRolls rolls, string tag, Random random) =>
        [.. Enumerable.Range(0, Rolls).Select(_ => rolls.Roll(tag, PointOfInterestLootValue.Unknown, random)).OfType<LootStack>()];

    private static LootContentRolls Containers(params LootProfile[] profiles) => Containers([], profiles);

    private static LootContentRolls Containers(string[] excluded, params LootProfile[] profiles) => new(s_data.Items.All, s_data.Loot, Settings(excluded, profiles));

    private static LooseItemRolls Loose(params LootProfile[] profiles) => Loose([], profiles);

    private static LooseItemRolls Loose(string[] excluded, params LootProfile[] profiles) => new(s_data.Items.All, s_data.Loot, Settings(excluded, profiles));

    // Tiers Common (salvage), Rare and Other; one table pool for every container (2 target stacks, no extra stack);
    // a Raider type pool of salvage; a root extra stack of ammo that always comes; loose items from the same table,
    // with a supply pool of bandages.
    private static LootContentSettings Settings(string[] excluded, LootProfile[] profiles) => new(
        excluded,
        [
            new LootTier("Common", Salvage, 1, new Dictionary<PointOfInterestLootValue, double>()),
            new LootTier("Rare", "Item.Drop.Tier.Rare", 1, new Dictionary<PointOfInterestLootValue, double>()),
            new LootTier("Other", null, 1, new Dictionary<PointOfInterestLootValue, double>()),
        ],
        "Common",
        [new LootTargetStacks(2, 1)],
        [new LootTablePool("Synth", "Socket.LootContainer", [Table], null, false)],
        new LootExtraStack(1, [Entry("Item.Drop.Type.Ammo", 5)]),
        [new LootTypePool("Raider", [RaiderClass], [], new LootRange(2, 2), [Entry(Salvage, 1)])],
        profiles,
        new LooseItemSettings([Table], new LootRange(1, 1), [new LooseItemPool("Supplies", [SupplyTag], 1, [Entry("Item.Drop.Type.Consumable.Healing", 2)])]));

    private static GameDataSet Data()
    {
        LootPlacement[] placed = [new(Tag, []), new(LooseTag, [])];
        string[] tabled = [Fabric, Wires, GunParts, Gem];
        return new SyntheticGameData()
            .AddItem(Fabric, item => item with { MaxStack = 20, DropTags = [$"{Salvage}.Fabric"], LootPlacements = placed })
            .AddItem(Wires, item => item with { MaxStack = 20, DropTags = [$"{Salvage}.Wires"], LootPlacements = placed })
            .AddItem(GunParts, item => item with { MaxStack = 20, DropTags = [$"{Salvage}.GunParts"], LootPlacements = placed })
            .AddItem(Gem, item => item with { MaxStack = 20, DropTags = ["Item.Drop.Type.Valuable", "Item.Drop.Tier.Rare"], LootPlacements = placed })
            .AddItem(Seeds, item => item with { MaxStack = 20, DropTags = ["Item.Drop.Type.Nature.Seeds"] })
            .AddItem(Ammo, item => item with { MaxStack = 20, DropTags = ["Item.Drop.Type.Ammo.Light"] })
            .AddItem(Bandage, item => item with { MaxStack = 20, DropTags = ["Item.Drop.Type.Consumable.Healing"] })
            .With(content => content with
            {
                StaticValueLootTables =
                [
                    new StaticValueLootTable(Table, "/Game/Synthetic/" + Table, [.. tabled.Select(name => new StaticValueLootTableEntry(LootItemQueryType.Exact, null, name, -1,
                        new LootRange(1, 1), new LootRange(100, 100), new LootRange(100, 100), LootDistributionMethod.None))]),
                ],
            })
            .Build();
    }

    private static ItemDefinition Item(string name) => s_data.Items.ByTechnicalName[name];

    private static LootTypeEntry Entry(string dropTag, int amount) => new(dropTag, [], new LootRange(amount, amount), 1);

    private static LootMatch Match(string key, bool item = false) => item ? new LootMatch(null, key) : new LootMatch(key, null);

    private static LootProfileWeight Weight(string key, double multiplier, bool item = false) => new(Match(key, item), multiplier);

    private static LootProfileAdd Add(string key, int amount, bool item = false, string? tier = null) => new(Match(key, item), new LootRange(amount, amount), tier);

    private static LootProfile Profile(string name, string? tag = null, string? containerClass = null, LootMatch[]? only = null, LootProfileWeight[]? weights = null,
        LootProfileAdd[]? add = null, Dictionary<string, double>? tiers = null, LootFirstStackTier? first = null, LootRange? stacks = null, LootProfileExtraStack? extra = null) =>
        new(name, tag, containerClass, only, weights ?? [], add ?? [], tiers ?? [], first, stacks, extra);
}
