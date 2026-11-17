using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Collision;

namespace Ares.GameDataExtract.Scripts;

/// <summary>
/// Property chains of script classes and structs as cooked unversioned blocks index them, built from the script cache:
/// a type's reflected members in declaration order over its script super class's chain, ending at its native super
/// class: a native class or struct given at construction continues with its chain, <c>USceneComponent</c> with the scene
/// members the readers know (<see cref="SceneComponentMembers"/>: attach parent, socket and relative transform), and any
/// other native class ends the chain (so a block that stores a native member cannot be read). Member types map from the
/// cache's type references: primitives, names, strings, texts, object references (handles, <c>U…</c>, <c>A…</c>,
/// <c>TSubclassOf</c>, weak pointers), soft pointers, arrays, sets, maps, enums (one byte, or a name inside a
/// container), script structs, and native structs from <see cref="EngineStructs"/> or the given native schemas. A member
/// of an unknown type gets an unknown struct, so only a block storing it fails. Results are cached per type.
/// </summary>
internal sealed class ScriptSchemas
{
    /// <summary>Package of the script classes in script paths, for example <c>/Script/Angelscript.ItemDataAsset</c>.</summary>
    public const string ScriptPackage = "/Script/Angelscript.";

    /// <summary>Scene member holding the attach parent (an object reference).</summary>
    public const string AttachParent = "AttachParent";

    /// <summary>Scene member holding the attach socket name.</summary>
    public const string AttachSocketName = "AttachSocketName";

    /// <summary>Scene member holding the relative location.</summary>
    public const string RelativeLocation = "RelativeLocation";

    /// <summary>Scene member holding the relative rotation.</summary>
    public const string RelativeRotation = "RelativeRotation";

    /// <summary>Scene member holding the relative scale.</summary>
    public const string RelativeScale3D = "RelativeScale3D";

    private const int MaxDepth = 64;
    private const string SceneComponentClass = "USceneComponent";

    // Modifier bit of a handle (an object reference) in ScriptDataType.Packed.
    private const ushort ObjectHandleFlag = 0x400;

    // Primitive type tokens of the script cache (ScriptDataType.Token).
    private const byte BoolToken = 65;
    private const byte Int32Token = 68;
    private const byte Int8Token = 69;
    private const byte Int16Token = 70;
    private const byte Int64Token = 71;
    private const byte ByteToken = 76;
    private const byte UInt16Token = 77;
    private const byte UInt32Token = 78;
    private const byte UInt64Token = 79;
    private const byte FloatToken = 80;
    private const byte DoubleToken = 81;

    private static readonly Dictionary<string, StructSchema> s_engineStructs = EngineStructs.ByPath
        .ToDictionary(pair => "F" + pair.Key[(pair.Key.LastIndexOf('.') + 1)..], pair => pair.Value, StringComparer.Ordinal);

    private static readonly StructSchema s_scene = SceneSchema();

    private readonly ScriptCache _cache;
    private readonly IReadOnlyDictionary<string, StructSchema> _natives;
    private readonly HashSet<string> _enums;
    private readonly Dictionary<string, StructSchema> _built = new(StringComparer.Ordinal);

    /// <summary>
    /// Chains of the script types of <paramref name="cache"/>, over the native classes and structs <paramref name="natives"/>
    /// names (with their prefix, e.g. <c>UItemDataAssetBaseNative</c> or <c>FWeaponStatModifier</c>).
    /// </summary>
    public ScriptSchemas(ScriptCache cache, IReadOnlyDictionary<string, StructSchema>? natives = null)
    {
        _cache = cache;
        _natives = natives ?? new Dictionary<string, StructSchema>(StringComparer.Ordinal);
        _enums = new HashSet<string>(cache.Modules.SelectMany(module => module.Enums).Select(type => type.Name), StringComparer.Ordinal);
    }

    /// <summary>
    /// The chain of the script class or struct at script path <paramref name="path"/> (<see cref="ScriptPackage"/> then
    /// its name without prefix), an engine struct of <see cref="EngineStructs.ByPath"/>, or <see langword="null"/>.
    /// </summary>
    public StructSchema? ForPath(string path)
    {
        if (EngineStructs.ByPath.TryGetValue(path, out StructSchema? engine))
        {
            return engine;
        }

        if (!path.StartsWith(ScriptPackage, StringComparison.Ordinal))
        {
            return null;
        }

        string name = path[ScriptPackage.Length..];
        foreach (string prefix in (ReadOnlySpan<string>)["U", "A", "F"])
        {
            if (_cache.TryGetClass(prefix + name, out _))
            {
                return Of(prefix + name);
            }
        }

        return null;
    }

