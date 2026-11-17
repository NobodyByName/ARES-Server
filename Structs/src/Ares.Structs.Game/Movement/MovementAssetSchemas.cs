using Ares.Structs.Core.Assets;

namespace Ares.Structs.Game.Movement;

/// <summary>
/// Property chains of the movement data assets and the structs they hold, as their cooked unversioned property blocks
/// index them (each type's own members, in declaration order).
/// </summary>
public static class MovementAssetSchemas
{
    /// <summary>Chain length of <see cref="SnaphookItemChain"/>: up to its last member the class default sets.</summary>
    private const int SnaphookItemChainLength = 50;

    /// <summary><c>FDodgeRollSequenceData</c>: one dodge-roll variant.</summary>
    public static StructSchema DodgeRollSequenceData { get; } = new("DodgeRollSequenceData",
    [
        new PropertyField("Distance", PropertyType.Double),
        new PropertyField("Speed", PropertyType.Double),
        new PropertyField("StaminaCost", PropertyType.Double),
        new PropertyField("ContactAnimationTime", PropertyType.Double),
        new PropertyField("Sequence", PropertyType.Object),
    ]);

    /// <summary><c>UDodgeRollMetrics</c>: the dodge-roll variants, in <see cref="DodgeRollKind"/> order.</summary>
    public static StructSchema DodgeRollMetrics { get; } = new("DodgeRollMetrics",
    [
        new PropertyField("StandardDodgeRoll", PropertyType.StructOf(DodgeRollSequenceData)),
        new PropertyField("SprintDodgeRoll", PropertyType.StructOf(DodgeRollSequenceData)),
        new PropertyField("ExhaustedDodgeRoll", PropertyType.StructOf(DodgeRollSequenceData)),
        new PropertyField("NinjaDodgeRoll", PropertyType.StructOf(DodgeRollSequenceData)),
    ]);

    /// <summary><c>FDirectionalLandings</c>: one landing level and its animations by impact direction.</summary>
    public static StructSchema DirectionalLandings { get; } = new("DirectionalLandings",
    [
        new PropertyField("HeightDistanceThreshold", PropertyType.Double),
        new PropertyField("NoDirection", PropertyType.Object),
        new PropertyField("Forward", PropertyType.Object),
        new PropertyField("Left", PropertyType.Object),
        new PropertyField("Right", PropertyType.Object),
        new PropertyField("Backward", PropertyType.Object),
    ]);

    /// <summary><c>UHeavyLandingAnimations</c>: the landing levels of heavy, recovery-roll and crash-pad landings.</summary>
    public static StructSchema HeavyLandingAnimations { get; } = new("HeavyLandingAnimations",
    [
        new PropertyField("HeavyLandingAnimations", PropertyType.ArrayOf(PropertyType.StructOf(DirectionalLandings))),
        new PropertyField("RecoveryLandingAnimations", PropertyType.ArrayOf(PropertyType.StructOf(DirectionalLandings))),
        new PropertyField("CrashPadLandingAnimations", PropertyType.ArrayOf(PropertyType.StructOf(DirectionalLandings))),
    ]);

    /// <summary>A vault horizontal distance clamp: the most horizontal distance a vault may cover in a height range.</summary>
    public static StructSchema VaultHorizontalDistanceClamp { get; } = new("VaultHorizontalDistanceClamp",
    [
        new PropertyField("HeightRange", PropertyType.StructOf(EngineStructs.Vector2D)),
        new PropertyField("MaxHorizontalDistanceAllowedForVault", PropertyType.Double),
    ]);

    /// <summary><c>FVaultDefinitionSettings</c>: settings of a vault definition set.</summary>
    public static StructSchema VaultDefinitionSettings { get; } = new("VaultDefinitionSettings",
    [
        new PropertyField("HorizontalDistanceClamps", PropertyType.ArrayOf(PropertyType.StructOf(VaultHorizontalDistanceClamp))),
        new PropertyField("GlobalPlayRate", PropertyType.Double),
    ]);

    /// <summary>A cooked root position of a distance-matched animation and its animation time.</summary>
    public static StructSchema RootMotionDistanceMatchingPoint { get; } = new("RootMotionDistanceMatchingPoint",
    [
        new PropertyField("Position", PropertyType.StructOf(EngineStructs.Vector3F)),
        new PropertyField("Time", PropertyType.Float),
    ]);

    /// <summary><c>FRootMotionDistanceMatchingRuntimeData</c>: the cooked root path of a distance-matched animation.</summary>
    public static StructSchema RootMotionDistanceMatchingRuntimeData { get; } = new("RootMotionDistanceMatchingRuntimeData",
    [
        new PropertyField("PositionAtTimes", PropertyType.ArrayOf(PropertyType.StructOf(RootMotionDistanceMatchingPoint))),
        new PropertyField("SampleFreq", PropertyType.Float),
        new PropertyField("MaxAllowedEntryTimeFromOnGround", PropertyType.Float),
        new PropertyField("MaxAllowedEntryTime", PropertyType.Float),
    ]);

