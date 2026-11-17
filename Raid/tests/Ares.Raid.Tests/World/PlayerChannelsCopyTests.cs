using System.Collections;
using System.Reflection;
using Ares.Raid.World.Players;
using Ares.Structs.Core.Geometry;

namespace Ares.Raid.Tests.World;

/// <summary>
/// The connections copy a player's channels for every packet and drop the copy when the packet is invalid, so a
/// collection the copy shares with the original would let a dropped packet change the committed state.
/// </summary>
public sealed class PlayerChannelsCopyTests
{
    // Collections only ever replaced as a whole, never changed in place, so a copy may share them.
    private static readonly string[] s_replacedWhole =
    [
        "_bindings",
        "_keys",
        "_inputAckKeys",
        "_blackboardKeys",
        "_blackboardState",
        "_abilityKeys",
        "_abilityDefaults",
        "_weaponCreationKeys",
        "_weaponReferenceKeys",
    ];

    [Fact]
    public void A_copy_shares_no_collection_the_channels_change_in_place()
    {
        ScriptedPlayer session = new();
        Vector3D at = session.Member.Spawn.Location;
        session.Possess();
        session.Settle(30, 100, at);
        PlayerChannels channels = session.Channels;

        PlayerChannels copy = (PlayerChannels)channels.Copy();

        FieldInfo[] collections =
        [
            .. typeof(PlayerChannels).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => field.FieldType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(field.FieldType)),
        ];
        Assert.All(s_replacedWhole, name => Assert.Contains(collections, field => field.Name == name));
        Assert.True(collections.Length > s_replacedWhole.Length);
        string[] shared =
        [
            .. collections
                .Where(field => !s_replacedWhole.Contains(field.Name))
                .Where(field => field.GetValue(channels) is { } value && ReferenceEquals(value, field.GetValue(copy)))
                .Select(field => field.Name),
        ];
        Assert.Empty(shared);

        // The world's own collections (bounded logs, actor channel pools) must copy what they hold as well.
        string[] sharedInside =
        [
            .. collections
                .Where(field => field.FieldType.Assembly == typeof(PlayerChannels).Assembly)
                .SelectMany(field => SharedCollections(field.GetValue(channels), field.GetValue(copy)).Select(inner => $"{field.Name}.{inner}")),
        ];
        Assert.Contains(collections, field => field.FieldType.Assembly == typeof(PlayerChannels).Assembly);
        Assert.Empty(sharedInside);
    }

    private static IEnumerable<string> SharedCollections(object? original, object? copy) =>
        original is null || copy is null
            ? []
            : original.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field => field.FieldType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(field.FieldType))
                .Where(field => field.GetValue(original) is { } value && ReferenceEquals(value, field.GetValue(copy)))
                .Select(field => field.Name);
}
