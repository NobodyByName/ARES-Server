using Ares.Structs.Game.Loot;

namespace Ares.Structs.Tests.Game;

public sealed class LootTableDataTests
{
    [Fact]
    public void Every_entry_is_usable()
    {
        Assert.Equal(71, StaticValueLootTables.All.Count);
        Assert.All(StaticValueLootTables.All.SelectMany(table => table.Entries), entry =>
        {
            Assert.True(entry.StackSize.Min >= 1 && entry.StackSize.Min <= entry.StackSize.Max);
            Assert.True(entry.Durability.Min <= entry.Durability.Max && entry.DurabilityCap.Min <= entry.DurabilityCap.Max);
            Assert.InRange(entry.QualityLevel, -1, 255);
            Assert.NotEqual(LootDistributionMethod.None, entry.Distribution);
            if (entry.QueryType == LootItemQueryType.Exact)
            {
                Assert.StartsWith("DA_Item_", entry.ExactItem, StringComparison.Ordinal);
            }
            else
            {
                Assert.NotNull(entry.ItemQuery?.Parse());
            }
        });
    }

    [Fact]
    public void Queries_select_items_by_drop_tags()
    {
        StaticValueLootTableEntry uncommon = StaticValueLootTables.Find("StaticValueLootTable_ElectricalAll")!.Entries[0];
        string[] battery = ["Item.Drop.Category.Area.Electrical", "Item.Drop.Category.Area.Technological", "Item.Drop.Type.Salvage.Material.Uncommon"];
        Assert.True(uncommon.Selects("DA_Item_Salvage_Battery", battery));
        Assert.False(uncommon.Selects("DA_Item_Salvage_Battery", [.. battery, "Item.Drop.Category.Map.FrozenTrail"]));
        Assert.False(uncommon.Selects("DA_Item_Salvage_Sensors", ["Item.Drop.Category.Area.Technological", "Item.Drop.Type.Salvage.Material.Rare"]));
        Assert.Equal(new LootRange(2, 5), uncommon.StackSize);
        Assert.Equal(LootDistributionMethod.MixAllPlacements, uncommon.Distribution);

        StaticValueLootTableEntry weapons = StaticValueLootTables.Find("StaticValueLootTable_BuriedTresureSingleItems")!.Entries[1];
        Assert.True(weapons.Selects("DA_Item_Any", ["Item.Drop.Rarity.01Common", "Item.Drop.Type.Equipment.Weapon.Firearm.LightAmmo"]));
        Assert.Equal(new LootRange(20, 60), weapons.Durability);
    }

    [Fact]
    public void Exact_entries_select_their_item_only()
    {
        StaticValueLootTableEntry paint = StaticValueLootTables.Find("StaticValueLootTable_GlobalOutpostMaterials")!.Entries[0];
        Assert.Equal(LootItemQueryType.Exact, paint.QueryType);
        Assert.True(paint.Selects("da_item_salvage_paint", []));
        Assert.False(paint.Selects("DA_Item_Salvage_Planks", ["Item.Drop.Type.Salvage.Material.OutpostMaterial"]));
    }

    [Fact]
    public void Same_named_tables_need_their_path()
    {
        Assert.Null(StaticValueLootTables.Find("StaticValueLootTable_GlobalRecyclables"));
        Assert.Equal(2, StaticValueLootTables.All.Count(table => table.Name == "StaticValueLootTable_GlobalRecyclables"));
    }

    [Fact]
    public void Containers_target_two_or_three_stacks()
    {
        Assert.Equal([new LootTargetStacks(2, 2.0), new LootTargetStacks(3, 3.0)], StaticValueLootTables.ContainerTargetStacks);
    }

    [Fact]
    public void Field_craft_skill_finds_one_item_in_raider_containers()
    {
        SkillLoot skill = Assert.Single(Assert.Single(SkillLootTables.All).Skills);
        Assert.Equal("CharacterSkillData.FieldCraftFind", skill.Skill);
        SkillLootGroup group = Assert.Single(skill.Groups);
        Assert.True(group.ContainerQuery!.Matches(["Container.Raider"]));
        Assert.False(group.ContainerQuery.Matches(["Container.Other"]));
        Assert.Equal(7, group.Items.Count);
        Assert.Equal(1.0, group.Items.Sum(item => item.Weight), 9);
        Assert.All(group.Items, item => Assert.NotNull(item.Item));
    }
}
