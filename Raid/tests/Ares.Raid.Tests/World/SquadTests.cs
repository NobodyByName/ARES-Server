using Ares.Raid.World;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Replication;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class SquadTests
{
    [Fact]
    public void The_squad_opens_before_the_pawn_and_all_references_resolve()
    {
        ScriptedPlayer session = new();
        session.Flush("start");
        Assert.Single(session.Sent, bunch => bunch is { Channel: 48, Opened: true });
        Assert.DoesNotContain(session.Sent, bunch => bunch.Channel == 6);

        WorldPlayer player = session.Player;
        Dictionary<ulong, string> objects = session.Raid.State.Objects([player.Controller, player.PlayerState, player.Pawn, player.Squad]);
        objects[player.SquadMember] = PlayerClasses.SquadMember;
        PropertyCodec codec = new(new ValueCodec(GameReplication.Catalog, [KnownEnums.NetRole], objects));
        ChannelBunch[] squadUpdates = [.. session.Sent.Where(bunch => bunch is { Channel: 48, Opened: false })];
        Assert.Equal(2, squadUpdates.Length);
        DynamicSubobjectContent member = DynamicSubobjectContent.Read(new BitReader(squadUpdates[0].Payload, squadUpdates[0].PayloadBits));
        Assert.Equal((player.SquadMember, session.Raid.Imports.Classes[PlayerClasses.SquadMember], null), (member.Guid, member.ClassGuid, member.OuterGuid));
        Dictionary<string, object?> memberValues = codec.Decode(new PropertyLayout(GameReplication.Catalog, PlayerClasses.SquadMember), member.Content);
        Assert.Equal(player.PlayerState, memberValues[PropertyNames.PlayerState]);
        Assert.Equal(new UniqueNetId(5, "42", "", false, false), memberValues[PropertyNames.UniqueId]);

        Dictionary<string, object?> squad = Decode(codec, PlayerClasses.Squad.Class, squadUpdates[1]);
        Assert.Equal([player.SquadMember], Assert.IsType<IReadOnlyList<object?>>(squad[PropertyNames.Members], exactMatch: false));
        Assert.Equal($"{player.Principal.MatchId}-0", squad[PropertyNames.SquadId]);
        Assert.Equal(false, squad[PropertyNames.IsGeneratedSquad]);
        ChannelBunch roster = Assert.Single(session.Sent, bunch => bunch is { Channel: RaidWorld.GameStateChannel, Opened: false });
        Dictionary<string, object?> rosterValues = Decode(codec, session.Raid.State.GameState.ClassPath, roster);
        Assert.Equal([player.Squad], Assert.IsType<IReadOnlyList<object?>>(rosterValues[PropertyNames.AllSquads], exactMatch: false));
        Assert.Equal([player.PlayerState], Assert.IsType<IReadOnlyList<object?>>(rosterValues[PropertyNames.PlayerStates], exactMatch: false));
        Dictionary<string, object?> state = Decode(codec, PlayerClasses.PlayerState.Class, Assert.Single(session.Sent, bunch => bunch is { Channel: 3, Opened: false }));
        Assert.Equal(player.Squad, state[PropertyNames.Squad]);
        Assert.Equal(0, state[PropertyNames.SquadSlotIndex]);

        session.Call(FunctionNames.ServerNotifyHasLoadedInitialLevels, []);
        session.Flush("loaded");
        Assert.True(session.Sent.FindIndex(bunch => bunch is { Channel: 48, Opened: true })
            < session.Sent.FindIndex(bunch => bunch is { Channel: 6, Opened: true }));
    }

    [Fact]
    public void Squad_profiles_are_sent_at_start_and_only_reserved_profiles_are_returned_on_request()
    {
        CompactPlayerProfile profile = new(new TenancyUserId(42), "Raider", "1234", "platform", "Raider");
        ScriptedPlayer session = new([profile]);
        session.Flush("start");
        Assert.Equal([profile], Assert.Single(ProfileReplies(session)));

        session.Sent.Clear();
        session.Call(FunctionNames.ServerRequestSquadPlayersProfiles, new Dictionary<string, object?>
        {
            [PropertyNames.TenancyUserIds] = new object?[] { new TenancyUserId(43) },
            [PropertyNames.AuthType] = "",
        });
        session.Flush("foreign profile");
        Assert.Empty(Assert.Single(ProfileReplies(session)));

        session.Sent.Clear();
        session.Call(FunctionNames.ServerRequestSquadPlayersProfiles, new Dictionary<string, object?>
        {
            [PropertyNames.TenancyUserIds] = new object?[] { new TenancyUserId(42) },
            [PropertyNames.AuthType] = "",
        });
        session.Flush("squad profile");
        Assert.Equal([profile], Assert.Single(ProfileReplies(session)));
    }

    private static IEnumerable<IReadOnlyList<object?>> ProfileReplies(ScriptedPlayer session)
    {
        RpcCodec calls = session.Raid.State.Replication.Calls(session.Raid.State.Objects([session.Player.Pawn]));
        foreach (ChannelBunch bunch in session.Sent.Where(bunch => bunch is { Channel: 2, Opened: false }))
        {
            ActorContentBlock block = ActorContentBlock.Read(new BitReader(bunch.Payload, bunch.PayloadBits));
            if (block.HasRepLayout)
            {
                continue;
            }

            (string function, Dictionary<string, object?> parameters) = calls.ReadCall(PlayerClasses.PlayerController.Class, block, RpcDirection.Client);
            if (function == FunctionNames.ClientReceiveSquadPlayersProfiles)
            {
                yield return Assert.IsType<IReadOnlyList<object?>>(parameters[PropertyNames.Profiles], exactMatch: false);
            }
        }
    }

    private static Dictionary<string, object?> Decode(PropertyCodec codec, string classPath, ChannelBunch bunch) =>
        codec.Decode(new PropertyLayout(GameReplication.Catalog, classPath), ActorContentBlock.Read(new BitReader(bunch.Payload, bunch.PayloadBits)));
}
