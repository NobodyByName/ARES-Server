using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>
/// Whether a character is off the ground, read from the positions its client reports. A jumping or falling
/// character's height changes by the fall acceleration from one tick to the next, which ground movement does
/// not produce. A jump lifts the character a few ticks after its press, so the press counts until then. A heavy
/// landing leaves the character recovering; its client blocks firing for that time, so the landing lasts as long.
/// </summary>
/// <param name="JumpTicks">Ticks a jump press may still take to lift the character.</param>
/// <param name="FallTicks">Ticks the last seen fall, with its landing, still counts for.</param>
/// <param name="RecoveryTicks">Ticks the landing's recovery has run.</param>
public readonly record struct AirMotion(int JumpTicks, int FallTicks, int RecoveryTicks = 0)
{
    // A jump leaves the ground within this many ticks of its press.
    private const int JumpStartTicks = 9;

    // Reported positions are coarse, so a fall is over after this many ticks without its acceleration.
    private const int LandingTicks = 2;

    // The share of the fall acceleration a falling character's reported positions show at least and at most.
    private const double LeastFallShare = 0.55;
    private const double MostFallShare = 1.95;

    // Downward speed that ground movement never reaches; a long fall stops accelerating.
    private const double FallSpeed = 1200;

    // A landing recovery is over within this many ticks; a client blocking fire for longer is busy with something else.
    private const int MostRecoveryTicks = 60;

    private const double FallPerTick = AirMovementParameters.FallAcceleration * PawnBlackboard.TickSeconds * PawnBlackboard.TickSeconds;

    /// <summary>Whether the character is jumping, falling or recovering from a heavy landing.</summary>
    public bool Unsettled => JumpTicks > 0 || FallTicks > 0;

    /// <summary>The motion a tick later.</summary>
    /// <param name="jumpPressed">Whether the tick's input presses jump.</param>
    /// <param name="fireBlocked">Whether the client blocks firing after the tick.</param>
    /// <param name="before">Reported position before the previous tick, if known.</param>
    /// <param name="position">Reported position before the tick, if known.</param>
    /// <param name="after">Reported position after the tick, if known.</param>
    /// <returns>The motion during the tick; an unknown position leaves the fall as it was.</returns>
    public AirMotion Next(bool jumpPressed, bool fireBlocked, Vector3D? before, Vector3D? position, Vector3D? after)
    {
        int jump = jumpPressed ? JumpStartTicks : Math.Max(0, JumpTicks - 1);
        if (before is not { } first || position is not { } second || after is not { } third)
        {
            return this with { JumpTicks = jump };
        }

        double step = third.Z - second.Z;
        double change = step - (second.Z - first.Z);
        if (change is <= -LeastFallShare * FallPerTick and >= -MostFallShare * FallPerTick || step <= -FallSpeed * PawnBlackboard.TickSeconds)
        {
            return new AirMotion(jump, LandingTicks + 1);
        }

        return FallTicks > 0 && fireBlocked && RecoveryTicks < MostRecoveryTicks
            ? new AirMotion(jump, FallTicks, RecoveryTicks + 1)
            : new AirMotion(jump, Math.Max(0, FallTicks - 1), RecoveryTicks);
    }
}
