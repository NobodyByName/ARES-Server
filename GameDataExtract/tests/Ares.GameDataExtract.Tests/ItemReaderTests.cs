using Ares.GameDataExtract.Items;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Assets;

namespace Ares.GameDataExtract.Tests;

public sealed class ItemReaderTests
{
    private const uint Reflected = ScriptProperty.ReflectedFlag;
    private const long ArrayType = 0x7000;
    private const long MapType = 0x7001;
    private const long StructType = 0x7002;
    private const long ObjectType = 0x7003;
    private const long EnumType = 0x7004;
    private const long NameType = 0x7005;
    private const long VectorType = 0x7006;
    private const long NativeType = 0x7007;
    private const long UnknownType = 0x7008;
    private static readonly ScriptDataType s_int64 = new(0, 71);
    private static readonly ScriptDataType s_double = new(0, 81);

    [Fact]
    public void Tag_queries_match_tags_and_tags_under_them()
    {
        // ANY( Inventory.ItemType.Armor.Medium, Inventory.ItemType.Armor.Light )
        GameplayTagQuery any = Query(["Inventory.ItemType.Armor.Medium", "Inventory.ItemType.Armor.Light"], [0, 1, 1, 2, 0, 1]);

        Assert.True(any.Matches(["Inventory.ItemType.Armor.Light"]));
        Assert.True(any.Matches(["Inventory.ItemType.Armor.Medium.Heavy"]));
        Assert.False(any.Matches(["Inventory.ItemType.Armor"]));
        Assert.False(any.Matches(["Inventory.ItemType.Armor.LightBody"]));
        Assert.Equal(1, any.FirstTagHeld(["Other", "Inventory.ItemType.Armor.Light"]));
        Assert.Equal(int.MaxValue, any.FirstTagHeld(["Other"]));
    }

    [Fact]
    public void Tag_query_expressions_combine_and_an_empty_query_matches_nothing()
    {
        // ALL( ANY_EXACT( A ), NONE( B ) )
        GameplayTagQuery query = Query(["A", "B"], [0, 1, 5, 2, 7, 1, 0, 3, 1, 1]);

        Assert.True(query.Matches(["A", "C"]));
        Assert.False(query.Matches(["A.Sub"]));
        Assert.False(query.Matches(["A", "B.Sub"]));
        Assert.False(Query([], [0, 0]).Matches(["A"]));
        Assert.False(Query([], []).Matches(["A"]));
        Assert.Throws<InvalidDataException>(() => Query(["A"], [0, 1, 1, 1, 4]).Matches(["A"]));
        Assert.Throws<InvalidDataException>(() => Query(["A"], [0, 1, 1, 2, 0]).Matches(["A"]));
        Assert.Throws<InvalidDataException>(() => Query(["A"], [0, 1, 9, 0]).Matches(["A"]));
    }

    [Fact]
    public void Script_schemas_map_member_types_and_end_at_known_native_supers()
    {
        SyntheticScriptCache cache = new()
        {
            Classes =
            {
                new TestClass("UThing", "UBase",
                [
                    new TestProperty("Id", s_int64, Reflected),
                    new TestProperty("Hidden", s_double, 0),
                    new TestProperty("Entries", new ScriptDataType(ArrayType, 5), Reflected),
                    new TestProperty("Values", new ScriptDataType(MapType, 5), Reflected),
                    new TestProperty("Owner", new ScriptDataType(ObjectType, 5), Reflected),
                    new TestProperty("Kind", new ScriptDataType(EnumType, 5), Reflected),
                    new TestProperty("Label", new ScriptDataType(NameType, 5), Reflected),
                    new TestProperty("Where", new ScriptDataType(VectorType, 5), Reflected),
                    new TestProperty("Native", new ScriptDataType(NativeType, 5), Reflected),
                    new TestProperty("Unknown", new ScriptDataType(UnknownType, 5), Reflected),
                ]),
                new TestClass("UBase", "UNativeBase", [new TestProperty("Weight", s_double, Reflected)]),
                new TestClass("FEntry", null, [new TestProperty("Amount", s_double, Reflected)]),
            },
            Types =
            {
                [ArrayType] = ("TArray", [new ScriptDataType(StructType, 5)]),
                [MapType] = ("TMap", [s_int64, s_double]),
                [StructType] = ("FEntry", []),
                [ObjectType] = ("UItemDataAsset", []),
                [EnumType] = ("EItemCategory", []),
                [NameType] = ("FName", []),
                [VectorType] = ("FVector", []),
                [NativeType] = ("FNativeThing", []),
                [UnknownType] = ("FMystery", []),
            },
        };
        StructSchema native = new("NativeBase", [new PropertyField("Flags", PropertyType.Int32)]);
        StructSchema nativeStruct = new("NativeThing", [new PropertyField("X", PropertyType.Float)]);
        ScriptSchemas schemas = new(cache.Parse(), new Dictionary<string, StructSchema> { ["UNativeBase"] = native, ["FNativeThing"] = nativeStruct });

        StructSchema thing = schemas.ForPath(ScriptSchemas.ScriptPackage + "Thing")!;

        Assert.Equal(["Id", "Entries", "Values", "Owner", "Kind", "Label", "Where", "Native", "Unknown"], thing.Members.Select(member => member!.Name));
        PropertyType[] types = [.. thing.Members.Select(member => member!.Type)];
        Assert.Equal(PropertyKind.Int64, types[0].Kind);
        Assert.Equal(["Amount"], types[1].Inner!.Struct!.Members.Select(member => member!.Name));
        Assert.Equal((PropertyKind.Int64, PropertyKind.Double), (types[2].Inner!.Kind, types[2].Value!.Kind));
        Assert.Equal([PropertyKind.Object, PropertyKind.Enum, PropertyKind.Name], types[3..6].Select(type => type.Kind));
        Assert.Same(EngineStructs.Vector, types[6].Struct);
        Assert.Same(nativeStruct, types[7].Struct);
        Assert.Equal(NativeLayout.Unknown, types[8].Struct!.Native);
        Assert.Equal("Base", thing.Super!.Name);
        Assert.Same(native, thing.Super.Super);
        Assert.Equal(11, thing.ChainLength);
        Assert.Same(thing, schemas.Of("UThing"));
        Assert.Null(schemas.ForPath(ScriptSchemas.ScriptPackage + "Missing"));
        Assert.Same(EngineStructs.GameplayTag, schemas.ForPath("/Script/GameplayTags.GameplayTag"));
    }

    private static GameplayTagQuery Query(string[] tags, byte[] tokens)
    {
        object[] dictionary = [.. tags.Select(tag => new PropertyBag(EngineStructs.GameplayTag, new Dictionary<string, object> { ["TagName"] = tag }))];
        return GameplayTagQuery.Of(new PropertyBag(EngineStructs.GameplayTagQuery, new Dictionary<string, object>
        {
            ["TagDictionary"] = dictionary,
            ["QueryTokenStream"] = tokens.Cast<object>().ToArray(),
        }));
    }
}
