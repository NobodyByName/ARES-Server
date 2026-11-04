namespace Ares.Structs.Game.Inventory;

/// <summary>Layout of inventory commands in the player input stream.</summary>
public static class InventoryInput
{
    /// <summary>Slot in the inventory input state.</summary>
    public const int Slot = 5;

    /// <summary>Descriptor in the inventory input state.</summary>
    public const int Descriptor = 97;

    /// <summary>FromContainerField in the inventory input state.</summary>
    public const int FromContainerField = 0;

    /// <summary>ToContainerField in the inventory input state.</summary>
    public const int ToContainerField = 1;

    /// <summary>AmountField in the inventory input state.</summary>
    public const int AmountField = 2;

    /// <summary>TargetContainerField in the inventory input state.</summary>
    public const int TargetContainerField = 3;

    /// <summary>FromRevisionField in the inventory input state.</summary>
    public const int FromRevisionField = 5;

    /// <summary>FromSlotField in the inventory input state.</summary>
    public const int FromSlotField = 6;

    /// <summary>ToRevisionField in the inventory input state.</summary>
    public const int ToRevisionField = 7;

    /// <summary>ToSlotField in the inventory input state.</summary>
    public const int ToSlotField = 8;

    /// <summary>TargetRevisionField in the inventory input state.</summary>
    public const int TargetRevisionField = 9;

    /// <summary>CommandField in the inventory input state.</summary>
    public const int CommandField = 10;

}

/// <summary>The requested inventory operation.</summary>
public enum InventoryCommandType
{
    /// <summary>None operation.</summary>
    None = 0,

    /// <summary>SplitItems operation.</summary>
    SplitItems = 1,

    /// <summary>DropItem operation.</summary>
    DropItem = 2,

    /// <summary>DropPartialItem operation.</summary>
    DropPartialItem = 3,

    /// <summary>SwapItems operation.</summary>
    SwapItems = 4,

    /// <summary>MoveFillItem operation.</summary>
    MoveFillItem = 5,

}
