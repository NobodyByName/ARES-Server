using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>Persistent directional input state for a slide.</summary>
/// <param name="InitialVelocity">Velocity when slide input is first evaluated.</param>
/// <param name="Slowdown">Accumulated slowdown between zero and one.</param>
/// <param name="Ticks">Slide steps already taken. On the first step the character still moves with the
/// velocity of the movement it leaves; the slide velocity applies from the second step.</param>
public readonly record struct SlideState(Vector3D InitialVelocity, float Slowdown, int Ticks = 0);

/// <summary>Result of one level-ground slide step.</summary>
/// <param name="State">Persistent slide state.</param>
/// <param name="Velocity">Next horizontal world velocity.</param>
/// <param name="Input">Continuous world-space slide input.</param>
/// <param name="Finished">Whether cancellation requests leaving the slide node.</param>
public readonly record struct SlideStep(SlideState State, Vector3D Velocity, Vector3D Input, bool Finished);

/// <summary>Slide motion on level, stationary ground.</summary>
public static class FlatSlideMovement
{
    /// <summary>Advances slide input, steering and ground velocity for an active slide.</summary>
    /// <param name="state">Persistent state belonging to this slide.</param>
    /// <param name="previous">Previous world velocity.</param>
    /// <param name="input">Current movement axes and buttons.</param>
    /// <param name="yaw">Control heading in radians.</param>
    /// <param name="elapsed">Elapsed time in the slide node, in seconds.</param>
    /// <param name="seconds">Simulation step duration.</param>
    /// <param name="parameters">Slide speed and friction modifiers.</param>
    /// <param name="steerAboveMaximum">Whether overspeed input steering is enabled.</param>
    /// <param name="fixBrakingOvershoot">Whether braking overshoot is corrected.</param>
    /// <param name="forceCancel">Whether another action forces the slide to end.</param>
    /// <returns>Slide state, velocity and cancellation result.</returns>
    public static SlideStep Step(SlideState state, Vector3D previous, MovementInput input, double yaw, double elapsed, float seconds,
        SlideMovementParameters parameters, bool steerAboveMaximum, bool fixBrakingOvershoot, bool forceCancel = false)
    {
        if (!double.IsFinite(elapsed) || elapsed < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        if (!double.IsFinite(yaw))
        {
            throw new ArgumentOutOfRangeException(nameof(yaw));
        }

        if (!float.IsFinite(seconds) || seconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        SlideState actual = state.InitialVelocity == default ? state with { InitialVelocity = previous } : state;
        double speed = Math.Sqrt((previous.X * previous.X) + (previous.Y * previous.Y));
        double forward = input.Forward / (double)PendulumInput.MovementAxisMaximum;
        double right = input.Right / (double)PendulumInput.MovementAxisMaximum;
        double relativeAngle = yaw + Math.Atan2(right, forward) - Math.Atan2(previous.Y, previous.X);
        bool hasInput = (forward * forward) + (right * right) > SlideMovementParameters.InputTolerance;
        Vector3D relativeInput = hasInput ? new Vector3D(Math.Cos(relativeAngle), Math.Sin(relativeAngle), 0) : default;
        GroundMovementParameters ground = SlideMovementParameters.Ground with { MaximumSpeed = parameters.MaximumSpeed };
        Vector3D desired = default;
        bool finished = false;
        bool cancel = forceCancel || (elapsed >= SlideMovementParameters.MinimumCancelSeconds - SlideMovementParameters.ElapsedTolerance
            && SlideMovement.WantsToCancel(relativeInput.X, input.Buttons.HasFlag(MovementButtons.Crouch), speed, GroundMovementParameters.Crouched.MaximumSpeed, false));
        if (cancel)
        {
            finished = forceCancel || speed <= SlideMovementParameters.CancelSpeed;
            if (!finished)
            {
                ground = SlideMovementParameters.CancellingGround with { MaximumSpeed = parameters.MaximumSpeed };
            }
        }
        else
        {
            actual = actual with { Slowdown = SlideMovement.AccumulateSlowdown(actual.Slowdown, 0, speed, parameters, seconds) };
            if (speed > 0)
            {
                Vector3D boost = SlideMovement.InitialBoost(Direction(actual.InitialVelocity), speed, parameters, elapsed);
                Vector3D momentum = SlideMovement.Momentum(Direction(previous), speed, parameters);
                desired = Clamp(new Vector3D(boost.X + momentum.X, boost.Y + momentum.Y, 0), speed / parameters.MaximumSpeed);
            }

            double steering = SlideMovementParameters.MaximumSteeringDegrees + ((SlideMovementParameters.MinimumSteeringDegrees - SlideMovementParameters.MaximumSteeringDegrees)
                * Math.Clamp((speed - SlideMovementParameters.MinimumSteeringSpeed) / (parameters.MaximumSpeed - SlideMovementParameters.MinimumSteeringSpeed), 0, 1));
            desired = SlideSteering.Rotate(relativeInput, true, default, desired, steering);
            double limit = relativeInput.X > SlideMovementParameters.ForwardInputThreshold ? 1 : SlideMovementParameters.SidewaysInputLimit;
            desired = Clamp(desired, limit);
        }

        Vector3D velocity = FlatGroundMovement.CalculateVelocity(previous, desired, seconds, ground, steerAboveMaximum, fixBrakingOvershoot);
        if (state.Ticks == 1)
        {
            velocity = new Vector3D(velocity.X * SlideMovementParameters.CrouchStanceSpeedFactor, velocity.Y * SlideMovementParameters.CrouchStanceSpeedFactor, velocity.Z);
        }

        return new SlideStep(actual with { Ticks = state.Ticks + 1 }, velocity, desired, finished);
    }

    private static Vector3D Direction(Vector3D value)
    {
        double squared = (value.X * value.X) + (value.Y * value.Y);
        return squared < SlideMovementParameters.DirectionTolerance ? default : new Vector3D(value.X / Math.Sqrt(squared), value.Y / Math.Sqrt(squared), 0);
    }

    private static Vector3D Clamp(Vector3D value, double maximum)
    {
        double squared = (value.X * value.X) + (value.Y * value.Y);
        return squared <= maximum * maximum || squared == 0 ? value : new Vector3D(value.X * maximum / Math.Sqrt(squared), value.Y * maximum / Math.Sqrt(squared), 0);
    }
}
