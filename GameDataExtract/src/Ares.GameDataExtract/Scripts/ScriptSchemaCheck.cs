using Ares.Structs.Core.Assets;

namespace Ares.GameDataExtract.Scripts;

/// <summary>
/// Checks a property chain written for a script class or struct against the script cache: the type's own members must
/// be its reflected members, by name and in declaration order (a member left unknown matches any name), and so must
/// those of every struct member (or array of structs) whose type is itself a script struct.
/// </summary>
internal static class ScriptSchemaCheck
{
    private const int MaxDepth = 16;

    /// <summary>Checks <paramref name="schema"/>'s own members against script type <paramref name="scriptType"/>.</summary>
    /// <exception cref="InvalidDataException">The type is not in the script cache, or a member differs.</exception>
    public static void OwnMembers(ScriptCache cache, string scriptType, StructSchema schema) => Check(cache, scriptType, schema, 0);

    /// <summary>
    /// Checks the known members of <paramref name="schema"/>, a chain that may leave members unknown, against the
    /// reflected chain of script class <paramref name="scriptClass"/> (derived class first).
    /// </summary>
    /// <exception cref="InvalidDataException">The class is not in the script cache, or a known member is not at its chain index.</exception>
    public static void Chain(ScriptCache cache, string scriptClass, StructSchema schema)
    {
        IReadOnlyList<ScriptProperty> chain = cache.ReflectedChain(scriptClass, out _);
        for (int index = 0; index < schema.ChainLength; index++)
        {
            if (schema.At(index).Field is { } field && (index >= chain.Count || chain[index].Name != field.Name))
            {
                throw new InvalidDataException($"{scriptClass}: chain index {index} is {(index < chain.Count ? chain[index].Name : "past the chain")}, expected {field.Name}");
            }
        }
    }

    private static void Check(ScriptCache cache, string scriptType, StructSchema schema, int depth)
    {
        if (depth > MaxDepth)
        {
            throw new InvalidDataException($"{scriptType}: structs nest too deeply");
        }

        ScriptProperty[] members = [.. cache.Class(scriptType).ReflectedProperties];
        if (members.Length != schema.Members.Count
            || members.Zip(schema.Members).Any(pair => pair.Second is { } known && known.Name != pair.First.Name))
        {
            throw new InvalidDataException(
                $"{scriptType}: members {string.Join(", ", members.Select(member => member.Name))}, expected {string.Join(", ", schema.Members.Select(member => member?.Name ?? "(unknown)"))}");
        }

        for (int i = 0; i < members.Length; i++)
        {
            PropertyType? type = schema.Members[i]?.Type;
            ScriptDataType scriptMember = members[i].Type;
            if (type is { Kind: PropertyKind.Array, Inner: { } inner } && cache.Subtypes(scriptMember) is [var element])
            {
                type = inner;
                scriptMember = element;
            }

            if (type is { Kind: PropertyKind.Struct, Struct: { Native: NativeLayout.None } nested }
                && cache.TypeName(scriptMember) is { } name && cache.TryGetClass(name, out _))
            {
                Check(cache, name, nested, depth + 1);
            }
        }
    }
}
