namespace Ares.Structs.Game.Movement;

/// <summary>
/// Grappling hook item rules (<c>UGrapplingHookItem</c> and its item node <c>UPendulumGrapplingHookItemNode</c>). Holding
/// the primary input aims, releasing it throws the hook; the hook is a projectile the character simulation moves each
/// tick until it attaches, misses or runs out of time. The secondary input retracts a thrown hook.
/// </summary>
public static class GrapplingHookParameters
{
    /// <summary>Longest rope: the hook flies at most this far from the character and attaches only within it.</summary>
    public const double HookRange = 2000;

    /// <summary>Rope length at which the server breaks the rope (<c>BreakLength</c>); the client's movement never reads it.</summary>
    public const double BreakLength = 3000;

    /// <summary>Seconds a retracting hook stays where it was before it travels back.</summary>
    public const double RetractDelay = 0.2;

    /// <summary>Seconds a retracting hook takes to travel back to the throw location after <see cref="RetractDelay"/>.</summary>
    public const double RetractDuration = 0.3;

    /// <summary>Speed of the hook along the camera aim when thrown (<c>MaxLaunchVelocity</c>).</summary>
    public const double MaxLaunchVelocity = 2160;

    /// <summary>Upward speed added to a throw (<c>ThrowUpwardVelocity</c>).</summary>
    public const double ThrowUpwardVelocity = 450;

    /// <summary>Share of the character's fall acceleration that pulls the hook down in flight.</summary>
    public const double HookGravityScale = 0.5;

    /// <summary>Seconds of flight after which a hook that has not attached is a miss.</summary>
    public const double MaxFlightDuration = 2;

    /// <summary>Smallest angle, in degrees, a hook turns by when it glances off a player.</summary>
    public const double PlayerDeflectMinAngle = 15;

    /// <summary>Largest angle, in degrees, a hook turns by when it glances off a player.</summary>
    public const double PlayerDeflectMaxAngle = 30;

    /// <summary>Share of its speed a hook keeps when it glances off a player.</summary>
    public const double PlayerDeflectSpeedScale = 0.9;

    /// <summary>Speed at which holding the primary input reels the rope in while not swinging (<c>ReelSpeed</c>).</summary>
    public const double ReelSpeed = 200;

    /// <summary>Factor on <see cref="ReelSpeed"/> while the movement input points away from the anchor.</summary>
    public const double BackpedalReelSpeedScale = 0.5;

    /// <summary>
    /// Cosine between the flat movement input and the flat direction to the anchor at or below which the input counts
    /// as pointing away from the anchor.
    /// </summary>
    public const double BackpedalCosine = -0.5;

    /// <summary>Shortest rope the ground reel pulls the character to.</summary>
    public const double MinHeldRopeLength = 200;

    /// <summary>Distance off the hit surface, along its normal, at which an attached or deflected hook rests.</summary>
    public const double SurfaceOffset = 1;

    /// <summary>Distance within which the rope's flight anchor counts as following the hook.</summary>
    public const double FlightAnchorReachedTolerance = 2;

    /// <summary>Distance within which a line-of-sight hit between the character and the hook counts as the hook itself.</summary>
    public const double BacktraceTolerance = 5;

    /// <summary>Whether the rope wraps around edges while the hook is still in flight (console variable default).</summary>
    public const bool WrapInFlight = true;

    /// <summary>
    /// Share of the rope at which the rope starts slowing the hook's outward flight (console variable default); 1 or
    /// more turns the slowing off.
    /// </summary>
    public const double RopeSoftStart = 1;

    /// <summary>Deceleration of a hook rubbing over a wrap point, in centimetres per second squared (console variable default).</summary>
    public const double WrapFrictionDeceleration = 20000;

    /// <summary>Speed at which a hook below a wrap point is reeled to it (console variable default).</summary>
    public const double ReelSpeedBelowWrap = 1500;

    /// <summary>Speed at which a hook above a wrap point is reeled to it (console variable default).</summary>
    public const double ReelSpeedAboveWrap = 500;

    /// <summary>Seconds after which reeling a hook to a wrap point gives up.</summary>
    public const double ReelToWrapTimeout = 3;

    /// <summary>Speed away from the character above which an anchor counts as fleeing (no ground reel).</summary>
    public const double AnchorFleeSpeed = 300;

    /// <summary>Speed at which a taut rope starts dragging a character on the ground.</summary>
    public const double TetherDragEnterSpeed = 60;

    /// <summary>Speed below which a rope stops dragging a character on the ground.</summary>
    public const double TetherDragExitSpeed = 30;

    /// <summary>Speed above which a dragging rope overpowers the character's own movement.</summary>
    public const double TetherDragOverpowerSpeed = 320;

    /// <summary>Smallest speed of the ground reel's drag.</summary>
    public const double GroundReelDragMinSpeed = 30;

    /// <summary>Most players a single hook trace skips over.</summary>
    public const int MaxPlayerSkips = 4;

    /// <summary>Trace query of the hook's first world trace (also the line-of-sight trace from the character).</summary>
    public const int PrimaryTraceQuery = 5;

    /// <summary>Trace query of the hook's second world trace.</summary>
    public const int SecondaryTraceQuery = 10;

    /// <summary>Bone index that means "no bone" in the item node's attachment.</summary>
    public const byte NoBone = 255;

    /// <summary>End-use reason of a primary release that cancels the aim instead of throwing.</summary>
    public const byte CancelEndUseReason = 1;
}
