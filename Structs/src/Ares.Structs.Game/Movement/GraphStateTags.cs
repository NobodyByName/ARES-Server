namespace Ares.Structs.Game.Movement;

/// <summary>
/// Gameplay tags 0–63 the character graph tracks, as bits of the graph-state tag word
/// <see cref="PawnBlackboard.GraphTagsField"/>: each bit is set while the graph holds its tag.
/// </summary>
public static class GraphStateTags
{
    /// <summary>Number of tracked tags, both tag words together.</summary>
    public const int Count = 112;

    /// <summary>Tag <c>Ability.Block.FirePrimary.PendulumMove</c>.</summary>
    public const ulong AbilityBlockFirePrimaryPendulumMove = 1UL << 0;

    /// <summary>Tag <c>Ability.Block.FireSecondary.PendulumMove</c>.</summary>
    public const ulong AbilityBlockFireSecondaryPendulumMove = 1UL << 1;

    /// <summary>Tag <c>Ability.Block.EquipItem.PendulumMove</c>.</summary>
    public const ulong AbilityBlockEquipItemPendulumMove = 1UL << 2;

    /// <summary>Tag <c>Ability.Block.EquipItem.PendulumMelee</c>.</summary>
    public const ulong AbilityBlockEquipItemPendulumMelee = 1UL << 3;

    /// <summary>Tag <c>Ability.Block.Reload.PendulumMove</c>.</summary>
    public const ulong AbilityBlockReloadPendulumMove = 1UL << 4;

    /// <summary>Tag <c>Ability.Block.ContextInteract.PendulumMove</c>.</summary>
    public const ulong AbilityBlockContextInteractPendulumMove = 1UL << 5;

    /// <summary>Tag <c>Ability.Block.ContextInteract.PendulumMelee</c>.</summary>
    public const ulong AbilityBlockContextInteractPendulumMelee = 1UL << 6;

    /// <summary>Tag <c>Ability.Block.ContextInteract.PendulumCamera</c>.</summary>
    public const ulong AbilityBlockContextInteractPendulumCamera = 1UL << 7;

    /// <summary>Tag <c>SoftBlockInteractions.PendulumItem</c>.</summary>
    public const ulong SoftBlockInteractionsPendulumItem = 1UL << 8;

    /// <summary>Tag <c>Ability.Block.Knockback.PendulumMove</c>.</summary>
    public const ulong AbilityBlockKnockbackPendulumMove = 1UL << 9;

    /// <summary>Tag <c>Ability.Block.Crouch.PendulumMove</c>.</summary>
    public const ulong AbilityBlockCrouchPendulumMove = 1UL << 10;

    /// <summary>Tag <c>Ability.BlockInput.Sprint.PendulumMove</c>.</summary>
    public const ulong AbilityBlockInputSprintPendulumMove = 1UL << 11;

    /// <summary>Tag <c>Ability.BlockInput.Sprint.FirearmBash</c>.</summary>
    public const ulong AbilityBlockInputSprintFirearmBash = 1UL << 12;

    /// <summary>Tag <c>Ability.BlockInput.Crouch.PendulumMove</c>.</summary>
    public const ulong AbilityBlockInputCrouchPendulumMove = 1UL << 13;

    /// <summary>Tag <c>Ability.Block.Vault.PendulumMove</c>.</summary>
    public const ulong AbilityBlockVaultPendulumMove = 1UL << 14;

    /// <summary>Tag <c>Ability.Block.Vault.PendulumItem</c>.</summary>
    public const ulong AbilityBlockVaultPendulumItem = 1UL << 15;

    /// <summary>Tag <c>Ability.Block.DodgeRoll.PendulumMove</c>.</summary>
    public const ulong AbilityBlockDodgeRollPendulumMove = 1UL << 16;

    /// <summary>Tag <c>Ability.Block.DodgeRoll.PendulumMelee</c>.</summary>
    public const ulong AbilityBlockDodgeRollPendulumMelee = 1UL << 17;

    /// <summary>Tag <c>Ability.Block.Emote.PendulumMove</c>.</summary>
    public const ulong AbilityBlockEmotePendulumMove = 1UL << 18;

    /// <summary>Tag <c>Status.Immunity.Damage.PendulumMove</c>.</summary>
    public const ulong StatusImmunityDamagePendulumMove = 1UL << 19;

