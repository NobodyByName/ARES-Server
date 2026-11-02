namespace Ares.Structs.Game.World;

/// <summary>Authored runtime data layers of the Dam map.</summary>
public static class DamDataLayers
{
    /// <summary>All layers, in authored order.</summary>
    public static IReadOnlyList<DataLayer> All { get; } =
    [
        new("DataLayer_FA3D89B449BBEB0AB9D5C5A8EB600487", "/Game/Pioneer/Core/DataLayers/RoundConfiguration_LockedDoors_Open.RoundConfiguration_LockedDoors_Open", DataLayerRuntimeState.Unloaded),
        new("DataLayer_F76AF601465EAE9DF47AD999FEE36240", "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_Thunderstorm.TheDam_01_Lighting_Thunderstorm", DataLayerRuntimeState.Unloaded),
        new("DataLayer_EA86CCFC497AF4DCF6416E9C424A5E78", "/Game/Pioneer/Core/DataLayers/RoundConfiguration_ToxicSwamp_AirPurifier.RoundConfiguration_ToxicSwamp_AirPurifier", DataLayerRuntimeState.Unloaded),
        new("DataLayer_E73D033B4DA6A55A73CD8E92DD235AF7", "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_Rainstorm.TheDam_01_Lighting_Rainstorm", DataLayerRuntimeState.Unloaded),
        new("DataLayer_CE4553134CE8DB192BFFA0A525909E9E", "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_ForestFire.TheDam_01_Lighting_ForestFire", DataLayerRuntimeState.Unloaded),
        new("DataLayer_C48C0CBE4C6D88F2E505F3A172B056B3", "/Game/Pioneer/Maps/TheDam_02/DataLayers/TheDam_02_Lighting_Frigate_Overcast.TheDam_02_Lighting_Frigate_Overcast", DataLayerRuntimeState.Unloaded),
        new("DataLayer_B2C4865D406F6C778C38AA8A8BBC3F51", "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_HazyDay.TheDam_01_Lighting_HazyDay", DataLayerRuntimeState.Unloaded),
        new("DataLayer_A6C81DA64CD900E894AB9A954ECE501E", "/Game/Pioneer/Core/DataLayers/RoundConfiguration_Harvester.RoundConfiguration_Harvester", DataLayerRuntimeState.Unloaded),
        new("DataLayer_A48B2B494FFE7484C2F0E5B6EFF4B656", "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_WinterSnowDam.TheDam_01_Lighting_WinterSnowDam", DataLayerRuntimeState.Unloaded),
        new("DataLayer_9F7DA4C64C3079CAD8B2F39EC4901AB3", "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_PinkSunrise.TheDam_01_Lighting_PinkSunrise", DataLayerRuntimeState.Unloaded),
        new("DataLayer_9898BDB44BC983B7FF05A29308C84D0A", "/Game/Pioneer/Core/DataLayers/RoundConfiguration_ElectromagneticStorm.RoundConfiguration_ElectromagneticStorm", DataLayerRuntimeState.Unloaded),
        new("DataLayer_921215FA481904AE4423DA9210CABC3D", "/Game/Pioneer/Core/DataLayers/RoundConfiguration_Turbulence.RoundConfiguration_Turbulence", DataLayerRuntimeState.Unloaded),
        new("DataLayer_8F6F03E14CD5062ED5B8CB9C87BCE96E", "/Game/Pioneer/Core/DataLayers/Gameplay_Salvage.Gameplay_Salvage", DataLayerRuntimeState.Unloaded),
        new("DataLayer_8D013B6346A2B53C40A1CABB0E580BB5", "/Game/Pioneer/Core/DataLayers/SmartObjects_Default.SmartObjects_Default", DataLayerRuntimeState.Activated),
        new("DataLayer_7EBAC4774598B90B562D4E881DFCB4FB", "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_WindyStorm.TheDam_01_Lighting_WindyStorm", DataLayerRuntimeState.Unloaded),
        new("DataLayer_7565040548B66AF257C03ABCED75785B", "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_GreyMorning.TheDam_01_Lighting_GreyMorning", DataLayerRuntimeState.Unloaded),
        new("DataLayer_6FA0DAED4834136F9FD6009206D7376E", "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_DefaultL.TheDam_01_Lighting_DefaultL", DataLayerRuntimeState.Unloaded),
        new("DataLayer_6E02D05F44ACC9D32E71D09CCBA70D38", "/Game/Pioneer/Core/DataLayers/RoundConfiguration_ToxicSwamp.RoundConfiguration_ToxicSwamp", DataLayerRuntimeState.Unloaded),
        new("DataLayer_6AE80371423C97978420B0B058A75D1A", "/Game/Pioneer/Core/DataLayers/BirdsAndTumbleweeds.BirdsAndTumbleweeds", DataLayerRuntimeState.Activated),
        new("DataLayer_62F93B724A8CCAA8F2498883691DD74C", "/Game/Pioneer/Core/DataLayers/RoundConfiguration_ToxicSwamp_Standard.RoundConfiguration_ToxicSwamp_Standard", DataLayerRuntimeState.Unloaded),
        new("DataLayer_58CEBF0045446258FBD4CCAC72405CA7", "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_Foggy.TheDam_01_Lighting_Foggy", DataLayerRuntimeState.Unloaded),
        new("DataLayer_52C006434D304F6D91245392A866367F", "/Game/Pioneer/Maps/TheDam_01/DataLayers/TheDam_01_Lighting_Night.TheDam_01_Lighting_Night", DataLayerRuntimeState.Unloaded),
        new("DataLayer_5137D9F643D5555ADA866486A77BF077", "/Game/Pioneer/Core/DataLayers/RoundConfiguration_LockedDoors_Standard.RoundConfiguration_LockedDoors_Standard", DataLayerRuntimeState.Unloaded),
        new("DataLayer_3E5D143841DB59A6407488B4ED6D9ACD", "/Game/Pioneer/Core/DataLayers/RoundConfiguration_Extractor.RoundConfiguration_Extractor", DataLayerRuntimeState.Unloaded),
        new("DataLayer_3D6B641041B18DF5CAFA5CBE822CC44A", "/Game/Pioneer/Core/DataLayers/RoundConfiguration_Coldsnap.RoundConfiguration_Coldsnap", DataLayerRuntimeState.Unloaded),
        new("DataLayer_3C3EE7E64D63869AA76648BDD7C85FBB", "/Game/Pioneer/Core/DataLayers/RoundConfiguration_NightRaid.RoundConfiguration_NightRaid", DataLayerRuntimeState.Unloaded),
        new("DataLayer_37D0D12649E565F171954BADF3819818", "/Game/Pioneer/Core/DataLayers/Navmesh_Salvage.Navmesh_Salvage", DataLayerRuntimeState.Unloaded),
        new("DataLayer_302DC7E64361781C2D1C2BB18B5F4785", "/Game/Pioneer/Core/DataLayers/RoundConfiguration_Frigate.RoundConfiguration_Frigate", DataLayerRuntimeState.Unloaded),
    ];
}
