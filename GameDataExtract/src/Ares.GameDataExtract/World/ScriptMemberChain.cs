using Ares.GameDataExtract.Scripts;
using Ares.Structs.Game.Scripts;

namespace Ares.GameDataExtract.World;

/// <summary>
/// The script part of a cooked object's unversioned property chain, from the script layouts: the reflected members of a
/// script class and its script superclasses, derived class first. A member's chain index is its position here; the
/// native superclass's members follow at <see cref="Count"/>.
/// </summary>
internal sealed class ScriptMemberChain
{
    private readonly string _name;
    private readonly IReadOnlyList<string> _members;

    private ScriptMemberChain(string name, IReadOnlyList<string> members)
    {
        _name = name;
        _members = members;
    }

    /// <summary>Number of script members (the chain index of the first native member).</summary>
    public int Count => _members.Count;

    /// <summary>
    /// The chain of script class <paramref name="name"/>, whose own reflected members must be exactly
    /// <paramref name="ownMembers"/> in that order.
    /// </summary>
    /// <exception cref="InvalidDataException">The class is missing, or its members changed.</exception>
    public static ScriptMemberChain Of(ScriptLayouts layouts, string name, params IReadOnlyList<string> ownMembers)
    {
        ExpectReflected(layouts, name, ownMembers);
        return new ScriptMemberChain(name, [.. layouts.ReflectedChain(name, out _).Select(member => member.Name)]);
    }

    /// <summary>
    /// The chain of script class <paramref name="name"/>, whose own reflected members must start with
    /// <paramref name="leadingMembers"/> in that order.
    /// </summary>
    /// <exception cref="InvalidDataException">The class is missing, or its first members changed.</exception>
    public static ScriptMemberChain OfLeading(ScriptLayouts layouts, string name, params IReadOnlyList<string> leadingMembers)
    {
        ExpectLeading(layouts, name, leadingMembers);
        return new ScriptMemberChain(name, [.. layouts.ReflectedChain(name, out _).Select(member => member.Name)]);
    }

    /// <summary>Checks that the reflected members script class or struct <paramref name="name"/> declares are exactly <paramref name="expected"/>.</summary>
    /// <exception cref="InvalidDataException">The class is missing, or its members differ.</exception>
    public static void ExpectReflected(ScriptLayouts layouts, string name, params IReadOnlyList<string> expected)
    {
        string[] found = [.. layouts.Type(name).Members.Select(member => member.Name)];
        if (!found.SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new InvalidDataException($"{name} members are ({string.Join(", ", found)}), expected ({string.Join(", ", expected)})");
        }
    }

    /// <summary>Checks that the reflected members script class or struct <paramref name="name"/> declares start with <paramref name="expected"/>.</summary>
    /// <exception cref="InvalidDataException">The class is missing, or its first members differ.</exception>
    private static void ExpectLeading(ScriptLayouts layouts, string name, params IReadOnlyList<string> expected)
    {
        string[] found = [.. layouts.Type(name).Members.Take(expected.Count).Select(member => member.Name)];
        if (!found.SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new InvalidDataException($"{name} members start ({string.Join(", ", found)}), expected ({string.Join(", ", expected)})");
        }
    }

    /// <summary>Checks that script enum <paramref name="name"/> has exactly the entries <paramref name="expected"/>, valued 0, 1, 2, ...</summary>
    /// <exception cref="InvalidDataException">The enum is missing, or its entries differ.</exception>
    public static void ExpectEnum(ScriptLayouts layouts, string name, params IReadOnlyList<string> expected)
    {
        ScriptEnumDefinition found = layouts.Enum(name);
        if (!ScriptLayouts.IsSequential(found) || !found.Names.SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new InvalidDataException($"{name} entries are ({string.Join(", ", found.Names)}), expected ({string.Join(", ", expected)})");
        }
    }

    /// <summary>The chain index of script member <paramref name="member"/>.</summary>
    /// <exception cref="InvalidDataException">The chain has no such member, or several.</exception>
    public int Index(string member)
    {
        int index = -1;
        for (int i = 0; i < _members.Count; i++)
        {
            if (_members[i] != member)
            {
                continue;
            }

            if (index >= 0)
            {
                throw new InvalidDataException($"The {_name} chain has several members {member}");
            }

            index = i;
        }

        return index >= 0 ? index : throw new InvalidDataException($"The {_name} chain has no member {member}");
    }
}