    /// <summary>Tag <c>Status.Immunity.Electrified.PendulumMove</c>.</summary>
    public const ulong StatusImmunityElectrifiedPendulumMove = 1UL << 20;

    /// <summary>Tag <c>Attribute.Stamina.BlockRecharge.PendulumMove</c>.</summary>
    public const ulong AttributeStaminaBlockRechargePendulumMove = 1UL << 21;

    /// <summary>Tag <c>Attribute.Stamina.BlockDrain.ForrestRun</c>.</summary>
    public const ulong AttributeStaminaBlockDrainForrestRun = 1UL << 22;

    /// <summary>Tag <c>Attribute.Stamina.BlockRecharge.ForrestRun</c>.</summary>
    public const ulong AttributeStaminaBlockRechargeForrestRun = 1UL << 23;

    /// <summary>Tag <c>Ability.Slide</c>.</summary>
    public const ulong AbilitySlide = 1UL << 24;

    /// <summary>Tag <c>Ability.Landing.Heavy</c>.</summary>
    public const ulong AbilityLandingHeavy = 1UL << 25;

    /// <summary>Tag <c>Status.Tumbling</c>.</summary>
    public const ulong StatusTumbling = 1UL << 26;

    /// <summary>Tag <c>Status.Scrambling</c>.</summary>
    public const ulong StatusScrambling = 1UL << 27;

    /// <summary>Tag <c>Animation.Pendulum.DisplaySyncedPose</c>.</summary>
    public const ulong AnimationPendulumDisplaySyncedPose = 1UL << 28;

    /// <summary>Tag <c>Ability.Sprint.Active</c>.</summary>
    public const ulong AbilitySprintActive = 1UL << 29;

    /// <summary>Tag <c>GameplayEffect.AttachedTraversal.Ladder</c>.</summary>
    public const ulong GameplayEffectAttachedTraversalLadder = 1UL << 30;

    /// <summary>Tag <c>GameplayEffect.AttachedTraversal.Zipline</c>.</summary>
    public const ulong GameplayEffectAttachedTraversalZipline = 1UL << 31;

    /// <summary>Tag <c>Animation.PendulumState.Death</c>.</summary>
    public const ulong AnimationPendulumStateDeath = 1UL << 32;

    /// <summary>Tag <c>Animation.PendulumState.DeathToDBNO</c>.</summary>
    public const ulong AnimationPendulumStateDeathToDbno = 1UL << 33;

    /// <summary>Tag <c>Animation.PendulumState.DBNO</c>.</summary>
    public const ulong AnimationPendulumStateDbno = 1UL << 34;

    /// <summary>Tag <c>Animation.PendulumState.DBNOToBaseMove</c>.</summary>
    public const ulong AnimationPendulumStateDbnoToBaseMove = 1UL << 35;

    /// <summary>Tag <c>Animation.PendulumState.SnapHookActive</c>.</summary>
    public const ulong AnimationPendulumStateSnapHookActive = 1UL << 36;

    /// <summary>Tag <c>Animation.Pendulum.BlendToRep</c>.</summary>
    public const ulong AnimationPendulumBlendToRep = 1UL << 37;

    /// <summary>Tag <c>Ability.Jump</c>.</summary>
    public const ulong AbilityJump = 1UL << 38;

    /// <summary>Tag <c>Animation.Ability.Jump.Started</c>.</summary>
    public const ulong AnimationAbilityJumpStarted = 1UL << 39;

    /// <summary>Tag <c>Ability.Crouch</c>.</summary>
    public const ulong AbilityCrouch = 1UL << 40;

    /// <summary>Tag <c>Ability.Stomp</c>.</summary>
    public const ulong AbilityStomp = 1UL << 41;

    /// <summary>Tag <c>Ability.Jump.Pending</c>.</summary>
    public const ulong AbilityJumpPending = 1UL << 42;

    /// <summary>Tag <c>Ability.Death</c>.</summary>
    public const ulong AbilityDeath = 1UL << 43;

    /// <summary>Tag <c>Ability.DodgeRoll.Active</c>.</summary>
    public const ulong AbilityDodgeRollActive = 1UL << 44;

