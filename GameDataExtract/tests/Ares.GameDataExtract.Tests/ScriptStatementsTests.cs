using Ares.GameDataExtract.Scripts;
using static Ares.GameDataExtract.Tests.SyntheticScriptCache;

namespace Ares.GameDataExtract.Tests;

public sealed class ScriptStatementsTests
{
    private const long ArrayType = 0x7300;
    private const long WeightType = 0x7400;
    private const long ConfigType = 0x7500;

    [Fact]
    public void Constructor_stores_are_read_by_member_offset()
    {
        ScriptCache cache = Cache();
        uint[] code = Code(
            Op("SetV8", 2, Quad(0.022)), Op("LoadThisR", 0xC8, 11), Op("WRTV8", 2),
            Op("SetV1", 3, 0x101), Op("LoadThisR", 0x10, 11), Op("WRTV1", 3),
            Op("SetV4", 4, 8), Op("LoadThisR", 0x20, 11), Op("WRTV4", 4),
            Op("RET"));

        ScriptStatements statements = Evaluate(cache, code);

        Assert.Equal([0x10, 0x20, 0xC8], statements.MemberStores.Keys);
        Assert.Equal((1, 1), (statements.MemberStores[0x10].Size, statements.MemberStores[0x10].Constant!.AsByte));
        Assert.Equal((4, 8), (statements.MemberStores[0x20].Size, statements.MemberStores[0x20].Constant!.AsInt32));
        Assert.Equal(0.022, statements.MemberStores[0xC8].Constant!.AsDouble);
        Assert.Equal(5, statements.MemberStores[0xC8].Position);
        Assert.True(statements.IsComplete);
        Assert.False(statements.HasBranches);
    }

    [Fact]
    public void Map_entries_added_through_temporaries_keep_key_and_value()
    {
        ScriptCache cache = ScriptCacheTests.Sample().Parse();

        ScriptStatements statements = ScriptStatements.Evaluate(cache, cache.Class("UWidgetTweakables").MethodsNamed(ScriptClass.InitDefaults).Single());

        ScriptCall add = Assert.Single(statements.Calls);
        Assert.Equal("Add", add.Name);
        Assert.Equal(new ScriptMember(ScriptThis.Instance, 0xD0, false), add.Object);
        Assert.Equal(7, add.Constant(0)!.AsInt32);
        Assert.Equal(1.5, add.Constant(1)!.AsDouble);
    }

    [Fact]
    public void Struct_values_built_in_a_local_are_passed_as_constructed_objects()
    {
        ScriptCache cache = Cache();
        uint[] entry1 = Code(Op("PshC8", 0, Quad(2.0)), Op("PshC4", 0, 2), Op("PSF", 4), Op("CALL", 0, 77), Op("PSF", 4), Op("PshThisMemRef", 0x4D8, 11), Op("CALLSYS", 0, Quad(0x9300UL)));
        uint[] entry2 = Code(Op("PshC8", 0, Quad(3.0)), Op("PshC4", 0, 3), Op("PSF", 4), Op("CALL", 0, 77), Op("PSF", 4), Op("PshThisMemRef", 0x4D8, 11), Op("CALLSYS", 0, Quad(0x9300UL)));

        ScriptStatements statements = Evaluate(cache, [.. entry1, .. entry2, .. Code(Op("RET"))]);

        ScriptCall[] adds = [.. statements.Calls.Where(call => call.Name == "Add")];
        Assert.Equal(2, adds.Length);
        Assert.All(adds, add => Assert.Equal(new ScriptMember(ScriptThis.Instance, 0x4D8, false), add.Object));
        ScriptCall[] built = [.. adds.Select(add => Assert.IsType<ScriptConstructed>(add.Arguments[0]).Call)];
        Assert.Equal([(2, 2.0), (3, 3.0)], built.Select(call => (call.Constant(0)!.AsInt32, call.Constant(1)!.AsDouble)));
        Assert.All(built, call => Assert.Equal(4, Assert.IsType<ScriptVariable>(call.Object).Slot));
        Assert.Equal([0x4D8], statements.ReferencedMembers);
        Assert.Equal(25, adds[1].Position);
    }

    [Fact]
    public void A_global_initializer_constructs_the_global_with_arguments_in_declaration_order()
    {
        ScriptCache cache = Cache();
        uint[] code = Code(Op("PGA", 0, Quad(0x6000UL)), Op("PshC8", 0, Quad(375.0)), Op("PshC8", 0, Quad(250.0)), Op("PshC8", 0, Quad(135.0)),
            Op("ALLOC", 0, [.. Quad((ulong)ConfigType), 78]), Op("RET"));

        ScriptCall call = Assert.Single(Evaluate(cache, code).Calls);

        Assert.Equal("FPromptConfig", call.Name);
        Assert.Equal("Standard", Assert.IsType<ScriptGlobalAddress>(call.Object).Name);
        Assert.Equal([135.0, 250.0, 375.0], call.Arguments.Select(argument => ((ScriptConstant)argument).AsDouble));
    }

