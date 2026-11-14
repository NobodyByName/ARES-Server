using Ares.Raid.World.Collision;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

/// <summary>
/// The positions an unarmed character reaches from rest when nothing but its own input moves it, ticked the way
/// <see cref="PlayerMovement"/> ticks a raid's character: a client that reports exactly these positions agrees with Raid.
/// </summary>
internal static class ReferenceRun
{
    /// <summary>
    /// Simulates from <paramref name="first"/> to <paramref name="last"/>; the position "after" the tick before
    /// <paramref name="first"/> is the start. A scripted client reports the entry of frame - 1 in frame.
    /// </summary>
    public static Dictionary<uint, CharacterState> Run(CollisionWorld world, Vector3D start, uint first, uint last,
        Func<uint, (MovementInput Movement, CharacterInput Claims)> input)
    {
        CharacterSimulation simulation = new(start, world: world);
        Dictionary<uint, CharacterState> states = new() { [first - 1] = simulation.State };
        for (uint frame = first; frame <= last; frame++)
        {
            (MovementInput movement, CharacterInput claims) = input(frame);
            states[frame] = simulation.StepOnGround(claims, movement, 0, PawnBlackboard.TickSeconds, GroundMovementParameters.Unarmed,
                steerAboveMaximum: true, fixBrakingOvershoot: true, sprint: SprintMovementParameters.Unarmed,
                slide: SlideMovementParameters.ForSurface(simulation.State.Body?.Ground.Surface ?? default),
                vaults: VaultParameters.Definitions(armed: false, exhausted: false));
        }

        return states;
    }

    /// <summary>The same input as a scripted frame carries it.</summary>
    public static (MovementInput Movement, CharacterInput Claims) Input(uint frame, ScriptedFrame scripted, ScriptedFrame before)
    {
        MovementButtons buttons = scripted.Buttons
            | (before.Buttons.HasFlag(MovementButtons.Jump) ? MovementButtons.PreviousJump : MovementButtons.None)
            | (before.Buttons.HasFlag(MovementButtons.Crouch) ? MovementButtons.PreviousCrouch : MovementButtons.None)
            | (before.Buttons.HasFlag(MovementButtons.DodgeRoll) ? MovementButtons.PreviousDodgeRoll : MovementButtons.None);
        return (new MovementInput(frame, scripted.Forward, scripted.Right, buttons), new CharacterInput(frame, scripted.Claims));
    }
}