    /// <summary>Tag <c>Ability.Ram</c>.</summary>
    public const ulong AbilityRam = 1UL << 45;

    /// <summary>Tag <c>Ability.Vault</c>.</summary>
    public const ulong AbilityVault = 1UL << 46;

    /// <summary>Tag <c>Ability.Knockback</c>.</summary>
    public const ulong AbilityKnockback = 1UL << 47;

    /// <summary>Tag <c>Ability.Landing.Recovery</c>.</summary>
    public const ulong AbilityLandingRecovery = 1UL << 48;

    /// <summary>Tag <c>Ability.Zooming</c>.</summary>
    public const ulong AbilityZooming = 1UL << 49;

    /// <summary>Tag <c>Ability.TraversableLineExit.ClimbOffTop</c>.</summary>
    public const ulong AbilityTraversableLineExitClimbOffTop = 1UL << 50;

    /// <summary>Tag <c>GameplayCue.Ability.Ladder.Slide</c>.</summary>
    public const ulong GameplayCueAbilityLadderSlide = 1UL << 51;

    /// <summary>Tag <c>Ability.FirePrimary.PendulumWep</c>.</summary>
    public const ulong AbilityFirePrimaryPendulumWep = 1UL << 52;

    /// <summary>Tag <c>Ability.StaminaFreeSprintAfterSprintDodgeRoll</c>.</summary>
    public const ulong AbilityStaminaFreeSprintAfterSprintDodgeRoll = 1UL << 53;

    /// <summary>Tag <c>Ability.Block.StaminaFreeSprintAfterSprintDodgeRoll</c>.</summary>
    public const ulong AbilityBlockStaminaFreeSprintAfterSprintDodgeRoll = 1UL << 54;

    /// <summary>Tag <c>Ability.Flashlight.Active</c>.</summary>
    public const ulong AbilityFlashlightActive = 1UL << 55;

    /// <summary>Tag <c>Ability.ShoulderSwap</c>.</summary>
    public const ulong AbilityShoulderSwap = 1UL << 56;

    /// <summary>Tag <c>Attribute.Movement.Heavy</c>.</summary>
    public const ulong AttributeMovementHeavy = 1UL << 57;

    /// <summary>Tag <c>Attribute.Movement.SuperHeavy</c>.</summary>
    public const ulong AttributeMovementSuperHeavy = 1UL << 58;

    /// <summary>Tag <c>Attribute.Power.BlockRecharge.PendulumItem</c>.</summary>
    public const ulong AttributePowerBlockRechargePendulumItem = 1UL << 59;

    /// <summary>Tag <c>Animation.Item.Gadget.Handheld.TetherLauncher.Reload</c>.</summary>
    public const ulong AnimationItemGadgetHandheldTetherLauncherReload = 1UL << 60;

    /// <summary>Tag <c>Debug.FreeFlyMode</c>.</summary>
    public const ulong DebugFreeFlyMode = 1UL << 61;

    /// <summary>Tag <c>Animation.PendulumState.StationaryInteraction.NonRootMotion</c>.</summary>
    public const ulong AnimationPendulumStateStationaryInteractionNonRootMotion = 1UL << 62;

    /// <summary>Tag <c>Animation.PendulumState.StationaryInteraction.RootMotion</c>.</summary>
    public const ulong AnimationPendulumStateStationaryInteractionRootMotion = 1UL << 63;
}

/// <summary>
/// Gameplay tags 64–111 the character graph tracks, as bits of the graph-state tag word
/// <see cref="PawnBlackboard.GraphUpperTagsField"/>: each bit is set while the graph holds its tag.
/// </summary>
public static class GraphStateUpperTags
{
    /// <summary>Tag <c>Ability.Block.EquipItem.Interact</c>.</summary>
    public const ulong AbilityBlockEquipItemInteract = 1UL << 0;

    /// <summary>Tag <c>Ability.Block.FirePrimary.Interact</c>.</summary>
    public const ulong AbilityBlockFirePrimaryInteract = 1UL << 1;

    /// <summary>Tag <c>Ability.Block.FireSecondary.Interact</c>.</summary>
    public const ulong AbilityBlockFireSecondaryInteract = 1UL << 2;

    /// <summary>Tag <c>Ability.Block.Jump.Interact</c>.</summary>
    public const ulong AbilityBlockJumpInteract = 1UL << 3;

