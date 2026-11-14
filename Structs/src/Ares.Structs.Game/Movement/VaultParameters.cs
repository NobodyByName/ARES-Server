namespace Ares.Structs.Game.Movement;

/// <summary>How a vault crosses the obstacle it found.</summary>
public enum VaultKind : byte
{
    /// <summary>No vault.</summary>
    None = 0,

    /// <summary>Over the obstacle and down its far side.</summary>
    Over = 1,

    /// <summary>Onto the top of the obstacle.</summary>
    Onto = 2,
}

/// <summary>Root position of a vault animation relative to the obstacle edge it was authored against.</summary>
/// <param name="X">Along the vault direction, in world units (negative before the edge).</param>
/// <param name="Y">To the right of the vault direction.</param>
/// <param name="Z">Up from the edge.</param>
public readonly record struct VaultPathPoint(float X, float Y, float Z);

/// <summary>One vault animation.</summary>
/// <param name="Name">Animation sequence name.</param>
/// <param name="Length">Play length in animation seconds.</param>
/// <param name="SampleSeconds">Animation time between two root path points.</param>
/// <param name="Path">Root positions from animation time 0 every <paramref name="SampleSeconds"/>; empty for animations without one.</param>
public sealed record VaultSequence(string Name, float Length, float SampleSeconds, IReadOnlyList<VaultPathPoint> Path);

/// <summary>An animation chosen for one vault case.</summary>
/// <param name="Sequence">The animation.</param>
/// <param name="PlayRate">Play rate of this case.</param>
/// <param name="KeepDirection">Whether the vault keeps the character's approach direction instead of facing the wall.</param>
/// <param name="StepUp">Whether the vault is a step up.</param>
/// <param name="DistanceMatching">Whether the animation starts where its root path is closest to the character.</param>
/// <param name="MaxEntrySecondsFromGround">Latest start time, in animation seconds, for a vault from the ground.</param>
/// <param name="MaxEntrySeconds">Latest start time, in animation seconds, otherwise.</param>
public sealed record VaultClip(
    VaultSequence Sequence,
    double PlayRate,
    bool KeepDirection,
    bool StepUp,
    bool DistanceMatching,
    float MaxEntrySecondsFromGround,
    float MaxEntrySeconds);

/// <summary>The clips for one approach speed.</summary>
/// <param name="Speed">Horizontal approach speed the clips were made for.</param>
/// <param name="Over">Clip for a vault over the obstacle.</param>
/// <param name="Onto">Clip for a vault onto the obstacle.</param>
public sealed record VaultSpeedDefinition(double Speed, VaultClip Over, VaultClip Onto);

/// <summary>The clips for obstacles from one height up.</summary>
/// <param name="Name">Name of the row.</param>
/// <param name="Height">Lowest obstacle height the row covers.</param>
/// <param name="Speeds">Clips by approach speed.</param>
public sealed record VaultHeightDefinition(string Name, double Height, IReadOnlyList<VaultSpeedDefinition> Speeds);

/// <summary>One set of vault definitions.</summary>
/// <param name="GlobalPlayRate">Play rate applied to every clip of the set.</param>
/// <param name="FallCatch">Rows used when catching a ledge while falling fast.</param>
/// <param name="InAir">Rows used in the air otherwise.</param>
/// <param name="Ground">Rows used on the ground.</param>
public sealed record VaultDefinitionSet(
    double GlobalPlayRate,
    IReadOnlyList<VaultHeightDefinition> FallCatch,
    IReadOnlyList<VaultHeightDefinition> InAir,
    IReadOnlyList<VaultHeightDefinition> Ground);

/// <summary>Vault detection, selection and timing rules.</summary>
public static class VaultParameters
{
    /// <summary>Bit of the character claims flags (<see cref="PendulumInput.CharacterClaimsFlagsField"/>) a client sets to request a vault.</summary>
    public const int RequestBit = 7;

    /// <summary>Bit of the character claims flags set while the character is immobilized.</summary>
    public const int ImmobilizedBit = 2;

    /// <summary>Seconds after a jump press, or after a jump took off, that count as recent.</summary>
    public const double JumpWindowSeconds = 0.5;

