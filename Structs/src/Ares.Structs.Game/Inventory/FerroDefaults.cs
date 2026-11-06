namespace Ares.Structs.Game.Inventory;

/// <summary>Decoded Ferro weapon state and authored defaults for the supported build.</summary>
public static class FerroDefaults
{
    /// <summary>ClipSize in the supported weapon protocol.</summary>
    public const int ClipSize = 1;

    /// <summary>FireRate in the supported weapon protocol.</summary>
    public const double FireRate = 50.0;

    /// <summary>ReloadSeconds in the supported weapon protocol.</summary>
    public const double ReloadSeconds = 2.875;

    /// <summary>EquipSeconds in the supported weapon protocol.</summary>
    public const double EquipSeconds = 1.3;

    /// <summary>UnequipSeconds in the supported weapon protocol.</summary>
    public const double UnequipSeconds = 0.7;

    /// <summary>ZoomSeconds in the supported weapon protocol.</summary>
    public const double ZoomSeconds = 1.0;

    /// <summary>FiringMode in the supported weapon protocol.</summary>
    public const int FiringMode = 0;

    /// <summary>ModSlotCount in the supported weapon protocol.</summary>
    public const int ModSlotCount = 3;

    /// <summary>PartialReloadSeconds in the supported weapon protocol.</summary>
    public const double PartialReloadSeconds = 1.75;

    /// <summary>ItemClass in the supported weapon protocol.</summary>
    public const string ItemClass = "/Game/Pioneer/Items/Firearms/BattleRifle_BreachAction_01/BP_Item_BattleRifle_BreachAction_01.BP_Item_BattleRifle_BreachAction_01_C";

}