    [Fact]
    public void Setter_arguments_follow_returned_values_and_conversions()
    {
        ScriptCache cache = Cache();
        uint[] code = Code(
            Op("PshC4", 0, 20000), Op("CALLSYS", 0, Quad(0x9500UL)), Op("CpyRtoV4", 2), Op("iTOf", 2, 2), Op("PshV4", 2), Op("PshVPtr", 0), Op("CALLSYS", 0, Quad(0x9600UL)),
            Op("SetV4", 3, 3), Op("iTOf", 3, 3), Op("PshV4", 3), Op("PshVPtr", 0), Op("CALLSYS", 0, Quad(0x9600UL)),
            Op("SetV1", 5, 4), Op("PshV1", 5), Op("PshVPtr", 0), Op("CALLSYS", 0, Quad(0x9700UL)),
            Op("RET"));

        ScriptStatements statements = Evaluate(cache, code);

        Assert.Equal(["Square", "SetDistance", "SetDistance", "SetDormancy"], statements.Calls.Select(call => call.Name));
        ScriptConverted squared = Assert.IsType<ScriptConverted>(statements.Calls[1].Arguments[0]);
        Assert.Equal("iTOf", squared.Conversion);
        ScriptCall square = Assert.IsType<ScriptReturned>(squared.Value).Call;
        Assert.Equal(("Square", 20000), (square.Name, square.Constant(0)!.AsInt32));
        Assert.Null(square.Object);
        Assert.Equal(3.0f, statements.Calls[2].Constant(0)!.AsSingle);
        Assert.Equal(ScriptThis.Instance, statements.Calls[3].Object);
        Assert.Equal(4, statements.Calls[3].Constant(0)!.AsByte);
    }

    [Fact]
    public void Unknown_calls_and_jumps_are_flagged()
    {
        ScriptCache cache = Cache();

        ScriptStatements lost = Evaluate(cache, Code(Op("PshC4", 0, 1), Op("CALLSYS", 0, Quad(0xDEADUL)), Op("RET")));
        ScriptStatements branching = Evaluate(cache, Code(Op("JMP", 0, 0), Op("PshNull"), Op("PshVPtr", 0), Op("CALLSYS", 0, Quad(0x9600UL)), Op("RET")));

        Assert.False(lost.IsComplete);
        Assert.Null(Assert.Single(lost.Calls).Function);
        Assert.True(branching.HasBranches);
        Assert.True(branching.IsComplete);
        Assert.Equal("PshNull", Assert.IsType<ScriptUnknown>(Assert.Single(branching.Calls).Arguments[0]).Opcode);
    }

    [Fact]
    public void Class_defaults_apply_the_base_class_code_first()
    {
        SyntheticScriptCache sample = new();
        uint[] baseCode = Code(Op("SetV4", 2, 1), Op("LoadThisR", 0x10, 11), Op("WRTV4", 2), Op("SetV4", 2, 5), Op("LoadThisR", 0x14, 11), Op("WRTV4", 2), Op("RET"));
        uint[] derivedCode = Code(Op("SetV4", 2, 9), Op("LoadThisR", 0x10, 11), Op("WRTV4", 2), Op("RET"));
        uint[] derivedDefaults = Code(Op("SetV4", 2, 12), Op("LoadThisR", 0x18, 11), Op("WRTV4", 2), Op("PshC4", 0, 1), Op("CALLSYS", 0, Quad(0x9500UL)), Op("RET"));
        sample.Classes.Add(new TestClass("UDerived", "UBase", [], [new TestFunction(ScriptClass.InitDefaults, derivedDefaults)], [new TestFunction("UDerived", derivedCode)]));
        sample.Classes.Add(new TestClass("UBase", "UObject", [], [], [new TestFunction("UBase", baseCode)]));
        sample.Functions[0x9500] = ("Square", false, 0, 1);
        ScriptCache cache = sample.Parse();

        ScriptClassDefaults defaults = ScriptClassDefaults.Of(cache, "UDerived");

        Assert.Equal([(0x10, 9), (0x14, 5), (0x18, 12)], defaults.Stores.Select(pair => (pair.Key, pair.Value.Constant!.AsInt32)));
        Assert.Equal(9, defaults.Constant(0x10)!.AsInt32);
        Assert.Equal("Square", Assert.Single(defaults.Calls).Name);
        Assert.True(defaults.IsComplete);
        Assert.False(defaults.HasBranches);
    }

    private static ScriptStatements Evaluate(ScriptCache cache, uint[] code)
    {
        return ScriptStatements.Evaluate(cache, new ScriptFunction("Test", code));
    }

    private static ScriptCache Cache()
    {
        SyntheticScriptCache cache = new();
        cache.Types[WeightType] = ("FWeight", []);
        cache.Types[ConfigType] = ("FPromptConfig", []);
        cache.Functions[0x9100] = ("FWeight", true, WeightType, 2);
        cache.Functions[0x9200] = ("FPromptConfig", true, ConfigType, 3);
        cache.Functions[0x9300] = ("Add", true, ArrayType, 1);
        cache.Functions[0x9500] = ("Square", false, 0, 1);
        cache.Functions[0x9600] = ("SetDistance", true, 0x7600, 1);
        cache.Functions[0x9700] = ("SetDormancy", true, 0x7600, 1);
        cache.FunctionIds[77] = 0x9100;
        cache.FunctionIds[78] = 0x9200;
        cache.GlobalNames[0x6000] = "Standard";
        return cache.Parse();
    }
}
