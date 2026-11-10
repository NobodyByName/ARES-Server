using Ares.Raid.Transport;
using Ares.Raid.World;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Control;

namespace Ares.Raid.Tests.World;

public sealed class MultiplayerTests
{
    [Fact]
    public void Two_parties_share_one_raid_with_their_own_actors_inventory_and_start()
    {
        ScriptedPlayer first = new(loadout: InventoryTests.StandardLoadout());
        ScriptedPlayer second = new(first, 43);
        first.Possess();
        second.Possess();

        Assert.Same(first.Raid, second.Raid);
        Assert.Equal([42L, 43L], first.Raid.Players.Select(player => player.Id));
        Assert.Equal((DeliveryPhase.Possessed, DeliveryPhase.Possessed), (first.Channels.Phase, second.Channels.Phase));
        Assert.True(first.Player.PossessionAcknowledged && second.Player.PossessionAcknowledged);
        Assert.Empty(new[] { first.Player.Controller, first.Player.PlayerState, first.Player.Pawn, first.Player.Squad }
            .Intersect([second.Player.Controller, second.Player.PlayerState, second.Player.Pawn, second.Player.Squad]));
        Assert.NotEqual(first.Raid.State.Actor(first.Player.Squad).Properties[PropertyNames.SquadId],
            second.Raid.State.Actor(second.Player.Squad).Properties[PropertyNames.SquadId]);
        Assert.NotSame(first.Member.Inventory, second.Member.Inventory);
        Assert.NotEqual(first.Member.Inventory.Containers.Sum(container => container.Items.Count(item => item is not null)),
            second.Member.Inventory.Containers.Sum(container => container.Items.Count(item => item is not null)));
        Assert.Equal(first.Member.Spawn.Start, second.Member.Spawn.Start);
        Assert.Equal((0, 1), (first.Member.Spawn.Slot, second.Member.Spawn.Slot));
    }

    [Fact]
    public void A_surrendering_player_leaves_while_the_raid_goes_on_until_the_last_one_leaves()
    {
        ScriptedPlayer first = new();
        ScriptedPlayer second = new(first, 43);
        first.Possess();
        second.Possess();
        RaidWorld raid = first.Raid;

        Surrender(first);
        Assert.True(first.Member.Left);
        Assert.True(first.Worlds.HasLeft(raid.MatchId, 42));
        Assert.False(first.Worlds.HasLeft(raid.MatchId, 43));
        Assert.False(raid.Ended);
        Assert.True(first.Worlds.AcceptingPlayers(raid.MatchId));
        Assert.Null(first.Worlds.WelcomeFor(first.Principal));
        Assert.Equal(RaidWorld.Welcome, first.Worlds.WelcomeFor(second.Principal));
        Assert.Throws<WorldPreparationException>(() => first.Worlds.Join(raid.MatchId, [42]));

        Surrender(second);
        Assert.True(raid.Ended);
        Assert.False(first.Worlds.AcceptingPlayers(raid.MatchId));
        Assert.Throws<WorldPreparationException>(() => first.Worlds.Join(raid.MatchId, [44]));
    }

    [Fact]
    public void An_expired_reservation_takes_its_player_out_of_the_raid()
    {
        ScriptedPlayer first = new();
        ScriptedPlayer second = new(first, 43);
        string match = first.Raid.MatchId;
        first.Worlds.Leave("another-match", [43]);
        Assert.False(second.Member.Left);

        first.Worlds.Leave(match, [43, 99]);
        Assert.True(second.Member.Left);
        Assert.Null(first.Worlds.WelcomeFor(second.Principal));
        Assert.Throws<InvalidPacketException>(() => first.Worlds.StartPlayer(second.Principal, new PlayerLogin(7, 0, new UniqueNetId(5, "43", "", false, false)), second.Io));
        Assert.False(first.Raid.Ended);
    }

    [Fact]
    public void Every_party_shares_one_start_and_the_raid_refuses_players_it_cannot_place()
    {
        DamWorlds worlds = new(() => 0, null, () => 7);
        worlds.Prepare("a", [1]);
        RaidWorld raid = worlds.Current!;
        IReadOnlyList<PlayerStart> starts = Spawns.Eligible(DamRounds.All[raid.Seed % DamRounds.All.Count]);
        int points = starts[raid.Seed % starts.Count].SpawnPoints.Count;
        Assert.Throws<WorldPreparationException>(() => worlds.Join("b", [2]));
        Assert.Throws<WorldPreparationException>(() => worlds.Join("a", [1]));
        for (long player = 2; player <= points; player++)
        {
            worlds.Join("a", [player]);
        }

        Assert.Throws<WorldPreparationException>(() => worlds.Join("a", [points + 1]));
        Assert.Single(raid.Players.Select(player => player.Spawn.Start).Distinct());
        Assert.Equal(Enumerable.Range(0, points).Select(slot => (int?)slot), raid.Players.Select(player => player.Spawn.Slot));
    }

    private static void Surrender(ScriptedPlayer session)
    {
        session.Call(FunctionNames.ServerSurrender, []);
        session.Flush("surrender");
        session.Channels.Closed();
    }
}
