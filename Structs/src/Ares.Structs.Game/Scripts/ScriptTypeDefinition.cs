namespace Ares.Structs.Game.Scripts;

/// <summary>
/// A script class or struct: its name with prefix (<c>U</c>, <c>A</c> or <c>F</c>), its Unreal super class (a script
/// class or a native one, with prefix; <see langword="null"/> for a struct) and its reflected members in declaration
/// order.
/// </summary>
public sealed record ScriptTypeDefinition(string Name, string? Super, IReadOnlyList<ScriptMemberDefinition> Members);

/// <summary>A reflected member of a script class or struct.</summary>
public sealed record ScriptMemberDefinition(string Name, ScriptMemberType Type);

/// <summary>A script enum: entry names and their values, in declaration order.</summary>
public sealed record ScriptEnumDefinition(string Name, IReadOnlyList<string> Names, IReadOnlyList<long> Values);
