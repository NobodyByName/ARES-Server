namespace Ares.Structs.Game.Movement;

/// <summary>
/// Snap hook rules: the item (<c>USnaphookItem</c>, its pull <c>FSnaphookPull</c>) throws a hook along the aim, and once
/// it holds on something heavy enough a move target pulls the character to it (<c>UPendulumSnapHookNodeData</c>).
/// Values the item Blueprint overrides are in <see cref="SnapHookItem"/>.
/// </summary>
public static class SnapHookParameters
{
    /// <summary>Seconds the hook takes to fly out to the full range (<c>SnaphookExtendDuration</c>); a hit is reached sooner.</summary>
    public const double ExtendSeconds = 0.75;

    /// <summary>Seconds a hook that hit nothing takes to come back (<c>SnaphookPullbackDuration</c>).</summary>
    public const double PullbackSeconds = 0.5;

    /// <summary>Seconds from the hook attaching until the pull may start (<c>PullDelayAfterAttach</c>).</summary>
    public const double PullDelaySeconds = 0.35;

    /// <summary>Share of the fall acceleration that lowers the end of a missed throw while it flies out (<c>MissArcGravityScale</c>).</summary>
    public const double MissArcGravityScale = 0.16;

    /// <summary>Height above the character's location the hook is thrown from (<c>ThrowOffset</c>).</summary>
    public const double ThrowHeight = 80;

    /// <summary>Distance from its target at which a flying hook attaches (<c>ReachedTargetDistance</c>).</summary>
    public const double ReachedTargetDistance = 100;

    /// <summary>Pull strength of the item's pull (<c>FSnaphookPull.PullStrength</c>).</summary>
    public const double PullStrength = 250000;

    /// <summary>Share of the pull strength that becomes the character's pull speed (<c>PlayerForceMultiplier</c> of a pull end).</summary>
    public const double PlayerForceMultiplier = 0.008;

    /// <summary>
    /// Mass below which a pulled object moves instead of the character (<c>TargetWeightThresholdForPlayerMovement</c>).
    /// </summary>
    public const double PlayerMovementWeightThreshold = 195;

    /// <summary>Mass of a pull end with no physics-simulating body (characters, unknown bodies).</summary>
    public const double DefaultMass = 100;

    /// <summary>Smallest combined mass used when sharing the pull between two ends.</summary>
    public const double MinimumCombinedMass = 0.0001;

    /// <summary>Speed the movement input steers with while pulled (<c>AllowedWiggleSpeed</c> of the move target).</summary>
    public const double AllowedWiggleSpeed = 750;

    /// <summary>Seconds after a move target becomes valid during which the pull may still start.</summary>
    public const double StartWindowSeconds = 1;

    /// <summary>Distance from the target within which the pull ends.</summary>
    public const double ArrivalDistance = 200;

    /// <summary>Distance from the target within which the pull ends even before <see cref="ArrivalDistance"/> is checked.</summary>
    public const double StopDistance = 100;

    /// <summary>Cosine of the largest angle between the first pull direction and the current one; past it the pull ends.</summary>
    public const double MaximumTurnCosine = 0.5;

    /// <summary>Factor on the speed moving away from the target that scales the pull up while moving away.</summary>
    public const double OpposedPullScale = 0.001;

    /// <summary>Pull speed below which the pull ends.</summary>
    public const double MinimumPullSpeed = 300;

    /// <summary>Seconds into the pull after which a character slower than <see cref="StallSpeed"/> stops being pulled.</summary>
    public const double StallSeconds = 5;

    /// <summary>Speed below which a long pull counts as stalled.</summary>
    public const double StallSpeed = 100;

    /// <summary>Pitch, in degrees, a jump turns the steering by about its right axis (negative tilts it up).</summary>
    public const double JumpTiltDegrees = -10;

    /// <summary>Upward speed a jump adds to the requested velocity.</summary>
    public const double JumpUpSpeed = 750;

    /// <summary>Seconds over which the velocity's catch-up rate grows to <see cref="CatchUpRate"/>.</summary>
    public const double CatchUpSeconds = 0.5;

    /// <summary>Highest rate, in world units per second squared, at which the velocity moves towards the pulled velocity.</summary>
    public const double CatchUpRate = 5000;

    /// <summary>Height above the ground above which a pulled character's action stance is in the air.</summary>
    public const double AirStanceHeight = 50;

    /// <summary>Speed the gameplay wind pushes with at full intensity.</summary>
    public const double WindPushSpeed = 1500;

    /// <summary>Acceleration of the pulled flight (<c>MaxAcceleration</c> of the node's flying parameters).</summary>
    public const double FlyingAcceleration = 7500;

    /// <summary>Braking deceleration of the pulled flight.</summary>
    public const double FlyingBrakingDeceleration = 0.9;

    /// <summary>Friction of the pulled flight (turning towards the request, braking, fluid drag).</summary>
    public const double FlyingFriction = 0.9;

    /// <summary>Factor on <see cref="FlyingFriction"/> while braking.</summary>
    public const double FlyingBrakingFrictionFactor = 1;

    /// <summary>Acceleration of the ground step the pull ends with on walkable ground.</summary>
    public const double EndGroundAcceleration = 750;

    /// <summary>Braking deceleration of the ground step the pull ends with.</summary>
    public const double EndGroundBrakingDeceleration = 50;

    /// <summary>Friction of the ground step the pull ends with.</summary>
    public const double EndGroundFriction = 0.9;

    /// <summary>Factor on <see cref="EndGroundFriction"/> while braking.</summary>
    public const double EndGroundBrakingFrictionFactor = 1;

    /// <summary>Gadget power a full charge holds (and the item starts with).</summary>
    public const double PowerMaximum = 100;

    /// <summary>Gadget power recharged per second while below <see cref="PowerMaximum"/>.</summary>
    public const double PowerRechargePerSecond = 4.5;

    /// <summary>Trace type query (<c>ETraceTypeQuery</c> value) of the throw's line trace.</summary>
    public const int ThrowTraceQuery = 10;

    /// <summary>
    /// Graph state flags while pulled: the snap hook animation state, and equipping and context interactions blocked.
    /// </summary>
    public const ulong PullingFlags = GraphStateTags.AnimationPendulumStateSnapHookActive | GraphStateTags.AbilityBlockEquipItemPendulumMove
        | GraphStateTags.AbilityBlockContextInteractPendulumMove;

    /// <summary>Graph state flags of the previous tick that cancel a thrown hook.</summary>
    public const ulong CancellingFlags = GraphStateTags.AbilityLandingHeavy;
}
