using Ares.Structs.Core.Assets;
using Ares.Structs.Game.Collision;

namespace Ares.Structs.Game.Scripts;

/// <summary>
/// Property chains of script classes and structs as cooked unversioned blocks index them: a type's reflected members in
/// declaration order over its script super class's chain, ending at its native super class: a native class or struct
/// given at construction continues with its chain, <c>USceneComponent</c> with the scene members the readers know
/// (<see cref="SceneComponentMembers"/>: attach parent, socket and relative transform), and any other native class ends
/// the chain (so a block that stores a native member cannot be read). Member types map from
/// <see cref="ScriptMemberType"/>: enums are one byte, or a name inside a container; structs resolve to an
/// <see cref="EngineStructs"/> struct, a given native struct, a script struct, or an unknown struct; a delegate is its
/// signature (a struct without members); unknown types are unknown structs, so only a block storing one fails. Results
/// are cached per type.
/// </summary>
public sealed class ScriptTypeSchemas
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

    private static readonly Dictionary<string, StructSchema> s_engineStructs = EngineStructs.ByPath
        .ToDictionary(pair => "F" + pair.Key[(pair.Key.LastIndexOf('.') + 1)..], pair => pair.Value, StringComparer.Ordinal);

    private static readonly StructSchema s_scene = SceneSchema();

    private readonly IReadOnlyDictionary<string, ScriptTypeDefinition> _types;
    private readonly IReadOnlyDictionary<string, StructSchema> _natives;
    private readonly Dictionary<string, StructSchema> _built = new(StringComparer.Ordinal);

    /// <summary>
    /// Chains of the script types of the current game build (<see cref="ScriptTypes.ByName"/>), over the native classes
    /// and structs <paramref name="natives"/> names (with their prefix, e.g. <c>UItemDataAssetBaseNative</c> or
    /// <c>FWeaponStatModifier</c>).
    /// </summary>
    public ScriptTypeSchemas(IReadOnlyDictionary<string, StructSchema>? natives = null)
        : this(ScriptTypes.ByName, natives)
    {
    }

    /// <summary>Chains of the script types <paramref name="types"/> (by name with prefix), over <paramref name="natives"/>.</summary>
    public ScriptTypeSchemas(IReadOnlyDictionary<string, ScriptTypeDefinition> types, IReadOnlyDictionary<string, StructSchema>? natives = null)
    {
        _types = types;
        _natives = natives ?? new Dictionary<string, StructSchema>(StringComparer.Ordinal);
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
            if (_types.ContainsKey(prefix + name))
            {
                return Of(prefix + name);
            }
        }

        return null;
    }

    /// <summary>The chain of script class or struct <paramref name="name"/> (with its prefix).</summary>
    /// <exception cref="InvalidDataException">No script type has that name, or its types nest too deeply.</exception>
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

        ScriptTypeDefinition type = _types.GetValueOrDefault(name) ?? throw new InvalidDataException($"No script type {name}");
        StructSchema? super = type.Super switch
        {
            null => null,
            { } parent when _types.ContainsKey(parent) => Build(parent, depth + 1),
            { } parent => _natives.GetValueOrDefault(parent) ?? (parent == SceneComponentClass ? s_scene : null),
        };
        PropertyField[] members = [.. type.Members.Select(member => new PropertyField(member.Name, TypeOf(member.Type, depth, element: false)))];
        StructSchema schema = new(name[1..], members, super);
        _built[name] = schema;
        return schema;
    }

    /// <summary>The stored type of a member, or of a container element when <paramref name="element"/> is set (enums are names there).</summary>
    private PropertyType TypeOf(ScriptMemberType type, int depth, bool element)
    {
        return type.Kind switch
        {
            PropertyKind.Bool => PropertyType.Bool,
            PropertyKind.Byte => PropertyType.Byte,
            PropertyKind.Int8 => PropertyType.Int8,
            PropertyKind.Int16 => PropertyType.Int16,
            PropertyKind.UInt16 => PropertyType.UInt16,
            PropertyKind.Int32 => PropertyType.Int32,
            PropertyKind.UInt32 => PropertyType.UInt32,
            PropertyKind.Int64 => PropertyType.Int64,
            PropertyKind.UInt64 => PropertyType.UInt64,
            PropertyKind.Float => PropertyType.Float,
            PropertyKind.Double => PropertyType.Double,
            PropertyKind.Name => PropertyType.Name,
            PropertyKind.String => PropertyType.String,
            PropertyKind.Text => PropertyType.Text,
            PropertyKind.Object => PropertyType.Object,
            PropertyKind.SoftObject => PropertyType.SoftObject,
            PropertyKind.Enum => element ? PropertyType.EnumOf(PropertyType.Name) : PropertyType.ByteEnum,
            PropertyKind.Array => PropertyType.ArrayOf(TypeOf(type.Inner!, depth, element: true)),
            PropertyKind.Set => PropertyType.SetOf(TypeOf(type.Inner!, depth, element: true)),
            PropertyKind.Map => PropertyType.MapOf(TypeOf(type.Inner!, depth, element: true), TypeOf(type.Value!, depth, element: true)),
            PropertyKind.Delegate => PropertyType.StructOf(Signature(type.TypeName!)),
            PropertyKind.Struct when type.IsUnknown => PropertyType.StructOf(StructSchema.UnknownStruct(type.TypeName!)),
            PropertyKind.Struct => PropertyType.StructOf(StructNamed(type.TypeName!, depth)),
            PropertyKind.MulticastDelegate or PropertyKind.Opaque => throw NotAMemberKind(type),
            _ => throw NotAMemberKind(type),
        };
    }

    private static ArgumentOutOfRangeException NotAMemberKind(ScriptMemberType type)
    {
        return new ArgumentOutOfRangeException(nameof(type), type.Kind, "Not a script member kind");
    }

    private StructSchema StructNamed(string name, int depth)
    {
        return s_engineStructs.TryGetValue(name, out StructSchema? engine) ? engine
            : _natives.TryGetValue(name, out StructSchema? native) ? native
            : _types.ContainsKey(name) ? Build(name, depth + 1)
            : StructSchema.UnknownStruct(name[1..]);
    }

    /// <summary>A delegate signature as a struct without members (an unknown struct when the name has no <c>F</c> prefix).</summary>
    private static StructSchema Signature(string name)
    {
        return name is ['F', _, ..] ? new StructSchema(name[1..], []) : StructSchema.UnknownStruct(name);
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
