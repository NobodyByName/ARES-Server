using System.Buffers.Binary;
using System.Text;
using Ares.Common.Archives;
using Ares.GameDataExtract.Archives;
using Ares.GameDataExtract.Scripts;
using static Ares.GameDataExtract.Tests.SyntheticScriptCache;

namespace Ares.GameDataExtract.Tests;

public sealed class ScriptCacheTests
{
    private const uint Reflected = ScriptProperty.ReflectedFlag;
    private const long MapType = 0x7000;
    private const long ActorType = 0x7100;
    private static readonly ScriptDataType s_double = new(0, 81);
    private static readonly ScriptDataType s_int = new(0, 68);

    [Fact]
    public void Classes_keep_members_in_declaration_order_with_their_flags()
    {
        ScriptCache cache = Sample().Parse();

        ScriptClass type = cache.Class("UWidgetTweakables");

        Assert.Equal(["Weight", "Hidden", "Count", "Map"], type.Properties.Select(property => property.Name));
        Assert.Equal(["Weight", "Count", "Map"], type.ReflectedProperties.Select(property => property.Name));
        Assert.True(type.Properties[2].IsReplicated);
        Assert.Equal("double", cache.TypeName(type.Properties[0].Type));
        Assert.Equal("int", cache.TypeName(type.Properties[2].Type));
        Assert.Equal("TMap", cache.TypeName(type.Properties[3].Type));
        Assert.Equal(["int", "double"], cache.Subtypes(type.Properties[3].Type).Select(cache.TypeName));
        Assert.Equal("UBaseTweakables", type.UnrealSuper);
    }

    [Fact]
    public void A_class_chain_runs_up_the_script_superclasses_to_the_native_one()
    {
        ScriptCache cache = Sample().Parse();

        IReadOnlyList<ScriptClass> chain = cache.Chain("UWidgetTweakables", out string? native);

        Assert.Equal(["UWidgetTweakables", "UBaseTweakables"], chain.Select(type => type.Name));
        Assert.Equal("UObject", native);
        Assert.Equal(["Weight", "Count", "Map", "Base"], cache.ReflectedChain("UWidgetTweakables", out _).Select(property => property.Name));
    }

    [Fact]
    public void Enums_globals_and_member_offsets_are_read()
    {
        ScriptCache cache = Sample().Parse();

        ScriptEnum method = cache.Enum("EGatheringMethod");
        Assert.Equal(["None", "WithinSphere"], method.Names);
        Assert.True(method.IsSequential);
        Assert.Equal(0x4010000000000000UL, cache.GlobalsIn("Presets").Single(global => global.Name == "Constant").Constant);
        Assert.Equal(new Dictionary<string, int> { ["Weight"] = 0xA0, ["Map"] = 0xD0 }, cache.MemberOffsets("UWidgetTweakables"));
        Assert.Equal("Add", cache.FunctionByKey(0x9000)!.Name);
        Assert.Equal("FWeight", cache.FunctionById(77)!.Name);
        Assert.Equal("UWidgetTweakables", cache.TypeById(11)!.Name);
    }

    [Fact]
    public void Missing_and_repeated_names_fail_loudly()
    {
        SyntheticScriptCache sample = Sample();
        sample.Classes.Add(new TestClass("UBaseTweakables", null, []));
        ScriptCache cache = sample.Parse();

        Assert.Contains("No script class UMissing", Assert.Throws<InvalidDataException>(() => cache.Class("UMissing")).Message, StringComparison.Ordinal);
        Assert.Contains("Several", Assert.Throws<InvalidDataException>(() => cache.Class("UBaseTweakables")).Message, StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => cache.Enum("EMissing"));
        Assert.False(cache.TryGetClass("UBaseTweakables", out _));
    }

