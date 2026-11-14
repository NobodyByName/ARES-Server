using Ares.Structs.Game.Collision;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>
/// The ground acceleration on ice: the walking and sliding ground steps change their acceleration parameters when the
/// ground under the character has the ice surface type. Other surface types leave them unchanged.
/// </summary>
public static class IceMovement
{
    /// <summary>The acceleration parameters on a ground.</summary>
    /// <param name="ground">Acceleration parameters before the ground's surface is considered.</param>
    /// <param name="surface">Physical surface type of the ground under the character.</param>
    /// <param name="materialFriction">Friction of the ground's physical material.</param>
    /// <param name="ice">Ice values of the step (<see cref="IceMovementParameters.General"/> or <see cref="IceMovementParameters.Sliding"/>).</param>
    /// <returns>The parameters, each changed value rounded to a float.</returns>
    public static GroundMovementParameters OnGround(GroundMovementParameters ground, PhysicalSurfaceType surface, float materialFriction, IceMovementParameters ice)
    {
        if (surface != IceMovementParameters.Surface)
        {
            return ground;
        }

        return ground with
        {
            Friction = (float)(materialFriction * ice.FrictionMultiplier),
            BrakingDeceleration = (float)((float)ground.BrakingDeceleration * ice.AccelerationMultiplier),
            Acceleration = (float)((float)ground.Acceleration * ice.AccelerationMultiplier),
        };
    }
}
