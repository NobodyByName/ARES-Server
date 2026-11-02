namespace Ares.Structs.Game.Characters;

/// <summary>An ability class granted to a character.</summary>
/// <param name="Package">Package of the ability class.</param>
/// <param name="ClassName">Ability class name.</param>
/// <param name="ExportChecksum">Class-default export checksum, if verified; 0 means the game skips the check.</param>
public readonly record struct AbilityGrant(string Package, string ClassName, uint ExportChecksum = 0);

/// <summary>Abilities the server grants a Raider.</summary>
public static class DefaultAbilities
{
    /// <summary>
    /// The abilities the game grants a Raider, in grant order. Their export checksums are not verified yet.
    /// </summary>
    public static IReadOnlyList<AbilityGrant> Raider { get; } =
    [
        new("/Script/Angelscript", "CharacterAbilityHealthRegenSkill"),
        new("/Script/Angelscript", "CharacterAbilityCriticalHealthRegenSkill"),
        new("/Game/Pioneer/Core/Abilities/HumanAbilities/GA_ContextInteract", "GA_ContextInteract_C"),
        new("/Game/Pioneer/Core/Abilities/HumanAbilities/GA_EquipItem", "GA_EquipItem_C"),
        new("/Script/Angelscript", "CharacterAbilityReloadIntent"),
        new("/Script/Angelscript", "CharacterAbilityReloadInteractIntent"),
        new("/Script/Angelscript", "EmoteAbility"),
        new("/Script/Angelscript", "CharacterAbilityCrouchIntent"),
        new("/Script/Angelscript", "CharacterAbilitySprintIntent"),
        new("/Script/Angelscript", "CharacterAbilityWalkIntent"),
        new("/Script/Angelscript", "CharacterAbilityJumpIntent"),
        new("/Game/Pioneer/Core/Abilities/HumanAbilities/GA_AliveTaskAbility", "GA_AliveTaskAbility_C"),
    ];
}
