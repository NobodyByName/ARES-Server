using System.Reflection;
using Ares.Common.Game.Loot;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Common.Tests;

/// <summary>
/// Real-data tier: the loaded <see cref="GameDataSet"/> of the extracted game data offers what the generated
/// Structs classes offer, so that consumers switch to it without a change of behaviour, and every game name resolves to
/// the generated constant it replaces. Skipped without <see cref="RealGameData.DirectoryVariable"/>. The one known difference: the
/// catalogue has <c>DA_Item_Zipline</c>, which the generated data lists as not cooked (see <see cref="ItemDataParityTests"/>).
/// </summary>
public sealed class GameDataRealTests
{
    private static GameDataSet Data => RealGameData.Data;

    private static ResolvedGameNames Names => Data.Names;

    [RealDataFact]
    public void Item_lookups_equal_the_generated_ones()
    {
        ItemCatalog items = Data.Items;

        Assert.Equal(UsableItems.Count, items.Count);
        Assert.Equal([items.ByTechnicalName["DA_Item_Zipline"].AssetId], items.ByAssetId.Keys.Except(UsableItems.ByAssetId.Keys));
        Assert.All(UsableItems.ByAssetId, pair => Assert.Equal(pair.Value.TechnicalName, items.ByAssetId[pair.Key].TechnicalName));
        Assert.Equal(["DA_Item_Zipline"], items.ByTechnicalName.Keys.Except(UsableItems.ByTechnicalName.Keys));
        Assert.All(UsableItems.All, item => Assert.Equal(
            (item.ItemType, item.Stackable, string.Join(',', item.DescriptionTags)),
            (items.All[item.UsableIndex].ItemType, items.All[item.UsableIndex].Stackable, string.Join(',', items.All[item.UsableIndex].DescriptionTags))));
        Assert.Equal(ItemValues.ScrapValues, items.ScrapValues);
        Assert.Equal(WorldItemList.Classes, Data.WorldItems.Classes);
        Assert.Equal((WorldItemList.WordCount, WorldItemList.IndexOf(WorldItemList.Classes[^1])), (Data.WorldItems.WordCount, Data.WorldItems.IndexOf(WorldItemList.Classes[^1])));
        Assert.Null(Data.WorldItems.IndexOf("/Game/Not/A.Class_C"));
    }

    [RealDataFact]
    public void Menu_tables_equal_the_generated_dictionaries()
    {
        MenuItemTables menu = Data.Menu;

        Assert.Equal(CurrencyItems.Definitions.Count, menu.Currency.Definitions.Length);
        Assert.Equal(CurrencyItems.ItemAssetIds, menu.Currency.AssetIds);
        Assert.Equal((GadgetItems.Definitions.Count, ShieldItems.Definitions.Count, WeaponItems.Definitions.Count, AugmentItems.Definitions.Count),
            (menu.Gadgets.Items.Length, menu.Shields.Definitions.Length, menu.Weapons.Definitions.Length, menu.Augments.Definitions.Length));
        Assert.Equal(SalvageItems.Definitions.Count, menu.Salvage.Definitions.Length);
        Assert.Equal(SalvageItems.ItemAssetIds, menu.Salvage.ItemAssetIds);
        Assert.Equal(SalvageItems.UsableIndices, menu.Salvage.UsableIndices);
        Assert.Equal(SalvageItems.ItemWeights, menu.Salvage.ItemWeights);
        Assert.Equal(AugmentItems.ItemAssetIds, menu.Augments.ItemAssetIds);
        Assert.Equal(AugmentItems.UsableIndices, menu.Augments.UsableIndices);
        Assert.Equal(AugmentItems.ItemWeights, menu.Augments.ItemWeights);
        Assert.Equal(AugmentItems.FrameAssetIds, menu.Augments.FrameAssetIds);
        Assert.Equal(AugmentItems.MaxWeights, menu.Augments.MaxWeights);
        Assert.Equal(AugmentItems.BranchAssetIds, menu.Augments.BranchAssetIds);
    }

