using System.Text.Json.Serialization;
using Ares.Structs.Game.World;

namespace Ares.Common.GameData;

/// <summary>
/// Source-generated JSON metadata of the game data files: camelCase members, every constructor parameter without a
/// default required, nullable annotations enforced, unknown members rejected, nulls omitted when writing.
/// Table row types are listed here with their own <see cref="JsonSerializableAttribute"/>; they stay free of JSON
/// attributes.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    NumberHandling = JsonNumberHandling.Strict,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(GameDataManifest))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(MapWorldObjects))]
[JsonSerializable(typeof(LootSocketMesh))]
[JsonSerializable(typeof(LootSocket))]
[JsonSerializable(typeof(PlacedLootContainer))]
public sealed partial class GameDataJsonContext : JsonSerializerContext;
