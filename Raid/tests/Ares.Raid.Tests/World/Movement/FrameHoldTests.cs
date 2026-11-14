using System.Numerics;
using Ares.Common.Maps;
using Ares.Raid.Tests.World.Collision;
using Ares.Raid.World.Collision;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class FrameHoldTests
{
    private const uint Start = 30;
    private const double Tick = 1.0 / 30;

    private static readonly CollisionWorld s_floor = ScriptedClient.World((new Vector3D(0, 0, -10), new Vector3(4000, 4000, 10)));

    [Fact]
    public void Reports_of_a_fall_hold_frames_while_the_simulated_body_still_stands()
    {
        const uint Edge = Start + 10;
        PlayerMovement movement = new(Start, default, 0, s_floor, double.PositiveInfinity);
        Dictionary<uint, CharacterState> states = ScriptedClient.Deliver(movement, Script, ScriptedClient.InOrder(Start + 3, Edge + 15))
            .ToDictionary(state => state.Frame);

        Assert.False(states[Edge - 3].Unsettled);
        foreach (CharacterState state in states.Values.Where(entry => entry.Frame > Edge + 2))
        {
            Assert.False(state.Body!.Value.Airborne);
            Assert.True(state.Unsettled);
        }

        return;

        ScriptedFrame Script(uint frame)
        {
            double seconds = Math.Max(0, (double)frame - Edge) * Tick;
            return new ScriptedFrame(new Vector3D(0, 0, -(AirMovementParameters.FallAcceleration * seconds * seconds / 2)));
        }
    }

    [Fact]
    public void A_reseed_into_the_air_holds_the_frame_of_that_tick()
    {
        const uint Lift = Start + 10;
        PlayerMovement movement = new(Start, default, 0, s_floor);
        Dictionary<uint, CharacterState> states = ScriptedClient.Deliver(movement,
            frame => new ScriptedFrame(new Vector3D(0, 0, frame > Lift ? 150 : 0)), ScriptedClient.InOrder(Start + 3, Lift + 3)).ToDictionary(state => state.Frame);

        Assert.True(states[Lift].Body!.Value.Airborne);
        Assert.True(states[Lift].Unsettled);
    }

    [Fact]
    public void A_landing_while_walking_up_a_slope_hands_over_within_the_settle_time()
    {
        MapCollisionBuilder builder = SyntheticMaps.Builder();
        int cell = builder.AddCell("/Game/Cell", []);
        builder.AddPlacement(builder.AddBody(SyntheticMaps.Body("slope", boxes: [SyntheticMaps.Box(new Vector3(3000, 3000, 10))])), cell,
            SyntheticMaps.At(0, 0, 0, SyntheticMaps.Rotation(-20, 0, 1, 0)), PlacementFlags.BlocksPawn);
        CollisionWorld world = new(builder.Build());
        double height = world.GroundHeight(0, 0, 1000, 2000, PlacementFlags.BlocksPawn)!.Value;
        BodyState body = BodyMotion.Start(world, new Vector3D(0, 0, height + 40), new Vector3D(300, 0, -400)) with { Airborne = true, Jumping = true };
        Vector3D walk = new(300, 0, 0);

        int landed = -1;
        for (int tick = 0; tick < 40; tick++)
        {
            body = BodyMotion.Move(world, BodyMotion.Begin(body, (uint)tick, false), walk, Tick);
            landed = landed < 0 && body is { Airborne: false, Landing: true } ? tick : landed;
            if (landed < 0 || body.Landing)
            {
                continue;
            }

            Assert.InRange(tick - landed, 1, (int)Math.Ceiling(GroundContactParameters.LandingSettleSeconds / Tick));
            return;
        }

        Assert.Fail("The landing never handed over.");
    }

    [Theory]
    [InlineData(GroundContactParameters.StandingDistance - 1, false)]
    [InlineData(GroundContactParameters.StandingDistance + 1, true)]
    [InlineData(99.8, true)]
    public void A_body_placed_above_the_floor_stands_only_within_the_standing_distance(double height, bool airborne)
    {
        BodyState body = BodyMotion.Start(s_floor, new Vector3D(0, 0, height));

        Assert.Equal(airborne, body.Airborne);
        Assert.Equal(airborne, BodyMotion.Begin(body, 1, false).Airborne);
    }

    [Theory]
    [InlineData(GroundContactParameters.StepDownReach - 5, false)]
    [InlineData(GroundContactParameters.StepDownReach + 5, true)]
    public void A_character_whose_pill_finds_no_ground_within_a_step_down_falls(double drop, bool falls)
    {
        // A floor at 0 for x below 0 and a lower one beyond; one fast tick carries the whole pill past the edge.
        CollisionWorld world = ScriptedClient.World(
            (new Vector3D(-2000, 0, -10), new Vector3(2000, 2000, 10)),
            (new Vector3D(2000, 0, -drop - 10), new Vector3(2000, 2000, 10)));
        BodyState body = BodyMotion.Move(world, BodyMotion.Begin(BodyMotion.Start(world, new Vector3D(-20, 0, 0)), 0, false), new Vector3D(3000, 0, 0), Tick);

        Assert.Equal(falls, BodyMotion.Begin(body, 1, false).Airborne);
    }

    [Fact]
    public void A_restart_at_a_teleport_follows_the_new_reports_without_a_reseed()
    {
        const uint Teleport = Start + 10;
        Vector3D far = new(3000, 0, 0);
        PlayerMovement movement = new(Start, default, 0, s_floor);
        bool restarted = false;
        ScriptedClient.Deliver(movement, Script, ScriptedClient.InOrder(Start + 3, Teleport + 10), packet =>
        {
            if (packet != Teleport + 2)
            {
                return;
            }

            // The tick that read the first report from there has run; it runs again as the restart's first.
            movement.Restart(Teleport - 1, far, 0);
            restarted = true;
        });

        Assert.True(restarted);
        Assert.Equal(0, movement.Reseeds);
        // The tick the restart replaces had counted the teleport distance; its records go with it.
        Assert.Null(movement.LastReseed);
        Assert.Equal(0, movement.WorstReseed);
        Assert.InRange(movement.Latest.Position.X, far.X - 1, far.X + 1);
        Assert.Equal(CharacterStance.Stand, movement.Latest.Stance);
        return;

        ScriptedFrame Script(uint frame) => new(frame > Teleport ? far : default);
    }
}
