using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>Horizontal ground velocity on a level, stationary surface.</summary>
public static class FlatGroundMovement
{
    /// <summary>Normal of level ground.</summary>
    public static readonly Vector3D Level = new(0, 0, 1);

    /// <summary>Advances acceleration, turning and braking for one simulation step.</summary>
    /// <param name="previous">Previous world velocity.</param>
    /// <param name="input">Signed movement axes.</param>
    /// <param name="yaw">Control heading in radians.</param>
    /// <param name="seconds">Simulation step duration in seconds.</param>
    /// <param name="parameters">Movement speed, acceleration and friction.</param>
    /// <param name="steerAboveMaximum">Whether input steering is enabled above maximum speed.</param>
    /// <param name="fixBrakingOvershoot">Whether braking overshoot is corrected when accelerating forward.</param>
    /// <returns>The next world velocity, with zero vertical velocity.</returns>
    public static Vector3D StepVelocity(Vector3D previous, MovementInput input, double yaw, double seconds,
        GroundMovementParameters parameters, bool steerAboveMaximum, bool fixBrakingOvershoot)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        return double.IsFinite(yaw)
            ? StepVelocity(previous, Direction(input, yaw), seconds, parameters, steerAboveMaximum, fixBrakingOvershoot)
            : throw new ArgumentOutOfRangeException(nameof(yaw));
    }

    /// <summary>
    /// The movement axes as a world direction; its length is the input strength. Axes that together are a full input
    /// up to their rounding, like a diagonal, count as full.
    /// </summary>
    /// <param name="input">Movement axes.</param>
    /// <param name="yaw">Control heading in radians.</param>
    /// <returns>The horizontal direction.</returns>
    public static Vector3D Direction(MovementInput input, double yaw)
    {
        double forward = input.Forward / (double)PendulumInput.MovementAxisMaximum;
        double right = input.Right / (double)PendulumInput.MovementAxisMaximum;
        double strength = Math.Sqrt((forward * forward) + (right * right));
        if (strength > 0 && Math.Abs(1 - strength) < GroundMovementParameters.InputStrengthSnapTolerance)
        {
            forward /= strength;
            right /= strength;
        }

        double cosine = Math.Cos(yaw);
        double sine = Math.Sin(yaw);
        return new Vector3D((cosine * forward) - (sine * right), (sine * forward) + (cosine * right), 0);
    }

    /// <summary>
    /// Advances ground velocity on the ground the character stands on. The velocity runs along the ground: its whole
    /// speed, vertical part included, is held to the maximum, and the input pushes and steers along the ground in the
    /// input's horizontal direction.
    /// </summary>
    /// <param name="previous">Previous world velocity, vertical part included.</param>
    /// <param name="direction">Horizontal world-space movement input (<see cref="SlopeInput"/> applied), clamped to unit length.</param>
    /// <param name="groundNormal">Normal of the ground under the character; straight up on level ground.</param>
    /// <param name="seconds">Simulation step duration in seconds.</param>
    /// <param name="parameters">Movement speed, acceleration and friction.</param>
    /// <param name="steerAboveMaximum">Whether input steering is enabled above maximum speed.</param>
    /// <param name="fixBrakingOvershoot">Whether braking overshoot is corrected when accelerating forward.</param>
    /// <returns>The next world velocity in whole units per component.</returns>
    public static Vector3D StepVelocity(Vector3D previous, Vector3D direction, Vector3D groundNormal, double seconds,
        GroundMovementParameters parameters, bool steerAboveMaximum, bool fixBrakingOvershoot)
    {
        Vector3D velocity = CalculateVelocity(previous, direction, groundNormal, seconds, parameters, steerAboveMaximum, fixBrakingOvershoot);
        return new Vector3D(RoundVelocity(velocity.X), RoundVelocity(velocity.Y), RoundVelocity(velocity.Z));
    }

    /// <summary>Calculates ground velocity along the ground before the walking node's component rounding.</summary>
    /// <param name="previous">Previous world velocity, vertical part included.</param>
    /// <param name="direction">Horizontal world-space movement input, clamped to unit length.</param>
    /// <param name="groundNormal">Normal of the ground under the character, with a positive vertical part.</param>
    /// <param name="seconds">Simulation step duration in seconds.</param>
    /// <param name="parameters">Movement speed, acceleration and friction.</param>
    /// <param name="steerAboveMaximum">Whether input steering is enabled above maximum speed.</param>
    /// <param name="fixBrakingOvershoot">Whether braking overshoot is corrected when accelerating forward.</param>
    /// <returns>The next world velocity, preserving fractional components.</returns>
    public static Vector3D CalculateVelocity(Vector3D previous, Vector3D direction, Vector3D groundNormal, double seconds,
        GroundMovementParameters parameters, bool steerAboveMaximum, bool fixBrakingOvershoot)
    {
        if (!double.IsFinite(groundNormal.Z) || groundNormal.Z <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(groundNormal));
        }

        return Calculate(previous, direction, groundNormal, seconds, parameters, steerAboveMaximum, fixBrakingOvershoot);
    }

    /// <summary>
    /// Weakens input on ground steeper than <see cref="DownedMovementParameters.SlopeStartDegrees"/>: the steeper the
    /// ground and the more the input points uphill, the weaker the input (<see cref="DownedMovement.Slope"/>).
    /// </summary>
    /// <param name="direction">Horizontal world-space movement input.</param>
    /// <param name="groundNormal">Normal of the ground under the character.</param>
    /// <returns>The input to move with, and the scale when the slope set one.</returns>
    public static (Vector3D Input, double? Scalar) SlopeInput(Vector3D direction, Vector3D groundNormal)
    {
        if (DownedMovement.SlopeDegrees(groundNormal) <= DownedMovementParameters.SlopeStartDegrees)
        {
            return (direction, null);
        }

        (Vector3D input, double scalar) = DownedMovement.Slope(direction, groundNormal);
        return (input, scalar);
    }

    /// <summary>Advances ground velocity from continuous world-space movement input.</summary>
    /// <param name="previous">Previous world velocity.</param>
    /// <param name="direction">World-space movement input, clamped to unit horizontal length.</param>
    /// <param name="seconds">Simulation step duration in seconds.</param>
    /// <param name="parameters">Movement speed, acceleration and friction.</param>
    /// <param name="steerAboveMaximum">Whether input steering is enabled above maximum speed.</param>
    /// <param name="fixBrakingOvershoot">Whether braking overshoot is corrected when accelerating forward.</param>
    /// <returns>The next horizontal world velocity.</returns>
    private static Vector3D StepVelocity(Vector3D previous, Vector3D direction, double seconds,
        GroundMovementParameters parameters, bool steerAboveMaximum, bool fixBrakingOvershoot)
    {
        Vector3D velocity = CalculateVelocity(previous, direction, seconds, parameters, steerAboveMaximum, fixBrakingOvershoot);
        return new Vector3D(RoundVelocity(velocity.X), RoundVelocity(velocity.Y), 0);
    }

    /// <summary>Calculates horizontal ground velocity before the walking node's component rounding.</summary>
    /// <param name="previous">Previous world velocity.</param>
    /// <param name="direction">Continuous world-space movement input.</param>
    /// <param name="seconds">Simulation step duration in seconds.</param>
    /// <param name="parameters">Movement speed, acceleration and friction.</param>
    /// <param name="steerAboveMaximum">Whether input steering is enabled above maximum speed.</param>
    /// <param name="fixBrakingOvershoot">Whether braking overshoot is corrected when accelerating forward.</param>
    /// <returns>The next horizontal velocity, preserving fractional components.</returns>
    public static Vector3D CalculateVelocity(Vector3D previous, Vector3D direction, double seconds,
        GroundMovementParameters parameters, bool steerAboveMaximum, bool fixBrakingOvershoot) =>
        Calculate(previous with { Z = 0 }, direction, Level, seconds, parameters, steerAboveMaximum, fixBrakingOvershoot) with { Z = 0 };

    private static Vector3D Calculate(Vector3D previous, Vector3D direction, Vector3D groundNormal, double seconds,
        GroundMovementParameters parameters, bool steerAboveMaximum, bool fixBrakingOvershoot)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        double directionX = direction.X;
        double directionY = direction.Y;
        double magnitude = Length(directionX, directionY, 0);
        if (magnitude > 1)
        {
            directionX /= magnitude;
            directionY /= magnitude;
            magnitude = 1;
        }

        double x = previous.X;
        double y = previous.Y;
        double z = previous.Z;
        double speed = Length(x, y, z);
        double maximum = Math.Max(0, parameters.MaximumSpeed) * magnitude;
        double friction = Math.Max(0, parameters.Friction);

        // The input pushes along the ground, in its own horizontal direction and with its own strength.
        double accelerationX = directionX * parameters.Acceleration;
        double accelerationY = directionY * parameters.Acceleration;
        double accelerationZ = 0;
        double rise = Rise(directionX, directionY, groundNormal);
        if (rise != 0)
        {
            double share = parameters.Acceleration * magnitude / Length(directionX, directionY, rise);
            (accelerationX, accelerationY, accelerationZ) = (directionX * share, directionY * share, rise * share);
        }

        bool overspeed = speed > maximum * GroundMovementParameters.OverspeedTolerance;
        if (magnitude == 0 || overspeed)
        {
            double deceleration = Math.Max(0, parameters.BrakingDeceleration);
            double brakingFriction = Math.Max(0, friction * Math.Max(0, parameters.BrakingFrictionFactor));
            if (speed > 0 && (brakingFriction != 0 || deceleration != 0))
            {
                x += ((-brakingFriction * x) - (deceleration * x / speed)) * seconds;
                y += ((-brakingFriction * y) - (deceleration * y / speed)) * seconds;
                z += ((-brakingFriction * z) - (deceleration * z / speed)) * seconds;
                double squared = (x * x) + (y * y) + (z * z);
                if ((x * previous.X) + (y * previous.Y) + (z * previous.Z) <= 0 || squared <= GroundMovementParameters.NearlyZeroSquaredSpeed
                    || (deceleration != 0 && squared <= GroundMovementParameters.BrakingStopSpeed * GroundMovementParameters.BrakingStopSpeed))
                {
                    (x, y, z) = (0, 0, 0);
                }
            }

            if (fixBrakingOvershoot && overspeed && Length(x, y, z) < maximum
                && (previous.X * accelerationX) + (previous.Y * accelerationY) + (previous.Z * accelerationZ) > 0)
            {
                x = previous.X * maximum / speed;
                y = previous.Y * maximum / speed;
                z = previous.Z * maximum / speed;
            }
        }
        else if (speed > 0)
        {
            // Friction pulls the velocity towards the input scaled by the current speed, so partial input also slows it.
            // That target follows the ground under the input's horizontal direction.
            double amount = Math.Min(1, seconds * friction);
            double targetX = directionX * speed;
            double targetY = directionY * speed;
            x -= (x - targetX) * amount;
            y -= (y - targetY) * amount;
            z -= (z - Rise(targetX, targetY, groundNormal)) * amount;
        }

        if (magnitude <= 0)
        {
            return new Vector3D(x, y, z);
        }

        double limit = steerAboveMaximum && overspeed ? Math.Max(maximum, Length(x, y, z)) : maximum;
        x += accelerationX * seconds;
        y += accelerationY * seconds;
        z += accelerationZ * seconds;
        double acceleratedSpeed = Length(x, y, z);
        if (acceleratedSpeed <= limit)
        {
            return new Vector3D(x, y, z);
        }

        x *= limit / acceleratedSpeed;
        y *= limit / acceleratedSpeed;
        z *= limit / acceleratedSpeed;
        return new Vector3D(x, y, z);
    }

    // Vertical part that puts a horizontal vector on the ground plane straight above or below it.
    private static double Rise(double x, double y, Vector3D groundNormal) =>
        -((x * groundNormal.X) + (y * groundNormal.Y)) / groundNormal.Z;

    private static double Length(double x, double y, double z) => Math.Sqrt((x * x) + (y * y) + (z * z));

    private static double RoundVelocity(double value) => Math.Floor(((float)Math.Floor((value * 100) + 0.5) / 100.0f) + 0.5);
}
