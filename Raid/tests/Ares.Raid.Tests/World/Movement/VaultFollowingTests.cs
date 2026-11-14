using System.Numerics;
using Ares.Raid.World.Collision;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class VaultFollowingTests
{
    private const uint Start = 30;
    private const uint Request = Start + 12;
    private const ulong RequestClaim = 1UL << VaultParameters.RequestBit;

    [Fact]
    public void A_requested_vault_with_nothing_to_vault_follows_the_client_while_it_is_locked_counted_once()
    {
        CollisionWorld world = ScriptedClient.World((new Vector3D(0, 0, -10), new Vector3(4000, 4000, 10)));
        Vector3D here = new(0, 0, 0);
        PlayerMovement movement = new(Start, here, 0, world);
        Dictionary<uint, CharacterState> states = ScriptedClient.Deliver(movement,
            frame => new ScriptedFrame(here, Claims: frame == Request ? RequestClaim : 0, FireBlocked: frame is > Request and <= Request + 12),
            ScriptedClient.InOrder(Start + 3, Request + 30)).ToDictionary(state => state.Frame);

        Assert.Equal(1, states[Request].VaultFollowedTicks);
        Assert.Equal(12, states.Values.Max(state => state.VaultFollowedTicks));
        Assert.All(states.Values.Where(state => state.VaultFollowedTicks > 0),
            state => Assert.Equal(VaultParameters.VaultingFlags, state.GraphFlags & VaultParameters.VaultingFlags));
        Assert.False(states[Request + 20].Vaulting);
        Assert.Equal(1, movement.Reseeds);
    }

    [Fact]
    public void A_vault_whose_path_leaves_the_reports_follows_them_for_the_rest_of_the_vault_counted_once()
    {
        // A floor and a wall 115 high across the way; the character walks at it and asks for the vault.
        CollisionWorld world = ScriptedClient.World((new Vector3D(0, 0, -10), new Vector3(4000, 4000, 10)), (new Vector3D(100, 0, 57.5), new Vector3(10, 500, 57.5f)));
        Vector3D start = new(-100, 0, 0);
        Dictionary<uint, CharacterState> reference = ReferenceRun.Run(world, start, Start + 2, Request + 40,
            frame => ReferenceRun.Input(frame, Input(frame), Input(frame - 1)));
        Assert.NotNull(reference[Request].Vault);

        PlayerMovement movement = new(Start, start, 0, world);
        Dictionary<uint, int> reseeds = [];
        Dictionary<uint, CharacterState> states = ScriptedClient.Deliver(movement, Script, ScriptedClient.InOrder(Start + 3, Request + 27),
            packet => reseeds[packet] = movement.Reseeds).ToDictionary(state => state.Frame);

        Assert.NotNull(states[Request].Vault);
        Assert.Equal(0, reseeds[Request + 1]);
        Assert.Equal(1, states[Request + 3].VaultFollowedTicks);
        Assert.All(states.Values.Where(state => state.Frame is > Request + 3 and < Request + 24), state => Assert.True(state.VaultFollowedTicks > 1));
        Assert.Equal(1, reseeds[Request + 25]);
        return;

        ScriptedFrame Input(uint frame) => new(default, frame >= Start + 2 ? (sbyte)126 : (sbyte)0, Claims: frame == Request ? RequestClaim : 0,
            FireBlocked: frame is > Request and <= Request + 25);

        // The client's vault drifts sideways from the simulated path after a few ticks.
        ScriptedFrame Script(uint frame)
        {
            Vector3D at = frame <= Start + 2 ? start : reference[frame - 1].Position;
            return Input(frame) with { Position = frame is > Request + 3 and <= Request + 25 ? at with { Y = at.Y + 10 } : at };
        }
    }
}