    /// <summary>Tag <c>Ability.Block.Reload.Interact</c>.</summary>
    public const ulong AbilityBlockReloadInteract = 1UL << 4;

    /// <summary>Tag <c>Status.Immobilized.Interact</c>.</summary>
    public const ulong StatusImmobilizedInteract = 1UL << 5;

    /// <summary>Tag <c>Advanced.HACK.HidePlayer</c>.</summary>
    public const ulong AdvancedHackHidePlayer = 1UL << 6;

    /// <summary>Tag <c>CharacterSkillEffect.RevivalTopUp</c>.</summary>
    public const ulong CharacterSkillEffectRevivalTopUp = 1UL << 7;

    /// <summary>Tag <c>CharacterSkillEffect.MobileRepair</c>.</summary>
    public const ulong CharacterSkillEffectMobileRepair = 1UL << 8;

    /// <summary>Tag <c>CharacterSkillEffect.EmergencySmoke</c>.</summary>
    public const ulong CharacterSkillEffectEmergencySmoke = 1UL << 9;

    /// <summary>Tag <c>CharacterSkillEffect.FullBodyTourniquet</c>.</summary>
    public const ulong CharacterSkillEffectFullBodyTourniquet = 1UL << 10;

    /// <summary>Tag <c>CharacterSkillEffect.ArmorBreakAdrenalineShot</c>.</summary>
    public const ulong CharacterSkillEffectArmorBreakAdrenalineShot = 1UL << 11;

    /// <summary>Tag <c>CharacterSkillEffect.AutomaticTickDetach</c>.</summary>
    public const ulong CharacterSkillEffectAutomaticTickDetach = 1UL << 12;

    /// <summary>Tag <c>CharacterSkillEffect.Quickdraw</c>.</summary>
    public const ulong CharacterSkillEffectQuickdraw = 1UL << 13;

    /// <summary>Tag <c>CharacterSkillEffect.WalkingRegeneratesMoreStamina</c>.</summary>
    public const ulong CharacterSkillEffectWalkingRegeneratesMoreStamina = 1UL << 14;

    /// <summary>Tag <c>CharacterSkillEffect.VaultSpring</c>.</summary>
    public const ulong CharacterSkillEffectVaultSpring = 1UL << 15;

    /// <summary>Tag <c>CharacterSkillEffect.WallClimb</c>.</summary>
    public const ulong CharacterSkillEffectWallClimb = 1UL << 16;

    /// <summary>Tag <c>CharacterSkillEffect.FreeSprintAfterSprintDodgeRoll</c>.</summary>
    public const ulong CharacterSkillEffectFreeSprintAfterSprintDodgeRoll = 1UL << 17;

    /// <summary>Tag <c>CharacterSkillEffect.ReducedWeaponEncumbrance</c>.</summary>
    public const ulong CharacterSkillEffectReducedWeaponEncumbrance = 1UL << 18;

    /// <summary>Tag <c>CharacterSkillEffect.MineDisarm</c>.</summary>
    public const ulong CharacterSkillEffectMineDisarm = 1UL << 19;

    /// <summary>Tag <c>CharacterSkillEffect.OneHitKillMeleeVulnerableEnemies</c>.</summary>
    public const ulong CharacterSkillEffectOneHitKillMeleeVulnerableEnemies = 1UL << 20;

    /// <summary>Tag <c>CharacterSkillEffect.LevelTwoBreach</c>.</summary>
    public const ulong CharacterSkillEffectLevelTwoBreach = 1UL << 21;

    /// <summary>Tag <c>CharacterSkillEffect.CriticalHealthReducedMovementNoise</c>.</summary>
    public const ulong CharacterSkillEffectCriticalHealthReducedMovementNoise = 1UL << 22;

    /// <summary>Tag <c>CharacterSkillEffect.BreachGeneratesResources</c>.</summary>
    public const ulong CharacterSkillEffectBreachGeneratesResources = 1UL << 23;

    /// <summary>Tag <c>CharacterSkillEffect.BonusFieldCraftRecipes</c>.</summary>
    public const ulong CharacterSkillEffectBonusFieldCraftRecipes = 1UL << 24;

