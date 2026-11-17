using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.Structs.Core.Assets;
using Ares.Structs.Core.Geometry;

namespace Ares.GameDataExtract.Tests;

public sealed class PropertyReaderTests
{
    private const string PackagePath = "/Game/Test";

    /// <summary>Names after the package path and the export name, in name-map order from index 2.</summary>
    private static readonly string[] s_names = ["None", "Alpha", "Beta", "/Game/Other", "Other", "Table", "Tag.A", "Tag.B"];

    private static readonly StructSchema s_scalars = new("Scalars",
    [
        new PropertyField("Count", PropertyType.Int32),
        new PropertyField("Speed", PropertyType.Double),
        new PropertyField("Enabled", PropertyType.Bool),
        new PropertyField("Label", PropertyType.Name),
        new PropertyField("Comment", PropertyType.String),
        new PropertyField("Id", PropertyType.Int64),
        new PropertyField("Scale", PropertyType.Float),
        new PropertyField("Kind", PropertyType.ByteEnum),
        new PropertyField("Target", PropertyType.Object),
    ]);

    [Fact]
    public void Scalar_members_decode_in_chain_order()
    {
        byte[] block =
        [
            .. PropertyBlocks.Header(0, 1, 2, 3, 4, 5, 6, 7, 8),
            .. BitConverter.GetBytes(7), .. BitConverter.GetBytes(2.5), 1, .. PropertyBlocks.Name(Name("Alpha")),
            .. PropertyBlocks.String("hi"), .. BitConverter.GetBytes(-5L), .. BitConverter.GetBytes(0.5f), 3, .. BitConverter.GetBytes(-2),
            0, 0, 0, 0,
        ];
        (CookedPackage package, ZenExport export) = Package(block);

        PropertyBag bag = PropertyReader.ReadExport(package, export, s_scalars, 4);

        Assert.Equal(7, bag.Get<int>("Count"));
        Assert.Equal(2.5, bag.Get<double>("Speed"));
        Assert.True(bag.Get<bool>("Enabled"));
        Assert.Equal("Alpha", bag.Get<string>("Label"));
        Assert.Equal("hi", bag.Get<string>("Comment"));
        Assert.Equal(-5L, bag.Get<long>("Id"));
        Assert.Equal(0.5f, bag.Get<float>("Scale"));
        Assert.Equal((byte)3, bag.Get<byte>("Kind"));
        Assert.Equal(new ObjectReference(-2), bag.Get<ObjectReference>("Target"));
        Assert.False(bag.Get<ObjectReference>("Target").IsNull);
        Assert.Equal(["Count", "Speed", "Enabled", "Label", "Comment", "Id", "Scale", "Kind", "Target"], bag.Names);
        Assert.Throws<InvalidDataException>(() => PropertyReader.ReadExport(package, export, s_scalars, 2));
    }

    [Fact]
    public void Zero_masked_members_hold_zero_values_and_absent_ones_fall_back()
    {
        byte[] block = [.. PropertyBlocks.Fragment(0, 3, zeroes: true, last: true), 0b101, .. BitConverter.GetBytes(4.0)];

        PropertyBag bag = Read(block, s_scalars, out int end);

        Assert.Equal(0, bag.Get<int>("Count"));
        Assert.Equal(4.0, bag.Get<double>("Speed"));
        Assert.False(bag.Get<bool>("Enabled"));
        Assert.False(bag.Contains("Label"));
        Assert.Equal("fallback", bag.Get("Label", "fallback"));
        Assert.Equal(block.Length, end);
        Assert.Throws<InvalidDataException>(() => bag.Get<string>("Label"));
        Assert.Throws<InvalidDataException>(() => bag.Get<string>("Speed"));
    }

    [Fact]
    public void Containers_decode_their_elements()
    {
        StructSchema schema = new("Containers",
        [
            new PropertyField("Numbers", PropertyType.ArrayOf(PropertyType.Int32)),
            new PropertyField("Lookup", PropertyType.MapOf(PropertyType.Name, PropertyType.Double)),
            new PropertyField("Unique", PropertyType.SetOf(PropertyType.Name)),
        ]);
        byte[] block =
        [
            .. PropertyBlocks.Header(0, 1, 2),
            .. BitConverter.GetBytes(2), .. BitConverter.GetBytes(10), .. BitConverter.GetBytes(20),
            .. BitConverter.GetBytes(0), .. BitConverter.GetBytes(1), .. PropertyBlocks.Name(Name("Beta")), .. BitConverter.GetBytes(1.5),
            .. BitConverter.GetBytes(0), .. BitConverter.GetBytes(2), .. PropertyBlocks.Name(Name("Alpha")), .. PropertyBlocks.Name(Name("Beta")),
        ];

        PropertyBag bag = Read(block, schema, out _);

        Assert.Equal([10, 20], bag.Get<object[]>("Numbers"));
        Assert.Equal([new KeyValuePair<object, object>("Beta", 1.5)], bag.Get<KeyValuePair<object, object>[]>("Lookup"));
        Assert.Equal(["Alpha", "Beta"], bag.Get<object[]>("Unique"));
    }

