using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.GameplayTags;

namespace Ares.Structs.Tests.Net;

public sealed class GameplayTagQueryTests
{
    [Fact]
    public void Queries_match_the_reference()
    {
        foreach ((byte[] tokens, bool[] results) in GameplayTagQueryVectors.Queries)
        {
            GameplayTagQuery query = new(GameplayTagQueryVectors.Dictionary, tokens);
            Assert.Equal(results, GameplayTagQueryVectors.TagSets.Select(query.Matches));
        }
    }

    [Fact]
    public void Malformed_queries_are_rejected()
    {
        foreach ((string[] dictionary, byte[] tokens, string reason) in GameplayTagQueryVectors.Malformed)
        {
            Assert.Throws<InvalidPacketException>(() => new GameplayTagQuery(dictionary, tokens).Parse());
            Assert.False(string.IsNullOrEmpty(reason));
        }

        Assert.Throws<InvalidPacketException>(() => new GameplayTagQuery([], []).Matches(["bad tag"]));
    }

    [Fact]
    public void Tag_segments_may_start_with_a_digit()
    {
        GameplayTagQuery query = new(["Item.Drop.Rarity.01Common"], [0, 1, 1, 1, 0]);
        Assert.True(query.Matches(["Item.Drop.Rarity.01Common"]));
        Assert.False(query.Matches(["Item.Drop.Rarity.02Uncommon"]));
    }
}