    /// <summary>Tag <c>CharacterSkillEffect.SuperFieldCrafting</c>.</summary>
    public const ulong CharacterSkillEffectSuperFieldCrafting = 1UL << 25;

    /// <summary>Tag <c>CharacterSkillEffect.ExpertScavenging</c>.</summary>
    public const ulong CharacterSkillEffectExpertScavenging = 1UL << 26;

    /// <summary>Tag <c>CharacterSkillEffect.FreeDodgeRollAfterArmorBreak</c>.</summary>
    public const ulong CharacterSkillEffectFreeDodgeRollAfterArmorBreak = 1UL << 27;

    /// <summary>Tag <c>GameplayEffect.StaminaRegenSkill</c>.</summary>
    public const ulong GameplayEffectStaminaRegenSkill = 1UL << 28;

    /// <summary>Tag <c>GameplayEffect.AllowSprintReload</c>.</summary>
    public const ulong GameplayEffectAllowSprintReload = 1UL << 29;

    /// <summary>Tag <c>GameplayEffect.LowHealthImpairedAITargetAcquisition</c>.</summary>
    public const ulong GameplayEffectLowHealthImpairedAiTargetAcquisition = 1UL << 30;

    /// <summary>Tag <c>GameplayEffect.AllowHeavyMeleeAttack</c>.</summary>
    public const ulong GameplayEffectAllowHeavyMeleeAttack = 1UL << 31;

    /// <summary>Tag <c>GameplayEffect.DamageStaminaBoost</c>.</summary>
    public const ulong GameplayEffectDamageStaminaBoost = 1UL << 32;

    /// <summary>Tag <c>GameplayEffect.ArmorSpeedCompensation</c>.</summary>
    public const ulong GameplayEffectArmorSpeedCompensation = 1UL << 33;

    /// <summary>Tag <c>GameplayEffect.StaminaFreeVault</c>.</summary>
    public const ulong GameplayEffectStaminaFreeVault = 1UL << 34;

    /// <summary>Tag <c>GameplayEffect.DisableExhaustedVault</c>.</summary>
    public const ulong GameplayEffectDisableExhaustedVault = 1UL << 35;

    /// <summary>Tag <c>CharacterSkillEffect.FirearmBash</c>.</summary>
    public const ulong CharacterSkillEffectFirearmBash = 1UL << 36;

    /// <summary>Tag <c>CharacterSkillEffect.FlowState</c>.</summary>
    public const ulong CharacterSkillEffectFlowState = 1UL << 37;

    /// <summary>Tag <c>CharacterSkillEffect.Juggernaut</c>.</summary>
    public const ulong CharacterSkillEffectJuggernaut = 1UL << 38;

    /// <summary>Tag <c>CharacterSkillEffect.CarryableVaulting</c>.</summary>
    public const ulong CharacterSkillEffectCarryableVaulting = 1UL << 39;

    /// <summary>Tag <c>CharacterSkillEffect.RunForrest</c>.</summary>
    public const ulong CharacterSkillEffectRunForrest = 1UL << 40;

    /// <summary>Tag <c>CharacterSkillEffect.FallRecoveryDuration</c>.</summary>
    public const ulong CharacterSkillEffectFallRecoveryDuration = 1UL << 41;

    /// <summary>Tag <c>CharacterSkillEffect.IncreasedMaxStamina</c>.</summary>
    public const ulong CharacterSkillEffectIncreasedMaxStamina = 1UL << 42;

    /// <summary>Tag <c>CharacterSkillEffect.ArmorSpeedCompensation</c>.</summary>
    public const ulong CharacterSkillEffectArmorSpeedCompensation = 1UL << 43;

    /// <summary>Tag <c>UI.SkillState.Available.FirearmBash</c>.</summary>
    public const ulong UiSkillStateAvailableFirearmBash = 1UL << 44;

    /// <summary>Tag <c>UI.SkillState.Active.FirearmBash</c>.</summary>
    public const ulong UiSkillStateActiveFirearmBash = 1UL << 45;

    /// <summary>Tag <c>UI.SkillState.Active.SprintBoost</c>.</summary>
    public const ulong UiSkillStateActiveSprintBoost = 1UL << 46;

    /// <summary>Tag <c>Status.Exhausted</c>.</summary>
    public const ulong StatusExhausted = 1UL << 47;
}
