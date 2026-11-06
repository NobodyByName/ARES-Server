using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>Horizontal ground velocity on a level, stationary surface.</summary>
public static class FlatGroundMovement
{
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

        if (!double.IsFinite(yaw))
        {
            throw new ArgumentOutOfRangeException(nameof(yaw));
        }

        double forward = input.Forward / (double)PendulumInput.MovementAxisMaximum;
        double right = input.Right / (double)PendulumInput.MovementAxisMaximum;
        double cosine = Math.Cos(yaw);
        double sine = Math.Sin(yaw);
        double directionX = (cosine * forward) - (sine * right);
        double directionY = (sine * forward) + (cosine * right);
        return StepVelocity(previous, new Vector3D(directionX, directionY, 0), seconds, parameters, steerAboveMaximum, fixBrakingOvershoot);
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
        GroundMovementParameters parameters, bool steerAboveMaximum, bool fixBrakingOvershoot)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        double directionX = direction.X;
        double directionY = direction.Y;
        double magnitude = Length(directionX, directionY);
        if (magnitude > 1)
        {
            directionX /= magnitude;
            directionY /= magnitude;
            magnitude = 1;
        }

        double x = previous.X;
        double y = previous.Y;
        double speed = Length(x, y);
        double maximum = Math.Max(0, parameters.MaximumSpeed) * magnitude;
        double friction = Math.Max(0, parameters.Friction);
        double accelerationX = directionX * parameters.Acceleration;
        double accelerationY = directionY * parameters.Acceleration;
        bool overspeed = speed > maximum * GroundMovementParameters.OverspeedTolerance;
        if (magnitude == 0 || overspeed)
        {
            double deceleration = Math.Max(0, parameters.BrakingDeceleration);
            double brakingFriction = Math.Max(0, friction * Math.Max(0, parameters.BrakingFrictionFactor));
            if (speed > 0 && (brakingFriction != 0 || deceleration != 0))
            {
                x += ((-brakingFriction * x) - (deceleration * x / speed)) * seconds;
                y += ((-brakingFriction * y) - (deceleration * y / speed)) * seconds;
                double squared = (x * x) + (y * y);
                if ((x * previous.X) + (y * previous.Y) <= 0 || squared <= GroundMovementParameters.NearlyZeroSquaredSpeed
                    || (deceleration != 0 && squared <= GroundMovementParameters.BrakingStopSpeed * GroundMovementParameters.BrakingStopSpeed))
                {
                    (x, y) = (0, 0);
                }
            }

            if (fixBrakingOvershoot && overspeed && Length(x, y) < maximum
                && (previous.X * accelerationX) + (previous.Y * accelerationY) > 0)
            {
                x = previous.X * maximum / speed;
                y = previous.Y * maximum / speed;
            }
        }
        else if (speed > 0)
        {
            double amount = Math.Min(1, seconds * friction);
            x -= (x - (directionX * speed / magnitude)) * amount;
            y -= (y - (directionY * speed / magnitude)) * amount;
        }

        if (magnitude <= 0)
        {
            return new Vector3D(x, y, 0);
        }

        double limit = steerAboveMaximum && overspeed ? Math.Max(maximum, Length(x, y)) : maximum;
        x += accelerationX * seconds;
        y += accelerationY * seconds;
        double acceleratedSpeed = Length(x, y);
        if (acceleratedSpeed <= limit)
        {
            return new Vector3D(x, y, 0);
        }

        x *= limit / acceleratedSpeed;
        y *= limit / acceleratedSpeed;
        return new Vector3D(x, y, 0);
    }

    private static double Length(double x, double y) => Math.Sqrt((x * x) + (y * y));

    private static double RoundVelocity(double value) => Math.Floor(((float)Math.Floor((value * 100) + 0.5) / 100.0f) + 0.5);
}
