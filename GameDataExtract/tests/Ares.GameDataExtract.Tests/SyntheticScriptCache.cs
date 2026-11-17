using System.Text;
using Ares.GameDataExtract.Scripts;

namespace Ares.GameDataExtract.Tests;

/// <summary>Made-up script declarations for <see cref="SyntheticScriptCache"/>.</summary>
internal sealed record TestFunction(string Name, uint[] Bytecode, string? UnrealName = null);

internal sealed record TestProperty(string Name, ScriptDataType Type, uint Flags);

internal sealed record TestClass(string Name, string? UnrealSuper, TestProperty[] Properties, TestFunction[]? Methods = null, TestFunction[]? Constructors = null);

internal sealed record TestGlobal(string Name, string Namespace, ulong? Constant = null, TestFunction? Initializer = null);

/// <summary>
/// Writes a tiny precompiled script cache in the game's format (revision 4): one module, then the reference tables.
/// </summary>
internal sealed class SyntheticScriptCache
{
    public List<TestClass> Classes { get; } = [];

    public List<(string Name, string[] Names, int[] Values)> Enums { get; } = [];

    public List<TestGlobal> Globals { get; } = [];

    public Dictionary<long, (string Name, ScriptDataType[] Subtypes)> Types { get; } = [];

    public Dictionary<int, long> TypeIds { get; } = [];

    public Dictionary<long, (string Name, bool IsMethod, long ObjectType, int ParameterCount)> Functions { get; } = [];

    public Dictionary<int, long> FunctionIds { get; } = [];

    public Dictionary<long, string> GlobalNames { get; } = [];

    /// <summary>Members the bytecode refers to: name, declaring type id, offset.</summary>
    public List<(string Name, int TypeId, int Offset)> Properties { get; } = [];

    public int Revision { get; init; } = ScriptCacheReader.Revision;

    /// <summary>The bytecode of a sequence of instructions: opcode name, 16-bit argument, then further words.</summary>
    public static uint[] Code(params (string Name, int Word, uint[] Words)[] instructions)
    {
        return [.. instructions.SelectMany(instruction => (uint[])[(uint)OpcodeOf(instruction.Name) | ((uint)(ushort)instruction.Word << 16), .. instruction.Words])];
    }

    public static (string, int, uint[]) Op(string name, int word = 0, params uint[] words) => (name, word, words);

    public static uint[] Quad(ulong value) => [(uint)value, (uint)(value >> 32)];

    public static uint[] Quad(double value) => Quad((ulong)BitConverter.DoubleToInt64Bits(value));

    public static int OpcodeOf(string name) => Enumerable.Range(0, ScriptOpcodes.Count).Single(code => ScriptOpcodes.Find(code)!.Name == name);

    public ScriptCache Parse() => ScriptCacheReader.Parse(Write());

    public byte[] Write()
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        writer.Write(new byte[ScriptCacheReader.HeaderSize]);
        writer.Write(Revision);
        writer.Write(1);
        byte[] key = Encoding.UTF8.GetBytes("module-key\0");
        writer.Write(key.Length);
        writer.Write(key);

        Text(writer, "Test.Module");
        writer.Write(0); // free functions
        Items(writer, Classes, WriteClass);
        Items(writer, Enums, (w, e) =>
        {
            Text(w, e.Name);
            Text(w, "");
            Items(w, e.Names, Text);
            Items(w, e.Values, (x, v) => x.Write(v));
        });
        Items(writer, Globals, WriteGlobal);
        Text(writer, "");
        writer.Write(0);
        writer.Write(0);
        Text(writer, "Test/Module.as");
        writer.Write(0);

