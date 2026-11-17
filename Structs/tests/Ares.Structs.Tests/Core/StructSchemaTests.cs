using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Inventory;

namespace Ares.Structs.Tests.Core;

public sealed class StructSchemaTests
{
    [Fact]
    public void A_chain_lists_declared_members_then_the_super_chain()
    {
        StructSchema super = new("Base", [new PropertyField("Health", PropertyType.Int32), null]);
        StructSchema schema = new("Derived", [new PropertyField("Slots", PropertyType.Byte, ArrayDim: 2)], super);

        Assert.Equal(4, schema.ChainLength);
        Assert.Equal(("Slots", 1), (schema.At(1).Field!.Name, schema.At(1).Element));
        Assert.Equal("Health", schema.At(2).Field!.Name);
        Assert.Null(schema.At(3).Field);
        Assert.Null(schema.At(4).Field);
        Assert.Null(schema.At(-1).Field);
    }

    [Fact]
    public void A_name_repeated_in_the_chain_is_rejected()
    {
        StructSchema super = new("Base", [new PropertyField("Health", PropertyType.Int32)]);

        Assert.Throws<ArgumentException>(() => new StructSchema("Derived", [new PropertyField("Health", PropertyType.Double)], super));
    }

    [Fact]
    public void Item_data_assets_chain_their_native_bases_after_the_script_members()
    {
        StructSchema item = ItemAssetSchemas.ItemDataAsset;

        Assert.Equal("bIsUpgradedVariation", item.At(0).Field!.Name);
        Assert.Equal("PersistenceDataAsset", item.At(item.Members.Count).Field!.Name);
        Assert.Equal("NativeClass", item.At(item.ChainLength - 1).Field!.Name);
        Assert.Same(item, ItemAssetSchemas.AugmentItemDataAsset.Super);
        Assert.All(ItemAssetSchemas.ByClassPath.Keys, path => Assert.StartsWith("/Script/Angelscript.", path, StringComparison.Ordinal));
    }

    [Fact]
    public void An_opaque_member_needs_a_size()
    {
        Assert.Equal(3, PropertyType.Opaque(3).Size);
        Assert.Throws<ArgumentOutOfRangeException>(() => PropertyType.Opaque(0));
    }
}
