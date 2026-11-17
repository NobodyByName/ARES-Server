using Ares.Structs.Game.Loot;
using Ares.Structs.Net.GameplayTags;

namespace Ares.Structs.Tests.Game;

public sealed class LootTableDataTests
{
    private static readonly LootRange s_one = new(1, 1);

    [Fact]
    public void Queries_select_items_by_drop_tags()
    {
        StaticValueLootTableEntry common = new(LootItemQueryType.Query, new GameplayTagQuery(["Item.Drop.Rarity.01Common"], [0, 1, 1, 1, 0]),
            null, -1, new LootRange(2, 5), s_one, s_one, LootDistributionMethod.MixAllPlacements);

        Assert.True(common.Selects("DA_Item_Any", ["Item.Drop.Rarity.01Common", "Item.Drop.Type.Salvage.Material"]));
        Assert.False(common.Selects("DA_Item_Any", ["Item.Drop.Rarity.02Uncommon"]));
    }

    [Fact]
    public void Exact_entries_select_their_item_only()
    {
        StaticValueLootTableEntry paint = new(LootItemQueryType.Exact, null, "DA_Item_Salvage_Paint", -1, s_one, s_one, s_one,
            LootDistributionMethod.RoundRobin);

        Assert.True(paint.Selects("da_item_salvage_paint", []));
        Assert.False(paint.Selects("DA_Item_Salvage_Planks", ["Item.Drop.Type.Salvage.Material.OutpostMaterial"]));
    }
}
