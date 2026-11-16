using Ares.Structs.Net.GameplayTags;

namespace Ares.Structs.Game.Loot;

/// <summary>The game's skill loot tables (<c>UPlayerSkillLootTableDataAsset</c>).</summary>
public static class SkillLootTables
{
    /// <summary>Every skill loot table, ordered by asset path.</summary>
    public static IReadOnlyList<SkillLootTable> All { get; } =
    [
        new(
            "DA_PlayerSkillLootTable_RaiderFieldCraft",
            "/Game/Pioneer/LootTables/SkillLootTables/DA_PlayerSkillLootTable_RaiderFieldCraft",
            [
                new SkillLoot(
                    "CharacterSkillData.FieldCraftFind",
                    [
                        new SkillLootGroup(
                            new GameplayTagQuery(["Container.Raider"], [0, 1, 2, 1, 0]),
                            [
                                new SkillLootItem(0.14285714285714285, "DA_Item_LaserTrap_DoTGrenade", new LootRange(1, 1), 0),
                                new SkillLootItem(0.14285714285714285, "DA_Item_LaserTrap_StaminaDoT", new LootRange(1, 1), 0),
                                new SkillLootItem(0.14285714285714285, "DA_Item_LaserTrap_SmokeGrenade", new LootRange(1, 1), 0),
                                new SkillLootItem(0.14285714285714285, "DA_Item_LaserTrap_ThrowableDecoy", new LootRange(1, 1), 0),
                                new SkillLootItem(0.14285714285714285, "DA_Item_SprayCan_Fire", new LootRange(1, 1), 0),
                                new SkillLootItem(0.14285714285714285, "DA_Item_Deployable_SoundTrap", new LootRange(1, 1), 0),
                                new SkillLootItem(0.14285714285714285, "DA_Item_HealingHoT_Improvised", new LootRange(1, 1), 0),
                            ]),
                    ]),
            ]),
    ];
}
