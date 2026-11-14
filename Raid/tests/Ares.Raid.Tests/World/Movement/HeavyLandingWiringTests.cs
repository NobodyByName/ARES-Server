using System.Numerics;
using Ares.Raid.World.Collision;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class HeavyLandingWiringTests
{
    private const double Tick = 1.0 / 30;
    private const MovementButtons Everything = MovementButtons.Jump | MovementButtons.Crouch | MovementButtons.DodgeRoll | MovementButtons.Sprint;

    private static readonly CollisionWorld s_floor = ScriptedClient.World((new Vector3D(0, 0, -10), new Vector3(4000, 4000, 10)));

    [Theory]
    [InlineData(900.0, 0)]
    [InlineData(1500.0, 1)]
    public void A_fall_lands_at_the_level_its_height_is_worth_and_ignores_all_input_until_it_hands_over(double height, int level)
    {
        CharacterSimulation pressing = new(new Vector3D(0, 0, height), world: s_floor);
        CharacterSimulation idle = new(new Vector3D(0, 0, height), world: s_floor);
        (CharacterState before, CharacterState landed, uint frame) = FallUntilLanding(pressing, idle);

        double fall = HeavyLandingParameters.FallHeight(before.Body!.Value.Position.Z - before.Body.Value.Ground.Height, before.Body.Value.Velocity.Z,
            AirMovementParameters.FallAcceleration);
        Assert.Equal(level, landed.Landing!.Value.Level);
        Assert.Equal(HeavyLandingMovement.Level(fall, HeavyLandingAnimations.Heavy), landed.Landing.Value.Level);
        int duration = HeavyLandingMovement.Duration(landed.Landing.Value.Clip);

        // Every button pressed and the stick pushed: nothing starts, and the character moves as it would without input.
        int tagged = 1;
        for (int tick = 1; tick < duration + 20; tick++, frame++)
        {
            CharacterState state = Step(pressing, frame, 126, tick % 2 == 0 ? Everything : MovementButtons.None);
            CharacterState reference = Step(idle, frame, 0, MovementButtons.None);
            if (state.HeavyLandingTicks == 0)
            {
                Assert.Equal(0UL, state.GraphFlags & GraphStateTags.AbilityLandingHeavy);
                continue;
            }

            tagged++;
            Assert.Null(state.Slide);
            Assert.Null(state.Roll);
            Assert.Null(state.Vault);
            Assert.Null(state.Body!.Value.JumpRequestedAt);
            Assert.Equal(GraphStateFlags.HeavyLandingRecovery, state.GraphFlags & GraphStateFlags.HeavyLandingRecovery);
            Assert.Equal(LocoMode.Still, state.Locomotion);
            Assert.Equal(reference.Position, state.Position);
            Assert.Equal(reference.Stance, state.Stance);
        }

        Assert.Equal(duration, tagged);
        Assert.Null(pressing.State.Landing);
    }

    [Fact]
    public void A_roll_press_shortly_before_the_landing_turns_it_into_a_recovery_roll_whose_tag_stays()
    {
        CharacterSimulation pressed = new(new Vector3D(0, 0, 700), world: s_floor);
        CharacterSimulation plain = new(new Vector3D(0, 0, 700), world: s_floor);
        uint frame = 1;
        double seconds = Math.Sqrt(2 * 700 / AirMovementParameters.FallAcceleration);
        uint press = (uint)(seconds / Tick) - 5;
        CharacterState rolled = default;
        CharacterState heavy = default;
        for (; rolled.Landing is null || heavy.Landing is null; frame++)
        {
            rolled = Step(pressed, frame, 0, frame == press ? MovementButtons.DodgeRoll : MovementButtons.None);
            heavy = Step(plain, frame, 0, MovementButtons.None);
        }

        Assert.True(rolled.Landing!.Value.Recovery);
        Assert.False(heavy.Landing!.Value.Recovery);
        for (int tick = 0; tick < 120; tick++, frame++)
        {
            rolled = Step(pressed, frame, 0, MovementButtons.None);
        }

        Assert.Null(rolled.Landing);
        Assert.Equal(GraphStateTags.AbilityLandingRecovery, rolled.GraphFlags & GraphStateTags.AbilityLandingRecovery);
        Assert.Equal(0UL, plain.State.GraphFlags & GraphStateTags.AbilityLandingRecovery);
    }

    [Fact]
    public void A_landing_that_loses_the_ground_while_falling_fast_ends_at_once()
    {
        Vector3D high = new(0, 0, 2000);
        Vector3D falling = new(0, 0, -1500);
        HeavyLandingState landing = HeavyLandingMovement.Begin(700, falling, falling, 0) with { Ticks = 2 };
        CharacterSimulation simulation = CharacterSimulation.From(new CharacterState(5, high, false)
        {
            Velocity = falling,
            Body = BodyMotion.Start(s_floor, high, falling),
            Landing = landing,
            HeavyLandingTicks = 2,
        }, s_floor);

        CharacterState state = Step(simulation, 6, 0, MovementButtons.None);

        Assert.True(state.LandingFinished);
        Assert.Equal(3, state.HeavyLandingTicks);
        Assert.True(state.LandingBlend.Total > 0);
        Assert.True(state.HeavyLandingTicks < HeavyLandingMovement.Duration(landing.Clip));
    }

    [Fact]
    public void A_landing_whose_path_leaves_the_reports_follows_them_and_counts_once()
    {
        const uint Start = 30;
        const uint Last = Start + 110;
        Vector3D top = new(0, 0, 900);
        Dictionary<uint, CharacterState> reference = ReferenceRun.Run(s_floor, top, Start + 2, Last, frame => Input(frame, MovementButtons.None));
        uint landed = reference.First(entry => entry.Value.Landing is not null).Key;

        PlayerMovement movement = new(Start, top, 0, s_floor);
        List<CharacterState> states = ScriptedClient.Deliver(movement, Script, ScriptedClient.InOrder(Start + 3, Last));

        Assert.Equal(1, movement.Reseeds);
        Assert.Contains(states, state => state.LandingFollowed);
        Assert.All(states.Where(state => state.LandingFollowed), state => Assert.NotNull(state.Landing));
        return;

        // The client's recovery drifts sideways from the landing's animation.
        ScriptedFrame Script(uint frame)
        {
            Vector3D at = frame <= Start + 2 ? top : reference[Math.Min(frame - 1, Last)].Position;
            return new ScriptedFrame(frame > landed + 3 && frame < landed + 20 ? at with { X = at.X + (8.0 * (frame - landed - 3)) } : at);
        }
    }

    private static (CharacterState Before, CharacterState Landed, uint Next) FallUntilLanding(CharacterSimulation pressing, CharacterSimulation idle)
    {
        CharacterState before = pressing.State;
        for (uint frame = 1; ; frame++)
        {
            CharacterState state = Step(pressing, frame, 0, MovementButtons.None);
            Step(idle, frame, 0, MovementButtons.None);
            if (state.Landing is not null)
            {
                return (before, state, frame + 1);
            }

            before = state;
        }
    }

    private static (MovementInput, CharacterInput) Input(uint frame, MovementButtons buttons) =>
        (new MovementInput(frame, 0, 0, buttons), new CharacterInput(frame, 0));

    private static CharacterState Step(CharacterSimulation simulation, uint frame, sbyte forward, MovementButtons buttons) =>
        simulation.StepOnGround(new CharacterInput(frame, 0), new MovementInput(frame, forward, 0, buttons), 0, Tick, GroundMovementParameters.Unarmed,
            steerAboveMaximum: true, fixBrakingOvershoot: true, sprint: SprintMovementParameters.Unarmed, slide: SlideMovementParameters.Default,
            vaults: VaultParameters.Definitions(armed: false, exhausted: false));
}
