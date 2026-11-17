using System.Diagnostics.CodeAnalysis;
using Ares.GameDataExtract.Archives;

namespace Ares.GameDataExtract.Scripts;

/// <summary>The cache's reference tables: what the cook-time keys and ids in signatures and bytecode name.</summary>
internal sealed record ScriptReferences(
    IReadOnlyDictionary<long, ScriptTypeReference> Types,
    IReadOnlyDictionary<int, long> TypeIds,
    IReadOnlyDictionary<long, ScriptFunctionReference> Functions,
    IReadOnlyDictionary<int, long> FunctionIds,
    IReadOnlyDictionary<long, string> Globals,
    IReadOnlyList<ScriptPropertyReference> Properties);

/// <summary>
/// The game's compiled scripts (the precompiled script cache): script classes with their members in declaration order
/// and their flags, enums, globals and function bytecode, plus lookups by name. Lookups that must find exactly one
/// match throw <see cref="InvalidDataException"/> naming what is missing or ambiguous.
/// </summary>
internal sealed class ScriptCache
{
    private const int MaxChainLength = 256;

    private readonly ILookup<string, ScriptClass> _classes;
    private readonly ILookup<string, ScriptEnum> _enums;
    private readonly ILookup<string, int> _typeIdsByName;
    private readonly ILookup<int, ScriptPropertyReference> _propertiesByType;

    public ScriptCache(IReadOnlyList<ScriptModule> modules, ScriptReferences references)
    {
        Modules = modules;
        References = references;
        _classes = modules.SelectMany(module => module.Classes).ToLookup(type => type.Name, StringComparer.Ordinal);
        _enums = modules.SelectMany(module => module.Enums).ToLookup(type => type.Name, StringComparer.Ordinal);
        _typeIdsByName = references.TypeIds
            .Where(pair => references.Types.ContainsKey(pair.Value))
            .ToLookup(pair => references.Types[pair.Value].Name, pair => pair.Key, StringComparer.Ordinal);
        _propertiesByType = references.Properties.ToLookup(property => property.TypeId);
    }

    public IReadOnlyList<ScriptModule> Modules { get; }

    public ScriptReferences References { get; }

    /// <summary>Reads and parses the cache from the pak holding it.</summary>
    /// <exception cref="InvalidDataException">The pak has no cache, or the cache is malformed.</exception>
    public static ScriptCache Read(PakFile pak)
    {
        return pak.Contains(ScriptCacheReader.CachePath)
            ? ScriptCacheReader.Parse(pak.Read(ScriptCacheReader.CachePath))
            : throw new InvalidDataException($"No {ScriptCacheReader.CachePath} in the pak");
    }

    /// <summary>The script class or struct named <paramref name="name"/> (with its prefix, e.g. <c>U</c>, <c>A</c>, <c>F</c>).</summary>
    /// <exception cref="InvalidDataException">No class or several classes have that name.</exception>
    public ScriptClass Class(string name) => Single(_classes[name], "script class", name);

    /// <summary>The script class named <paramref name="name"/>, when exactly one has that name.</summary>
    public bool TryGetClass(string name, [NotNullWhen(true)] out ScriptClass? type)
    {
        ScriptClass[] found = [.. _classes[name].Take(2)];
        type = found.Length == 1 ? found[0] : null;
        return type is not null;
    }

    /// <summary>The script enum named <paramref name="name"/>.</summary>
    /// <exception cref="InvalidDataException">No enum or several enums have that name.</exception>
    public ScriptEnum Enum(string name) => Single(_enums[name], "script enum", name);

    /// <summary>
    /// The script classes from <paramref name="name"/> up its engine superclasses while they are script classes, derived
    /// class first; <paramref name="nativeSuper"/> is the first superclass that is not a script class.
    /// </summary>
    public IReadOnlyList<ScriptClass> Chain(string name, out string? nativeSuper)
    {
        List<ScriptClass> chain = [];
        string? current = name;
        while (current is not null && TryGetClass(current, out ScriptClass? type))
        {
            if (chain.Count > MaxChainLength)
            {
                throw new InvalidDataException($"Script class chain of {name} does not end");
            }

            chain.Add(type);
            current = type.UnrealSuper;
        }

        nativeSuper = current;
        return chain;
    }

    /// <summary>
    /// Reflected members along <paramref name="name"/>'s script class chain, derived class first (each class's own
    /// members in declaration order).
    /// </summary>
    public IReadOnlyList<ScriptProperty> ReflectedChain(string name, out string? nativeSuper)
    {
        return [.. Chain(name, out nativeSuper).SelectMany(type => type.ReflectedProperties)];
    }

    /// <summary>Byte offsets of the members of script type <paramref name="name"/> that the bytecode refers to, by member name.</summary>
    /// <exception cref="InvalidDataException">The type has no type id, or several.</exception>
    public IReadOnlyDictionary<string, int> MemberOffsets(string name)
    {
        int typeId = Single(_typeIdsByName[name], "script type id of", name);
        Dictionary<string, int> offsets = new(StringComparer.Ordinal);
        foreach (ScriptPropertyReference property in _propertiesByType[typeId])
        {
            offsets[property.Name] = property.Offset;
        }

        return offsets;
    }

    /// <summary>Name of a data type: the referenced type's, a primitive's, or <see langword="null"/> when unknown.</summary>
    public string? TypeName(ScriptDataType type)
    {
        return type.IsPrimitive ? ScriptDataType.PrimitiveName(type.Token) : References.Types.GetValueOrDefault(type.TypeKey)?.Name;
    }

    /// <summary>Template subtypes of a data type (e.g. the element type of a <c>TArray</c>).</summary>
    public IReadOnlyList<ScriptDataType> Subtypes(ScriptDataType type)
    {
        return type.IsPrimitive ? [] : References.Types.GetValueOrDefault(type.TypeKey)?.Subtypes ?? [];
    }

    /// <summary>The type id names a type: its reference, or <see langword="null"/>.</summary>
    public ScriptTypeReference? TypeById(int id)
    {
        return References.TypeIds.TryGetValue(id, out long key) ? References.Types.GetValueOrDefault(key) : null;
    }

    /// <summary>The function a function id (as in <c>CALL</c>) names, or <see langword="null"/>.</summary>
    public ScriptFunctionReference? FunctionById(int id)
    {
        return References.FunctionIds.TryGetValue(id, out long key) ? References.Functions.GetValueOrDefault(key) : null;
    }

    /// <summary>The function a cook-time key (as in <c>CALLSYS</c>) names, or <see langword="null"/>.</summary>
    public ScriptFunctionReference? FunctionByKey(long key) => References.Functions.GetValueOrDefault(key);

    /// <summary>Name of the global a cook-time key (as in <c>PGA</c>) names, or <see langword="null"/>.</summary>
    public string? GlobalByKey(long key) => References.Globals.GetValueOrDefault(key);

    /// <summary>Globals declared in namespace <paramref name="ns"/> (e.g. a struct's named presets).</summary>
    public IEnumerable<ScriptGlobal> GlobalsIn(string ns)
    {
        return Modules.SelectMany(module => module.Globals).Where(global => global.Namespace == ns);
    }

    private static T Single<T>(IEnumerable<T> found, string what, string name)
    {
        using IEnumerator<T> items = found.GetEnumerator();
        if (!items.MoveNext())
        {
            throw new InvalidDataException($"No {what} {name} in the script cache");
        }

        T item = items.Current;
        return items.MoveNext() ? throw new InvalidDataException($"Several of {what} {name} in the script cache") : item;
    }
}
