using System.Text.RegularExpressions;
using Ares.Structs.Core.Serialization;

namespace Ares.Structs.Net.GameplayTags;

/// <summary>A serialized gameplay-tag query (<c>FGameplayTagQuery</c>): its tags and encoded expression.</summary>
/// <param name="TagDictionary">Tags the token stream refers to by index.</param>
/// <param name="TokenStream">The encoded query expression.</param>
public sealed partial record GameplayTagQuery(IReadOnlyList<string> TagDictionary, IReadOnlyList<byte> TokenStream)
{
    private const int MaxDictionary = 256;
    private const int MaxTokens = 8192;
    private const int MaxDepth = 32;
    private const int MaxNodes = 4096;
    private const int MaxTags = 4096;
    private const int MaxTagLength = 1024;

    /// <summary>
    /// Parses the token stream: a version byte (0), a root marker (0 for an empty query, 1 for an
    /// expression), then the expression. Throws <see cref="InvalidPacketException"/> on anything else.
    /// </summary>
    /// <returns>The expression, or null for an empty query.</returns>
    public GameplayTagExpression? Parse() => new Parser(this).Parse();

    /// <summary>
    /// Whether <paramref name="tags"/> satisfy the query. A tag satisfies a query tag when it equals it or is
    /// below it (<c>A.B.C</c> satisfies <c>A.B</c>), ignoring case. An empty query matches nothing.
    /// </summary>
    /// <param name="tags">The tags of the evaluated object.</param>
    /// <returns>Whether the query matches.</returns>
    public bool Matches(IReadOnlyCollection<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        if (tags.Count > MaxTags)
        {
            throw new InvalidPacketException("Too many tags");
        }

        string[] normalized = [.. tags.Select(Normalize)];
        return Parse() is { } expression && expression.Evaluate(normalized);
    }

    /// <summary>A valid tag name, lowercased for comparison.</summary>
    /// <param name="tag">The tag.</param>
    /// <returns>The lowercased tag.</returns>
    internal static string Normalize(string tag) =>
        tag.Length <= MaxTagLength && !tag.Equals("none", StringComparison.OrdinalIgnoreCase) && TagPattern().IsMatch(tag)
            ? tag.ToLowerInvariant()
            : throw new InvalidPacketException("Invalid gameplay tag");

    [GeneratedRegex("^[A-Za-z_][A-Za-z_0-9]*(?:\\.[A-Za-z_][A-Za-z_0-9]*)*$")]
    private static partial Regex TagPattern();

    private sealed class Parser
    {
        private readonly string[] _dictionary;
        private readonly IReadOnlyList<byte> _tokens;
        private int _position;
        private int _nodes;

        public Parser(GameplayTagQuery query)
        {
            if (query.TagDictionary.Count > MaxDictionary || query.TokenStream.Count > MaxTokens)
            {
                throw new InvalidPacketException("Query too large");
            }

            _dictionary = [.. query.TagDictionary.Select(Normalize)];
            if (_dictionary.Distinct(StringComparer.Ordinal).Count() != _dictionary.Length)
            {
                throw new InvalidPacketException("Duplicate query tag");
            }

            _tokens = query.TokenStream;
        }

        public GameplayTagExpression? Parse()
        {
            if (_tokens.Count == 0)
            {
                return null;
            }

            if (Take() != 0)
            {
                throw new InvalidPacketException("Unsupported query token version");
            }

            GameplayTagExpression? expression = Take() switch
            {
                0 => null,
                1 => Expression(0),
                _ => throw new InvalidPacketException("Invalid query root marker"),
            };
            return _position == _tokens.Count ? expression : throw new InvalidPacketException("Trailing query tokens");
        }

        private byte Take() => _position < _tokens.Count ? _tokens[_position++] : throw new InvalidPacketException("Truncated query");

