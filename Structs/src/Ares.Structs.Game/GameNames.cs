namespace Ares.Structs.Game;

/// <summary>
/// Game names of everything a server treats specially: the maps it runs, the items, inventory structures, animation
/// containers and character graph tags its rules name. Hand-written; the values themselves (asset ids, usable indices,
/// timings, bits) come from the game data extracted from the operator's install, where a server looks these names up
/// once at startup and refuses to start when one is missing.
/// </summary>
/// <remarks>
/// A game update that renames or removes one of these needs a code change here; everything else follows from the
/// extracted data.
/// </remarks>
public static class GameNames
{
    /// <summary>Maps, by world name (the persistent level's package name).</summary>
    public static class Maps
    {
        /// <summary>The Dam.</summary>
        public const string Dam = "TheDam_02_P";
    }

    /// <summary>Items of the usable-item catalogue, by technical name (the item data asset's name).</summary>
    public static class Items
    {
        /// <summary>The empty-handed item every character holds when nothing else is equipped.</summary>
        public const string Unarmed = "DA_Item_Unarmed";

        /// <summary>The Raider Tool, the melee weapon every loadout starts with.</summary>
        public const string RaiderTool = "DA_Item_BasicMeleeWeapon";

        /// <summary>The Ferro I battle rifle.</summary>
        public const string FerroI = "DA_Item_BattleRifle_BreachAction_01";

        /// <summary>Heavy Ammo, the Ferro's ammunition.</summary>
        public const string HeavyAmmo = "DA_Item_HeavyAmmo";

        /// <summary>The Snap Hook mobility gadget.</summary>
        public const string SnapHook = "DA_Item_SnapHook";

        /// <summary>The Light Shield.</summary>
        public const string LightShield = "DA_Item_Armor_LightBody";

        /// <summary>Coins, the currency item.</summary>
        public const string Coins = "DA_Item_GrindCurrency";

        /// <summary>The Combat Mk. 1 augment.</summary>
        public const string CombatMk1 = "DA_Item_Augment_Combat_T1";

        /// <summary>The Looting Mk. 1 augment.</summary>
        public const string LootingMk1 = "DA_Item_Augment_Looting_T1";

        /// <summary>The Tactical Mk. 1 augment.</summary>
        public const string TacticalMk1 = "DA_Item_Augment_Utility_T1";
    }

    /// <summary>Inventory structures of the menu item tables and loadout frames, by technical name.</summary>
    public static class ItemStructures
    {
        /// <summary>A firearm's muzzle mod slot.</summary>
        public const string MuzzleModSlot = "DA_ModSlot_Firearm_Muzzle";

        /// <summary>A firearm's under-barrel mod slot.</summary>
        public const string UnderBarrelModSlot = "DA_ModSlot_Firearm_UnderBarrel";

        /// <summary>A firearm's stock mod slot.</summary>
        public const string StockModSlot = "DA_ModSlot_Firearm_Stock";

        /// <summary>The loadout's augment slot.</summary>
        public const string AugmentSlot = "DA_Inventory_ContainerSlot_Augment";

        /// <summary>The container an equipped augment sits in.</summary>
        public const string AugmentContainer = "DA_Inventory_AugmentContainer";

        /// <summary>The content slot of the augment container.</summary>
        public const string AugmentContentSlot = "DA_Inventory_ContentSlot_Augment_Generic";

        /// <summary>The loadout frame without an augment; augment frames' branches map onto its branches.</summary>
        public const string StandardLoadoutFrame = "DA_Inventory_LF_Standard";
    }

    /// <summary>Interaction animation containers, by object path.</summary>
    public static class InteractionAnimations
    {
        /// <summary>The door breach animation.</summary>
        public const string DoorBreach = "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_DoorBreach.DA_InteractionAnimation_DoorBreach";
    }

    /// <summary>Gameplay tags the character graph tracks that a server reads or sets, by tag name.</summary>
    public static class GraphTags
    {
        /// <summary>Tag <c>Ability.Block.ContextInteract.PendulumMove</c>.</summary>
        public const string AbilityBlockContextInteractPendulumMove = "Ability.Block.ContextInteract.PendulumMove";

