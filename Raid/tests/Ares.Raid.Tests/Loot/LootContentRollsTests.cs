using Ares.Raid.World.Loot;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.Loot;

public sealed class LootContentRollsTests
{
    private const string ServerCabinet = "Socket.LootContainer.Tech.Server.Door";
    private const string WeaponCaseHigh = "/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C";
    private const string MedicalBag = "/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C";

    [Fact]
    public void An_item_goes_only_where_its_placement_list_accepts_the_container_tag()
    {
        ItemDefinition shot = Item("DA_Item_AdrenalinShot");
        ItemDefinition sensors = Item("DA_Item_Salvage_Sensors");

        // The placement tag itself or any tag below it, compared without case (the game misspells one medical tag).
        Assert.True(LootContentRolls.MatchesContainerTag(shot, "Socket.LootContainer.Commercial.GvtDesk.Drawer"));
        Assert.True(LootContentRolls.MatchesContainerTag(shot, "Socket.LootCOntainer.Medical.RsrCartShelf.Drawer01"));
        Assert.True(LootContentRolls.MatchesContainerTag(sensors, ServerCabinet));

        // Not below an excluded tag, not a sibling whose name merely starts the same, not a loose-item socket placement.
        Assert.False(LootContentRolls.MatchesContainerTag(shot, "Socket.LootContainer.Commercial.CashRegister"));
        Assert.False(LootContentRolls.MatchesContainerTag(sensors, "Socket.LootContainer.Tech.Server.DoorFrame"));
        Assert.False(LootContentRolls.MatchesContainerTag(sensors, "Socket.LootContainer.Security.Locker01"));
        Assert.False(LootContentRolls.MatchesContainerTag(shot, ServerCabinet));
        Assert.False(LootContentRolls.MatchesContainerTag(Item("DA_Item_HeavyAmmo"), ServerCabinet));
    }

    [Fact]
    public void Candidates_are_plain_items_the_pool_selects_and_the_container_accepts()
    {
        HashSet<ItemDefinition> plain = [.. TestGameData.Items.Definitions.PlainItems];
        foreach (LootContainerType type in TestGameData.Loot.ContainerTypes)
        {
            IReadOnlyList<ItemDefinition> candidates = TestGameData.Raid.LootRolls.Candidates(type.Tag);
            Assert.All(candidates, item =>
            {
                Assert.Contains(item, plain);
                Assert.True(LootContentRolls.MatchesContainerTag(item, type.Tag));
                Assert.DoesNotContain(item.DropTags, tag => tag.StartsWith("Item.Drop.Category.Map.", StringComparison.Ordinal));
            });
            Assert.Equal(candidates.Count, candidates.Distinct().Count());
        }

        // Typical server cabinet items are candidates there; the Mushroom socket holds only Mushroom.
        HashSet<string> cabinetItems = ["DA_Item_Salvage_Sensors", "DA_Item_Salvage_Processor", "DA_Item_Salvage_FriedMotherboard", "DA_Item_Salvage_CoolingFan", "DA_Item_Salvage_Plastic"];
        Assert.Superset(cabinetItems, new HashSet<string>(TestGameData.Raid.LootRolls.Candidates(ServerCabinet).Select(item => item.TechnicalName)));
        Assert.Equal(["DA_Item_Consumable_Mushroom"], TestGameData.Raid.LootRolls.Candidates("Socket.LootContainer.Nature.Mushroom").Select(item => item.TechnicalName));
        Assert.Empty(TestGameData.Raid.LootRolls.Candidates("Socket.LootContainer.Nature.Resin"));
        Assert.Empty(TestGameData.Raid.LootRolls.Candidates("Socket.LootContainer.ARC.Wreck02"));
        Assert.Equal("Medical", TestGameData.Raid.LootRolls.TablePool("Socket.LootCOntainer.Medical.RsrCartShelf.Drawer01")?.Name);
    }

    [Fact]
    public void A_seeded_roll_is_repeatable_and_follows_the_stack_rules()
    {
        Assert.Equal(Rolls(ServerCabinet, 7, 50), Rolls(ServerCabinet, 7, 50));
        Random random = new(11);
        foreach (string tag in (string[])[ServerCabinet, "Socket.LootContainer.Commercial.GvtDesk.Drawer", "Socket.LootContainer.Industrial.Lockers.Door",
                     "Socket.LootCOntainer.Medical.RsrCartShelf.Drawer01", "Socket.LootContainer.Mechanical.Car01.Trunk", "Socket.LootContainer.Residential.Closet.Door"])
        {
            IReadOnlyList<ItemDefinition> candidates = TestGameData.Raid.LootRolls.Candidates(tag);
            for (int roll = 0; roll < 200; roll++)
            {
                LootContents contents = TestGameData.Raid.LootRolls.Roll(tag, "", 8, PointOfInterestLootValue.Unknown, random);
                // Two or three drawn stacks (fewer when avoid groups leave no item), and at most one extra stack.
                List<LootStack> salvage = [.. contents.Stacks.Where(stack => candidates.Contains(stack.Item))];
                Assert.InRange(contents.Stacks.Count, 1, 4);
                Assert.InRange(contents.Stacks.Count - salvage.Count, 0, 1);
                Assert.Equal(contents.Stacks.Count, contents.Stacks.Select(stack => stack.Item).Distinct().Count());
                if (candidates.Any(item => LootContentRolls.TierOf(item) == LootTier.Common))
                {
                    Assert.Contains(salvage, stack => LootContentRolls.TierOf(stack.Item) == LootTier.Common);
                }

                Assert.All(contents.Stacks, stack =>
                {
                    Assert.InRange(stack.Amount, 1, stack.Item.MaxStack);
                    Assert.Equal(1, stack.Durability);
                    Assert.DoesNotContain(contents.Stacks, other => other != stack && stack.Item.LootAvoidGroups
                        .Any(group => other.Item.DropTags.Any(drop => LootContentRolls.IsTagOrBelow(drop, group))));
                });
            }
        }

        // A container never holds more stacks than slots.
        Assert.Single(TestGameData.Raid.LootRolls.Roll(ServerCabinet, "", 1, PointOfInterestLootValue.Unknown, new Random(3)).Stacks);
    }

