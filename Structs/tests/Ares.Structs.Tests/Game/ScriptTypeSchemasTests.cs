using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.Scripts;

namespace Ares.Structs.Tests.Game;

public sealed class ScriptTypeSchemasTests
{
    private static readonly StructSchema s_nativeBase = new("NativeBase", [new PropertyField("NativeValue", PropertyType.Int32)]);
    private static readonly StructSchema s_nativeStruct = new("NativeStruct", [new PropertyField("Inside", PropertyType.Float)]);

    private static readonly Dictionary<string, ScriptTypeDefinition> s_types = Types().ToDictionary(type => type.Name, StringComparer.Ordinal);

    private static ScriptTypeDefinition[] Types() =>
    [
        new("UBase", "UNativeBase", [new ScriptMemberDefinition("BaseValue", ScriptMemberType.Double)]),
        new("UDerived", "UBase",
        [
            new ScriptMemberDefinition("Flag", ScriptMemberType.Bool),
            new ScriptMemberDefinition("Kind", ScriptMemberType.Enum("EKind")),
            new ScriptMemberDefinition("Kinds", ScriptMemberType.ArrayOf(ScriptMemberType.Enum("EKind"))),
            new ScriptMemberDefinition("Point", ScriptMemberType.Struct("FVector")),
            new ScriptMemberDefinition("Native", ScriptMemberType.Struct("FNativeStruct")),
            new ScriptMemberDefinition("Script", ScriptMemberType.Struct("FScriptStruct")),
            new ScriptMemberDefinition("Missing", ScriptMemberType.Struct("FMissing")),
            new ScriptMemberDefinition("Lookup", ScriptMemberType.MapOf(ScriptMemberType.Name, ScriptMemberType.SetOf(ScriptMemberType.SoftObject))),
            new ScriptMemberDefinition("Changed", ScriptMemberType.Delegate("FOnChanged")),
            new ScriptMemberDefinition("Ended", ScriptMemberType.Delegate("OnEnded")),
            new ScriptMemberDefinition("Odd", ScriptMemberType.Unknown("TOptional")),
        ]),
        new("FScriptStruct", null,
        [
            new ScriptMemberDefinition("Values", ScriptMemberType.ArrayOf(ScriptMemberType.Int64)),
            new ScriptMemberDefinition("Label", ScriptMemberType.Text),
        ]),
        new("UPart", "USceneComponent", [new ScriptMemberDefinition("Owner", ScriptMemberType.Object)]),
        new("ALoop", "ALoop", []),
    ];

    private static ScriptTypeSchemas Schemas() => new(s_types, new Dictionary<string, StructSchema>(StringComparer.Ordinal)
    {
        ["UNativeBase"] = s_nativeBase,
        ["FNativeStruct"] = s_nativeStruct,
    });

    [Fact]
    public void ChainRunsFromDeclaredMembersThroughScriptSupersToTheNativeBase()
    {
        StructSchema schema = Schemas().Of("UDerived");

        Assert.Equal("Derived", schema.Name);
        Assert.Equal(11 + 1 + 1, schema.ChainLength);
        Assert.Equal("Flag", schema.At(0).Field!.Name);
        Assert.Equal("BaseValue", schema.At(11).Field!.Name);
        Assert.Same(s_nativeBase, schema.Super!.Super);
        Assert.Equal("NativeValue", schema.At(12).Field!.Name);
    }

    [Fact]
    public void MemberTypesResolveEnumsStructsDelegatesAndContainers()
    {
        Dictionary<string, PropertyType> types = Schemas().Of("UDerived").Members.ToDictionary(member => member!.Name, member => member!.Type);

        Assert.Same(PropertyType.ByteEnum, types["Kind"]);
        Assert.Equal(PropertyType.ArrayOf(PropertyType.EnumOf(PropertyType.Name)), types["Kinds"]);
        Assert.Same(EngineStructs.Vector, types["Point"].Struct);
        Assert.Same(s_nativeStruct, types["Native"].Struct);
        Assert.Equal(["Values", "Label"], types["Script"].Struct!.Members.Select(member => member!.Name));
        Assert.Equal(PropertyKind.Array, types["Script"].Struct!.Members[0]!.Type.Kind);
        Assert.Equal(NativeLayout.Unknown, types["Missing"].Struct!.Native);
        Assert.Equal("Missing", types["Missing"].Struct!.Name);
        Assert.Equal(PropertyType.MapOf(PropertyType.Name, PropertyType.SetOf(PropertyType.SoftObject)), types["Lookup"]);
        Assert.Equal(("OnChanged", NativeLayout.None, 0), (types["Changed"].Struct!.Name, types["Changed"].Struct!.Native, types["Changed"].Struct!.ChainLength));
        Assert.Equal(("OnEnded", NativeLayout.Unknown), (types["Ended"].Struct!.Name, types["Ended"].Struct!.Native));
        Assert.Equal(("TOptional", NativeLayout.Unknown), (types["Odd"].Struct!.Name, types["Odd"].Struct!.Native));
    }

    [Fact]
    public void SceneComponentBaseContinuesWithTheKnownSceneMembers()
    {
        StructSchema schema = Schemas().Of("UPart");

        Assert.Equal(SceneComponentMembers.ClassPath, schema.Super!.Name);
        Assert.Equal(
            [ScriptTypeSchemas.AttachParent, ScriptTypeSchemas.AttachSocketName, ScriptTypeSchemas.RelativeLocation, ScriptTypeSchemas.RelativeRotation, ScriptTypeSchemas.RelativeScale3D],
            [
                .. new[] { SceneComponentMembers.AttachParent, SceneComponentMembers.AttachSocketName, SceneComponentMembers.RelativeLocation, SceneComponentMembers.RelativeRotation, SceneComponentMembers.RelativeScale3D }
                    .Select(index => schema.At(1 + index).Field!.Name),
            ]);
    }

    [Fact]
    public void PathsFindScriptTypesByAnyPrefixAndEngineStructs()
    {
        ScriptTypeSchemas schemas = Schemas();

        Assert.Same(schemas.Of("UDerived"), schemas.ForPath(ScriptTypeSchemas.ScriptPackage + "Derived"));
        Assert.Same(schemas.Of("FScriptStruct"), schemas.ForPath(ScriptTypeSchemas.ScriptPackage + "ScriptStruct"));
        Assert.Same(EngineStructs.Transform, schemas.ForPath("/Script/CoreUObject.Transform"));
        Assert.Null(schemas.ForPath(ScriptTypeSchemas.ScriptPackage + "Missing"));
        Assert.Null(schemas.ForPath("/Script/Engine.Actor"));
    }

    [Fact]
    public void UnknownOrEndlessTypesAreRejected()
    {
        ScriptTypeSchemas schemas = Schemas();

        Assert.Throws<InvalidDataException>(() => schemas.Of("UMissing"));
        Assert.Throws<InvalidDataException>(() => schemas.Of("ALoop"));
    }

    [Fact]
    public void CurrentBuildTypesAndEnumsAreConsistent()
    {
        Assert.All(ScriptTypes.ByName, pair => Assert.Equal(pair.Key, pair.Value.Name));
        Assert.All(ScriptTypes.Enums.Values, type => Assert.Equal(type.Names.Count, type.Values.Count));
        Assert.All(ScriptTypes.ByName.Values.SelectMany(type => type.Members), member => Assert.NotNull(member.Type));
        Assert.Same(EngineStructs.Vector, new ScriptTypeSchemas().ForPath("/Script/CoreUObject.Vector"));
    }
}
