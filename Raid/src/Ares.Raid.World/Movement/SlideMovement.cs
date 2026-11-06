using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>Slide entry, cancellation and directional input contributions.</summary>
public static class SlideMovement
{
    /// <summary>Checks the movement-input and speed requirements for entering a slide.</summary>
    /// <param name="input">Movement axes and button edges.</param>
    /// <param name="speed">Current horizontal world speed.</param>
    /// <param name="movementSpeedMultiplier">Combined movement speed multiplier.</param>
    /// <returns>Whether the movement requirements allow a new slide.</returns>
    public static bool CanStart(MovementInput input, double speed, double movementSpeedMultiplier = 1)
    {
        double strength = (Math.Abs((int)input.Forward) + Math.Abs((int)input.Right)) / (double)PendulumInput.MovementAxisMaximum;
        return input.CrouchPressed && input.Buttons.HasFlag(MovementButtons.Sprint)
            && strength > SlideMovementParameters.EntryInputThreshold
            && speed >= SlideMovementParameters.EntrySpeed * movementSpeedMultiplier;
    }

    /// <summary>Checks whether slide input or speed requests cancellation.</summary>
    /// <param name="relativeForward">Forward input relative to the velocity heading.</param>
    /// <param name="crouching">Whether crouch is held.</param>
    /// <param name="speed">Current horizontal world speed.</param>
    /// <param name="crouchSpeed">Base crouch movement speed.</param>
    /// <param name="playingRootMotion">Whether root motion is playing.</param>
    /// <returns>Whether the slide requests cancellation.</returns>
    public static bool WantsToCancel(double relativeForward, bool crouching, double speed, double crouchSpeed, bool playingRootMotion)
        => relativeForward < SlideMovementParameters.CancelForwardThreshold || !crouching || speed < crouchSpeed || playingRootMotion;

    /// <summary>Evaluates the initial directional boost as normalized slide input.</summary>
    /// <param name="initialDirection">Normalized initial slide direction.</param>
    /// <param name="speed">Current horizontal world speed.</param>
    /// <param name="parameters">Slide speed and friction parameters.</param>
    /// <param name="seconds">Elapsed slide time in seconds.</param>
    /// <returns>The boost contribution, which falls to zero at the boost duration.</returns>
    public static Vector3D InitialBoost(Vector3D initialDirection, double speed, SlideMovementParameters parameters, double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        if (seconds >= SlideMovementParameters.BoostSeconds)
        {
            return default;
        }

        double amount = speed / parameters.MaximumSpeed * (1 - (seconds / SlideMovementParameters.BoostSeconds));
        return Scale(initialDirection, amount);
    }

    /// <summary>Evaluates the directional momentum contribution as normalized slide input.</summary>
    /// <param name="velocityDirection">Normalized current velocity direction.</param>
    /// <param name="speed">Current horizontal world speed.</param>
    /// <param name="parameters">Slide speed and friction parameters.</param>
    /// <returns>The momentum contribution.</returns>
    public static Vector3D Momentum(Vector3D velocityDirection, double speed, SlideMovementParameters parameters)
        => Scale(Scale(velocityDirection, speed / parameters.MaximumSpeed), SlideMovementParameters.MomentumFactor);

    /// <summary>Evaluates the per-second change in accumulated slowdown.</summary>
    /// <param name="steepness">Slide slope steepness scale.</param>
    /// <param name="speed">Current horizontal world speed.</param>
    /// <param name="parameters">Slide speed and combined friction parameters.</param>
    /// <returns>The rate before multiplying by the simulation step duration.</returns>
    public static double SlowdownRate(double steepness, double speed, SlideMovementParameters parameters)
    {
        if (steepness > SlideMovementParameters.SlowdownRecoveryThreshold)
        {
            return SlideMovementParameters.SlowdownRecoveryRate;
        }

        double rate = SlideMovementParameters.SlowdownRate * parameters.FrictionMultiplier * (1 - steepness);
        return rate * (parameters.MaximumSpeed / Math.Max(SlideMovementParameters.SlowdownMinimumSpeed, speed));
    }

    /// <summary>Accumulates slide slowdown with the movement node's float precision and bounds.</summary>
    public static float AccumulateSlowdown(float previous, double steepness, double speed, SlideMovementParameters parameters, float seconds)
    {
        if (!float.IsFinite(seconds) || seconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        return Math.Clamp((float)(previous + (SlowdownRate(steepness, speed, parameters) * seconds)), 0, 1);
    }

    private static Vector3D Scale(Vector3D direction, double amount) => new(direction.X * amount, direction.Y * amount, direction.Z * amount);
}
