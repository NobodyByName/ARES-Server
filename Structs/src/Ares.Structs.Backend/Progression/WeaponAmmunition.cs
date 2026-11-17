namespace Ares.Structs.Backend.Progression;

/// <summary>The ammunition item each supported firearm loads.</summary>
public static class WeaponAmmunition
{
    /// <summary>Asset id of the ammunition item, by firearm asset id.</summary>
    public static IReadOnlyDictionary<long, long> ByWeapon { get; } = new Dictionary<long, long>
    {
        [WeaponItems.FerroIAssetId] = WeaponItems.HeavyAmmoAssetId,
    };
}