        private GameplayTagExpression Expression(int depth)
        {
            if (depth > MaxDepth || ++_nodes > MaxNodes)
            {
                throw new InvalidPacketException("Query nesting too deep");
            }

            GameplayTagExpressionKind kind = (GameplayTagExpressionKind)Take();
            int count = Take();
            switch (kind)
            {
                case GameplayTagExpressionKind.AnyTagsMatch or GameplayTagExpressionKind.AllTagsMatch or GameplayTagExpressionKind.NoTagsMatch:
                    string[] tags = new string[count];
                    for (int index = 0; index < count; index++)
                    {
                        int tag = Take();
                        tags[index] = tag < _dictionary.Length ? _dictionary[tag] : throw new InvalidPacketException("Query tag index out of range");
                    }

                    return new GameplayTagTest(kind, tags);
                case GameplayTagExpressionKind.AnyExprMatch or GameplayTagExpressionKind.AllExprMatch or GameplayTagExpressionKind.NoExprMatch:
                    GameplayTagExpression[] children = new GameplayTagExpression[count];
                    for (int index = 0; index < count; index++)
                    {
                        children[index] = Expression(depth + 1);
                    }

                    return new GameplayTagGroup(kind, children);
                default:
                    throw new InvalidPacketException("Unsupported query expression");
            }
        }
    }
}

/// <summary>Gameplay-tag query expression kinds, as numbered in the token stream.</summary>
public enum GameplayTagExpressionKind : byte
{
    /// <summary>Any query tag is present.</summary>
    AnyTagsMatch = 1,

    /// <summary>Every query tag is present.</summary>
    AllTagsMatch = 2,

    /// <summary>No query tag is present.</summary>
    NoTagsMatch = 3,

    /// <summary>Any child expression matches.</summary>
    AnyExprMatch = 4,

    /// <summary>Every child expression matches.</summary>
    AllExprMatch = 5,

    /// <summary>No child expression matches.</summary>
    NoExprMatch = 6,
}

/// <summary>A node of a parsed gameplay-tag query.</summary>
/// <param name="Kind">Expression kind.</param>
public abstract record GameplayTagExpression(GameplayTagExpressionKind Kind)
{
    /// <summary>Evaluates the expression on lowercased tags.</summary>
    /// <param name="tags">Lowercased tags.</param>
    /// <returns>Whether it matches.</returns>
    internal abstract bool Evaluate(IReadOnlyList<string> tags);

    /// <summary>Combines child results by the expression kind (any, all or none).</summary>
    /// <param name="results">Child results.</param>
    /// <returns>The combined result.</returns>
    protected bool Combine(IEnumerable<bool> results) => Kind switch
    {
        GameplayTagExpressionKind.AnyTagsMatch or GameplayTagExpressionKind.AnyExprMatch => results.Any(result => result),
        GameplayTagExpressionKind.AllTagsMatch or GameplayTagExpressionKind.AllExprMatch => results.All(result => result),
        _ => !results.Any(result => result),
    };
}

/// <summary>A test of query tags against the evaluated tags.</summary>
/// <param name="Kind">Any, all or no tags.</param>
/// <param name="Tags">Query tags (lowercased).</param>
public sealed record GameplayTagTest(GameplayTagExpressionKind Kind, IReadOnlyList<string> Tags) : GameplayTagExpression(Kind)
{
    /// <inheritdoc />
    internal override bool Evaluate(IReadOnlyList<string> tags) =>
        Combine(Tags.Select(needed => tags.Any(tag => tag == needed || tag.StartsWith(needed + ".", StringComparison.Ordinal))));
}

/// <summary>A combination of child expressions.</summary>
/// <param name="Kind">Any, all or no children.</param>
/// <param name="Children">Child expressions.</param>
public sealed record GameplayTagGroup(GameplayTagExpressionKind Kind, IReadOnlyList<GameplayTagExpression> Children) : GameplayTagExpression(Kind)
{
    /// <inheritdoc />
    internal override bool Evaluate(IReadOnlyList<string> tags) => Combine(Children.Select(child => child.Evaluate(tags)));
}