    /// <summary><c>FVaultDefinition</c>: the animation of one vault case.</summary>
    public static StructSchema VaultDefinition { get; } = new("VaultDefinition",
    [
        new PropertyField("Sequence", PropertyType.Object),
        new PropertyField("AssetPlayRate", PropertyType.Double),
        new PropertyField("SequenceIndex", PropertyType.Int32),
        new PropertyField("bKeepDirection", PropertyType.Bool),
        new PropertyField("bStepUp", PropertyType.Bool),
        new PropertyField("RootMotionAnimMatchingType", PropertyType.ByteEnum),
        new PropertyField("DistanceMatchingRuntimeData", PropertyType.StructOf(RootMotionDistanceMatchingRuntimeData)),
    ]);

    /// <summary><c>FVaultDefinitionBasedOnSpeed</c>: the vault cases of one approach speed.</summary>
    public static StructSchema VaultDefinitionBasedOnSpeed { get; } = new("VaultDefinitionBasedOnSpeed",
    [
        new PropertyField("Speed", PropertyType.Double),
        new PropertyField("VaultOver", PropertyType.StructOf(VaultDefinition)),
        new PropertyField("VaultOnTo", PropertyType.StructOf(VaultDefinition)),
    ]);

    /// <summary><c>FVaultDefinitionVertical</c>: the vault cases of obstacles from one height up.</summary>
    public static StructSchema VaultDefinitionVertical { get; } = new("VaultDefinitionVertical",
    [
        new PropertyField("Name", PropertyType.Name),
        new PropertyField("Height", PropertyType.Double),
        new PropertyField("Speeds", PropertyType.ArrayOf(PropertyType.StructOf(VaultDefinitionBasedOnSpeed))),
    ]);

    /// <summary>
    /// <c>UVaultDefinitionsDataAsset</c>: one vault definition set. Its last member, <c>AllAnimSequences</c>, is unknown:
    /// the block is read up to it.
    /// </summary>
    public static StructSchema VaultDefinitionsDataAsset { get; } = new("VaultDefinitionsDataAsset",
    [
        new PropertyField("Settings", PropertyType.StructOf(VaultDefinitionSettings)),
        new PropertyField("FallToVaultDefinitions", PropertyType.ArrayOf(PropertyType.StructOf(VaultDefinitionVertical))),
        new PropertyField("InAirVaultDefinitions", PropertyType.ArrayOf(PropertyType.StructOf(VaultDefinitionVertical))),
        new PropertyField("VaultDefinitions", PropertyType.ArrayOf(PropertyType.StructOf(VaultDefinitionVertical))),
        new PropertyField("JumpOffBackwards", PropertyType.StructOf(VaultDefinition)),
        new PropertyField("JumpOffLeft", PropertyType.StructOf(VaultDefinition)),
        new PropertyField("JumpOffRight", PropertyType.StructOf(VaultDefinition)),
        new PropertyField("JumpOffDown", PropertyType.StructOf(VaultDefinition)),
        new PropertyField("SpringJumpOffForwards", PropertyType.StructOf(VaultDefinition)),
        new PropertyField("SpringJumpOffBackwards", PropertyType.StructOf(VaultDefinition)),
        new PropertyField("SpringJumpOffDown", PropertyType.StructOf(VaultDefinition)),
        null,
    ]);

    /// <summary>
    /// The snap hook item class's chain (<c>USnaphookItem</c>, then <c>UGadgetItem</c>, <c>UGameplayAbilityItem</c> and
    /// <c>UItemBase</c>) up to the last member its class default sets; only the members the class default sets are known.
    /// </summary>
    public static StructSchema SnaphookItemChain { get; } = new("SnaphookItem", Sparse(SnaphookItemChainLength,
    [
        (0, new PropertyField("SnaphookRange", PropertyType.Double)),
        (10, new PropertyField("ActivationCost", PropertyType.Double)),
        (22, new PropertyField("ItemDataAsset", PropertyType.Object)),
        (30, new PropertyField("bDestroyWhenConsumed", PropertyType.Bool)),
        (31, new PropertyField("BaseCameraZoomTime", PropertyType.Double)),
        (49, new PropertyField("bWalkOnSecondaryUse", PropertyType.Bool)),
    ]));

    /// <summary>Chain indices of <see cref="SnaphookItemChain"/> the class default stores a value for.</summary>
    public static IReadOnlyList<int> SnaphookItemStoredMembers { get; } = [0, 10, 22, 31, 49];

    /// <summary>Chain indices of <see cref="SnaphookItemChain"/> the class default marks as zero.</summary>
    public static IReadOnlyList<int> SnaphookItemZeroMembers { get; } = [30];

    private static PropertyField?[] Sparse(int length, IReadOnlyList<(int Index, PropertyField Field)> known)
    {
        PropertyField?[] members = new PropertyField?[length];
        foreach ((int index, PropertyField field) in known)
        {
            members[index] = field;
        }

        return members;
    }
}
