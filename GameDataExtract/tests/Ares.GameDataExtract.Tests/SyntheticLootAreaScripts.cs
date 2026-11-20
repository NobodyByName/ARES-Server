using Ares.Structs.Game.Scripts;

namespace Ares.GameDataExtract.Tests;

/// <summary>
/// Made-up script layouts with the classes, structs and enums the loot area readers check: their member names in the
/// order the readers expect.
/// </summary>
internal static class SyntheticLootAreaScripts
{
    /// <summary>Adds the loot area classes, structs and enums.</summary>
    public static void Add(Dictionary<string, ScriptTypeDefinition> types, Dictionary<string, ScriptEnumDefinition> enums)
    {
        Class(types, "APioneerPointOfInterestVolume", "APointOfInterestVolume", "MapMarkerComponent", "AreaAnnouncementComponent");
        Class(types, "APointOfInterestVolume", "AHierarchicalAreaVolume", "PingInfo", "bDebug", "ParentPointOfInterest", "bConsiderForLocationPing");
        Class(types, "AHierarchicalAreaVolume", "AVolume", "ParentVolume", "CachedHierarchyDepth");
        Class(types, "UPOIMapMarkerComponent", "UClientMapMarkerComponent", "MarkerVisibilityType", "MapLayer", "LootThemes", "bCenterMapMarkerInVolume",
            "bDynamicThreatLevel", "EnemyThreatLevel", "LocationImage", "DialogueTag");
        Class(types, "UClientMapMarkerComponent", "USceneComponent", "Tag", "bIsVisible");
        Class(types, "UPingInfoComponent", "UActorComponent", "PingInformation", "PingVisible_Server", "bAlwaysAttachToRoot", "bSkipPingAttachment",
            "bKeepRelativePositionWhenAttachingToRoot", "FluffyTrackedPrimitiveComponent");
        Class(types, "FPingInformation", null, "DescriptionCategory", "PingDescription");
        Class(types, "ALootPlacementLimiter", "AActor", "Root", "LootContainerLimiterSocketGatherer", "Limits");
        Class(types, "ULootPlacementLimiterComponent", "USceneComponent", "SocketGatheringMethod", "SocketGatheringRadius", "Volumes");
        Class(types, "FLootContainerLimitConfig", null, "bUnlimited", "Limit", "NumSpawned");
        Class(types, "AActorSpawner", "AActor", "Root", "ActorSpawnerComponent", "PlacementMethod", "RotationMethod", "bSpawnOnBeginPlay", "bUseRoundConfiguration",
            "SpawnerMode", "RandomActorCount", "ActorType", "ActorTypes", "WeightedActorTypes");
        Class(types, "UActorSpawnerComponent", "USceneComponent", "PlacementMethod", "PlacementPointList");
        Class(types, "FActorSpawnerPoint", null, "Transform", "Weight", "bIsLocked");
        Class(types, "FActorSpawnerWeightedActorClass", null, "ActorType", "Weight");
        Class(types, "UActivityNode", "UObject", "Title", "LocationTags", "ContentTags", "bInheritParentNodeLocationTags", "bForceNoDelayedActivation",
            "DelayedActivationEvent", "SubnodesMin", "SubnodesMax", "bUseWeightingForChildNodeSelection", "Weight", "NumSubnodesAvailable", "Subnodes", "Archetypes",
            "Activities");
        Class(types, "UActorSpawnerActivityContainer", "UObject", "SpawnersMin", "SpawnersMax", "SpawnerConfigs");
        Class(types, "FActorSpawnerContainerConfig", null, "Spawner");
        SyntheticLootScripts.Enum(enums, "EPointOfInterestEnemyThreatLevel", "Unknown", "Low", "Medium", "High");
        SyntheticLootScripts.Enum(enums, "EEnvironmentalLootSocketGatheringMethod", "None", "WithinSphere", "WithinBox", "WithinVolume");
        SyntheticLootScripts.Enum(enums, "EActorSpawnerPlacementMethod", "SpawnerLocation", "PointList", "Volumes");
        SyntheticLootScripts.Enum(enums, "EActorSpawnerRotationMethod", "SpawnerRotation", "Random");
        SyntheticLootScripts.Enum(enums, "EActorSpawnerMode", "RandomActor", "AllActors", "AllActorsAtOneLocation");
    }

    /// <summary>Adds a class with reflected int members.</summary>
    private static void Class(Dictionary<string, ScriptTypeDefinition> types, string name, string? super, params string[] members) =>
        SyntheticLootScripts.Class(types, name, super, [.. members.Select(member => (member, ScriptMemberType.Int32))]);
}
