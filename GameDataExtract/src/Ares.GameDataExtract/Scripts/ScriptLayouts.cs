using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Scripts;

namespace Ares.GameDataExtract.Scripts;

/// <summary>
/// The script classes, structs and enums cooked blocks are laid out by: each type's Unreal super class and reflected
/// members, enums with their entries, lookups by name and script path, and the property chains
/// <see cref="ScriptTypeSchemas"/> builds from them. <see cref="Current"/> holds the current game build's
/// (<see cref="ScriptTypes"/>). Lookups that must find a type throw <see cref="InvalidDataException"/> naming it.
/// </summary>
internal sealed class ScriptLayouts
{
    /// <summary>Package of the script classes in script paths, for example <c>/Script/Angelscript.ItemDataAsset</c>.</summary>
    public const string ScriptPackage = ScriptTypeSchemas.ScriptPackage;

    private const int MaxChainLength = 256;

    private readonly IReadOnlyDictionary<string, ScriptTypeDefinition> _types;
    private readonly IReadOnlyDictionary<string, ScriptEnumDefinition> _enums;

    /// <summary>Layouts of <paramref name="types"/> (by name with prefix) and <paramref name="enums"/> (by name).</summary>
    public ScriptLayouts(IReadOnlyDictionary<string, ScriptTypeDefinition> types, IReadOnlyDictionary<string, ScriptEnumDefinition> enums)
    {
        _types = types;
        _enums = enums;
    }

    /// <summary>The current game build's script types and enums.</summary>
    public static ScriptLayouts Current { get; } = new(ScriptTypes.ByName, ScriptTypes.Enums);

    /// <summary>Property chains of these types over the native classes and structs <paramref name="natives"/> names (with prefix).</summary>
    public ScriptTypeSchemas Schemas(IReadOnlyDictionary<string, StructSchema>? natives = null) => new(_types, natives);

    /// <summary>Whether a script class or struct is named <paramref name="name"/> (with its prefix).</summary>
    public bool Contains(string name) => _types.ContainsKey(name);

    /// <summary>The script class or struct named <paramref name="name"/> (with its prefix).</summary>
    /// <exception cref="InvalidDataException">No script type has that name.</exception>
    public ScriptTypeDefinition Type(string name) =>
        _types.GetValueOrDefault(name) ?? throw new InvalidDataException($"No script class {name}");

    /// <summary>The script enum named <paramref name="name"/>.</summary>
    /// <exception cref="InvalidDataException">No script enum has that name.</exception>
    public ScriptEnumDefinition Enum(string name) =>
        _enums.GetValueOrDefault(name) ?? throw new InvalidDataException($"No script enum {name}");

    /// <summary>Whether the values of <paramref name="type"/> are 0, 1, 2, … in order.</summary>
    public static bool IsSequential(ScriptEnumDefinition type) => type.Values.Select((value, index) => value == index).All(match => match);

    /// <summary>
    /// The script classes from <paramref name="name"/> up its Unreal super classes while they are script classes, derived
    /// class first; <paramref name="nativeSuper"/> is the first super class that is not a script class.
    /// </summary>
    /// <exception cref="InvalidDataException">The chain does not end.</exception>
    public IReadOnlyList<ScriptTypeDefinition> Chain(string name, out string? nativeSuper)
    {
        List<ScriptTypeDefinition> chain = [];
        string? current = name;
        while (current is not null && _types.TryGetValue(current, out ScriptTypeDefinition? type))
        {
            if (chain.Count > MaxChainLength)
            {
                throw new InvalidDataException($"Script class chain of {name} does not end");
            }

            chain.Add(type);
            current = type.Super;
        }

        nativeSuper = current;
        return chain;
    }

    /// <summary>
    /// Reflected members along <paramref name="name"/>'s script class chain, derived class first (each class's own
    /// members in declaration order): a member's chain index is its position here.
    /// </summary>
    public IReadOnlyList<ScriptMemberDefinition> ReflectedChain(string name, out string? nativeSuper) =>
        [.. Chain(name, out nativeSuper).SelectMany(type => type.Members)];

    /// <summary>Whether script class <paramref name="name"/> is <paramref name="ancestor"/> or derives from it (a script or native class).</summary>
    public bool Derives(string name, string ancestor) => Chain(name, out string? native).Any(type => type.Name == ancestor) || native == ancestor;

    /// <summary>
    /// The name (with the first of <paramref name="prefixes"/> that names a script type) of the script class at script path
    /// <paramref name="path"/>, or <see langword="null"/>.
    /// </summary>
    public string? NameOf(string path, params ReadOnlySpan<string> prefixes)
    {
        if (!path.StartsWith(ScriptPackage, StringComparison.Ordinal))
        {
            return null;
        }

        string name = path[ScriptPackage.Length..];
        foreach (string prefix in prefixes)
        {
            if (_types.ContainsKey(prefix + name))
            {
                return prefix + name;
            }
        }

        return null;
    }
}