        /// <summary>Tag <c>Ability.Block.Crouch.PendulumMove</c>.</summary>
        public const string AbilityBlockCrouchPendulumMove = "Ability.Block.Crouch.PendulumMove";

        /// <summary>Tag <c>Ability.Block.EquipItem.Interact</c>.</summary>
        public const string AbilityBlockEquipItemInteract = "Ability.Block.EquipItem.Interact";

        /// <summary>Tag <c>Ability.Block.EquipItem.PendulumMove</c>.</summary>
        public const string AbilityBlockEquipItemPendulumMove = "Ability.Block.EquipItem.PendulumMove";

        /// <summary>Tag <c>Ability.Block.FirePrimary.Interact</c>.</summary>
        public const string AbilityBlockFirePrimaryInteract = "Ability.Block.FirePrimary.Interact";

        /// <summary>Tag <c>Ability.Block.FirePrimary.PendulumMove</c>.</summary>
        public const string AbilityBlockFirePrimaryPendulumMove = "Ability.Block.FirePrimary.PendulumMove";

        /// <summary>Tag <c>Ability.Block.FireSecondary.Interact</c>.</summary>
        public const string AbilityBlockFireSecondaryInteract = "Ability.Block.FireSecondary.Interact";

        /// <summary>Tag <c>Ability.Block.FireSecondary.PendulumMove</c>.</summary>
        public const string AbilityBlockFireSecondaryPendulumMove = "Ability.Block.FireSecondary.PendulumMove";

        /// <summary>Tag <c>Ability.BlockInput.Crouch.PendulumMove</c>.</summary>
        public const string AbilityBlockInputCrouchPendulumMove = "Ability.BlockInput.Crouch.PendulumMove";

        /// <summary>Tag <c>Ability.BlockInput.Sprint.PendulumMove</c>.</summary>
        public const string AbilityBlockInputSprintPendulumMove = "Ability.BlockInput.Sprint.PendulumMove";

        /// <summary>Tag <c>Ability.Block.Jump.Interact</c>.</summary>
        public const string AbilityBlockJumpInteract = "Ability.Block.Jump.Interact";

        /// <summary>Tag <c>Ability.Block.Knockback.PendulumMove</c>.</summary>
        public const string AbilityBlockKnockbackPendulumMove = "Ability.Block.Knockback.PendulumMove";

        /// <summary>Tag <c>Ability.Block.Reload.Interact</c>.</summary>
        public const string AbilityBlockReloadInteract = "Ability.Block.Reload.Interact";

        /// <summary>Tag <c>Ability.Block.Reload.PendulumMove</c>.</summary>
        public const string AbilityBlockReloadPendulumMove = "Ability.Block.Reload.PendulumMove";

        /// <summary>Tag <c>Ability.Crouch</c>.</summary>
        public const string AbilityCrouch = "Ability.Crouch";

        /// <summary>Tag <c>Ability.DodgeRoll.Active</c>.</summary>
        public const string AbilityDodgeRollActive = "Ability.DodgeRoll.Active";

        /// <summary>Tag <c>Ability.FirePrimary.PendulumWep</c>.</summary>
        public const string AbilityFirePrimaryPendulumWep = "Ability.FirePrimary.PendulumWep";

        /// <summary>Tag <c>Ability.Flashlight.Active</c>.</summary>
        public const string AbilityFlashlightActive = "Ability.Flashlight.Active";

        /// <summary>Tag <c>Ability.Jump</c>.</summary>
        public const string AbilityJump = "Ability.Jump";

        /// <summary>Tag <c>Ability.Jump.Pending</c>.</summary>
        public const string AbilityJumpPending = "Ability.Jump.Pending";

        /// <summary>Tag <c>Ability.Landing.Heavy</c>.</summary>
        public const string AbilityLandingHeavy = "Ability.Landing.Heavy";

