namespace Ares.Structs.Backend.Progression;

/// <summary>The ammunition item each supported firearm loads.</summary>
public static class WeaponAmmunition
{
    /// <summary>Technical name of the ammunition item, by technical name of the firearm.</summary>
    public static IReadOnlyDictionary<string, string> ByWeapon { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ItemTechnicalNames.FerroI] = ItemTechnicalNames.HeavyAmmo,
    };
}
