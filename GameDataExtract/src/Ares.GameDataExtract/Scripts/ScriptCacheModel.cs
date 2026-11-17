namespace Ares.GameDataExtract.Scripts;

/// <summary>
/// A script data type: <see cref="TypeKey"/> is the cook-time key of a script type reference (0 for a primitive) and
/// <see cref="Packed"/> holds the type token in its low byte and modifiers (reference, const, in/out) above it.
/// </summary>
internal readonly record struct ScriptDataType(long TypeKey, ushort Packed)
{
    /// <summary>Type token: 5 for an object type (named by its type reference) or a primitive (see <see cref="PrimitiveName"/>).</summary>
    public byte Token => (byte)Packed;

    public bool IsPrimitive => TypeKey == 0;

    /// <summary>Name of a primitive type token seen in the cache, or <see langword="null"/>.</summary>
    public static string? PrimitiveName(byte token) => token switch
    {
        65 => "bool",
        68 => "int",
        80 => "float",
        81 => "double",
        82 => "void",
        _ => null,
    };
}

/// <summary>A member of a script class or struct, in declaration order.</summary>
internal sealed record ScriptProperty(string Name, ScriptDataType Type, uint Flags)
{
    /// <summary>Flag of a member reflected to the engine (a <c>UPROPERTY</c>); only those carry metadata.</summary>
    public const uint ReflectedFlag = 1u << 2;

    /// <summary>Flag of a replicated member; a replication condition byte follows its metadata.</summary>
    public const uint ReplicatedFlag = 1u << 12;

    public bool IsReflected => (Flags & ReflectedFlag) != 0;

    public bool IsReplicated => (Flags & ReplicatedFlag) != 0;
}

/// <summary>A compiled script function: its name and bytecode (32-bit words, decoded by <see cref="ScriptDisassembler"/>).</summary>
internal sealed record ScriptFunction(string Name, uint[] Bytecode);

/// <summary>
/// A script class or struct: members in declaration order, methods, constructors and the name of its engine
/// superclass (<see cref="UnrealSuper"/>, a script class or a native one; <see langword="null"/> for a plain struct).
/// </summary>
internal sealed record ScriptClass(
    string Name,
    IReadOnlyList<ScriptProperty> Properties,
    IReadOnlyList<ScriptFunction> Methods,
    IReadOnlyList<ScriptFunction> Constructors,
    string? UnrealSuper)
{
    /// <summary>Name of the method holding the class's default statements (assignments in the class body).</summary>
    public const string InitDefaults = "__InitDefaults";

    /// <summary>Reflected members, in declaration order.</summary>
    public IEnumerable<ScriptProperty> ReflectedProperties => Properties.Where(property => property.IsReflected);

    /// <summary>The methods named <paramref name="name"/>.</summary>
    public IEnumerable<ScriptFunction> MethodsNamed(string name) => Methods.Where(method => method.Name == name);
}

/// <summary>A script enum: entry names and their values, in declaration order.</summary>
internal sealed record ScriptEnum(string Name, IReadOnlyList<string> Names, IReadOnlyList<int> Values)
{
    /// <summary>Whether the values are 0, 1, 2, ... in order.</summary>
    public bool IsSequential => Values.Select((value, index) => value == index).All(match => match);
}

/// <summary>
/// A script global variable: a cooked constant (<see cref="Constant"/>, raw 64 bits) or an initializer function, or
/// neither for a variable with a default value.
/// </summary>
internal sealed record ScriptGlobal(string Name, string Namespace, ulong? Constant, ScriptFunction? Initializer);

/// <summary>One script module (a script file): its classes, enums and globals (free functions are not kept).</summary>
internal sealed record ScriptModule(string Name, IReadOnlyList<ScriptClass> Classes, IReadOnlyList<ScriptEnum> Enums, IReadOnlyList<ScriptGlobal> Globals);

/// <summary>A type the bytecode refers to (by cook-time key): its name and template subtypes.</summary>
internal sealed record ScriptTypeReference(string Name, IReadOnlyList<ScriptDataType> Subtypes);

/// <summary>
/// A function the bytecode calls: name, whether it is a method (called on an object, of type key
/// <see cref="ObjectType"/>) and its parameter types.
/// </summary>
internal sealed record ScriptFunctionReference(string Name, bool IsMethod, long ObjectType, IReadOnlyList<ScriptDataType> Parameters);

/// <summary>
/// A class member the bytecode refers to: its name, the type id of the class that declares it and its byte offset in
/// the script object (the offset <c>LoadThisR</c> and <c>PshThisMemRef</c> instructions carry).
/// </summary>
internal sealed record ScriptPropertyReference(string Name, int TypeId, int Offset);
