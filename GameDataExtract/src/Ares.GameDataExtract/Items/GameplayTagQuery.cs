using Ares.GameDataExtract.Properties;

namespace Ares.GameDataExtract.Items;

/// <summary>
/// A cooked <c>FGameplayTagQuery</c> (see <see cref="Ares.Structs.Core.Assets.EngineStructs.GameplayTagQuery"/>),
/// evaluated as the engine does: the token stream starts with its version and whether it has a root expression; an
/// expression is its type, then a count and that many tag dictionary indices (tag expressions) or sub-expressions.
/// A tag matches a container holding it or a tag under it, except in the exact variants.
/// </summary>
internal sealed class GameplayTagQuery
{
    private const byte AnyTagsMatch = 1;
    private const byte AllTagsMatch = 2;
    private const byte NoTagsMatch = 3;
    private const byte AnyExprMatch = 4;
    private const byte AllExprMatch = 5;
    private const byte NoExprMatch = 6;
    private const byte AnyTagsExactMatch = 7;
    private const byte AllTagsExactMatch = 8;
    private const int MaxDepth = 64;

    private readonly string[] _tags;
    private readonly byte[] _tokens;

    private GameplayTagQuery(string[] tags, byte[] tokens)
    {
        _tags = tags;
        _tokens = tokens;
    }

    /// <summary>The query a struct member holds.</summary>
    /// <exception cref="InvalidDataException">The member is not a gameplay tag query.</exception>
    public static GameplayTagQuery Of(PropertyBag query)
    {
        string[] tags = [.. query.Get<object[]>("TagDictionary", []).Select(tag => ((PropertyBag)tag).Get("TagName", ""))];
        byte[] tokens = [.. query.Get<object[]>("QueryTokenStream", []).Cast<byte>()];
        return new GameplayTagQuery(tags, tokens);
    }

    /// <summary>Whether a container of <paramref name="tags"/> matches the query; an empty query matches nothing.</summary>
    /// <exception cref="InvalidDataException">The token stream is malformed.</exception>
    public bool Matches(IReadOnlyCollection<string> tags)
    {
        if (_tokens.Length < 2 || _tokens[1] == 0)
        {
            return false;
        }

        int at = 2;
        bool result = Evaluate(tags, ref at, 0);
        return at == _tokens.Length ? result : throw new InvalidDataException($"{_tokens.Length - at} bytes follow a gameplay tag query");
    }

    /// <summary>
    /// The position in the query's tag dictionary of the first tag a container of <paramref name="tags"/> holds (or holds
    /// a tag under), or <see cref="int.MaxValue"/> when it holds none.
    /// </summary>
    public int FirstTagHeld(IReadOnlyCollection<string> tags)
    {
        int index = Array.FindIndex(_tags, tag => tags.Any(own => Holds(own, tag, exact: false)));
        return index < 0 ? int.MaxValue : index;
    }

    private static bool Holds(string own, string tag, bool exact) => own == tag || (!exact && own.StartsWith(tag + ".", StringComparison.Ordinal));

    private bool Evaluate(IReadOnlyCollection<string> tags, ref int at, int depth)
    {
        if (depth > MaxDepth)
        {
            throw new InvalidDataException("A gameplay tag query nests too deeply");
        }

        byte type = Token(ref at);
        int count = Token(ref at);
        switch (type)
        {
            case AnyTagsMatch or AllTagsMatch or NoTagsMatch or AnyTagsExactMatch or AllTagsExactMatch:
                bool exact = type is AnyTagsExactMatch or AllTagsExactMatch;
                bool[] found = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    int index = Token(ref at);
                    string tag = index < _tags.Length ? _tags[index] : throw new InvalidDataException($"Gameplay tag query index {index} is outside its dictionary");
                    found[i] = tags.Any(own => Holds(own, tag, exact));
                }

                return type switch
                {
                    AnyTagsMatch or AnyTagsExactMatch => found.Any(match => match),
                    AllTagsMatch or AllTagsExactMatch => found.All(match => match),
                    _ => !found.Any(match => match),
                };
            case AnyExprMatch or AllExprMatch or NoExprMatch:
                bool[] results = new bool[count];
                for (int i = 0; i < count; i++)
                {
                    results[i] = Evaluate(tags, ref at, depth + 1);
                }

                return type switch
                {
                    AnyExprMatch => results.Any(match => match),
                    AllExprMatch => results.All(match => match),
                    _ => !results.Any(match => match),
                };
            default:
                throw new InvalidDataException($"Unknown gameplay tag query expression {type}");
        }
    }

    private byte Token(ref int at) => at < _tokens.Length ? _tokens[at++] : throw new InvalidDataException("A gameplay tag query ends early");
}
