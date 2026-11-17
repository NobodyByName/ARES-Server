using Ares.GameDataExtract.Scripts;

namespace Ares.GameDataExtract.World;

/// <summary>The literal member stores of a script class's first constructor, by member offset in ascending order.</summary>
internal static class ScriptConstructorStores
{
    /// <summary>
    /// The first literal stores of <paramref name="name"/>'s first constructor in offset order, checked to store
    /// constants of the sizes <paramref name="sizes"/> (in bytes) into the members <paramref name="members"/> (by name,
    /// one per size); with <paramref name="exact"/> the constructor must store nothing else.
    /// </summary>
    /// <exception cref="InvalidDataException">The constructor is missing or stores other members or sizes.</exception>
    public static IReadOnlyList<ScriptConstant> Of(ScriptCache cache, string name, IReadOnlyList<int> sizes, IReadOnlyList<string> members, bool exact)
    {
        ScriptClass type = cache.Class(name);
        if (type.Constructors.Count == 0)
        {
            throw new InvalidDataException($"{name} has no constructor");
        }

        ScriptStore[] stores = [.. ScriptStatements.Evaluate(cache, type.Constructors[0]).MemberStores.Values.Where(store => store.Constant is not null)];
        IReadOnlyDictionary<string, int> offsets = cache.MemberOffsets(name);
        bool expected = (exact ? stores.Length == sizes.Count : stores.Length >= sizes.Count) && members.Count == sizes.Count
            && stores.Take(sizes.Count).Select(store => store.Constant!.Size).SequenceEqual(sizes)
            && members.Select((member, index) => offsets.TryGetValue(member, out int offset) && offset == stores[index].Offset).All(match => match);
        return expected
            ? [.. stores.Take(sizes.Count).Select(store => store.Constant!)]
            : throw new InvalidDataException($"The {name} constructor stores ({string.Join(", ", stores.Select(store => $"{store.Size} bytes at {store.Offset}"))})");
    }
}
