using System.Buffers.Binary;
using System.Text;
using Ares.GameDataExtract.Archives;

namespace Ares.GameDataExtract.Scripts;

/// <summary>
/// Parses the game's precompiled script cache (<c>PrecompiledScript.Cache</c>, revision 4 of the Angelscript plugin's
/// precompiled data): every module, then the reference tables that name the cook-time keys the bytecode uses. The
/// whole file must be consumed. Keys are cook-time identifiers, opaque here.
/// </summary>
internal ref struct ScriptCacheReader
{
    /// <summary>The cache file inside the install (pak entry path, relative to the game folder).</summary>
    public const string CachePath = "PioneerGame/Script/PrecompiledScript.Cache";

    /// <summary>The supported cache revision.</summary>
    public const int Revision = 4;

    /// <summary>Leading bytes before the revision, not interpreted.</summary>
    public const int HeaderSize = 16;

    private const int MaxStringLength = 1_000_000;
    private const int MaxCount = 10_000_000;
    private const int ModuleCountLimit = 100_000;

    /// <summary>The 7 integer flags of a class's engine class description (only skipped here).</summary>
    private const int UnrealClassFlagCount = 7;

    private ByteReader _reader;

    private ScriptCacheReader(ReadOnlySpan<byte> data)
    {
        _reader = new ByteReader(data);
    }

    /// <summary>Parses a whole cache file.</summary>
    /// <exception cref="InvalidDataException">The data is malformed, of another revision or has trailing bytes.</exception>
    public static ScriptCache Parse(ReadOnlySpan<byte> data)
    {
        ScriptCacheReader reader = new(data);
        return reader.ReadCache();
    }

    private ScriptCache ReadCache()
    {
        _reader.Skip(HeaderSize);
        int revision = _reader.ReadInt32();
        if (revision != Revision)
        {
            throw new InvalidDataException($"Script cache revision {revision}, expected {Revision}");
        }

        int moduleCount = _reader.ReadCount(ModuleCountLimit);
        List<ScriptModule> modules = new(moduleCount);
        for (int i = 0; i < moduleCount; i++)
        {
            // The module key (its file name hash) precedes each module.
            _reader.Skip(_reader.ReadCount(_reader.Remaining));
            modules.Add(ReadModule());
        }

        Dictionary<long, ScriptTypeReference> types = ReadMap(static (ref r) => r.ReadInt64(), static (ref r) =>
        {
            string name = r.ReadString();
            r.ReadString(); // module
            r.ReadString(); // namespace
            return new ScriptTypeReference(name, r.ReadArray(ReadDataType));
        });
        Dictionary<int, long> typeIds = ReadMap(static (ref r) => r._reader.ReadInt32(), static (ref r) => r.ReadInt64());
        Dictionary<long, ScriptFunctionReference> functions = ReadMap(static (ref r) => r.ReadInt64(), ReadFunctionReference);
        Dictionary<int, long> functionIds = ReadMap(static (ref r) => r._reader.ReadInt32(), static (ref r) => r.ReadInt64());
        Dictionary<long, string> globals = ReadMap(static (ref r) => r.ReadInt64(), static (ref r) =>
        {
            string name = r.ReadString();
            r.ReadString(); // module
            r.ReadString(); // namespace
            r._reader.ReadInt32(); // string constant
            return name;
        });
        SkipStrings(); // names used by the bytecode
        List<ScriptPropertyReference> properties = ReadArray(static (ref r) =>
        {
            // Key: 2 * (offset << 32 | declaring type id) + 1.
            long key = r.ReadInt64();
            return new ScriptPropertyReference(r.ReadString(), r._reader.ReadInt32(), (int)(key >> 33));
        });
        return _reader.Remaining == 0
            ? new ScriptCache(modules, new ScriptReferences(types, typeIds, functions, functionIds, globals, properties))
            : throw new InvalidDataException($"Script cache has {_reader.Remaining} bytes after its reference tables");
    }

    private ScriptModule ReadModule()
    {
        string name = ReadString();
        ReadArray(ReadFunction); // free functions
        List<ScriptClass> classes = ReadArray(ReadClass);
        List<ScriptEnum> enums = ReadArray(static (ref r) =>
        {
            string enumName = r.ReadString();
            r.ReadString(); // namespace
            return new ScriptEnum(enumName, r.ReadArray(static (ref s) => s.ReadString()),
                r.ReadArray(static (ref s) => s._reader.ReadInt32()));
        });
        List<ScriptGlobal> globals = ReadArray(ReadGlobal);
        ReadString(); // static class name
        SkipStrings(); // events
        SkipStrings(); // delegates
        ReadString(); // file name
        SkipStrings(); // post-init functions
        return new ScriptModule(name, classes, enums, globals);
    }

    private static ScriptFunction ReadFunction(ref ScriptCacheReader r)
    {
        string name = r.ReadString();
        r.ReadString(); // namespace
        ReadDataType(ref r); // return type
        r.ReadArray(ReadDataType); // parameter types
        r.SkipStrings(); // parameter names
        r.SkipInt32Values(); // parameter flags
        r.SkipStrings(); // parameter default expressions
        r._reader.ReadInt32(); // traits
        uint[] bytecode = [.. r.ReadArray(static (ref s) => s._reader.ReadUInt32())];
        r.SkipInt32Values(); // object variable positions in the bytecode
        r._reader.ReadInt32(); // virtual function table slot
        r.SkipInt64Values(); // object variable types
        r.SkipInt32Values(); // object variable offsets
        r._reader.ReadInt32(); // object variables on the heap
        r.SkipInt32Values(); // variable positions
        r.SkipInt32Values(); // variable offsets
        r.SkipInt32Values(); // optional variable info
        r._reader.ReadInt32(); // stack needed
        r._reader.ReadUInt32(); // function id
        r._reader.ReadInt32(); // declaration position
        r.SkipInt32Values(); // line numbers
        if (!r.ReadBoolean())
        {
            return new ScriptFunction(name, bytecode);
        }

        r.ReadString(); // engine name
        r.SkipStrings(); // metadata keys
        r.SkipStrings(); // metadata values
        r._reader.ReadUInt32(); // engine function flags
        return new ScriptFunction(name, bytecode);
    }

    private static ScriptClass ReadClass(ref ScriptCacheReader r)
    {
        string name = r.ReadString();
        r.ReadString(); // namespace
        r._reader.ReadUInt32(); // class flags
        List<ScriptProperty> properties = r.ReadArray(ReadProperty);
        List<ScriptFunction> methods = r.ReadArray(ReadFunction);
        r.SkipInt32Values(); // method table
        r.ReadInt64(); // base type
        r.ReadInt64(); // shadow type
        List<ScriptFunction> constructors = r.ReadArray(ReadFunction);
        r.SkipInt64Values(); // factories
        r.SkipInt64Values(); // behaviour keys
        r.ReadArray(ReadFunction); // behaviours
        r.SkipInt32Values(); // behaviour kinds
        if (r._reader.ReadInt32() == 0)
        {
            return new ScriptClass(name, properties, methods, constructors, null);
        }

        string unrealSuper = r.ReadString();
        r.ReadString(); // code superclass
        r._reader.Skip(UnrealClassFlagCount * sizeof(int));
        r.ReadString(); // config name
        r.ReadString(); // static class name
        r._reader.ReadInt32(); // placeable
        r.SkipStrings(); // metadata keys
        r.SkipStrings(); // metadata values
        r.ReadString(); // composed onto
        return new ScriptClass(name, properties, methods, constructors, unrealSuper);
    }

    private static ScriptProperty ReadProperty(ref ScriptCacheReader r)
    {
        string name = r.ReadString();
        ScriptDataType type = ReadDataType(ref r);
        uint flags = r._reader.ReadUInt32();
        if ((flags & ScriptProperty.ReflectedFlag) == 0)
        {
            return new ScriptProperty(name, type, flags);
        }

        r.SkipStrings(); // metadata keys
        r.SkipStrings(); // metadata values
        if ((flags & ScriptProperty.ReplicatedFlag) != 0)
        {
            r._reader.ReadByte(); // replication condition
        }

        return new ScriptProperty(name, type, flags);
    }

    private static ScriptGlobal ReadGlobal(ref ScriptCacheReader r)
    {
        string name = r.ReadString();
        string ns = r.ReadString();
        ReadDataType(ref r);
        if (r._reader.ReadInt32() != 0)
        {
            return new ScriptGlobal(name, ns, null, null);
        }

        if (r._reader.ReadInt32() != 0)
        {
            return new ScriptGlobal(name, ns, r._reader.ReadUInt64(), null);
        }

        r._reader.ReadInt32(); // initializer kind
        return new ScriptGlobal(name, ns, null, ReadFunction(ref r));
    }

    private static ScriptFunctionReference ReadFunctionReference(ref ScriptCacheReader r)
    {
        string name = r.ReadString();
        r.ReadString(); // module
        r.ReadString(); // namespace
        r._reader.ReadInt32(); // const method
        bool isMethod = r._reader.ReadInt32() != 0;
        long objectType = r.ReadInt64();
        List<ScriptDataType> parameters = r.ReadArray(ReadDataType);
        ReadDataType(ref r); // return type
        return new ScriptFunctionReference(name, isMethod, objectType, parameters);
    }

    private static ScriptDataType ReadDataType(ref ScriptCacheReader r)
    {
        long key = r.ReadInt64();
        return new ScriptDataType(key, BinaryPrimitives.ReadUInt16LittleEndian(r._reader.ReadBytes(sizeof(ushort))));
    }

    private delegate T Item<out T>(ref ScriptCacheReader reader);

    private List<T> ReadArray<T>(Item<T> read)
    {
        int count = _reader.ReadCount(Math.Min(MaxCount, _reader.Remaining));
        List<T> items = new(count);
        for (int i = 0; i < count; i++)
        {
            items.Add(read(ref this));
        }

        return items;
    }

    private Dictionary<TKey, TValue> ReadMap<TKey, TValue>(Item<TKey> readKey, Item<TValue> readValue)
        where TKey : notnull
    {
        int count = _reader.ReadCount(Math.Min(MaxCount, _reader.Remaining));
        Dictionary<TKey, TValue> map = new(count);
        for (int i = 0; i < count; i++)
        {
            TKey key = readKey(ref this);
            if (!map.TryAdd(key, readValue(ref this)))
            {
                throw new InvalidDataException($"Duplicate script reference key {key} before offset {_reader.Position}");
            }
        }

        return map;
    }

    private void SkipInt32Values() => _reader.Skip(checked(_reader.ReadCount(Math.Min(MaxCount, _reader.Remaining)) * sizeof(int)));

    private void SkipInt64Values() => _reader.Skip(checked(_reader.ReadCount(Math.Min(MaxCount, _reader.Remaining)) * sizeof(long)));

    private void SkipStrings()
    {
        int count = _reader.ReadCount(Math.Min(MaxCount, _reader.Remaining));
        for (int i = 0; i < count; i++)
        {
            ReadString();
        }
    }

    private long ReadInt64() => BinaryPrimitives.ReadInt64LittleEndian(_reader.ReadBytes(sizeof(long)));

    private bool ReadBoolean()
    {
        byte value = _reader.ReadByte();
        return value <= 1 ? value == 1 : throw new InvalidDataException($"Boolean {value} at offset {_reader.Position - 1}");
    }

    /// <summary>
    /// A length-prefixed string: a positive length counts UTF-8 bytes before a terminating zero (which follows them),
    /// a negative one UTF-16 code units including the terminator.
    /// </summary>
    private string ReadString()
    {
        int length = _reader.ReadInt32();
        if (length is > MaxStringLength or < -MaxStringLength)
        {
            throw new InvalidDataException($"String length {length} at offset {_reader.Position - sizeof(int)} is too large");
        }

        string text = length switch
        {
            > 0 => Encoding.UTF8.GetString(_reader.ReadBytes(length + 1)),
            < 0 => Encoding.Unicode.GetString(_reader.ReadBytes(-length * sizeof(char))),
            _ => "",
        };
        return text.TrimEnd('\0');
    }
}