    [Fact]
    public void A_map_that_removes_archetype_keys_is_rejected()
    {
        StructSchema schema = new("Map", [new PropertyField("Lookup", PropertyType.MapOf(PropertyType.Int32, PropertyType.Int32))]);
        byte[] block = [.. PropertyBlocks.Header(0), .. BitConverter.GetBytes(1), .. BitConverter.GetBytes(0)];

        Assert.Throws<InvalidDataException>(() => Read(block, schema, out _));
    }

    [Fact]
    public void Structs_decode_as_blocks_or_native_layouts()
    {
        StructSchema inner = new("Inner", [new PropertyField("Tag", PropertyType.StructOf(EngineStructs.GameplayTag)), new PropertyField("Value", PropertyType.Int32)]);
        StructSchema schema = new("Outer",
        [
            new PropertyField("Inner", PropertyType.StructOf(inner)),
            new PropertyField("Location", PropertyType.StructOf(EngineStructs.Vector)),
            new PropertyField("Asset", PropertyType.StructOf(EngineStructs.SoftObjectPath)),
            new PropertyField("Tags", PropertyType.StructOf(EngineStructs.GameplayTagContainer)),
            new PropertyField("Turn", PropertyType.StructOf(EngineStructs.Rotator)),
            new PropertyField("Small", PropertyType.StructOf(EngineStructs.Vector3f)),
        ]);
        byte[] block =
        [
            .. PropertyBlocks.Header(0, 1, 2, 3, 4, 5),
            .. PropertyBlocks.Header(0, 1), .. PropertyBlocks.Header(0), .. PropertyBlocks.Name(Name("Tag.A")), .. BitConverter.GetBytes(9),
            .. PropertyBlocks.Vector(1, 2, 3),
            .. PropertyBlocks.Name(Name("/Game/Other")), .. PropertyBlocks.Name(Name("Other")), .. PropertyBlocks.String(""),
            .. BitConverter.GetBytes(2), .. PropertyBlocks.Name(Name("Tag.A")), .. PropertyBlocks.Name(Name("Tag.B")),
            .. PropertyBlocks.Vector(10, 20, 30),
            .. BitConverter.GetBytes(1f), .. BitConverter.GetBytes(2f), .. BitConverter.GetBytes(3f),
        ];

        PropertyBag bag = Read(block, schema, out int end);

        PropertyBag innerBag = bag.Get<PropertyBag>("Inner");
        Assert.Equal("Tag.A", innerBag.Get<PropertyBag>("Tag").Get<string>("TagName"));
        Assert.Equal(9, innerBag.Get<int>("Value"));
        Assert.Equal(new Vector3D(1, 2, 3), bag.Get<Vector3D>("Location"));
        Assert.Equal("/Game/Other.Other", bag.Get<SoftObjectPath>("Asset").Path);
        Assert.Equal(["Tag.A", "Tag.B"], bag.Get<string[]>("Tags"));
        Assert.Equal(new Rotator(10, 20, 30), bag.Get<Rotator>("Turn"));
        Assert.Equal(new Vector3D(1, 2, 3), bag.Get<Vector3D>("Small"));
        Assert.Equal(block.Length, end);
    }

    [Fact]
    public void Texts_decode_each_supported_history()
    {
        StructSchema schema = new("Texts",
        [
            new PropertyField("Plain", PropertyType.Text),
            new PropertyField("Localized", PropertyType.Text),
            new PropertyField("Entry", PropertyType.Text),
            new PropertyField("Empty", PropertyType.Text),
        ]);
        byte[] block =
        [
            .. PropertyBlocks.Header(0, 1, 2, 3),
            .. BitConverter.GetBytes(0), 0xFF, .. BitConverter.GetBytes(1), .. PropertyBlocks.String("raw"),
            .. BitConverter.GetBytes(0), 0, .. PropertyBlocks.String("ns"), .. PropertyBlocks.String("key"), .. PropertyBlocks.String("Source"),
            .. BitConverter.GetBytes(0), 11, .. PropertyBlocks.Name(Name("Table")), .. PropertyBlocks.String("ID_X"),
            .. BitConverter.GetBytes(0), 0xFF, .. BitConverter.GetBytes(0),
        ];

        PropertyBag bag = Read(block, schema, out _);

        Assert.Equal(new TextValue(TextHistory.None, "", "", "raw"), bag.Get<TextValue>("Plain"));
        Assert.Equal(new TextValue(TextHistory.Base, "ns", "key", "Source"), bag.Get<TextValue>("Localized"));
        Assert.Equal(new TextValue(TextHistory.StringTableEntry, "Table", "ID_X", ""), bag.Get<TextValue>("Entry"));
        Assert.Equal(TextValue.Empty, bag.Get<TextValue>("Empty"));
    }

