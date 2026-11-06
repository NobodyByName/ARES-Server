namespace Ares.Structs.Game.Inventory;

/// <summary>Decoded Ferro weapon state and authored defaults for the supported build.</summary>
public static class WeaponProtocol
{
    /// <summary>InputSlot in the supported weapon protocol.</summary>
    public const int InputSlot = 3;

    /// <summary>InputDescriptor in the supported weapon protocol.</summary>
    public const int InputDescriptor = 101;

    /// <summary>InputFlagsField in the supported weapon protocol.</summary>
    public const int InputFlagsField = 22;

    /// <summary>InputEquipTargetField in the supported weapon protocol.</summary>
    public const int InputEquipTargetField = 24;

    /// <summary>InputEquipChangeField in the supported weapon protocol.</summary>
    public const int InputEquipChangeField = 23;

    /// <summary>FireBlockedBit in the supported weapon protocol.</summary>
    public const int FireBlockedBit = 0;

    /// <summary>FireDownBit in the supported weapon protocol.</summary>
    public const int FireDownBit = 1;

    /// <summary>FirePressedBit in the supported weapon protocol.</summary>
    public const int FirePressedBit = 3;

    /// <summary>AllowPrimaryBit in the supported weapon protocol.</summary>
    public const int AllowPrimaryBit = 6;

    /// <summary>ReloadDownBit in the supported weapon protocol.</summary>
    public const int ReloadDownBit = 8;

    /// <summary>ReloadOldBit in the supported weapon protocol.</summary>
    public const int ReloadOldBit = 9;

    /// <summary>InventorySlot in the supported weapon protocol.</summary>
    public const int InventorySlot = 4;

    /// <summary>InventoryDescriptor in the supported weapon protocol.</summary>
    public const int InventoryDescriptor = 61;

    /// <summary>WeaponDescriptor in the supported weapon protocol.</summary>
    public const int WeaponDescriptor = 96;

    /// <summary>FirstWeaponSlot in the supported weapon protocol.</summary>
    public const int FirstWeaponSlot = 11;

    /// <summary>FirstWeaponPosition in the supported weapon protocol.</summary>
    public const int FirstWeaponPosition = 33;

    /// <summary>WeaponNodeDataId in the supported weapon protocol.</summary>
    public const int WeaponNodeDataId = 25;

    /// <summary>InvalidHandle in the supported weapon protocol.</summary>
    public const int InvalidHandle = 255;

    /// <summary>NoItemHandle in the supported weapon protocol.</summary>
    public const int NoItemHandle = 254;

    /// <summary>UnarmedHandle in the supported weapon protocol.</summary>
    public const int UnarmedHandle = 6;

    /// <summary>AmmoTypeCount in the supported weapon protocol.</summary>
    public const int AmmoTypeCount = 6;

    /// <summary>HeavyAmmoIndex in the supported weapon protocol.</summary>
    public const int HeavyAmmoIndex = 2;

    /// <summary>ReserveCountField in the supported weapon protocol.</summary>
    public const int ReserveCountField = 39;

    /// <summary>ReserveArrayField in the supported weapon protocol.</summary>
    public const int ReserveArrayField = 66;

    /// <summary>InventoryPrimaryField in the supported weapon protocol.</summary>
    public const int InventoryPrimaryField = 46;

    /// <summary>InventorySecondaryField in the supported weapon protocol.</summary>
    public const int InventorySecondaryField = 47;

    /// <summary>InventoryCurrentField in the supported weapon protocol.</summary>
    public const int InventoryCurrentField = 50;

    /// <summary>InventoryTargetField in the supported weapon protocol.</summary>
    public const int InventoryTargetField = 51;

    /// <summary>InventoryUnequipFromField in the supported weapon protocol.</summary>
    public const int InventoryUnequipFromField = 52;

    /// <summary>InventoryOffhandField in the supported weapon protocol.</summary>
    public const int InventoryOffhandField = 53;

    /// <summary>InventoryEquipPhaseField in the supported weapon protocol.</summary>
    public const int InventoryEquipPhaseField = 54;

    /// <summary>InventoryLastWantedField in the supported weapon protocol.</summary>
    public const int InventoryLastWantedField = 55;

    /// <summary>InventoryEquipChangeField in the supported weapon protocol.</summary>
    public const int InventoryEquipChangeField = 58;

    /// <summary>InventoryEquipTimerField in the supported weapon protocol.</summary>
    public const int InventoryEquipTimerField = 33;

    /// <summary>EquipPhaseIdle in the supported weapon protocol.</summary>
    public const int EquipPhaseIdle = 0;

    /// <summary>EquipPhaseUnequipping in the supported weapon protocol.</summary>
    public const int EquipPhaseUnequipping = 1;