    /// <summary>Height above the floor beyond which a held or recent jump keeps looking for a vault.</summary>
    public const double AirAttemptFloorDistance = 50;

    /// <summary>Downward speed beyond which no vault is looked for.</summary>
    public const double MaximumFallSpeed = 2500;

    /// <summary>Downward speed beyond which a falling character catches a ledge instead of vaulting.</summary>
    public const double FallCatchSpeed = 100;

    /// <summary>Lowest obstacle height at rest.</summary>
    public const double MinimumHeightAtRest = 30;

    /// <summary>Lowest obstacle height at <see cref="MinimumHeightTopSpeed"/> and above.</summary>
    public const double MinimumHeightAtSpeed = 50;

    /// <summary>Horizontal speed at which the lowest obstacle height reaches <see cref="MinimumHeightAtSpeed"/>.</summary>
    public const double MinimumHeightTopSpeed = 300;

    /// <summary>Highest obstacle a vault from the ground climbs.</summary>
    public const double GroundMaximumHeight = 350;

    /// <summary>Highest obstacle a vault in the air climbs.</summary>
    public const double AirMaximumHeight = 250;

    /// <summary>Highest obstacle a vault while falling faster than <see cref="FallCatchSpeed"/> climbs.</summary>
    public const double FallingMaximumHeight = 150;

    /// <summary>Largest angle, in degrees, between the movement input and the view direction that still vaults.</summary>
    public const double MaximumInputAngle = 91;

    /// <summary>Added to the capsule radius for the shortest wall search.</summary>
    public const double SearchMargin = 75;

    /// <summary>Longest wall search on the ground.</summary>
    public const double GroundSearchDistance = 350;

    /// <summary>Longest wall search in the air.</summary>
    public const double AirSearchDistance = 150;

    /// <summary>Share of the speed that sets the wall search distance in the air.</summary>
    public const double AirSearchSpeedShare = 0.5;

    /// <summary>Ground slope, in degrees, from which the wall search stays level.</summary>
    public const double SlopeCutoffAngle = 65;

    /// <summary>Steepest ground slope, in degrees, the wall search follows.</summary>
    public const double MaximumSlopeAngle = 44.9;

    /// <summary>Lowest pitch, in degrees, of the wall search while rising in the air.</summary>
    public const double MinimumRisingPitch = -15;

    /// <summary>Highest pitch, in degrees, of the wall search while rising in the air.</summary>
    public const double MaximumRisingPitch = 60;

    /// <summary>Height the wall search starts above the capsule centre.</summary>
    public const double SearchLift = 0.35;

    /// <summary>Share of the capsule radius the wall search starts behind the capsule centre.</summary>
    public const double SearchSetBack = 0.25;

    /// <summary>Share of the capsule radius used by the wall search capsule.</summary>
    public const double SearchRadiusShare = 0.5;

    /// <summary>Number of steps the maximum height is cut into when the wall search finds only floor.</summary>
    public const int FloorSearchDivisions = 5;

    /// <summary>Number of steps the maximum height is cut into when scanning the wall for its edge.</summary>
    public const int EdgeScanDivisions = 12;

    /// <summary>Number of edge scan probes.</summary>
    public const int EdgeScanSteps = 12;

    /// <summary>Consecutive edge scan probes that miss the wall before the scan gives up.</summary>
    public const int EdgeScanMisses = 4;

    /// <summary>Capsule radii between the wall and the start of each edge scan probe.</summary>
    public const double EdgeScanStandOff = 2;

    /// <summary>Capsule radii each edge scan probe travels into the wall.</summary>
    public const double EdgeScanReach = 5;

    /// <summary>Radius of the edge scan probe.</summary>
    public const double EdgeProbeRadius = 2;

    /// <summary>Pitch, in degrees, of the edge scan probe facing the wall.</summary>
    public const double EdgeProbePitch = -35;

    /// <summary>Added to half a scan step for the vertical reach of the edge scan probe.</summary>
    public const double EdgeProbeMargin = 2;