    [Fact]
    public void A_stored_unknown_member_stops_a_read_but_not_a_prefix_read()
    {
        StructSchema schema = new("Partial", [new PropertyField("Known", PropertyType.Int32), null, new PropertyField("Later", PropertyType.Int32)]);
        byte[] block = [.. PropertyBlocks.Header(0, 1, 2), .. BitConverter.GetBytes(5), .. BitConverter.GetBytes(6), .. BitConverter.GetBytes(7)];
        (CookedPackage package, _) = Package(block);

        Assert.Throws<InvalidDataException>(() => PropertyReader.Read(package, Start(package), schema, out _));
        PropertyBag prefix = PropertyReader.ReadPrefix(package, Start(package), schema, out bool complete);

        Assert.False(complete);
        Assert.Equal(["Known"], prefix.Names);
        Assert.Equal(5, prefix.Get<int>("Known"));
    }

    [Fact]
    public void A_zero_masked_unknown_member_is_skipped_and_one_past_the_chain_fails()
    {
        StructSchema schema = new("Partial", [new PropertyField("Known", PropertyType.Int32), null]);
        byte[] zeroUnknown = [.. PropertyBlocks.Fragment(0, 2, zeroes: true, last: true), 0b10, .. BitConverter.GetBytes(5)];

        PropertyBag bag = Read(zeroUnknown, schema, out _);

        Assert.Equal(["Known"], bag.Names);
        Assert.Throws<InvalidDataException>(() => Read([.. PropertyBlocks.Header(2), .. BitConverter.GetBytes(1)], schema, out _));
    }

    [Fact]
    public void Member_arrays_and_super_chains_take_their_indices()
    {
        StructSchema super = new("Base", [new PropertyField("Health", PropertyType.Int32)]);
        StructSchema schema = new("Derived", [new PropertyField("Slots", PropertyType.Byte, ArrayDim: 2)], super);
        byte[] block = [.. PropertyBlocks.Header(1, 2), 4, .. BitConverter.GetBytes(100)];

        PropertyBag bag = Read(block, schema, out _);

        Assert.Equal(3, schema.ChainLength);
        Assert.Equal((byte)4, bag.Get<byte>("Slots[1]"));
        Assert.Equal(100, bag.Get<int>("Health"));
    }

    [Fact]
    public void A_struct_without_a_known_layout_fails_when_stored()
    {
        StructSchema schema = new("Holder", [new PropertyField("Mystery", PropertyType.StructOf(StructSchema.UnknownStruct("Mystery")))]);

        Assert.Throws<InvalidDataException>(() => Read([.. PropertyBlocks.Header(0), 1, 2, 3, 4], schema, out _));
    }

    [Fact]
    public void A_bag_over_its_archetype_merges_structs_and_replaces_other_values()
    {
        StructSchema inner = new("Inner", [new PropertyField("X", PropertyType.Int32), new PropertyField("Y", PropertyType.Int32)]);
        StructSchema schema = new("Outer",
        [
            new PropertyField("A", PropertyType.Int32),
            new PropertyField("S", PropertyType.StructOf(inner)),
            new PropertyField("L", PropertyType.ArrayOf(PropertyType.Int32)),
            new PropertyField("Z", PropertyType.StructOf(inner)),
        ]);
        PropertyBag archetype = Read(
        [
            .. PropertyBlocks.Header(0, 1, 2, 3), .. BitConverter.GetBytes(1), .. PropertyBlocks.Header(0), .. BitConverter.GetBytes(5),
            .. BitConverter.GetBytes(2), .. BitConverter.GetBytes(1), .. BitConverter.GetBytes(2), .. PropertyBlocks.Header(0), .. BitConverter.GetBytes(8),
        ], schema, out _);
        PropertyBag own = Read(
        [
            .. PropertyBlocks.Fragment(1, 3, zeroes: true, last: true), 0b100, .. PropertyBlocks.Header(1), .. BitConverter.GetBytes(7),
            .. BitConverter.GetBytes(1), .. BitConverter.GetBytes(3),
        ], schema, out _);

        PropertyBag merged = own.Over(archetype);

        Assert.Equal(1, merged.Get<int>("A"));
        Assert.Equal(5, merged.Get<PropertyBag>("S").Get<int>("X"));
        Assert.Equal(7, merged.Get<PropertyBag>("S").Get<int>("Y"));
        Assert.Equal([3], merged.Get<object[]>("L"));
        Assert.True(merged.Get<PropertyBag>("Z").IsZero);
        Assert.False(merged.Get<PropertyBag>("Z").Contains("X"));
    }

    private static int Name(string name) => 2 + Array.IndexOf(s_names, name);

    private static int Start(CookedPackage package) => CookedPackage.StartOf(package.Exports[0]);

    private static PropertyBag Read(byte[] block, StructSchema schema, out int end)
    {
        (CookedPackage package, ZenExport export) = Package(block);
        PropertyBag bag = PropertyReader.Read(package, CookedPackage.StartOf(export), schema, out int at);
        end = at - CookedPackage.StartOf(export);
        return bag;
    }

    private static (CookedPackage Package, ZenExport Export) Package(byte[] payload)
    {
        CookedPackage package = CookedPackage.Parse(SyntheticArchives.Package(PackagePath,
            [("Asset", PackageObjectIndex.Null, PackageObjectIndex.FromScriptPath("/Script/Test.Asset"), 0, payload)], extraNames: s_names));
        return (package, package.Exports[0]);
    }
}
