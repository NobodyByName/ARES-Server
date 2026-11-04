namespace Ares.Structs.Game.Inventory;

/// <summary>Layout of the player container state in the pawn blackboard.</summary>
public static class InventoryBlackboard
{
    /// <summary>Slot in the player container state.</summary>
    public const int Slot = 10;

    /// <summary>Descriptor in the player container state.</summary>
    public const int Descriptor = 35;

    /// <summary>LoadoutSlotCount in the player container state.</summary>
    public const int LoadoutSlotCount = 5;

    /// <summary>Firearm1Slot in the player container state.</summary>
    public const int Firearm1Slot = 0;

    /// <summary>Firearm2Slot in the player container state.</summary>
    public const int Firearm2Slot = 1;

    /// <summary>ArmorSlot in the player container state.</summary>
    public const int ArmorSlot = 2;

    /// <summary>MeleeSlot in the player container state.</summary>
    public const int MeleeSlot = 3;

    /// <summary>UnarmedSlot in the player container state.</summary>
    public const int UnarmedSlot = 4;

    /// <summary>RevealTimeField in the player container state.</summary>
    public const int RevealTimeField = 0;

    /// <summary>ContainerCountField in the player container state.</summary>
    public const int ContainerCountField = 1;

    /// <summary>SlotCountField in the player container state.</summary>
    public const int SlotCountField = 2;

    /// <summary>CurrentContainerIdField in the player container state.</summary>
    public const int CurrentContainerIdField = 3;

    /// <summary>DirtyCountField in the player container state.</summary>
    public const int DirtyCountField = 4;

    /// <summary>CurrentContainerRevisionField in the player container state.</summary>
    public const int CurrentContainerRevisionField = 5;

    /// <summary>ContainersField in the player container state.</summary>
    public const int ContainersField = 6;

    /// <summary>SlotsField in the player container state.</summary>
    public const int SlotsField = 7;

    /// <summary>ContainerIdField in the player container state.</summary>
    public const int ContainerIdField = 0;

    /// <summary>ContainerFlagsField in the player container state.</summary>
    public const int ContainerFlagsField = 1;

    /// <summary>ContainerMutationField in the player container state.</summary>
    public const int ContainerMutationField = 2;

    /// <summary>ContainerRevisionField in the player container state.</summary>
    public const int ContainerRevisionField = 3;

    /// <summary>ContainerSizeField in the player container state.</summary>
    public const int ContainerSizeField = 4;

    /// <summary>AmountField in the player container state.</summary>
    public const int AmountField = 0;

    /// <summary>GuidAField in the player container state.</summary>
    public const int GuidAField = 1;

    /// <summary>QualityField in the player container state.</summary>
    public const int QualityField = 5;

    /// <summary>DurabilityField in the player container state.</summary>
    public const int DurabilityField = 6;

    /// <summary>AssetIndexField in the player container state.</summary>
    public const int AssetIndexField = 8;

    /// <summary>AmmoField in the player container state.</summary>
    public const int AmmoField = 9;

    /// <summary>SubcontainerField in the player container state.</summary>
    public const int SubcontainerField = 11;

    /// <summary>ContainerFlags in the player container state.</summary>
    public const int ContainerFlags = 5;

    /// <summary>RubberDuckIndex in the player container state.</summary>
    public const int RubberDuckIndex = 777;

    /// <summary>DurabilityScale in the player container state.</summary>
    public const int DurabilityScale = 127;

    /// <summary>ComponentName in the player container state.</summary>
    public const string ComponentName = "InventoryComponent";

}
