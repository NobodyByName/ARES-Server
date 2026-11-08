namespace Ares.Structs.Game.Inventory;

/// <summary>Authored Ferro weapon values for the supported build.</summary>
public static class FerroDefaults
{
    /// <summary>Rounds the magazine holds.</summary>
    public const int ClipSize = 1;

    /// <summary>Shots per minute; the time between shots is 60 / FireRate seconds.</summary>
    public const double FireRate = 50.0;

    /// <summary>Full reload time, in seconds.</summary>
    public const double ReloadSeconds = 2.875;

    /// <summary>Time to draw the weapon, in seconds.</summary>
    public const double EquipSeconds = 1.3;

    /// <summary>Time to put the weapon away, in seconds.</summary>
    public const double UnequipSeconds = 0.7;

    /// <summary>Zoom time, in seconds.</summary>
    public const double ZoomSeconds = 1.0;

    /// <summary>Firing mode index sent in the weapon state.</summary>
    public const int FiringMode = 0;

    /// <summary>Number of mod slots on the weapon.</summary>
    public const int ModSlotCount = 3;

    /// <summary>Authored partial reload time, in seconds.</summary>
    public const double PartialReloadSeconds = 1.75;

    /// <summary>Blueprint class path of the Ferro item.</summary>
    public const string ItemClass = "/Game/Pioneer/Items/Firearms/BattleRifle_BreachAction_01/BP_Item_BattleRifle_BreachAction_01.BP_Item_BattleRifle_BreachAction_01_C";

}
