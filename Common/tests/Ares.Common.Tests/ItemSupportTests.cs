using Ares.Common.Game.Inventory;
using Ares.Common.GameData;
using Ares.Common.Testing;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;

namespace Ares.Common.Tests;

public sealed class ItemSupportTests
{
    [Fact]
    public void An_unsupported_catalogue_item_is_served_to_the_menu_but_never_held_carried_or_given_traits()
    {
        SyntheticGameData synthetic = new();
        foreach (string name in ItemTechnicalNames.All)
        {
            _ = synthetic.AddItem(name);
        }

        string unsupported = Assert.Single(ItemSupport.Unsupported.Keys);
        GameDataSet data = synthetic.AddItem(unsupported, item => item with { Category = ItemCategory.Deployable, MaxStack = 3 })
            .AddItem("DA_Item_Synthetic_Deployable", item => item with { Category = ItemCategory.Deployable, MaxStack = 3 }).Build();
        BackendItemContent content = new([], [], 0, 0, new Dictionary<long, IReadOnlyList<ItemAssetAmount>>(), new Dictionary<long, long>());
        LoadoutDefinitions definitions = new(data, content);
        ItemTraitsTable traits = new(data, definitions);
        ItemDefinition item = data.Items.ByTechnicalName[unsupported];
        ItemDefinition supported = data.Items.ByTechnicalName["DA_Item_Synthetic_Deployable"];

        Assert.Equal(3L, definitions.ByAsset[item.AssetId].MaxAmount);
        Assert.Contains(definitions.All, definition => definition.GameAssetId == item.AssetId);
        Assert.False(definitions.SupportsCarriedItem(item.AssetId));
        Assert.DoesNotContain(unsupported, definitions.AssetIds.Keys);
        Assert.DoesNotContain(definitions.PlainItems, plain => plain.AssetId == item.AssetId);
        Assert.DoesNotContain(definitions.CarriedItems, carried => carried.AssetId == item.AssetId);
        Assert.DoesNotContain(traits.All, entry => entry.UsableIndex == item.UsableIndex);

        Assert.True(definitions.SupportsCarriedItem(supported.AssetId));
        Assert.Contains(definitions.PlainItems, plain => plain.AssetId == supported.AssetId);
        Assert.Contains(traits.All, entry => entry.UsableIndex == supported.UsableIndex);
    }
}
