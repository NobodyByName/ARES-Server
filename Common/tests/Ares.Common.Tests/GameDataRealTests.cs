using System.Reflection;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Common.Tests;

/// <summary>
/// Real-data tier: the lookups of the loaded <see cref="GameDataSet"/> over the extracted game data, and the protocol
/// constants of Structs that name game data agree with it. Skipped without <see cref="RealGameData.DirectoryVariable"/>.
/// </summary>
public sealed class GameDataRealTests
{
    private static GameDataSet Data => RealGameData.Data;

    private static ResolvedGameNames Names => Data.Names;

    [RealDataFact]
    public void Item_lookups_cover_every_quality_level_and_the_world_item_list()
    {
        ItemCatalog items = Data.Items;

        Assert.Equal(items.All.Sum(item => Math.Max(item.QualityAssetIds.Count, 1)), items.ByAssetId.Count);
        Assert.All(items.All, item => Assert.Same(item, items.ByTechnicalName[item.TechnicalName]));
        Assert.Equal([.. items.All.Select(item => item.UsableIndex).Order()], items.All.Select(item => item.UsableIndex));
        Assert.Equal(Data.WorldItems.Classes.Length - 1, Data.WorldItems.IndexOf(Data.WorldItems.Classes[^1]));
        Assert.Null(Data.WorldItems.IndexOf("/Game/Not/A.Class_C"));
    }

    [RealDataFact]
    public void Item_names_resolve_to_their_classes()
    {
        Assert.Equal(Data.Items.FirearmsByClass[Names.FerroI.ItemClass], Names.FerroIFirearm);
        Assert.Equal(Data.Items.ShieldsByClass[Names.LightShield.ItemClass], Names.LightShieldClass);
        Assert.Equal(Data.Items.AugmentsByTechnicalName[Names.CombatMk1.TechnicalName].FrameAssetId, Data.Menu.Augments.FrameAssetIds[Names.CombatMk1.AssetId]);
        Assert.NotEmpty(Names.StandardLoadoutFrame.Containers.Select(container => container.ContainerSlotAssetId).Intersect(Data.Menu.Augments.BranchAssetIds.Values));
        Assert.All(ItemTechnicalNames.All, name => Assert.Contains(name, Data.Items.ByTechnicalName.Keys));
        Assert.All(ItemTechnicalNames.DataAssets, pair => Assert.Equal(pair.Value, Data.Items.ByTechnicalName[pair.Key].DataAsset));
        Assert.Equal(InventoryBlackboard.RubberDuckIndex, Data.Items.ByTechnicalName[ItemTechnicalNames.RubberDuck].UsableIndex);
    }

    [RealDataFact]
    public void Every_graph_tag_name_resolves_to_a_distinct_bit()
    {
        GraphTagBit[] bits = [.. typeof(GraphTagBits).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => (GraphTagBit)property.GetValue(Names.GraphTags)!)];

        Assert.NotEmpty(bits);
        Assert.Equal(bits.Length, bits.Select(bit => bit.Index).Distinct().Count());
        Assert.All(bits, bit => Assert.Equal(Data.Movement.GraphStateTags[bit.Index], bit.Tag));
    }

    [RealDataFact]
    public void The_dam_agrees_with_its_protocol_constants()
    {
        MapData dam = Data.Map(GameNames.Maps.Dam);

        Assert.Same(dam, Names.Dam);
        Assert.Same(dam, Data.FindMap(GameNames.Maps.Dam));
        Assert.All(dam.DynamicLootGroups.SelectMany(group => group.Spawners), spawner => Assert.Contains(spawner, dam.DynamicLootSpawners));
        Assert.Equal(dam.Rounds.Select(round => round.Package), RoundConfigurationAssets.Dam);
        Assert.Equal(dam.ExtractionButtons.Select(button => (button.Component, button.Role)), ExtractionClasses.Buttons);
        Assert.All(dam.LightingScenarios, scenario => Assert.Single(DamDataLayers.All, layer => layer.Asset.Split('.')[0] == scenario.DataLayerPackage));
        Assert.All(dam.RoundLightingGroups.Values.SelectMany(group => group), choice => Assert.Contains(dam.LightingScenarios, scenario => scenario.Tag == choice.Tag));
        Assert.All(dam.Doors, door => Assert.Contains(door.Cell, dam.StreamingCells));
        Assert.Contains(dam.PlayerStarts, start => start.Tags.Contains(DamProtocol.GameModeTag));
    }

    [RealDataFact]
    public void Loot_lookups_ignore_case_and_find_every_table()
    {
        LootData loot = Data.Loot;

        Assert.All(loot.ContainerTypes, type => Assert.Equal(type.Tag, loot.FindContainerType(type.Tag.ToUpperInvariant())?.Tag));
        Assert.All(loot.ContainerClasses, type => Assert.Same(type, loot.FindContainerClass(type.Container.Class)));
        Assert.All(loot.StaticValueTables.GroupBy(table => table.Name, StringComparer.OrdinalIgnoreCase),
            tables => Assert.Equal(tables.Count() == 1 ? tables.Single().Path : null, loot.FindStaticValueTable(tables.Key)?.Path));
    }
}
