using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>Which way a character rides a traversable line (<c>ETraversableLineTravelDirection</c>).</summary>
public enum ZiplineTravelDirection : byte
{
    /// <summary>Not riding.</summary>
    None = 0,

    /// <summary>From the line start towards its end.</summary>
    Away = 1,

    /// <summary>From the line end back towards its start.</summary>
    Home = 2,
}

/// <summary>
/// A point near which a rider is launched off a zipline (<c>FZiplineExitPoint</c>), in the line actor's space.
/// </summary>
/// <param name="LaunchTarget">Where the launch aims, in the line actor's space.</param>
/// <param name="LaunchFrom">Where the launch starts, in the line actor's space.</param>
/// <param name="LaunchSpeed">Launch speed; with <paramref name="MoreExact"/> the horizontal speed of a ballistic arc onto the target.</param>
/// <param name="LaunchFromDistance">Distance before <paramref name="LaunchFrom"/> where the exit is offered.</param>
/// <param name="Additive">Whether the launch adds the rider's velocity.</param>
/// <param name="MoreExact">Whether the launch is a ballistic arc that lands on the target.</param>
/// <param name="Advanced">The exit point's <c>bAdvanced</c> setting.</param>
/// <param name="TravelDirection">The riding direction the exit applies to.</param>
/// <param name="Fraction">Share of the line length where the exit triggers.</param>
/// <param name="DistanceAsFraction"><paramref name="LaunchFromDistance"/> as a share of the line length.</param>
/// <param name="ResetAndLaunch">The exit point's <c>bResetAndLaunchCharacterIfAbilityActivationSuccess</c> setting.</param>
public readonly record struct ZiplineExitPoint(
    Vector3D LaunchTarget,
    Vector3D LaunchFrom,
    double LaunchSpeed,
    double LaunchFromDistance,
    bool Additive,
    bool MoreExact,
    bool Advanced,
    ZiplineTravelDirection TravelDirection,
    double Fraction,
    double DistanceAsFraction,
    bool ResetAndLaunch);

/// <summary>
/// A rideable zipline placed in a World Partition cell's persistent level (<c>AZippableLine</c>): the line runs from
/// the actor's location to <see cref="EndAttachPosition"/> in the actor's space.
/// </summary>
/// <param name="Cell">Generated cell package.</param>
/// <param name="Actor">Line actor name in the cell.</param>
/// <param name="Location">World location of the actor (the line start), in centimetres.</param>
/// <param name="Rotation">World rotation of the actor.</param>
/// <param name="EndAttachPosition">End of the line in the actor's space.</param>
/// <param name="ExitPoints">The line's exit points.</param>
public readonly record struct ZiplinePlacement(
    string Cell,
    string Actor,
    Vector3D Location,
    Rotator Rotation,
    Vector3D EndAttachPosition,
    IReadOnlyList<ZiplineExitPoint> ExitPoints)
{
    /// <summary>World position of the line end.</summary>
    public Vector3D End
    {
        get
        {
            Vector3D local = Rotation.Rotate(EndAttachPosition);
            return new Vector3D(Location.X + local.X, Location.Y + local.Y, Location.Z + local.Z);
        }
    }
}

/// <summary>
/// An activatable zipline placed in a World Partition cell's persistent level (<c>AActivatableStaticZipline</c>): a
/// launcher whose activation interaction makes the server deploy an anchor at <see cref="AnchorLocation"/> and spawn
/// its line.
/// </summary>
/// <param name="Cell">Generated cell package.</param>
/// <param name="Actor">Launcher actor name in the cell.</param>
/// <param name="Location">World location of the launcher, in centimetres.</param>
/// <param name="Rotation">World rotation of the launcher.</param>
/// <param name="AnchorLocation">The launcher's <c>AnchorLocation</c> setting, in the launcher's space.</param>
/// <param name="StartDeployed">Whether the launcher starts deployed (<c>bStartDeployed</c>).</param>
/// <param name="TwoSideActivation">Whether the launcher is activated from both ends (<c>bTwoSideActivation</c>).</param>
/// <param name="LineClass">Line class package the launcher spawns, or <see langword="null"/> for the launcher class default.</param>
public readonly record struct ActivatableZiplinePlacement(
    string Cell,
    string Actor,
    Vector3D Location,
    Rotator Rotation,
    Vector3D AnchorLocation,
    bool StartDeployed,
    bool TwoSideActivation,
    string? LineClass);
