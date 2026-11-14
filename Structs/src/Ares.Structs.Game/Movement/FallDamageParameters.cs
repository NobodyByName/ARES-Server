namespace Ares.Structs.Game.Movement;

/// <summary>
/// How a heavy landing turns into damage (<c>UPendulumHeavyLandingASNodeData.ShouldDoFallDamage</c> and
/// <c>DoFallDamage</c>). The measure is the landing's fall height (<see cref="HeavyLandingParameters.FallHeight"/>), lowered
/// by the fall-damage armor modifier, a crash pad and the fall-height skill. A landing hurts when the impact's vertical speed
/// is below <see cref="MinimumImpactVerticalSpeed"/>, that height is above <see cref="MinimumDamageHeight"/> and the
/// character does not have <c>Ability.Block.Falldamage</c>. The damage maps the height linearly, without clamping, from
/// [<see cref="MinimumDamageHeight"/>, <see cref="MaximumDamageHeight"/>] to [<see cref="MinimumDamage"/>, the health
/// component's maximum health]. A recovery roll takes no damage. The damage is applied as <c>UFallDamageEffect</c> with a
/// set-by-caller magnitude and the fall damage type; the landing then reads the health and, at zero or below, plays as a
/// fatal fall.
/// </summary>
public static class FallDamageParameters
{
    /// <summary>
    /// Vertical impact speed a landing must be below to hurt (negative is down); the landing's own falling exit uses the
    /// same threshold.
    /// </summary>
    public const double MinimumImpactVerticalSpeed = HeavyLandingParameters.FallingExitVerticalSpeed;

    /// <summary>Damage height a landing must exceed to hurt, in world units.</summary>
    public const double MinimumDamageHeight = 600;

    /// <summary>Damage height at which the damage reaches the maximum health, in world units.</summary>
    public const double MaximumDamageHeight = 3000;

    /// <summary>Damage of a landing at <see cref="MinimumDamageHeight"/>.</summary>
    public const double MinimumDamage = 5;

    /// <summary>The health component's maximum health, the damage at <see cref="MaximumDamageHeight"/>.</summary>
    public const double MaximumHealth = 100;

    /// <summary>
    /// Height below the landing that decides whether a fall that would kill a healthy character leaves it alive instead:
    /// when the damage of a fall this much lower is below the health, the character keeps <see cref="SurvivingHealth"/>.
    /// </summary>
    public const double SurvivalHeightMargin = 200;

    /// <summary>Health a character keeps after a fall the survival margin saves.</summary>
    public const double SurvivingHealth = 5;

    /// <summary>Least damage of a fall the survival margin saves.</summary>
    public const double MinimumSurvivedDamage = 1;

    /// <summary>Damage height the fall-damage armor modifier (<c>GameplayEffect.FallDamage.ArmorModModifier</c>) removes.</summary>
    public const double ArmorModifierHeightReduction = HeavyLandingParameters.ArmorModifierHeightReduction;

    /// <summary>Damage height a crash pad under the landing removes.</summary>
    public const double CrashPadHeightReduction = 1000;

    /// <summary>
    /// Fall height the landing uses to pick its level after a fatal fall, so the fall plays the highest level. The landing
    /// also leaves the character downed (<see cref="CharacterStance.Dbno"/>).
    /// </summary>
    public const double FatalLevelHeight = 3.4e38;

    /// <summary>Damage multiplier of the fall-damage skill (<c>CharacterSkillData.FallDamageMultiplier</c>) without the skill.</summary>
    public const double DefaultDamageMultiplier = 1;

    /// <summary>Fall height the fall-height skill (<c>CharacterSkillData.FallHeightAddend</c>) removes without the skill.</summary>
    public const double DefaultHeightAddend = 0;
}
