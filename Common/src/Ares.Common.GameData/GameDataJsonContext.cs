using System.Text.Json.Serialization;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.Common.GameData;

/// <summary>
/// Source-generated JSON metadata of the game data files: camelCase members, every constructor parameter without a
/// default required, nullable annotations enforced, unknown members rejected, nulls written (so a nullable member
/// without a default, which is required, reads back), and members computed from the others (properties without a
/// setter) left out. Table row types are listed here with their own <see cref="JsonSerializableAttribute"/>; they stay
/// free of JSON attributes.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    IgnoreReadOnlyProperties = true,
    NumberHandling = JsonNumberHandling.Strict,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(GameDataManifest))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(MapWorldObjects))]
[JsonSerializable(typeof(LootSocketMesh))]
[JsonSerializable(typeof(LootSocket))]
[JsonSerializable(typeof(LooseItemSocket))]
[JsonSerializable(typeof(PlacedLootContainer))]
[JsonSerializable(typeof(PlayerStart))]
[JsonSerializable(typeof(LadderPlacement))]
[JsonSerializable(typeof(ArchetypeClass))]
[JsonSerializable(typeof(ZiplinePlacement))]
[JsonSerializable(typeof(ActivatableZiplinePlacement))]
[JsonSerializable(typeof(ItemDefinition))]
[JsonSerializable(typeof(ItemValue))]
[JsonSerializable(typeof(MenuItem))]
[JsonSerializable(typeof(AugmentDefinition))]
[JsonSerializable(typeof(LoadoutFrameDefinition))]
[JsonSerializable(typeof(FirearmDefinition))]
[JsonSerializable(typeof(ShieldDefinition))]
[JsonSerializable(typeof(DodgeRollSequence))]
[JsonSerializable(typeof(VaultDefinitionSet))]
[JsonSerializable(typeof(HeavyLandingLevel))]
[JsonSerializable(typeof(SnapHookItemDefaults))]
[JsonSerializable(typeof(WeightedCurveKey))]
[JsonSerializable(typeof(InteractionAnimationContainer))]
[JsonSerializable(typeof(TaggedInteractionAnimationContainer))]
[JsonSerializable(typeof(LootContainerType))]
[JsonSerializable(typeof(LootContainerClass))]
[JsonSerializable(typeof(StaticValueLootTable))]
[JsonSerializable(typeof(SkillLootTable))]
[JsonSerializable(typeof(PointOfInterest))]
[JsonSerializable(typeof(LootLimiter))]
[JsonSerializable(typeof(DynamicLootSpawner))]
[JsonSerializable(typeof(DynamicLootGroupEntry))]
[JsonSerializable(typeof(MapGameMode))]
[JsonSerializable(typeof(DoorPlacement))]
[JsonSerializable(typeof(ExploitPreventionAreaPlacement))]
[JsonSerializable(typeof(SplinePathPlacement))]
[JsonSerializable(typeof(RoundConfiguration))]
[JsonSerializable(typeof(LightingScenario))]
[JsonSerializable(typeof(RoundLightingGroup))]
[JsonSerializable(typeof(ExtractionElevator))]
[JsonSerializable(typeof(ExtractionButton))]
[JsonSerializable(typeof(ExtractionPointPlacement))]
[JsonSerializable(typeof(ZiplineLineDefaults))]
[JsonSerializable(typeof(PlayerStartCapsule))]
public sealed partial class GameDataJsonContext : JsonSerializerContext;
