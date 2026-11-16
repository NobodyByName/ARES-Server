namespace Ares.Structs.Game.Collision;

/// <summary>
/// The game's own collision channels: the engine channel slots its collision profile config
/// (<see cref="CollisionProfileConfig"/>) names, by those names.
/// </summary>
public static class GameCollisionChannels
{
    public const CollisionChannel Minion = CollisionChannel.GameTraceChannel1;
    public const CollisionChannel Harvester = CollisionChannel.GameTraceChannel2;

    /// <summary>The object type of the player character's movement capsule.</summary>
    public const CollisionChannel PlayerPawn = CollisionChannel.GameTraceChannel3;

    public const CollisionChannel Projectile = CollisionChannel.GameTraceChannel4;
    public const CollisionChannel AiVisibility = CollisionChannel.GameTraceChannel5;
    public const CollisionChannel ContextualActionCandidate = CollisionChannel.GameTraceChannel6;
    public const CollisionChannel RadialBlock = CollisionChannel.GameTraceChannel7;
    public const CollisionChannel InteractionProxy = CollisionChannel.GameTraceChannel8;
    public const CollisionChannel RagdollProxy = CollisionChannel.GameTraceChannel9;

    /// <summary>The trace channel of vault, mantle and wall checks.</summary>
    public const CollisionChannel Climbable = CollisionChannel.GameTraceChannel10;

    public const CollisionChannel HitScan = CollisionChannel.GameTraceChannel11;
    public const CollisionChannel DamageOcclusion = CollisionChannel.GameTraceChannel12;
    public const CollisionChannel AudioOcclusion = CollisionChannel.GameTraceChannel13;
    public const CollisionChannel Ui = CollisionChannel.GameTraceChannel14;

    /// <summary>The trace channel of bullets fired by AI.</summary>
    public const CollisionChannel AiTraceProjectile = CollisionChannel.GameTraceChannel15;

    /// <summary>The trace channel of bullets fired by players.</summary>
    public const CollisionChannel PlayerTraceProjectile = CollisionChannel.GameTraceChannel16;

    public const CollisionChannel WorldHitValidation = CollisionChannel.GameTraceChannel17;
    public const CollisionChannel PlayerPawnPhysics = CollisionChannel.GameTraceChannel18;

    /// <summary>Every named game channel by its config name.</summary>
    public static IReadOnlyDictionary<string, CollisionChannel> ByName { get; } = new Dictionary<string, CollisionChannel>(StringComparer.Ordinal)
    {
        ["Minion"] = Minion,
        ["Harvester"] = Harvester,
        ["PlayerPawn"] = PlayerPawn,
        ["Projectile"] = Projectile,
        ["AIVisibility"] = AiVisibility,
        ["ContextualActionCandidate"] = ContextualActionCandidate,
        ["RadialBlock"] = RadialBlock,
        ["InteractionProxy"] = InteractionProxy,
        ["RagdollProxy"] = RagdollProxy,
        ["Climbable"] = Climbable,
        ["HitScan"] = HitScan,
        ["DamageOcclusion"] = DamageOcclusion,
        ["AudioOcclusion"] = AudioOcclusion,
        ["UI"] = Ui,
        ["AITraceProjectile"] = AiTraceProjectile,
        ["PlayerTraceProjectile"] = PlayerTraceProjectile,
        ["WorldHitValidation"] = WorldHitValidation,
        ["PlayerPawnPhysics"] = PlayerPawnPhysics,
    };
}
