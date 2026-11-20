using Ares.GameDataExtract.Scripts;
using Ares.Structs.Game.Scripts;

namespace Ares.GameDataExtract.Tests;

public sealed class ScriptLayoutsTests
{
    private static readonly ScriptLayouts s_layouts = new(
        new Dictionary<string, ScriptTypeDefinition>(StringComparer.Ordinal)
        {
            ["UDerived"] = new("UDerived", "UBase", [new ScriptMemberDefinition("Count", ScriptMemberType.Int32)]),
            ["UBase"] = new("UBase", "UNativeBase", [new ScriptMemberDefinition("Weight", ScriptMemberType.Double), new ScriptMemberDefinition("Owner", ScriptMemberType.Object)]),
            ["AThing"] = new("AThing", "AActor", []),
        },
        new Dictionary<string, ScriptEnumDefinition>(StringComparer.Ordinal)
        {
            ["EKind"] = new("EKind", ["A", "B"], [0, 1]),
            ["EFlags"] = new("EFlags", ["A", "B"], [1, 2]),
        });

    [Fact]
    public void A_chain_runs_derived_class_first_and_ends_at_the_native_super()
    {
        Assert.Equal(["UDerived", "UBase"], s_layouts.Chain("UDerived", out string? native).Select(type => type.Name));
        Assert.Equal("UNativeBase", native);
        Assert.Equal(["Count", "Weight", "Owner"], s_layouts.ReflectedChain("UDerived", out _).Select(member => member.Name));
        Assert.True(s_layouts.Derives("UDerived", "UBase"));
        Assert.True(s_layouts.Derives("UDerived", "UNativeBase"));
        Assert.False(s_layouts.Derives("UBase", "UDerived"));
    }

    [Fact]
    public void Script_paths_name_types_by_the_first_matching_prefix()
    {
        Assert.Equal("UDerived", s_layouts.NameOf(ScriptLayouts.ScriptPackage + "Derived", "A", "U"));
        Assert.Equal("AThing", s_layouts.NameOf(ScriptLayouts.ScriptPackage + "Thing", "U", "A"));
        Assert.Null(s_layouts.NameOf(ScriptLayouts.ScriptPackage + "Thing", "U"));
        Assert.Null(s_layouts.NameOf("/Script/Engine.Derived", "U"));
    }

    [Fact]
    public void Missing_types_and_enums_are_refused_and_enum_order_is_checked()
    {
        Assert.Throws<InvalidDataException>(() => s_layouts.Type("UMissing"));
        Assert.Throws<InvalidDataException>(() => s_layouts.Enum("EMissing"));
        Assert.True(ScriptLayouts.IsSequential(s_layouts.Enum("EKind")));
        Assert.False(ScriptLayouts.IsSequential(s_layouts.Enum("EFlags")));
    }
}
