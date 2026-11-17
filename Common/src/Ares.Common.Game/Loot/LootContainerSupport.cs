namespace Ares.Common.Game.Loot;

/// <summary>
/// The loot container classes ARES does not serve yet. The game data lists every container class that decodes; a server
/// spawns and serves only the others.
/// </summary>
public static class LootContainerSupport
{
    /// <summary>Container classes ARES cannot serve yet, by class path, with what they need.</summary>
    public static IReadOnlyDictionary<string, string> Unsupported { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["/Game/Pioneer/Environment/Nature/Locations/Generic/Bushes/Mullein_01/BP_SalvageContainer_GreatMullein.BP_SalvageContainer_GreatMullein_C"] = "Blueprint graph (construction script); acquisition method AutoFill; instant loot",
        ["/Game/Pioneer/Environment/Nature/Locations/Generic/Trees/ApricotTree_01/BP_SalvageContainer_Apricot.BP_SalvageContainer_Apricot_C"] = "acquisition method Dispenser; instant loot",
        ["/Game/Pioneer/Environment/Nature/Locations/Generic/Trees/LemonTree_01/BP_SalvageContainer_Lemon.BP_SalvageContainer_Lemon_C"] = "acquisition method Dispenser; instant loot",
        ["/Game/Pioneer/Environment/Nature/Locations/Generic/Trees/Olive_01/BP_SalvageContainer_Olive.BP_SalvageContainer_Olive_C"] = "acquisition method Dispenser; instant loot",
        ["/Game/Pioneer/Environment/Nature/Locations/MountainCompound/Snowpile_01/BP_SalvageContainer_Snowpile.BP_SalvageContainer_Snowpile_C"] = "Blueprint graph (component event, event graph); Blueprint variables; acquisition method AutoFill",
        ["/Game/Pioneer/Environment/Nature/Locations/TheBlueGate/Bushes/CandleBerries_01/BP_SocketContainer_CandleBerries.BP_SocketContainer_CandleBerries_C"] = "acquisition method AutoFill; instant loot",
        ["/Game/Pioneer/Environment/Props/Commercial/Planters/BP_SalvageContainer_Shrub_03_H.BP_SalvageContainer_Shrub_03_H_C"] = "Blueprint graph (BeginPlay, construction script, event graph); root mesh with its own transform; acquisition method AutoFill; prompt on a proxy component; instant loot",
        ["/Game/Pioneer/Environment/Props/Dam/Planter_01/BP_SalvageContainer_Fertilizer.BP_SalvageContainer_Fertilizer_C"] = "Blueprint graph (BeginPlay, construction script, event graph); acquisition method AutoFill; prompt on a proxy component; instant loot",
        ["/Game/Pioneer/Environment/Props/Dam/WaterTank_01/BP_SalvageContainer_Moss.BP_SalvageContainer_Moss_C"] = "acquisition method AutoFill; prompt on a proxy component; instant loot",
        ["/Game/Pioneer/Environment/Props/Interactables/Android_01/BP_SocketContainer_Android_01_Lid_A.BP_SocketContainer_Android_01_Lid_A_C"] = "replicated construction-script component InteractQuestComponent",
        ["/Game/Pioneer/Environment/Props/Interactables/Android_01/BP_SocketContainer_Android_01_Lid_B.BP_SocketContainer_Android_01_Lid_B_C"] = "replicated construction-script component InteractQuestComponent",
        ["/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C"] = "replicated construction-script component Box",
        ["/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C"] = "replicated construction-script component Box",
        ["/Game/Pioneer/Environment/Props/_Generic/BirdNest_01/BP_SocketContainer_BirdNest_A.BP_SocketContainer_BirdNest_A_C"] = "Blueprint graph (component event, event graph); root mesh with its own transform",
        ["/Game/Pioneer/Environment/Props/_Generic/BirdNest_01/BP_SocketContainer_BirdNest_B.BP_SocketContainer_BirdNest_B_C"] = "Blueprint graph (component event, event graph); root mesh with its own transform",
        ["/Game/Pioneer/Environment/Props/_Generic/BirdNest_01/BP_SocketContainer_BirdNest_C.BP_SocketContainer_BirdNest_C_C"] = "Blueprint graph (component event, event graph); root mesh with its own transform",
        ["/Game/Pioneer/Environment/South/Base/Meshes/Rock_XL_07/BP_SalvageContainer_Moss_Rock.BP_SalvageContainer_Moss_Rock_C"] = "acquisition method AutoFill; prompt on a proxy component; instant loot",
        ["/Game/Pioneer/Environment/South/Base/Vegetation/Agave_01/BP_SalvageContainer_Agave.BP_SalvageContainer_Agave_C"] = "Blueprint graph (construction script); acquisition method AutoFill; instant loot",
        ["/Game/Pioneer/Environment/South/Forest/Vegetation/AleppoPine_01/BP_SalvageContainer_Resin.BP_SalvageContainer_Resin_C"] = "acquisition method AutoFill; prompt on a proxy component; instant loot",
        ["/Game/Pioneer/Environment/South/Swamp/Lootables/BP_SalvageContainer_MushroomCluster_01_A.BP_SalvageContainer_MushroomCluster_01_A_C"] = "Blueprint graph (construction script); acquisition method AutoFill; instant loot",
        ["/Game/Pioneer/Environment/South/VolcanicRed/Meshes/Cliff_XL_01/BP_SalvageContainer_VolcanicRock_Temp.BP_SalvageContainer_VolcanicRock_Temp_C"] = "acquisition method AutoFill; instant loot",
    };

    /// <summary>Whether ARES serves the container class at <paramref name="classPath"/>.</summary>
    public static bool IsSupported(string classPath) => !Unsupported.ContainsKey(classPath);
}
