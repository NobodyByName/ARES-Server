namespace Ares.Structs.Game.Inventory;

/// <summary>
/// The authored values of a firearm item class (a Blueprint deriving from <c>UFirearmItem</c>): its class default
/// object's members, or <see cref="WeaponItemDefaults"/> where it sets none.
/// </summary>
/// <param name="ItemClass">Class path of the firearm item, as <see cref="ItemDefinition.ItemClass"/> names it.</param>
/// <param name="ClipSize">Rounds the magazine holds (<c>BaseClipSize</c>).</param>
/// <param name="FireRate">Shots per minute (<c>BaseFireRate</c>); the time between shots is 60 / FireRate seconds.</param>
/// <param name="BaseReloadSeconds">Full reload time before quality modifiers, in seconds (<c>BaseReloadTime</c>).</param>
/// <param name="ReloadModifierSeconds">
/// Seconds the firearm's fixed quality set adds to the reload time, by quality level from 0 (its
/// <c>Reload Time Modifier Set</c>); empty when it has none. A quality's reload time is
/// <paramref name="BaseReloadSeconds"/> plus its modifier.
/// </param>
/// <param name="ZoomSeconds">Zoom time, in seconds (<c>BaseCameraZoomTime</c>).</param>
/// <param name="PartialReloadSeconds">Authored partial reload time, in seconds (<c>PartialReloadDuration</c>).</param>
/// <param name="ModSlots">Asset ids of the firearm's mod slots, in slot order.</param>
public sealed record FirearmDefinition(
    string ItemClass,
    double ClipSize,
    double FireRate,
    double BaseReloadSeconds,
    IReadOnlyList<double> ReloadModifierSeconds,
    double ZoomSeconds,
    double PartialReloadSeconds,
    IReadOnlyList<long> ModSlots);

/// <summary>
/// The class defaults of the weapon item scripts (<c>UWeaponItem</c>, and <c>UItemBase</c> for the zoom time) that a
/// firearm item class whose class default object sets none of them takes.
/// </summary>
public static class WeaponItemDefaults
{
    /// <summary><c>BaseClipSize</c>.</summary>
    public const double ClipSize = 5;

    /// <summary><c>BaseFireRate</c>, in shots per minute.</summary>
    public const double FireRate = 120;

    /// <summary><c>BaseReloadTime</c>, in seconds.</summary>
    public const double BaseReloadSeconds = 1;

    /// <summary><c>BaseCameraZoomTime</c>, in seconds.</summary>
    public const double ZoomSeconds = 0.35;

    /// <summary><c>PartialReloadDuration</c>, in seconds.</summary>
    public const double PartialReloadSeconds = 1;
}

/// <summary>
/// The authored values of a shield (armor) item class: its class default object's members, or the script class
/// defaults where it sets none.
/// </summary>
/// <param name="ItemClass">Class path of the shield item, as <see cref="ItemDefinition.ItemClass"/> names it.</param>
/// <param name="MaxArmorHealth">Shield health of a full shield (<c>MaxArmorHealth</c>).</param>
/// <param name="DamageMitigationPerPlate">Fraction of incoming damage the shield absorbs (<c>DamageMitigationPerPlate</c>).</param>
public sealed record ShieldDefinition(string ItemClass, double MaxArmorHealth, double DamageMitigationPerPlate);

/// <summary>An augment: the loadout frame it gives the raider and its carrying limit.</summary>
/// <param name="TechnicalName">Name of the augment's item data asset.</param>
/// <param name="AssetId">The augment's menu asset id (<see cref="ItemDefinition.AssetId"/>).</param>
/// <param name="FrameAssetId">Asset id of the loadout frame it selects.</param>
/// <param name="MaxWeight">Carrying limit while it is equipped (<c>MaxWeight</c>).</param>
public sealed record AugmentDefinition(string TechnicalName, long AssetId, long FrameAssetId, double MaxWeight);

/// <summary>A loadout frame: the container slots of a raider's equipment, one per container type.</summary>
/// <param name="TechnicalName">Name of the frame's data asset.</param>
/// <param name="AssetId">The frame's asset id.</param>
/// <param name="Containers">Its container slots, in the frame's order.</param>
public sealed record LoadoutFrameDefinition(string TechnicalName, long AssetId, IReadOnlyList<LoadoutFrameContainer> Containers);

/// <summary>A container slot of a loadout frame.</summary>
/// <param name="Type">Container type (<c>EInventoryContainerType</c>); slots of the same type in different frames hold the same equipment.</param>
/// <param name="ContainerSlotAssetId">Asset id of the frame's container slot of that type.</param>
public sealed record LoadoutFrameContainer(int Type, long ContainerSlotAssetId);
