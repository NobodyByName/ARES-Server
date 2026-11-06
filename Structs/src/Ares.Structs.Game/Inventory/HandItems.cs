namespace Ares.Structs.Game.Inventory;

/// <summary>Items every raider holds besides firearms: empty hands (Unarmed) and the Raider Tool.</summary>
public static class HandItems
{
    /// <summary>UnarmedItemClass in the supported weapon protocol.</summary>
    public const string UnarmedItemClass = "/Game/Pioneer/Items/Misc/Unarmed/BP_UnarmedItem.BP_UnarmedItem_C";

    /// <summary>UnarmedUsableIndex in the supported weapon protocol.</summary>
    public const int UnarmedUsableIndex = 852;

    /// <summary>RaiderToolItemClass in the supported weapon protocol.</summary>
    public const string RaiderToolItemClass = "/Game/Pioneer/Items/_GENERATED_/P2Prototype/BasicMeleeWeapon/BP_Item_BasicMeleeWeapon.BP_Item_BasicMeleeWeapon_C";

    /// <summary>RaiderToolUsableIndex in the supported weapon protocol.</summary>
    public const int RaiderToolUsableIndex = 45;

    /// <summary>UnarmedItemInstanceField in the supported weapon protocol.</summary>
    public const int UnarmedItemInstanceField = 11;

    /// <summary>RaiderToolHandle in the supported weapon protocol.</summary>
    public const int RaiderToolHandle = 5;

    /// <summary>RaiderToolItemInstanceField in the supported weapon protocol.</summary>
    public const int RaiderToolItemInstanceField = 18;

    /// <summary>UnarmedEquipSeconds in the supported weapon protocol.</summary>
    public const double UnarmedEquipSeconds = 0.2;

    /// <summary>UnarmedUnequipSeconds in the supported weapon protocol.</summary>
    public const double UnarmedUnequipSeconds = 0.2;

    /// <summary>RaiderToolEquipSeconds in the supported weapon protocol.</summary>
    public const double RaiderToolEquipSeconds = 0.3;

    /// <summary>RaiderToolUnequipSeconds in the supported weapon protocol.</summary>
    public const double RaiderToolUnequipSeconds = 0.3;

}
