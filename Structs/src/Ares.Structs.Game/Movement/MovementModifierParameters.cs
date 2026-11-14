using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Replication;

namespace Ares.Structs.Game.Movement;

/// <summary>
/// Rules by which a Raider's attributes and carried load change the ground movement. The ground node reads the speed
/// attributes of <c>UPioneerCharacterAttributeSet</c> every tick: <c>MaxSpeedModifier</c> scales every ground speed,
/// <c>RunSpeedModifier</c> the run speeds, <c>SprintSpeedModifier</c> the sprint speeds, <c>CrouchSpeedModifier</c> the
/// crouch speed and <c>ADSSpeedModifier</c> the aiming speed; each product of two attributes is a float. Gameplay effects
/// change these attributes: <c>UHeavyMovementGameplayEffect</c> multiplies <c>MaxSpeedModifier</c> by its set-by-caller
/// magnitude and grants <c>Attribute.Movement.Heavy</c>; <c>USuperHeavyMovementGameplayEffect</c> grants
/// <c>Attribute.Movement.SuperHeavy</c> and <c>Ability.Block.Jump</c>. Both last until removed.
/// </summary>
public static class MovementModifierParameters
{
    /// <summary>
    /// Base speed the ground node remaps stick input against (<c>SetInputDirectionAndInputStrength</c>), before
    /// <c>MaxSpeedModifier</c> and <c>RunSpeedModifier</c> scale it.
    /// </summary>
    public const float StickRemapBaseSpeed = 300;

    /// <summary>Share of the sprint speed a super-heavy character keeps when a skill lets it sprint.</summary>
    public const double EncumberedSprintSpeedShare = 0.525;

    /// <summary>
    /// Carried weight above the carrying limit from which a load is super heavy
    /// (<c>CalculateSuperHeavyLoadoutWeight</c>).
    /// </summary>
    public const double SuperHeavyWeightMargin = 30;

    /// <summary><c>MaxSpeedModifier</c> attribute of a new character.</summary>
    public static float StartingMaxSpeedModifier => Attribute(PropertyNames.MaxSpeedModifier);

    /// <summary><c>RunSpeedModifier</c> attribute of a new character.</summary>
    public static float StartingRunSpeedModifier => Attribute(PropertyNames.RunSpeedModifier);

    /// <summary><c>SprintSpeedModifier</c> attribute of a new character.</summary>
    public static float StartingSprintSpeedModifier => Attribute(PropertyNames.SprintSpeedModifier);

    /// <summary><c>CrouchSpeedModifier</c> attribute of a new character.</summary>
    public static float StartingCrouchSpeedModifier => Attribute(PropertyNames.CrouchSpeedModifier);

    /// <summary><c>ADSSpeedModifier</c> attribute of a new character.</summary>
    public static float StartingAdsSpeedModifier => Attribute(PropertyNames.AdsSpeedModifier);

    private static float Attribute(string name) =>
        (float)CharacterDefaults.AttributeSets.SelectMany(set => set.Defaults).First(attribute => attribute.Name == name).Value;
}
