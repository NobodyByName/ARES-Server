using Ares.GameDataExtract.Scripts;
using static Ares.GameDataExtract.Tests.SyntheticScriptCache;

namespace Ares.GameDataExtract.Tests;

/// <summary>
/// A made-up script cache with the classes, structs and enums the loot area readers check: their member names in the
/// order the readers expect, the constructor defaults they read and the member offsets those constructors store to.
/// </summary>
internal static class SyntheticLootAreaScripts
{
    private const uint Reflected = ScriptProperty.ReflectedFlag;
    private const long TypeKeyBase = 0x7100;
    private const int TypeIdBase = 70;

    private static readonly ScriptDataType s_int = new(0, 68);

    /// <summary>Adds the loot area classes, structs and enums to a precompiled script cache.</summary>
    public static void AddScripts(SyntheticScriptCache cache)
    {
        Class(cache, "APioneerPointOfInterestVolume", "APointOfInterestVolume", "MapMarkerComponent", "AreaAnnouncementComponent");
        Class(cache, "APointOfInterestVolume", "AHierarchicalAreaVolume", "PingInfo", "bDebug", "ParentPointOfInterest", "bConsiderForLocationPing");
        Class(cache, "AHierarchicalAreaVolume", "AVolume", "ParentVolume", "CachedHierarchyDepth");
        Class(cache, "UPOIMapMarkerComponent", "UClientMapMarkerComponent", "MarkerVisibilityType", "MapLayer", "LootThemes", "bCenterMapMarkerInVolume",
            "bDynamicThreatLevel", "EnemyThreatLevel", "LocationImage", "DialogueTag");
        Class(cache, "UClientMapMarkerComponent", "USceneComponent", "Tag", "bIsVisible");
        Class(cache, "UPingInfoComponent", "UActorComponent", "PingInformation", "PingVisible_Server", "bAlwaysAttachToRoot", "bSkipPingAttachment",
            "bKeepRelativePositionWhenAttachingToRoot", "FluffyTrackedPrimitiveComponent");
        Class(cache, "FPingInformation", null, "DescriptionCategory", "PingDescription");
        Class(cache, "ALootPlacementLimiter", "AActor", "Root", "LootContainerLimiterSocketGatherer", "Limits");
        Class(cache, "ULootPlacementLimiterComponent", "USceneComponent", [Store("SocketGatheringMethod", "SetV1", 0x100, 1), Store("SocketGatheringRadius", "SetV8", 0x108, Quad(1000.0))],
            "SocketGatheringMethod", "SocketGatheringRadius", "Volumes");
        Class(cache, "FLootContainerLimitConfig", null, [Store("bUnlimited", "SetV1", 0, 0), Store("Limit", "SetV4", 4, 0), Store("NumSpawned", "SetV4", 8, 0)], "bUnlimited", "Limit", "NumSpawned");
        Class(cache, "AActorSpawner", "AActor",
            [
                Store("bSpawnOnBeginPlay", "SetV1", 0x200, 0), Store("bUseRoundConfiguration", "SetV1", 0x201, 0), Store("SpawnerMode", "SetV1", 0x202, 0),
                Store("RandomActorCount", "SetV4", 0x204, 1),
            ],
            "Root", "ActorSpawnerComponent", "PlacementMethod", "RotationMethod", "bSpawnOnBeginPlay", "bUseRoundConfiguration", "SpawnerMode", "RandomActorCount",
            "ActorType", "ActorTypes", "WeightedActorTypes");
        cache.Classes[^1] = cache.Classes[^1] with
        {
            Methods = [new TestFunction(ScriptClass.InitDefaults, Code(Op("PshC8", 0, Quad(360.0)), Op("RET")))],
        };
        Class(cache, "UActorSpawnerComponent", "USceneComponent", "PlacementMethod", "PlacementPointList");
        Class(cache, "FActorSpawnerPoint", null, "Transform", "Weight", "bIsLocked");
        Class(cache, "FActorSpawnerWeightedActorClass", null, "ActorType", "Weight");
        Class(cache, "UActivityNode", "UObject", "Title", "LocationTags", "ContentTags", "bInheritParentNodeLocationTags", "bForceNoDelayedActivation",
            "DelayedActivationEvent", "SubnodesMin", "SubnodesMax", "bUseWeightingForChildNodeSelection", "Weight", "NumSubnodesAvailable", "Subnodes", "Archetypes",
            "Activities");
        Class(cache, "UActorSpawnerActivityContainer", "UObject", "SpawnersMin", "SpawnersMax", "SpawnerConfigs");
        Class(cache, "FActorSpawnerContainerConfig", null, "Spawner");
        cache.Enums.Add(("EPointOfInterestEnemyThreatLevel", ["Unknown", "Low", "Medium", "High"], [0, 1, 2, 3]));
        cache.Enums.Add(("EEnvironmentalLootSocketGatheringMethod", ["None", "WithinSphere", "WithinBox", "WithinVolume"], [0, 1, 2, 3]));
        cache.Enums.Add(("EActorSpawnerPlacementMethod", ["SpawnerLocation", "PointList", "Volumes"], [0, 1, 2]));
        cache.Enums.Add(("EActorSpawnerRotationMethod", ["SpawnerRotation", "Random"], [0, 1]));
        cache.Enums.Add(("EActorSpawnerMode", ["RandomActor", "AllActors", "AllActorsAtOneLocation"], [0, 1, 2]));
    }

    private static (string Member, string Op, int Offset, uint[] Value) Store(string member, string op, int offset, params uint[] value) => (member, op, offset, value);

    private static void Class(SyntheticScriptCache cache, string name, string? super, params string[] members) => Class(cache, name, super, [], members);

    /// <summary>
    /// Adds a class with reflected int members; with <paramref name="stores"/>, a constructor storing those literals and
    /// the stored members' offsets.
    /// </summary>
    private static void Class(SyntheticScriptCache cache, string name, string? super, (string Member, string Op, int Offset, uint[] Value)[] stores, params string[] members)
    {
        TestFunction[] constructors = [];
        if (stores.Length > 0)
        {
            int typeId = TypeIdBase + cache.TypeIds.Count;
            long typeKey = TypeKeyBase + cache.TypeIds.Count;
            cache.Types[typeKey] = (name, []);
            cache.TypeIds[typeId] = typeKey;
            cache.Properties.AddRange(stores.Select(store => (store.Member, typeId, store.Offset)));

            constructors =
            [
                new TestFunction(name, Code([.. stores.SelectMany(store => new[]
                {
                    Op(store.Op, 2, store.Value),
                    Op("LoadThisR", store.Offset, (uint)typeId),
                    Op("WRTV" + store.Op[^1], 2),
                }), Op("RET")])),
            ];
        }

        cache.Classes.Add(new TestClass(name, super, [.. members.Select(member => new TestProperty(member, s_int, Reflected))], [], constructors));
    }
}