    /// <summary>The chain of script class or struct <paramref name="name"/> (with its prefix).</summary>
    /// <exception cref="InvalidDataException">The cache has no such type, or several.</exception>
    public StructSchema Of(string name) => Build(name, 0);

    private StructSchema Build(string name, int depth)
    {
        if (_built.TryGetValue(name, out StructSchema? built))
        {
            return built;
        }

        if (depth > MaxDepth)
        {
            throw new InvalidDataException($"Script type {name} nests too deeply");
        }

        ScriptClass type = _cache.Class(name);
        StructSchema? super = type.UnrealSuper switch
        {
            null => null,
            { } parent when _cache.TryGetClass(parent, out _) => Build(parent, depth + 1),
            { } parent => _natives.GetValueOrDefault(parent) ?? (parent == SceneComponentClass ? s_scene : null),
        };
        PropertyField[] members = [.. type.ReflectedProperties.Select(property => new PropertyField(property.Name, TypeOf(property.Type, depth, element: false)))];
        StructSchema schema = new(name[1..], members, super);
        _built[name] = schema;
        return schema;
    }

    /// <summary>The type of a member, or of a container element when <paramref name="element"/> is set (enums are names there).</summary>
    private PropertyType TypeOf(ScriptDataType type, int depth, bool element)
    {
        if (type.IsPrimitive)
        {
            return type.Token switch
            {
                BoolToken => PropertyType.Bool,
                Int32Token => PropertyType.Int32,
                Int8Token => PropertyType.Int8,
                Int16Token => PropertyType.Int16,
                Int64Token => PropertyType.Int64,
                ByteToken => PropertyType.Byte,
                UInt16Token => PropertyType.UInt16,
                UInt32Token => PropertyType.UInt32,
                UInt64Token => PropertyType.UInt64,
                FloatToken => PropertyType.Float,
                DoubleToken => PropertyType.Double,
                _ => PropertyType.StructOf(StructSchema.UnknownStruct($"primitive {type.Token}")),
            };
        }

        if ((type.Packed & ObjectHandleFlag) != 0)
        {
            return PropertyType.Object;
        }

        string name = _cache.TypeName(type) ?? "";
        IReadOnlyList<ScriptDataType> subtypes = _cache.Subtypes(type);
        return name switch
        {
            "FName" => PropertyType.Name,
            "FString" => PropertyType.String,
            "FText" => PropertyType.Text,
            "TArray" when subtypes.Count == 1 => PropertyType.ArrayOf(TypeOf(subtypes[0], depth, element: true)),
            "TSet" when subtypes.Count == 1 => PropertyType.SetOf(TypeOf(subtypes[0], depth, element: true)),
            "TMap" when subtypes.Count == 2 => PropertyType.MapOf(TypeOf(subtypes[0], depth, element: true), TypeOf(subtypes[1], depth, element: true)),
            "TSubclassOf" or "TWeakObjectPtr" => PropertyType.Object,
            "TSoftObjectPtr" or "TSoftClassPtr" => PropertyType.SoftObject,
            ['U' or 'A', _, ..] => PropertyType.Object,
            _ when _enums.Contains(name) || name is ['E', _, ..] => element ? PropertyType.EnumOf(PropertyType.Name) : PropertyType.ByteEnum,
            ['F', _, ..] => PropertyType.StructOf(StructNamed(name, depth)),
            _ => PropertyType.StructOf(StructSchema.UnknownStruct(name)),
        };
    }

    private StructSchema StructNamed(string name, int depth)
    {
        return s_engineStructs.TryGetValue(name, out StructSchema? engine) ? engine
            : _natives.TryGetValue(name, out StructSchema? native) ? native
            : _cache.TryGetClass(name, out _) ? Build(name, depth + 1)
            : StructSchema.UnknownStruct(name[1..]);
    }

    /// <summary>The scene component's chain as far as the readers know it: attach parent, socket and relative transform.</summary>
    private static StructSchema SceneSchema()
    {
        PropertyField?[] members = new PropertyField?[SceneComponentMembers.RelativeScale3D + 1];
        members[SceneComponentMembers.AttachParent] = new PropertyField(AttachParent, PropertyType.Object);
        members[SceneComponentMembers.AttachSocketName] = new PropertyField(AttachSocketName, PropertyType.Name);
        members[SceneComponentMembers.RelativeLocation] = new PropertyField(RelativeLocation, PropertyType.StructOf(EngineStructs.Vector));
        members[SceneComponentMembers.RelativeRotation] = new PropertyField(RelativeRotation, PropertyType.StructOf(EngineStructs.Rotator));
        members[SceneComponentMembers.RelativeScale3D] = new PropertyField(RelativeScale3D, PropertyType.StructOf(EngineStructs.Vector));
        return new StructSchema(SceneComponentMembers.ClassPath, members);
    }
}