    [Fact]
    public void Wrapped_cache_is_identified_before_reading_the_inner_revision()
    {
        byte[] data = Sample().Write();
        "ASPJ"u8.CopyTo(data);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), 1);
        Assert.Contains("ASPJ v1", Assert.Throws<InvalidDataException>(() => ScriptCacheReader.Parse(data)).Message, StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => ScriptCacheReader.Parse(data.AsSpan(0, 7)));
    }

    [Fact]
    public void Another_revision_trailing_bytes_and_truncation_are_rejected()
    {
        Assert.Contains("revision 5", Assert.Throws<InvalidDataException>(() => new SyntheticScriptCache { Revision = 5 }.Parse()).Message, StringComparison.Ordinal);
        byte[] data = Sample().Write();
        Assert.Contains("after its reference tables", Assert.Throws<InvalidDataException>(() => ScriptCacheReader.Parse([.. data, 0])).Message, StringComparison.Ordinal);
        Assert.Throws<InvalidDataException>(() => ScriptCacheReader.Parse(data.AsSpan(0, data.Length - 3)));
    }

    [Fact]
    public void Negative_string_lengths_are_utf16_with_their_terminator()
    {
        byte[] data = new SyntheticScriptCache().Write();
        // The module name "Test.Module" follows the header, revision, module count and the module key.
        int at = ScriptCacheReader.HeaderSize + 8 + 4 + "module-key\0".Length;
        byte[] utf16 = [.. BitConverter.GetBytes(-12), .. Encoding.Unicode.GetBytes("Test.Module\0")];
        byte[] patched = [.. data.AsSpan(0, at), .. utf16, .. data.AsSpan(at + 4 + "Test.Module".Length + 1)];

        Assert.Equal("Test.Module", ScriptCacheReader.Parse(patched).Modules[0].Name);
    }

    [Fact]
    public void The_cache_is_read_from_its_pak_entry()
    {
        string directory = Directory.CreateTempSubdirectory("ares-script-cache-").FullName;
        try
        {
            string path = Path.Combine(directory, "test.pak");
            SyntheticPak.Write(path, PakFile.InstallMountPrefix, [new SyntheticPakEntry(ScriptCacheReader.CachePath, Sample().Write())]);
            using PakFile pak = PakFile.Open(path, new ArchiveKeys(SyntheticArchives.PakKey, SyntheticArchives.ContainerKey));

            ScriptCache cache = ScriptCache.Read(pak);

            Assert.Equal("Test.Module", Assert.Single(cache.Modules).Name);
            Assert.Equal(0.25, ScriptClassDefaults.Of(cache, "UWidgetTweakables").Constant(0xA0)!.AsDouble);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Bytecode_splits_into_instructions_by_operand_layout()
    {
        uint[] code = Code(Op("SetV8", 2, Quad(0.5)), Op("LoadThisR", 0xA0, 11), Op("WRTV8", 2), Op("ALLOC", 0, [.. Quad(0x55UL), 77]), Op("RET"));

        IReadOnlyList<ScriptInstruction> instructions = ScriptDisassembler.Disassemble(code, "test");

        Assert.Equal(["SetV8", "LoadThisR", "WRTV8", "ALLOC", "RET"], instructions.Select(instruction => instruction.Name));
        Assert.Equal([0, 3, 5, 6, 10], instructions.Select(instruction => instruction.Position));
        Assert.Equal(2, instructions[0].WordArgument);
        Assert.Equal(0.5, BitConverter.Int64BitsToDouble((long)instructions[0].QuadWord));
        Assert.Equal(0xA0, instructions[1].WordArgument);
        Assert.Equal(77u, instructions[3].DoubleWord(2));
    }

    [Fact]
    public void Undefined_opcodes_and_cut_instructions_are_rejected()
    {
        Assert.Contains("undefined opcode 250", Assert.Throws<InvalidDataException>(() => ScriptDisassembler.Disassemble([250], "f")).Message, StringComparison.Ordinal);
        Assert.Contains("does not fit", Assert.Throws<InvalidDataException>(() => ScriptDisassembler.Disassemble(Code(Op("SetV8", 2, 0)), "f")).Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A class <c>UWidgetTweakables : UBaseTweakables : UObject</c> with a constructor that stores a double and an
    /// <c>__InitDefaults</c> that adds two map entries, an enum and two globals.
    /// </summary>
    internal static SyntheticScriptCache Sample()
    {
        SyntheticScriptCache cache = new();
        uint[] constructor = Code(Op("SetV8", 2, Quad(0.25)), Op("LoadThisR", 0xA0, 11), Op("WRTV8", 2), Op("RET"));
        uint[] defaults = Code(
            Op("SetV8", 2, Quad(1.5)), Op("PSF", 2), Op("SetV4", 3, 7), Op("PSF", 3), Op("PshThisMemRef", 0xD0, 11), Op("CALLSYS", 0, Quad(0x9000UL)),
            Op("RET"));
        cache.Classes.Add(new TestClass("UWidgetTweakables", "UBaseTweakables",
            [
                new TestProperty("Weight", s_double, Reflected),
                new TestProperty("Hidden", s_double, 0),
                new TestProperty("Count", s_int, Reflected | ScriptProperty.ReplicatedFlag),
                new TestProperty("Map", new ScriptDataType(MapType, 5), Reflected),
            ],
            [new TestFunction(ScriptClass.InitDefaults, defaults)],
            [new TestFunction("UWidgetTweakables", constructor, "WidgetTweakables")]));
        cache.Classes.Add(new TestClass("UBaseTweakables", "UObject", [new TestProperty("Base", s_int, Reflected)]));
        cache.Enums.Add(("EGatheringMethod", ["None", "WithinSphere"], [0, 1]));
        cache.Globals.Add(new TestGlobal("Constant", "Presets", 0x4010000000000000UL));
        cache.Types[MapType] = ("TMap", [s_int, s_double]);
        cache.Types[ActorType] = ("UWidgetTweakables", []);
        cache.TypeIds[11] = ActorType;
        cache.Functions[0x9000] = ("Add", true, MapType, 2);
        cache.Functions[0x9100] = ("FWeight", true, 0x7200, 2);
        cache.FunctionIds[77] = 0x9100;
        cache.Properties.Add(("Weight", 11, 0xA0));
        cache.Properties.Add(("Map", 11, 0xD0));
        return cache;
    }
}