    [Fact]
    public void Areas_with_a_higher_loot_value_hold_more_rare_items_and_some_containers_an_extra_stack()
    {
        Random random = new(5);
        Assert.True(Rare(PointOfInterestLootValue.High, random) > Rare(PointOfInterestLootValue.Unknown, random) * 1.2);

        // The server cabinet's pool has no ammunition or healing items: those come from the extra stack.
        int extras = Enumerable.Range(0, 2000).Count(_ => TestGameData.Raid.LootRolls.Roll(ServerCabinet, "", 8, PointOfInterestLootValue.Unknown, random).Stacks
            .Any(stack => stack.Item.DropTags.Any(tag => tag.StartsWith("Item.Drop.Type.Ammo", StringComparison.Ordinal) || tag.StartsWith("Item.Drop.Type.Consumable.Healing", StringComparison.Ordinal))));
        Assert.InRange(extras, 200, 400);
    }

    [Fact]
    public void Nature_raider_and_unfilled_containers_follow_their_own_rules()
    {
        Random random = new(9);
        LootStack mushroom = Assert.Single(TestGameData.Raid.LootRolls.Roll("Socket.LootContainer.Nature.Mushroom", "", 8, PointOfInterestLootValue.High, random).Stacks);
        Assert.Equal(("DA_Item_Consumable_Mushroom", 1), (mushroom.Item.TechnicalName, mushroom.Amount));
        Assert.Equal(new LootContents("Nature", []), TestGameData.Raid.LootRolls.Roll("Socket.LootContainer.Nature.Resin", "", 8, PointOfInterestLootValue.Unknown, random));
        Assert.Equal(new LootContents("ARC", []), TestGameData.Raid.LootRolls.Roll("Socket.LootContainer.ARC.Wreck02", "", 8, PointOfInterestLootValue.Unknown, random));
        Assert.Equal(new LootContents(null, []), TestGameData.Raid.LootRolls.Roll(null, "/Game/Unknown.Unknown_C", 8, PointOfInterestLootValue.Unknown, random));

        for (int roll = 0; roll < 100; roll++)
        {
            LootContents weapons = TestGameData.Raid.LootRolls.Roll(null, WeaponCaseHigh, 8, PointOfInterestLootValue.Unknown, random);
            Assert.Equal("RaiderWeaponsHigh", weapons.Pool);
            Assert.InRange(weapons.Stacks.Count, 1, 3);
            Assert.All(weapons.Stacks, stack => Assert.True(stack.Item.Category == ItemCategory.Modification
                ? !stack.Item.DropTags.Contains("Item.Drop.Tier.Low")
                : stack.Item.DropTags.Any(tag => tag.StartsWith("Item.Drop.Type.Ammo.", StringComparison.Ordinal)) && stack.Amount >= Math.Min(10, stack.Item.MaxStack)));
            LootContents medical = TestGameData.Raid.LootRolls.Roll(null, MedicalBag, 8, PointOfInterestLootValue.Unknown, random);
            Assert.All(medical.Stacks, stack => Assert.Contains(stack.Item.DropTags, tag => tag.StartsWith("Item.Drop.Type.Consumable.Healing", StringComparison.Ordinal)));
            Assert.Equal(medical.Stacks.Count, medical.Stacks.Select(stack => stack.Item).Distinct().Count());
        }

        // Weapon lockers on sockets use the Raider weapon pool.
        Assert.Equal("RaiderWeapons", LootContentRolls.TypePool("Socket.LootContainer.Military.WeaponLocker.DoorLeft", "")?.Name);
    }

    // Rare stacks in 2000 server cabinets of an area's loot value.
    private static int Rare(PointOfInterestLootValue value, Random random) => Enumerable.Range(0, 2000)
        .Sum(_ => TestGameData.Raid.LootRolls.Roll(ServerCabinet, "", 8, value, random).Stacks.Count(stack => LootContentRolls.TierOf(stack.Item) == LootTier.Rare));

    private static ItemDefinition Item(string name) => TestGameData.Items.Definitions.CarriedItems.Single(item => item.TechnicalName == name);

    private static List<(string Item, int Amount)> Rolls(string tag, int seed, int count)
    {
        Random random = new(seed);
        return [.. Enumerable.Range(0, count).SelectMany(_ => TestGameData.Raid.LootRolls.Roll(tag, "", 8, PointOfInterestLootValue.Unknown, random).Stacks)
            .Select(stack => (stack.Item.TechnicalName, stack.Amount))];
    }
}