    /// <summary>Distance within which a probe hit counts as a hit on the top of the probe.</summary>
    public const double ProbeTopTolerance = 0.05;

    /// <summary>Angle, in degrees, from vertical within which a surface counts as floor (or ceiling).</summary>
    public const double FloorAngle = 35;

    /// <summary>Clearance kept above surfaces by the vault's own traces.</summary>
    public const double FloorMargin = 15;

    /// <summary>Capsule radii beyond the edge at which the far side is probed.</summary>
    public const double FarSideRadii = 2;

    /// <summary>Drop below the edge beyond which the far side counts as open (a vault over).</summary>
    public const double FarSideDrop = 50;

    /// <summary>Added to the capsule radius for the landing point beyond the edge of a vault onto.</summary>
    public const double LandingMargin = 10;

    /// <summary>Share of the capsule radius used by the landing and crossing probes.</summary>
    public const double ProbeRadiusShare = 0.85;

    /// <summary>Share of the capsule radius used by the climb path probe.</summary>
    public const double ClimbProbeRadiusShare = 0.5;

    /// <summary>Lift, scaled by the floor angle's cosine, of the stand-up probe above the standing capsule.</summary>
    public const double StandProbeLift = 75;

    /// <summary>Shrink, scaled by the floor angle's cosine, of the stand-up probe's half height.</summary>
    public const double StandProbeShrink = 25;

    /// <summary>Obstacles below this height count as low.</summary>
    public const double LowObstacleHeight = 99;

    /// <summary>Farthest horizontal distance to the edge of a low obstacle that still vaults.</summary>
    public const double LowObstacleDistance = 175;

    /// <summary>Depth of the trace from the edge height down to the ground under the character.</summary>
    public const double LedgeProbeDepth = 500;

    /// <summary>Steepest landing ground, as the lowest normal Z, when the landing slope is checked.</summary>
    public const double MinimumLandingNormalZ = 0.70723;

    /// <summary>Spacing of the landing slope traces.</summary>
    public const double LandingNormalSpacing = 30;

    /// <summary>Depth of the landing slope traces.</summary>
    public const double LandingNormalDepth = 200;

    /// <summary>Squared horizontal length below which a direction counts as none.</summary>
    public const double DirectionTolerance = 1e-8;

    /// <summary>Precision of the capsule centre the vault traces start from (tenths, then whole units).</summary>
    public const double PositionPrecision = 10;

    /// <summary>Ticks ahead the start position is predicted for distance matching.</summary>
    public const int MatchingLeadTicks = 2;

    /// <summary>Blend time into a vault from ground movement.</summary>
    public const double EntryBlendSeconds = 0.3f;

    /// <summary>Blend time from a vault back to ground movement.</summary>
    public const double ExitBlendSeconds = 0.1f;

    /// <summary>Stance of the action layer during a vault.</summary>
    public const CharacterStance ActionStance = CharacterStance.Uncontrolled;

    /// <summary>Locomotion of the action layer during a vault.</summary>
    public const LocoMode ActionLocomotion = LocoMode.Running;

    /// <summary>Graph-state tags (<see cref="GraphStateTags"/>) held while a vault runs.</summary>
    public const ulong VaultingFlags = GraphStateFlags.BlockedActions | GraphStateTags.AbilityBlockContextInteractPendulumMove
        | GraphStateTags.AbilityBlockCrouchPendulumMove | GraphStateTags.AnimationPendulumDisplaySyncedPose | GraphStateTags.AbilityVault;

    /// <summary>The definition set for the character's equipment and condition.</summary>
    /// <param name="armed">Whether an item other than empty hands is held.</param>
    /// <param name="exhausted">Whether the character is exhausted (and exhausted vaults are not disabled) or carries a heavy load.</param>
    /// <returns>The set.</returns>
    public static VaultDefinitionSet Definitions(bool armed, bool exhausted) => (armed, exhausted) switch
    {
        (true, false) => VaultDefinitions.Armed,
        (false, false) => VaultDefinitions.Unarmed,
        (true, true) => VaultDefinitions.ArmedExhausted,
        (false, true) => VaultDefinitions.UnarmedExhausted,
    };
}
