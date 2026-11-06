using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>Slope-dependent slide input and directional steering.</summary>
public static class SlideSteering
{
    /// <summary>Evaluates the unrestricted steering angle from speed and downward slope angle.</summary>
    public static double FreeMoveLimit(double maximumSpeed, double speed, double slopeDegrees)
    {
        double slope = Map(slopeDegrees, SlideMovementParameters.SlopeStartDegrees, SlideMovementParameters.SlopeFullDegrees,
            SlideMovementParameters.FreeMoveMaximumDegrees, SlideMovementParameters.FreeMoveMinimumDegrees);
        double velocity = Map(speed, 0, maximumSpeed, SlideMovementParameters.FreeMoveMinimumDegrees, SlideMovementParameters.FreeMoveMaximumDegrees);
        return (slope + velocity) / 2;
    }

    /// <summary>Evaluates the input scale for the downward slope angle.</summary>
    public static double SlopeSteepness(double slopeDegrees, double minimumInput)
        => slopeDegrees < SlideMovementParameters.SlopeStartDegrees
            ? Map(slopeDegrees, SlideMovementParameters.SlopeStartDegrees, SlideMovementParameters.SlopeFullDegrees, minimumInput, 1)
            : 0;

    /// <summary>Evaluates the input scale for the angle between horizontal velocity and ground normal.</summary>
    public static double SlopeAlignment(double freeMoveLimit, double alignmentDegrees, double slopeDegrees, double minimumInput)
        => alignmentDegrees < freeMoveLimit && slopeDegrees < SlideMovementParameters.SlopeStartDegrees
            ? Map(Math.Abs(alignmentDegrees), SlideMovementParameters.SlopeAlignmentDegrees, 0, minimumInput, 1)
            : Map(Math.Abs(alignmentDegrees), freeMoveLimit, SlideMovementParameters.SlopeAlignmentDegrees, minimumInput, 0);

    /// <summary>Adds velocity-relative braking input scaled by the downward slope angle.</summary>
    public static Vector3D Brake(Vector3D input, Vector3D velocityDirection, double slopeDegrees)
    {
        double amount = Map(slopeDegrees, 0, SlideMovementParameters.SlopeFullDegrees, 0, SlideMovementParameters.SlopeBrakingInput);
        return new Vector3D(input.X + (velocityDirection.X * amount), input.Y + (velocityDirection.Y * amount), input.Z + (velocityDirection.Z * amount));
    }

    /// <summary>Evaluates the downhill input contribution from slope, alignment and current speed.</summary>
    public static Vector3D SlopeNudge(double speed, Vector3D groundNormal, double maximumSpeed, double alignment, double slopeDegrees)
    {
        if (slopeDegrees > SlideMovementParameters.SlopeStartDegrees)
        {
            return default;
        }

        double slope = Map(slopeDegrees, 0, SlideMovementParameters.SlopeFullDegrees, 0, 1);
        double speedScale = Math.Max(1, 1 - (speed / (maximumSpeed / 2)));
        double amount = SlideMovementParameters.SlopeNudgeFactor * speedScale * slope * (1 - alignment) * (speed / maximumSpeed);
        double squared = (groundNormal.X * groundNormal.X) + (groundNormal.Y * groundNormal.Y);
        if (squared < SlideMovementParameters.DirectionTolerance)
        {
            return default;
        }

        double length = Math.Sqrt(squared);
        return new Vector3D(groundNormal.X / length * amount, groundNormal.Y / length * amount, 0);
    }

    /// <summary>Rotates slide input about the vertical axis when unrestricted or aligned downhill.</summary>
    /// <param name="rawInput">Movement input in the steering coordinate system.</param>
    /// <param name="withinFreeMoveZone">Whether the current alignment allows unrestricted steering.</param>
    /// <param name="groundNormal">Current ground surface normal.</param>
    /// <param name="slideInput">World-space slide input to steer.</param>
    /// <param name="strengthDegrees">Steering angle at full sideways input.</param>
    /// <returns>The steered slide input.</returns>
    public static Vector3D Rotate(Vector3D rawInput, bool withinFreeMoveZone, Vector3D groundNormal, Vector3D slideInput, double strengthDegrees)
    {
        Vector3D rotatedNormal = RotateHorizontal(groundNormal, Math.Atan2(rawInput.Y, rawInput.X));
        double alignment = (rotatedNormal.X * slideInput.X) + (rotatedNormal.Y * slideInput.Y) + (rotatedNormal.Z * slideInput.Z);
        return !withinFreeMoveZone && alignment >= 0
            ? slideInput
            : RotateHorizontal(slideInput, rawInput.Y * strengthDegrees * Math.PI / 180);
    }

    private static Vector3D RotateHorizontal(Vector3D value, double radians)
    {
        double cosine = Math.Cos(radians);
        double sine = Math.Sin(radians);
        return new Vector3D((value.X * cosine) - (value.Y * sine), (value.X * sine) + (value.Y * cosine), value.Z);
    }

    private static double Map(double value, double start, double end, double outputStart, double outputEnd)
        => outputStart + ((outputEnd - outputStart) * Math.Clamp((value - start) / (end - start), 0, 1));
}
