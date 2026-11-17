using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Collision;

namespace Ares.GameDataExtract.Tests;

public sealed class ScriptSchemasTests
{
    private const uint Reflected = ScriptProperty.ReflectedFlag;
    private const long VectorType = 0x7300;
    private const long ObjectType = 0x7400;
    private const ushort ObjectHandle = 0x405;
    private static readonly ScriptDataType s_float = new(0, 80);

    [Fact]
    public void A_class_chain_holds_the_reflected_members_derived_class_first()
    {
        ScriptSchemas schemas = new(ScriptCacheTests.Sample().Parse());

        StructSchema schema = Assert.IsType<StructSchema>(schemas.ForPath(ScriptSchemas.ScriptPackage + "WidgetTweakables"));

        Assert.Equal(["Weight", "Count", "Map", "Base"], Enumerable.Range(0, schema.ChainLength).Select(index => schema.At(index).Field?.Name));
        Assert.Equal(PropertyKind.Double, schema.At(0).Field!.Type.Kind);
        Assert.Equal(PropertyKind.Map, schema.At(2).Field!.Type.Kind);
        Assert.Equal(PropertyKind.Double, schema.At(2).Field!.Type.Value!.Kind);
        Assert.Same(schema, schemas.Of("UWidgetTweakables"));
        Assert.Null(schemas.ForPath("/Script/Engine.SceneComponent"));
    }

    [Fact]
    public void A_scene_component_chain_continues_with_the_scene_members()
    {
        SyntheticScriptCache sample = new();
        sample.Classes.Add(new TestClass("UPromptComponent", "USceneComponent",
            [
                new TestProperty("Delay", s_float, Reflected),
                new TestProperty("Target", new ScriptDataType(ObjectType, ObjectHandle), Reflected),
                new TestProperty("Offset", new ScriptDataType(VectorType, 5), Reflected),
            ]));
        sample.Types[VectorType] = ("FVector", []);
        sample.Types[ObjectType] = ("UPrimitiveComponent", []);
        ScriptSchemas schemas = new(sample.Parse());

        StructSchema schema = schemas.Of("UPromptComponent");

        Assert.Equal(PropertyKind.Float, schema.At(0).Field!.Type.Kind);
        Assert.Equal(PropertyKind.Object, schema.At(1).Field!.Type.Kind);
        Assert.Same(EngineStructs.Vector, schema.At(2).Field!.Type.Struct);
        Assert.Equal(ScriptSchemas.AttachParent, schema.At(3 + SceneComponentMembers.AttachParent).Field!.Name);
        Assert.Equal(ScriptSchemas.RelativeLocation, schema.At(3 + SceneComponentMembers.RelativeLocation).Field!.Name);
        Assert.Null(schema.At(3 + SceneComponentMembers.Visible).Field);
    }
}
