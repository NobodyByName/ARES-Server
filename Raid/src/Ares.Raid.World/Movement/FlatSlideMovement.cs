using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>Persistent directional input state for a slide.</summary>
/// <param name="InitialVelocity">Velocity when slide input is first evaluated.</param>
/// <param name="Slowdown">Accumulated slowdown between zero and one.</param>
/// <param name="Ticks">Slide steps already taken; for the first ones the caller blends the slide's velocity with
/// that of the movement it leaves (<see cref="SlideMovementParameters.BlendTicks"/>).</param>
public readonly record struct SlideState(Vector3D InitialVelocity, float Slowdown, int Ticks = 0)
{
    /// <summary>Input frame of the tick whose crouch press started the slide.</summary>
    public uint StartFrame { get; init; }

    /// <summary>The slide's maximum speed, once its first step has run.</summary>
    public double MaximumSpeed { get; init; }
}

/// <summary>Result of one level-ground slide step.</summary>
/// <param name="State">Persistent slide state.</param>
/// <param name="Velocity">Next horizontal world velocity.</param>
/// <param name="Input">Continuous world-space slide input.</param>
/// <param name="Finished">Whether cancellation requests leaving the slide node.</param>
public readonly record struct SlideStep(SlideState State, Vector3D Velocity, Vector3D Input, bool Finished);

/// <summary>Slide motion on stationary ground, level or sloped.</summary>
public static class FlatSlideMovement
{
    private static readonly Vector3D s_up = new(0, 0, 1);

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
    /// <param name="groundNormal">Unit normal of the ground under the character; zero for level ground.</param>
    /// <param name="falling">Whether the character is off the ground.</param>
    /// <returns>Slide state, velocity and cancellation result.</returns>
    public static SlideStep Step(SlideState state, Vector3D previous, MovementInput input, double yaw, double elapsed, float seconds,
        SlideMovementParameters parameters, bool steerAboveMaximum, bool fixBrakingOvershoot, bool forceCancel = false,
        Vector3D groundNormal = default, bool falling = false)
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
            // The slope's part of the input: downhill along the velocity, scaled by how steep the ground is and how well
            // the velocity follows the slope; uphill on a real slope it brakes instead.
            Vector3D normal = groundNormal == default ? s_up : groundNormal;
            Vector3D velocityDirection = Direction(previous);
            Vector3D downhill = Direction(normal);
            double slopeDegrees = -Degrees(Math.Acos(Math.Clamp(normal.Z, -1, 1)));
            double alignmentDegrees = Degrees(Math.Acos(Math.Clamp((velocityDirection.X * downhill.X) + (velocityDirection.Y * downhill.Y), -1, 1)));
            double freeMoveLimit = SlideSteering.FreeMoveLimit(SlideMovementParameters.SlopeStartDegrees, SlideMovementParameters.SlopeFullDegrees, slopeDegrees);
            bool uphill = Math.Abs(alignmentDegrees) > SlideMovementParameters.SlopeUphillAlignmentDegrees && slopeDegrees < SlideMovementParameters.SlopeStartDegrees;
            bool nearlyLevel = Math.Abs(slopeDegrees) < Math.Abs(SlideMovementParameters.SlopeStartDegrees);
            double minimumInput = SlideMovementParameters.SlopeMinimumInputSpeed / parameters.MaximumSpeed * SlideMovementParameters.SlopeMinimumInputMargin;
            double steepness = SlideSteering.SlopeSteepness(slopeDegrees, minimumInput);
            double alignment = CircularOut((float)SlideSteering.SlopeAlignment(freeMoveLimit, alignmentDegrees, slopeDegrees, minimumInput));
            desired = Scale(velocityDirection, (steepness + alignment) / 2);
            if (uphill)
            {
                desired = SlideSteering.Brake(desired, velocityDirection, slopeDegrees);
            }
            else if (!nearlyLevel)
            {
                Vector3D nudge = SlideSteering.SlopeNudge(speed, normal, parameters.MaximumSpeed, alignment, slopeDegrees);
                desired = new Vector3D(desired.X + nudge.X, desired.Y + nudge.Y, desired.Z + nudge.Z);
            }

            actual = actual with { Slowdown = SlideMovement.AccumulateSlowdown(actual.Slowdown, steepness, speed, parameters, seconds) };
            desired = Scale(desired, 1f - actual.Slowdown);

            // While the slope's part stays below the speed's share of the maximum, the initial boost and the momentum add to it.
            double share = speed / parameters.MaximumSpeed;
            if (!uphill && !falling && SquaredLength(desired) < share * share)
            {
                Vector3D boost = SlideMovement.InitialBoost(Direction(actual.InitialVelocity), speed, parameters, elapsed);
                Vector3D momentum = SlideMovement.Momentum(velocityDirection, speed, parameters);
                desired = Clamp(new Vector3D(desired.X + boost.X + momentum.X, desired.Y + boost.Y + momentum.Y, desired.Z + boost.Z + momentum.Z), share);
            }

            double steering = SlideMovementParameters.MaximumSteeringDegrees + ((SlideMovementParameters.MinimumSteeringDegrees - SlideMovementParameters.MaximumSteeringDegrees)
                * Math.Clamp((speed - SlideMovementParameters.MinimumSteeringSpeed) / (parameters.MaximumSpeed - SlideMovementParameters.MinimumSteeringSpeed), 0, 1));
            desired = SlideSteering.Rotate(relativeInput, alignmentDegrees < freeMoveLimit || nearlyLevel, downhill, desired, steering);
            double limit = relativeInput.X > SlideMovementParameters.ForwardInputThreshold ? 1 : SlideMovementParameters.SidewaysInputLimit;
            desired = Clamp(desired, limit);
        }

        // On the ground the slide moves along it like ground movement does.
        Vector3D velocity = groundNormal == default
            ? FlatGroundMovement.CalculateVelocity(previous, desired, seconds, ground, steerAboveMaximum, fixBrakingOvershoot)
            : FlatGroundMovement.CalculateVelocity(previous, desired, groundNormal, seconds, ground, steerAboveMaximum, fixBrakingOvershoot);
        return new SlideStep(actual with { Ticks = state.Ticks + 1, MaximumSpeed = parameters.MaximumSpeed }, velocity, desired, finished);
    }

    private static Vector3D Direction(Vector3D value)
    {
        double squared = (value.X * value.X) + (value.Y * value.Y);
        return squared < SlideMovementParameters.DirectionTolerance ? default : new Vector3D(value.X / Math.Sqrt(squared), value.Y / Math.Sqrt(squared), 0);
    }

    private static Vector3D Scale(Vector3D value, double amount) => new(value.X * amount, value.Y * amount, value.Z * amount);

    private static double SquaredLength(Vector3D value) => (value.X * value.X) + (value.Y * value.Y) + (value.Z * value.Z);

    private static double Degrees(double radians) => radians * 180 / Math.PI;

    // The alignment scale eased out along a quarter circle.
    private static float CircularOut(float alpha)
    {
        float shifted = alpha - 1;
        return Math.Clamp(MathF.Sqrt(1 - (shifted * shifted)), 0, 1);
    }

    private static Vector3D Clamp(Vector3D value, double maximum)
    {
        double squared = (value.X * value.X) + (value.Y * value.Y);
        return squared <= maximum * maximum || squared == 0 ? value : new Vector3D(value.X * maximum / Math.Sqrt(squared), value.Y * maximum / Math.Sqrt(squared), 0);
    }
}
