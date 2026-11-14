namespace Ares.Structs.Game.Movement;

/// <summary>How a held item changes its holder's movement: the item's movement members and their defaults.</summary>
/// <param name="BlockSprintDuringPrimaryUse">Whether using the item blocks sprinting, and sprinting blocks using it.</param>
/// <param name="BlockSprintAfterPrimaryUseSeconds">
/// Seconds sprinting stays blocked after a use, before <see cref="SprintBlockAfterUseExtraSeconds"/>.
/// </param>
/// <param name="BlockPrimaryUseAfterSprintSeconds">Seconds a sprint blocks using the item after it.</param>
/// <param name="WalkOnPrimaryUse">Whether using the item forces a walk.</param>
/// <param name="WalkOnSecondaryUse">Whether aiming or using the item's secondary action forces a walk.</param>
/// <param name="ZoomRetriggerDelaySeconds">Seconds after an aim ends before the item can aim again; 0 for none.</param>
public readonly record struct HeldItemMovementParameters(
    bool BlockSprintDuringPrimaryUse = true,
    double BlockSprintAfterPrimaryUseSeconds = 0.35,
    double BlockPrimaryUseAfterSprintSeconds = 0.2,
    bool WalkOnPrimaryUse = false,
    bool WalkOnSecondaryUse = false,
    double ZoomRetriggerDelaySeconds = 0)
{
    /// <summary>The members of an item that does not set them.</summary>
    public static HeldItemMovementParameters Default { get; } = new(BlockSprintDuringPrimaryUse: true);

    /// <summary>Empty hands: their use does not block sprinting.</summary>
    public static HeldItemMovementParameters Unarmed { get; } = new(BlockSprintDuringPrimaryUse: false);

    /// <summary>Seconds added to <see cref="BlockSprintAfterPrimaryUseSeconds"/> when a use starts the sprint block.</summary>
    public const double SprintBlockAfterUseExtraSeconds = 0.33;

    /// <summary>Seconds of sprinting after which a running reload is blocked.</summary>
    public const double ReloadBlockedAfterSprintSeconds = 0.5;
}