    [RealDataFact]
    public void Item_names_resolve_to_the_generated_constants()
    {
        Assert.Equal((HandItems.UnarmedItemClass, HandItems.UnarmedUsableIndex, HandItems.UnarmedEquipSeconds),
            (Names.Unarmed.ItemClass, Names.Unarmed.UsableIndex, Names.Unarmed.EquipSeconds));
        Assert.Equal((HandItems.RaiderToolItemClass, HandItems.RaiderToolUsableIndex, HandItems.RaiderToolEquipSeconds),
            (Names.RaiderTool.ItemClass, Names.RaiderTool.UsableIndex, Names.RaiderTool.EquipSeconds));
        Assert.Equal((WeaponItems.FerroIAssetId, WeaponItems.FerroIUsableIndex, WeaponItems.FerroIWeight, FerroDefaults.ItemClass),
            (Names.FerroI.AssetId, Names.FerroI.UsableIndex, Names.FerroI.Weight, Names.FerroI.ItemClass));
        Assert.Equal(((double)FerroDefaults.ClipSize, FerroDefaults.FireRate, FerroDefaults.ModSlotCount),
            (Names.FerroIFirearm.ClipSize, Names.FerroIFirearm.FireRate, Names.FerroIFirearm.ModSlots.Count));
        Assert.Equal((WeaponItems.HeavyAmmoAssetId, WeaponItems.HeavyAmmoUsableIndex), (Names.HeavyAmmo.AssetId, Names.HeavyAmmo.UsableIndex));
        Assert.Equal((GadgetItems.SnapHookAssetId, GadgetItems.SnapHookUsableIndex, GadgetItems.SnapHookItemClass),
            (Names.SnapHook.AssetId, Names.SnapHook.UsableIndex, Names.SnapHook.ItemClass));
        Assert.Equal((ShieldItems.LightShieldAssetId, ShieldItems.LightShieldUsableIndex, ShieldDefaults.HealthPerPlate, ShieldDefaults.ModSlotCount),
            (Names.LightShield.AssetId, Names.LightShield.UsableIndex, Names.LightShieldClass.ArmorPerPlate, Names.LightShieldClass.ModSlotCount));
        Assert.Equal(CurrencyItems.CoinsAssetId, Names.Coins.AssetId);
        Assert.Equal((AugmentItems.CombatMk1AssetId, AugmentItems.LootingMk1AssetId, AugmentItems.TacticalMk1AssetId),
            (Names.CombatMk1.AssetId, Names.LootingMk1.AssetId, Names.TacticalMk1.AssetId));
        Assert.Equal((WeaponItems.MuzzleModSlotAssetId, WeaponItems.UnderBarrelModSlotAssetId, WeaponItems.StockModSlotAssetId),
            (Names.MuzzleModSlotAssetId, Names.UnderBarrelModSlotAssetId, Names.StockModSlotAssetId));
        Assert.Equal((AugmentItems.AugmentSlotAssetId, AugmentItems.AugmentContainerAssetId, AugmentItems.AugmentContentSlotAssetId),
            (Names.AugmentSlotAssetId, Names.AugmentContainerAssetId, Names.AugmentContentSlotAssetId));
        Assert.NotEmpty(Names.StandardLoadoutFrame.Containers.Select(container => container.ContainerSlotAssetId).Intersect(AugmentItems.BranchAssetIds.Values));
        Assert.Equal(Data.Items.FirearmsByClass[FerroDefaults.ItemClass], Names.FerroIFirearm);
        Assert.Equal(Data.Items.ShieldsByClass[Names.LightShield.ItemClass], Names.LightShieldClass);
        Assert.Equal(Data.Items.AugmentsByTechnicalName[Names.CombatMk1.TechnicalName].FrameAssetId,
            Data.Items.LoadoutFramesByAssetId[AugmentItems.FrameAssetIds[AugmentItems.CombatMk1AssetId]].AssetId);
    }