        Items(writer, [.. Types], (w, t) =>
        {
            w.Write(t.Key);
            Text(w, t.Value.Name);
            Text(w, "");
            Text(w, "");
            Items(w, t.Value.Subtypes, DataType);
        });
        Items(writer, [.. TypeIds], (w, t) =>
        {
            w.Write(t.Key);
            w.Write(t.Value);
        });
        Items(writer, [.. Functions], (w, f) =>
        {
            w.Write(f.Key);
            Text(w, f.Value.Name);
            Text(w, "");
            Text(w, "");
            w.Write(0);
            w.Write(f.Value.IsMethod ? 1 : 0);
            w.Write(f.Value.ObjectType);
            Items(w, Enumerable.Repeat(new ScriptDataType(0, 68), f.Value.ParameterCount).ToArray(), DataType);
            DataType(w, new ScriptDataType(0, 82));
        });
        Items(writer, [.. FunctionIds], (w, f) =>
        {
            w.Write(f.Key);
            w.Write(f.Value);
        });
        Items(writer, [.. GlobalNames], (w, g) =>
        {
            w.Write(g.Key);
            Text(w, g.Value);
            Text(w, "");
            Text(w, "");
            w.Write(0);
        });
        Items(writer, ["Name"], Text);
        Items(writer, Properties, (w, p) =>
        {
            w.Write((2 * (((long)p.Offset << 32) | (uint)p.TypeId)) + 1);
            Text(w, p.Name);
            w.Write(p.TypeId);
        });
        writer.Flush();
        return stream.ToArray();
    }

    private static void WriteClass(BinaryWriter writer, TestClass type)
    {
        Text(writer, type.Name);
        Text(writer, "");
        writer.Write(0u);
        Items(writer, type.Properties, (w, p) =>
        {
            Text(w, p.Name);
            DataType(w, p.Type);
            w.Write(p.Flags);
            if ((p.Flags & ScriptProperty.ReflectedFlag) != 0)
            {
                Items(w, ["Category"], Text);
                Items(w, ["Test"], Text);
                if ((p.Flags & ScriptProperty.ReplicatedFlag) != 0)
                {
                    w.Write((byte)0);
                }
            }
        });
        Items(writer, type.Methods ?? [], WriteFunction);
        writer.Write(0); // method table
        writer.Write(0L); // base type
        writer.Write(0L); // shadow type
        Items(writer, type.Constructors ?? [], WriteFunction);
        writer.Write(0); // factories
        writer.Write(0); // behaviour keys
        writer.Write(0); // behaviours
        writer.Write(0); // behaviour kinds
        writer.Write(type.UnrealSuper is null ? 0 : 1);
        if (type.UnrealSuper is not null)
        {
            Text(writer, type.UnrealSuper);
            Text(writer, type.UnrealSuper);
            writer.Write(new byte[7 * sizeof(int)]);
            Text(writer, "");
            Text(writer, "");
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            Text(writer, "");
        }
    }

    private static void WriteGlobal(BinaryWriter writer, TestGlobal global)
    {
        Text(writer, global.Name);
        Text(writer, global.Namespace);
        DataType(writer, new ScriptDataType(0, 81));
        writer.Write(0); // not a plain default
        writer.Write(global.Constant is null ? 0 : 1);
        if (global.Constant is { } constant)
        {
            writer.Write(constant);
            return;
        }

        writer.Write(0);
        WriteFunction(writer, global.Initializer!);
    }

    private static void WriteFunction(BinaryWriter writer, TestFunction function)
    {
        Text(writer, function.Name);
        Text(writer, "");
        DataType(writer, new ScriptDataType(0, 82));
        Items(writer, [new ScriptDataType(0, 68)], DataType);
        Items(writer, ["value"], Text);
        Items(writer, [0], (w, v) => w.Write(v));
        Items(writer, [""], Text);
        writer.Write(0); // traits
        Items(writer, function.Bytecode, (w, v) => w.Write(v));
        writer.Write(0);
        writer.Write(-1);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(16);
        writer.Write(0u); // function id
        writer.Write(0);
        Items(writer, [1, 0], (w, v) => w.Write(v));
        writer.Write((byte)(function.UnrealName is null ? 0 : 1));
        if (function.UnrealName is not null)
        {
            Text(writer, function.UnrealName);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0u);
        }
    }

    private static void DataType(BinaryWriter writer, ScriptDataType type)
    {
        writer.Write(type.TypeKey);
        writer.Write(type.Packed);
    }

    /// <summary>The cache's string form: a positive byte count, the UTF-8 bytes and a terminating zero.</summary>
    private static void Text(BinaryWriter writer, string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        writer.Write(bytes.Length);
        if (bytes.Length > 0)
        {
            writer.Write(bytes);
            writer.Write((byte)0);
        }
    }

    private static void Items<T>(BinaryWriter writer, IReadOnlyCollection<T> items, Action<BinaryWriter, T> write)
    {
        writer.Write(items.Count);
        foreach (T item in items)
        {
            write(writer, item);
        }
    }
}