    /// <summary>EquipPhaseEquipping in the supported weapon protocol.</summary>
    public const int EquipPhaseEquipping = 2;

    /// <summary>WeaponItemInstanceField in the supported weapon protocol.</summary>
    public const int WeaponItemInstanceField = 41;

    /// <summary>WeaponNodeDataField in the supported weapon protocol.</summary>
    public const int WeaponNodeDataField = 4;

    /// <summary>WeaponVisualIndexField in the supported weapon protocol.</summary>
    public const int WeaponVisualIndexField = 5;

    /// <summary>WeaponAssetField in the supported weapon protocol.</summary>
    public const int WeaponAssetField = 19;

    /// <summary>WeaponDurabilityField in the supported weapon protocol.</summary>
    public const int WeaponDurabilityField = 20;

    /// <summary>WeaponFlagsField in the supported weapon protocol.</summary>
    public const int WeaponFlagsField = 18;

    /// <summary>WeaponEquipTimeField in the supported weapon protocol.</summary>
    public const int WeaponEquipTimeField = 21;

    /// <summary>WeaponUnequipTimeField in the supported weapon protocol.</summary>
    public const int WeaponUnequipTimeField = 22;

    /// <summary>WeaponReloadTimeField in the supported weapon protocol.</summary>
    public const int WeaponReloadTimeField = 25;

    /// <summary>WeaponZoomTimeField in the supported weapon protocol.</summary>
    public const int WeaponZoomTimeField = 26;

    /// <summary>WeaponAmmoField in the supported weapon protocol.</summary>
    public const int WeaponAmmoField = 27;

    /// <summary>WeaponReadyTimeField in the supported weapon protocol.</summary>
    public const int WeaponReadyTimeField = 9;

    /// <summary>WeaponFireCooldownField in the supported weapon protocol.</summary>
    public const int WeaponFireCooldownField = 28;

    /// <summary>WeaponShotCountField in the supported weapon protocol.</summary>
    public const int WeaponShotCountField = 10;

    /// <summary>WeaponAmmoIndexField in the supported weapon protocol.</summary>
    public const int WeaponAmmoIndexField = 34;

    /// <summary>WeaponBurstSizeField in the supported weapon protocol.</summary>
    public const int WeaponBurstSizeField = 36;

    /// <summary>WeaponStateField in the supported weapon protocol.</summary>
    public const int WeaponStateField = 37;

    /// <summary>WeaponFiringModeField in the supported weapon protocol.</summary>
    public const int WeaponFiringModeField = 38;

    /// <summary>WeaponReloadStepField in the supported weapon protocol.</summary>
    public const int WeaponReloadStepField = 16;

    /// <summary>WeaponClipSizeField in the supported weapon protocol.</summary>
    public const int WeaponClipSizeField = 29;

    /// <summary>WeaponQualityField in the supported weapon protocol.</summary>
    public const int WeaponQualityField = 17;

    /// <summary>WeaponFireRateField in the supported weapon protocol.</summary>
    public const int WeaponFireRateField = 2;

    /// <summary>WeaponItemCountField in the supported weapon protocol.</summary>
    public const int WeaponItemCountField = 6;

    /// <summary>WeaponModSlotCountField in the supported weapon protocol.</summary>
    public const int WeaponModSlotCountField = 13;

    /// <summary>WeaponModSlotStatesField in the supported weapon protocol.</summary>
    public const int WeaponModSlotStatesField = 42;

    /// <summary>EmptyModSlot in the supported weapon protocol.</summary>
    public const int EmptyModSlot = 255;

    /// <summary>ConsumeAmmoMask in the supported weapon protocol.</summary>
    public const int ConsumeAmmoMask = 64;

    /// <summary>BlockSprintMask in the supported weapon protocol.</summary>
    public const int BlockSprintMask = 128;

    /// <summary>PlayFiringEffectsMask in the supported weapon protocol.</summary>
    public const int PlayFiringEffectsMask = 256;

    /// <summary>QualityBias in the supported weapon protocol.</summary>
    public const int QualityBias = 128;

    /// <summary>NoReloadStep in the supported weapon protocol.</summary>
    public const int NoReloadStep = 127;

    /// <summary>DurabilityMaximum in the supported weapon protocol.</summary>
    public const int DurabilityMaximum = 65535;

    /// <summary>FireRateSteps in the supported weapon protocol.</summary>
    public const int FireRateSteps = 1024;

    /// <summary>FireRateMaximum in the supported weapon protocol.</summary>
    public const int FireRateMaximum = 10000;

    /// <summary>IdleState in the supported weapon protocol.</summary>
    public const int IdleState = 0;

    /// <summary>FiringState in the supported weapon protocol.</summary>
    public const int FiringState = 1;

    /// <summary>ReloadingState in the supported weapon protocol.</summary>
    public const int ReloadingState = 4;

}
