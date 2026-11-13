namespace Ares.Common.Game.Loot;

/// <summary>
/// ARES's default policy for the containers that dynamic loot spawners place. Only the listed classes spawn: when a
/// spawner that runs draws any other class, it places nothing there, and the rest of the raid's spawners are
/// unaffected.
/// </summary>
public static class DynamicLootDefaults
{
    /// <summary>Container classes (class paths) a spawner may place.</summary>
    public static IReadOnlyList<string> EnabledClasses { get; } =
    [
        "/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C",
        "/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C",
        "/Game/Pioneer/Environment/Props/Research/WeaponsRack_01/BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic.BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic_C",
    ];

    private static readonly HashSet<string> s_enabled = new(EnabledClasses, StringComparer.Ordinal);

    /// <summary>Whether a spawner may place a container of this class.</summary>
    /// <param name="classPath">The container's class path.</param>
    /// <returns>True when the class is enabled.</returns>
    public static bool Enabled(string classPath)
    {
        ArgumentNullException.ThrowIfNull(classPath);
        return s_enabled.Contains(classPath);
    }
}
