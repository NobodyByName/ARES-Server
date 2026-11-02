namespace Ares.Structs.Game.Characters;

/// <summary>Defaults of a newly spawned Raider.</summary>
public static class CharacterDefaults
{
    /// <summary>The character's ability system component.</summary>
    public static AbilitySystemComponentInfo AbilitySystem { get; } = new("/Script/EmbarkAbilitySystem.EmbarkAbilitySystemComponent", "AbilitySystem", 0xC2AC3758U);

    /// <summary>
    /// Attribute sets registered on the character, core set first, with their native defaults.
    /// Health starts at MaxHealth.
    /// </summary>
    public static IReadOnlyList<AttributeSetDefaults> AttributeSets { get; } =
    [
        new("/Script/Angelscript.PioneerCharacterAttributeSet", [new AttributeDefault("MaxHealth", 100.0), new AttributeDefault("OutgoingHealingModifier", 1.0), new AttributeDefault("IncomingHealingModifier", 1.0), new AttributeDefault("PhysicalResistance", 0.0), new AttributeDefault("ExplosiveResistance", 0.0), new AttributeDefault("FireResistance", 0.0), new AttributeDefault("ElectricResistance", 0.0), new AttributeDefault("MeleeResistance", 0.0), new AttributeDefault("Health", 100.0), new AttributeDefault("SpottingDistance", 0.0), new AttributeDefault("DBNOMaxHealth", 200.0), new AttributeDefault("DBNOHealth", 200.0), new AttributeDefault("DBNOBleedRateHealthPerSecond", 1.0), new AttributeDefault("AirControlModifier", 1.0), new AttributeDefault("MaxSpeedModifier", 1.0), new AttributeDefault("RunSpeedModifier", 1.0), new AttributeDefault("ADSSpeedModifier", 1.0), new AttributeDefault("SprintSpeedModifier", 1.0), new AttributeDefault("CrouchSpeedModifier", 1.0), new AttributeDefault("GravityScaleModifier", 1.0), new AttributeDefault("MeleeImpactForceModifier", 1.0), new AttributeDefault("MeleeDamageModifier", 1.0), new AttributeDefault("KnockbackResistanceModifier", 1.0), new AttributeDefault("DispersionModifier", 1.0), new AttributeDefault("RecoilModifier", 1.0), new AttributeDefault("CraftingCostModifier", 1.0), new AttributeDefault("CraftingHealingItemsCostModifier", 1.0), new AttributeDefault("DeploymentTimeModifier", 1.0), new AttributeDefault("RevivingSpeedMultiplier", 1.0), new AttributeDefault("ScavangeSpeedMultiplier", 1.0), new AttributeDefault("Stamina", 100.0), new AttributeDefault("MaxStamina", 100.0), new AttributeDefault("StaminaRechargeRate", 10.0), new AttributeDefault("StaminaRechargeWeightBuffer", 0.0), new AttributeDefault("Power", 100.0), new AttributeDefault("MaxPower", 100.0), new AttributeDefault("PowerRechargeRate", 4.5), new AttributeDefault("Weight", 0.0), new AttributeDefault("MaxWeight", 100.0), new AttributeDefault("AIDispersionScoreDelta", 0.0)]),
        new("/Script/Angelscript.PioneerTraversalAttributeSet", [new AttributeDefault("DodgeRollStack", 0.0)]),
        new("/Script/Angelscript.PioneerAttackerAttributeSet", [new AttributeDefault("PhysicalDamage", 0.0), new AttributeDefault("ExplosiveDamage", 0.0), new AttributeDefault("FireDamage", 0.0), new AttributeDefault("ElectricDamage", 0.0), new AttributeDefault("MeleeDamage", 0.0), new AttributeDefault("ArmorBypass", 0.0), new AttributeDefault("PhysicalDamageModifier", 1.0), new AttributeDefault("AIWeakSpotDamageModifier", 1.0)]),
    ];

    /// <summary>Default appearance: every slot uses its authored default.</summary>
    public static DefaultAppearance Appearance { get; } = new("/Script/Angelscript.CharacterCustomizationComponent", "CharacterCustomizationComponent", 24983, 2624, [0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF, 0xFFFF]);
}
