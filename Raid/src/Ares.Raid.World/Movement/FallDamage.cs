using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>What lowers or cancels the damage of a landing.</summary>
/// <param name="ArmorModifier">Whether the character has the fall-damage armor modifier effect.</param>
/// <param name="CrashPad">Whether the character lands on a crash pad.</param>
/// <param name="HeightAddend">Fall height the fall-height skill removes.</param>
/// <param name="DamageMultiplier">Damage multiplier of the fall-damage skill.</param>
/// <param name="Blocked">Whether the character has <c>Ability.Block.Falldamage</c>.</param>
public readonly record struct FallDamageModifiers(bool ArmorModifier, bool CrashPad, double HeightAddend, double DamageMultiplier, bool Blocked)
{
    /// <summary>A character without armor modifier, crash pad, skills or block.</summary>
    public static FallDamageModifiers None { get; } =
        new(false, false, FallDamageParameters.DefaultHeightAddend, FallDamageParameters.DefaultDamageMultiplier, false);
}

/// <summary>Damage of one heavy landing.</summary>
/// <param name="DamageHeight">Fall height after the modifiers; it also picks the landing level unless the fall is fatal.</param>
/// <param name="Damage">Damage the landing applies, zero when it does not hurt.</param>
/// <param name="Fatal">Whether the character has no health left after the landing.</param>
/// <param name="LevelHeight">Height that picks the landing level: <paramref name="DamageHeight"/>, or the fatal height.</param>
public readonly record struct FallDamageResult(double DamageHeight, double Damage, bool Fatal, double LevelHeight)
{
    /// <summary>The action stance a fatal landing shows instead of its own.</summary>
    public CharacterStance? FatalStance => Fatal ? CharacterStance.Dbno : null;
}

/// <summary>Fall damage of a heavy landing that is not a recovery roll (a recovery roll takes none).</summary>
public static class FallDamage
{
    /// <summary>The fall height the damage and the landing level are computed from.</summary>
    /// <param name="fallHeight">Fall height from <see cref="HeavyLandingParameters.FallHeight"/>.</param>
    /// <param name="modifiers">Armor modifier, crash pad and skill.</param>
    /// <returns>The lowered height.</returns>
    public static double DamageHeight(double fallHeight, FallDamageModifiers modifiers)
    {
        double height = fallHeight;
        if (modifiers.ArmorModifier)
        {
            height -= FallDamageParameters.ArmorModifierHeightReduction;
        }

        if (modifiers.CrashPad)
        {
            height -= FallDamageParameters.CrashPadHeightReduction;
        }

        return height - modifiers.HeightAddend;
    }

    /// <summary>Whether a landing hurts.</summary>
    /// <param name="impactVerticalSpeed">Vertical speed when the landing was detected, rounded to whole units (negative is down).</param>
    /// <param name="damageHeight">Height from <see cref="DamageHeight"/>.</param>
    /// <param name="blocked">Whether the character has <c>Ability.Block.Falldamage</c>.</param>
    /// <returns>True when the landing applies damage.</returns>
    public static bool Hurts(double impactVerticalSpeed, double damageHeight, bool blocked) =>
        impactVerticalSpeed < FallDamageParameters.MinimumImpactVerticalSpeed && damageHeight > FallDamageParameters.MinimumDamageHeight && !blocked;

    /// <summary>
    /// The damage of a hurting landing. A landing that would take all the health of a character with health left, while a
    /// landing <see cref="FallDamageParameters.SurvivalHeightMargin"/> lower would not, leaves it
    /// <see cref="FallDamageParameters.SurvivingHealth"/> instead.
    /// </summary>
    /// <param name="damageHeight">Height from <see cref="DamageHeight"/>.</param>
    /// <param name="health">Health before the landing.</param>
    /// <param name="damageMultiplier">Damage multiplier of the fall-damage skill; heights from the maximum on ignore it.</param>
    /// <returns>The damage.</returns>
    public static double Damage(double damageHeight, double health, double damageMultiplier)
    {
        double damage = Map(damageHeight);
        if (damageHeight < FallDamageParameters.MaximumDamageHeight)
        {
            damage *= damageMultiplier;
        }

        if (health > 0 && damage > health && Map(damageHeight - FallDamageParameters.SurvivalHeightMargin) < health)
        {
            damage = Math.Max(health - FallDamageParameters.SurvivingHealth, FallDamageParameters.MinimumSurvivedDamage);
        }

        return damage;
    }

    /// <summary>
    /// Damage and landing level of a heavy landing. The fall is fatal when no health is left once the damage is taken from
    /// it, which includes every heavy landing of a character that is already down.
    /// </summary>
    /// <param name="fallHeight">Fall height from <see cref="HeavyLandingParameters.FallHeight"/>.</param>
    /// <param name="impactVerticalSpeed">Vertical speed when the landing was detected, rounded to whole units.</param>
    /// <param name="health">Health before the landing; zero or less while the character is down.</param>
    /// <param name="modifiers">Armor modifier, crash pad, skills and block.</param>
    /// <returns>The damage, whether the fall is fatal and the height that picks the landing level.</returns>
    public static FallDamageResult Land(double fallHeight, double impactVerticalSpeed, double health, FallDamageModifiers modifiers)
    {
        double height = DamageHeight(fallHeight, modifiers);
        double damage = Hurts(impactVerticalSpeed, height, modifiers.Blocked) ? Damage(height, health, modifiers.DamageMultiplier) : 0;
        bool fatal = health - damage <= 0;
        return new FallDamageResult(height, damage, fatal, fatal ? FallDamageParameters.FatalLevelHeight : height);
    }

    private static double Map(double height) =>
        FallDamageParameters.MinimumDamage + ((height - FallDamageParameters.MinimumDamageHeight)
            / (FallDamageParameters.MaximumDamageHeight - FallDamageParameters.MinimumDamageHeight)
            * (FallDamageParameters.MaximumHealth - FallDamageParameters.MinimumDamage));
}
