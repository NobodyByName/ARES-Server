using Ares.Raid.World.Movement;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World.Movement;

public sealed class FallDamageTests
{
    private const double FullHealth = 100;
    private const double Falling = -2000;

    [Theory]
    [InlineData(-1451, 600.01, false, true)]
    [InlineData(-1450, 900, false, false)]
    [InlineData(-2500, 600, false, false)]
    [InlineData(-2500, 900, true, false)]
    public void A_landing_hurts_below_the_impact_speed_above_the_height_and_without_the_block(double vertical, double height, bool blocked, bool hurts)
    {
        Assert.Equal(hurts, FallDamage.Hurts(vertical, height, blocked));
    }

    [Theory]
    [InlineData(600, 5)]
    [InlineData(1800, 52.5)]
    [InlineData(3000, 100)]
    public void The_damage_grows_linearly_from_five_to_the_maximum_health(double height, double damage)
    {
        Assert.Equal(damage, FallDamage.Damage(height, FullHealth, 1), 9);
    }

    [Fact]
    public void The_skill_multiplier_scales_damage_below_the_maximum_height_only()
    {
        Assert.Equal(26.25, FallDamage.Damage(1800, FullHealth, 0.5), 9);
        Assert.Equal(100, FallDamage.Damage(3000, 200, 0.5), 9);
    }

    [Fact]
    public void A_fall_just_past_lethal_leaves_five_health_when_a_fall_two_hundred_lower_would_not_kill()
    {
        Assert.Equal(95, FallDamage.Damage(3100, FullHealth, 1), 9);
        Assert.Equal(111.875, FallDamage.Damage(3300, FullHealth, 1), 9);
        Assert.Equal(1, FallDamage.Damage(700, 5.5, 1), 9);
        Assert.Equal(FallDamage.Damage(3100, 0, 1), FallDamage.Damage(3100, -1, 1));
    }

    [Fact]
    public void Armor_modifier_crash_pad_and_skill_lower_the_damage_height()
    {
        FallDamageModifiers all = new(ArmorModifier: true, CrashPad: true, HeightAddend: 50, DamageMultiplier: 1, Blocked: false);
        Assert.Equal(1450, FallDamage.DamageHeight(3500, all));
        Assert.Equal(3500, FallDamage.DamageHeight(3500, FallDamageModifiers.None));
    }

    [Fact]
    public void A_soft_landing_keeps_its_height_for_the_level_and_takes_no_damage()
    {
        FallDamageResult result = FallDamage.Land(700, -1400, FullHealth, FallDamageModifiers.None);
        Assert.Equal(0, result.Damage);
        Assert.False(result.Fatal);
        Assert.Equal(700, result.LevelHeight);
        Assert.Null(result.FatalStance);
    }

    [Fact]
    public void A_fatal_fall_plays_the_highest_level_and_leaves_the_character_downed()
    {
        FallDamageResult result = FallDamage.Land(3300, Falling, FullHealth, FallDamageModifiers.None);
        Assert.True(result.Fatal);
        Assert.Equal(FallDamageParameters.FatalLevelHeight, result.LevelHeight);
        Assert.Equal(CharacterStance.Dbno, result.FatalStance);
        Assert.Equal(HeavyLandingAnimations.Heavy.Count - 1, HeavyLandingMovement.Level(result.LevelHeight, HeavyLandingAnimations.Heavy));
    }

    [Fact]
    public void Every_heavy_landing_of_a_downed_character_is_fatal()
    {
        FallDamageResult result = FallDamage.Land(650, -1300, 0, FallDamageModifiers.None);
        Assert.Equal(0, result.Damage);
        Assert.True(result.Fatal);
    }

    [Fact]
    public void A_blocked_landing_takes_no_damage_but_keeps_its_level()
    {
        FallDamageModifiers blocked = FallDamageModifiers.None with { Blocked = true };
        FallDamageResult result = FallDamage.Land(2500, Falling, FullHealth, blocked);
        Assert.Equal(0, result.Damage);
        Assert.Equal(2500, result.LevelHeight);
    }
}