        /// <summary>Tag <c>Ability.Landing.Recovery</c>.</summary>
        public const string AbilityLandingRecovery = "Ability.Landing.Recovery";

        /// <summary>Tag <c>Ability.Sprint.Active</c>.</summary>
        public const string AbilitySprintActive = "Ability.Sprint.Active";

        /// <summary>Tag <c>Ability.Vault</c>.</summary>
        public const string AbilityVault = "Ability.Vault";

        /// <summary>Tag <c>Ability.Zooming</c>.</summary>
        public const string AbilityZooming = "Ability.Zooming";

        /// <summary>Tag <c>Animation.Ability.Jump.Started</c>.</summary>
        public const string AnimationAbilityJumpStarted = "Animation.Ability.Jump.Started";

        /// <summary>Tag <c>Animation.Pendulum.DisplaySyncedPose</c>.</summary>
        public const string AnimationPendulumDisplaySyncedPose = "Animation.Pendulum.DisplaySyncedPose";

        /// <summary>Tag <c>Animation.PendulumState.DBNO</c>.</summary>
        public const string AnimationPendulumStateDbno = "Animation.PendulumState.DBNO";

        /// <summary>Tag <c>Animation.PendulumState.DBNOToBaseMove</c>.</summary>
        public const string AnimationPendulumStateDbnoToBaseMove = "Animation.PendulumState.DBNOToBaseMove";

        /// <summary>Tag <c>Animation.PendulumState.DeathToDBNO</c>.</summary>
        public const string AnimationPendulumStateDeathToDbno = "Animation.PendulumState.DeathToDBNO";

        /// <summary>Tag <c>Animation.PendulumState.SnapHookActive</c>.</summary>
        public const string AnimationPendulumStateSnapHookActive = "Animation.PendulumState.SnapHookActive";

        /// <summary>Tag <c>Animation.PendulumState.StationaryInteraction.NonRootMotion</c>.</summary>
        public const string AnimationPendulumStateStationaryInteractionNonRootMotion = "Animation.PendulumState.StationaryInteraction.NonRootMotion";

        /// <summary>Tag <c>Animation.PendulumState.StationaryInteraction.RootMotion</c>.</summary>
        public const string AnimationPendulumStateStationaryInteractionRootMotion = "Animation.PendulumState.StationaryInteraction.RootMotion";

        /// <summary>Tag <c>Attribute.Movement.Heavy</c>.</summary>
        public const string AttributeMovementHeavy = "Attribute.Movement.Heavy";

        /// <summary>Tag <c>Attribute.Movement.SuperHeavy</c>.</summary>
        public const string AttributeMovementSuperHeavy = "Attribute.Movement.SuperHeavy";

        /// <summary>Tag <c>Attribute.Stamina.BlockRecharge.PendulumMove</c>.</summary>
        public const string AttributeStaminaBlockRechargePendulumMove = "Attribute.Stamina.BlockRecharge.PendulumMove";

        /// <summary>Tag <c>GameplayCue.Ability.Ladder.Slide</c>.</summary>
        public const string GameplayCueAbilityLadderSlide = "GameplayCue.Ability.Ladder.Slide";

        /// <summary>Tag <c>GameplayEffect.AttachedTraversal.Ladder</c>.</summary>
        public const string GameplayEffectAttachedTraversalLadder = "GameplayEffect.AttachedTraversal.Ladder";

        /// <summary>Tag <c>GameplayEffect.AttachedTraversal.Zipline</c>.</summary>
        public const string GameplayEffectAttachedTraversalZipline = "GameplayEffect.AttachedTraversal.Zipline";

        /// <summary>Tag <c>Status.Exhausted</c>.</summary>
        public const string StatusExhausted = "Status.Exhausted";

        /// <summary>Tag <c>Status.Immobilized.Interact</c>.</summary>
        public const string StatusImmobilizedInteract = "Status.Immobilized.Interact";

        /// <summary>Tag <c>UI.SkillState.Active.SprintBoost</c>.</summary>
        public const string UiSkillStateActiveSprintBoost = "UI.SkillState.Active.SprintBoost";
    }
}
