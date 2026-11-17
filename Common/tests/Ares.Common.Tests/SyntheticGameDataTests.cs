using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Structs.Game;

namespace Ares.Common.Tests;

/// <summary>
/// The synthetic game data of logic tests resolves every game name and offers a one-of-each map, and
/// <see cref="GameData.GameData.Create"/> refuses data that lacks a game name or contradicts itself.
/// </summary>
public sealed class SyntheticGameDataTests
{
    [Fact]
    public void The_default_data_resolves_every_game_name()
    {
        GameData.GameData data = SyntheticGameData.Create();

        Assert.Null(data.Source);
        Assert.Equal(GameNames.Items.FerroI, data.Names.FerroI.TechnicalName);
        Assert.Same(data.Items.FirearmsByClass[data.Names.FerroI.ItemClass], data.Names.FerroIFirearm);
        Assert.Equal(data.Menu.Weapons.AssetIds[GameNames.ItemStructures.StockModSlot], data.Names.StockModSlotAssetId);
        Assert.Equal(GameNames.InteractionAnimations.DoorBreach, data.Names.DoorBreachAnimation.Path);
        Assert.Equal(data.Movement.FindGraphTag(GameNames.GraphTags.StatusExhausted), data.Names.GraphTags.StatusExhausted);
        Assert.Equal(data.Names.StandardLoadoutFrame.Containers.Select(container => container.ContainerSlotAssetId),
            data.Menu.Augments.BranchAssetIds.Where(pair => pair.Key / 10 == data.Menu.Augments.FrameAssetIds[data.Names.CombatMk1.AssetId]).Select(pair => pair.Value));
    }

    [Fact]
    public void The_default_map_has_one_of_each_placement()
    {
        MapData dam = SyntheticGameData.Create().Map(GameNames.Maps.Dam);

        Assert.Equal((1, 1, 1, 1, 1), (dam.Doors.Length, dam.Ladders.Length, dam.LootSockets.Length, dam.PlacedLoot.Length, dam.PlayerStarts.Length));
        Assert.Equal("/Game/Synthetic/Maps/TheDam_02_P", dam.MapPackage);
        Assert.NotNull(dam.LadderLogic);
        Assert.Same(dam.LootSocketMeshes[dam.LootSockets[0].Mesh], dam.LootSocketMeshes.Single());
    }

    [Fact]
    public void Items_get_usable_indices_in_name_order_and_tests_set_the_values_they_need()
    {
        GameData.GameData data = new SyntheticGameData()
            .AddItem("DA_Item_Salvage_Rubber", item => item with { MaxStack = 50, Weight = 0.25 })
            .AddItem(GameNames.Items.HeavyAmmo, item => item with { MaxStack = 100 })
            .Build();

        Assert.Equal(data.Items.All.Select(item => item.TechnicalName).Order(StringComparer.OrdinalIgnoreCase), data.Items.All.Select(item => item.TechnicalName));
        Assert.Equal((50, 0.25, true), (data.Items.ByTechnicalName["DA_Item_Salvage_Rubber"].MaxStack, data.Items.ByTechnicalName["DA_Item_Salvage_Rubber"].Weight,
            data.Items.ByTechnicalName["DA_Item_Salvage_Rubber"].Stackable));
        Assert.Equal(100, data.Names.HeavyAmmo.MaxStack);
        Assert.Same(data.Names.HeavyAmmo, data.Items.ByAssetId[data.Names.HeavyAmmo.AssetId]);
    }

    [Fact]
    public void Missing_game_names_are_all_named_in_one_error()
    {
        SyntheticGameData data = new SyntheticGameData()
            .With(content => content with
            {
                UsableItems = [.. content.UsableItems.Where(item => item.TechnicalName != GameNames.Items.FerroI).Select((item, index) => item with { UsableIndex = index })],
                GraphStateTags = [.. content.GraphStateTags.Where(tag => tag != GameNames.GraphTags.AbilityCrouch)],
            });

        string message = Assert.Throws<GameDataException>(data.Build).Message;

        Assert.Contains($"usable item '{GameNames.Items.FerroI}' (GameNames.Items.FerroI) is missing", message, StringComparison.Ordinal);
        Assert.Contains($"graph state tag '{GameNames.GraphTags.AbilityCrouch}' (GameNames.GraphTags.AbilityCrouch) is missing", message, StringComparison.Ordinal);
        Assert.DoesNotContain("class of", message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_ambiguous_game_name_is_named()
    {
        SyntheticGameData data = new SyntheticGameData()
            .With(content => content with { InteractionAnimationContainers = [.. content.InteractionAnimationContainers, content.InteractionAnimationContainers[0] with { CrouchHeight = 1 }] });

        string message = Assert.Throws<GameDataException>(data.Build).Message;

        Assert.Contains($"'{GameNames.InteractionAnimations.DoorBreach}' (GameNames.InteractionAnimations.DoorBreach) is ambiguous", message, StringComparison.Ordinal);
    }

    [Fact]
    public void Contradicting_tables_are_refused()
    {
        Assert.Contains("names mesh 3 of 1", Assert.Throws<GameDataException>(new SyntheticGameData()
            .WithMap(map => map with { LootSockets = [map.LootSockets[0] with { Mesh = 3 }] }).Build).Message, StringComparison.Ordinal);
        Assert.Contains("ladder logic", Assert.Throws<GameDataException>(new SyntheticGameData()
            .WithMap(map => map with { LadderLogic = null }).Build).Message, StringComparison.Ordinal);
        Assert.Contains("usable item 'DA_Item_Unarmed' twice", Assert.Throws<GameDataException>(new SyntheticGameData()
            .With(content => content with { UsableItems = [.. content.UsableItems, content.UsableItems.Single(item => item.TechnicalName == GameNames.Items.Unarmed) with { UsableIndex = content.UsableItems.Length, AssetId = 9, BaseAssetId = 9 }] })
            .Build).Message, StringComparison.Ordinal);
        Assert.Contains("ARES expects 4", Assert.Throws<GameDataException>(new SyntheticGameData()
            .With(content => content with { DodgeRolls = content.DodgeRolls[..3] }).Build).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Maps_are_found_by_world_name()
    {
        GameData.GameData data = new SyntheticGameData()
            .With(content => content with { Maps = [.. content.Maps, content.Maps[0] with { World = content.Maps[0].World with { WorldName = "Other_P" } }] })
            .Build();

        Assert.Equal(["Other_P", GameNames.Maps.Dam], data.Maps.Select(map => map.WorldName));
        Assert.Null(data.FindMap("Missing_P"));
        Assert.Contains("no map 'Missing_P'", Assert.Throws<GameDataException>(() => data.Map("Missing_P")).Message, StringComparison.Ordinal);
        Assert.Equal("Other_P", data.Map("Other_P").WorldName);
    }
}