    [RealDataFact]
    public void Graph_tags_resolve_to_the_generated_bits()
    {
        Dictionary<string, (int Word, ulong Mask)> generated = new(StringComparer.Ordinal);
        foreach ((Type type, int word) in new[] { (typeof(GraphStateTags), 0), (typeof(GraphStateUpperTags), 1) })
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Static).Where(field => field.FieldType == typeof(ulong)))
            {
                generated[field.Name] = (word, (ulong)field.GetRawConstantValue()!);
            }
        }

        PropertyInfo[] tags = typeof(GraphTagBits).GetProperties();

        Assert.Equal(42, tags.Length);
        Assert.All(tags, property =>
        {
            GraphTagBit bit = (GraphTagBit)property.GetValue(Names.GraphTags)!;
            Assert.Equal(generated[property.Name], (bit.Word, bit.Mask));
            Assert.Equal(Data.Movement.GraphStateTags[bit.Index], bit.Tag);
        });
        Assert.Equal(GraphStateTags.Count, Data.Movement.GraphStateTags.Length);
    }

    [RealDataFact]
    public void Dam_map_lookups_equal_the_generated_ones()
    {
        MapData dam = Data.Map(DamMap.WorldName);

        Assert.Same(dam, Names.Dam);
        Assert.Same(dam, Data.FindMap(DamMap.WorldName));
        Assert.Equal((DamMap.MapPackage, DamMap.PersistentLevelName, DamMap.GameModeClass, DamMap.LocationTag),
            (dam.MapPackage, dam.World.PersistentLevelName, dam.GameMode.GameModeClass, dam.GameMode.LocationTag));
        Assert.Equal((DamLootSockets.All.Count, DamLootSockets.Meshes.Count, DamPlacedLoot.Containers.Count, DamPlayerStarts.All.Count, DamPlayerStarts.Capsule),
            (dam.LootSockets.Length, dam.LootSocketMeshes.Length, dam.PlacedLoot.Length, dam.PlayerStarts.Length, dam.PlayerStartCapsule));
        Assert.Equal(DamLadders.All, dam.Ladders);
        Assert.Equal(DamLadders.Logic, dam.LadderLogic);
        Assert.Equal(DamZiplines.Launchers, dam.ZiplineLaunchers);
        Assert.Equal((DamZiplines.Lines.Count, DamZiplines.CharacterMeshYaw), (dam.ZiplineLines.Length, dam.ZiplineDefaults.CharacterMeshYaw));
        Assert.Equal(DamDoors.All, dam.Doors);
        Assert.Equal((DamRounds.All.Count, DamLighting.Scenarios.Count), (dam.Rounds.Length, dam.LightingScenarios.Length));
        Assert.Equal(DamLighting.RoundLightingGroups.Keys, dam.RoundLightingGroups.Keys);
        Assert.Equal(DamLighting.RoundLightingGroups.Values, dam.RoundLightingGroups.Values);
        Assert.Equal(DamExtraction.Buttons, dam.ExtractionButtons);
        Assert.Equal(DamExtraction.All, dam.ExtractionPoints);
        Assert.Equal((DamExtraction.DoorsSeconds, DamExtraction.Settings), (dam.ExtractionElevator.DoorsSeconds, dam.ExtractionElevator.Settings));
        Assert.Equal(DamStreamingCells.Packages, dam.StreamingCells);
        Assert.Equal((DamPointsOfInterest.All.Count, DamLootLimiters.All.Count), (dam.PointsOfInterest.Length, dam.LootLimiters.Length));
        Assert.Equal(DamDynamicLoot.Groups.Select(group => (group.Id, group.Spawners.Count)), dam.DynamicLootGroups.Select(group => (group.Id, group.Spawners.Count)));
        Assert.All(dam.DynamicLootGroups.SelectMany(group => group.Spawners), spawner => Assert.Contains(spawner, dam.DynamicLootSpawners));
    }

    [RealDataFact]
    public void Loot_and_movement_lookups_equal_the_generated_ones()
    {
        LootData loot = Data.Loot;
        MovementTuning movement = Data.Movement;

        Assert.All(LootContainerTypes.All, type => Assert.Equal(type.Tag, loot.FindContainerType(type.Tag.ToUpperInvariant())?.Tag));
        Assert.Equal(LootContainerClasses.All.Select(type => type.Container), loot.ContainerClasses.Where(type => LootContainerSupport.IsSupported(type.Container.Class)).Select(type => type.Container));
        Assert.All(LootContainerClasses.All, type => Assert.Equal(type.Container, loot.FindContainerClass(type.Container.Class)?.Container));
        Assert.Equal(LootContainerClasses.NetCullDistance, loot.ContainerDefaults.NetCullDistance);
        Assert.Equal(StaticValueLootTables.ContainerTargetStacks, loot.ContainerDefaults.TargetStacks);
        Assert.All(StaticValueLootTables.All, table => Assert.Equal(StaticValueLootTables.Find(table.Name)?.Path, loot.FindStaticValueTable(table.Name)?.Path));
        Assert.Equal((SkillLootTables.All.Count, StaticLootSpawnTweakables.NumItemsWeight), (loot.SkillTables.Length, loot.SpawnTweakables.NumItemsWeight));

        Assert.Equal((DodgeRollSequences.Ninja.Distance, VaultDefinitions.UnarmedExhausted.GlobalPlayRate), (movement.DodgeRoll(DodgeRollKind.Ninja).Distance, movement.UnarmedExhaustedVaults.GlobalPlayRate));
        Assert.Equal((HeavyLandingAnimations.Heavy.Count, HeavyLandingAnimations.Recovery.Count), (movement.HeavyLandingLevels.Length, movement.RecoveryLandingLevels.Length));
        Assert.Equal((SnapHookItem.Range, SnapHookItem.ActivationCost), (movement.SnapHookItem.Range, movement.SnapHookItem.ActivationCost));
        Assert.Equal(SprintMovementParameters.BurstCurve, movement.SprintBurstCurve);
        Assert.Equal(InteractionAnimationContainers.DoorBreach, Names.DoorBreachAnimation);
        Assert.All(InteractionAnimationContainers.All, container => Assert.Equal(container, movement.FindInteractionAnimation(container.Path[..container.Path.LastIndexOf('.')])));
        Assert.Equal(InteractionAnimationContainers.TagBased.Select(entry => entry.Container), movement.TaggedInteractionAnimationContainers.Select(entry => entry.Container));
    }
}
