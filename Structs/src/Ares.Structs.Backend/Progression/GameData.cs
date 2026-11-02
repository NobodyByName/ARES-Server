using System.Text.Json.Serialization;

namespace Ares.Structs.Backend.Progression;

/// <summary>Response of <c>GET /v1/pioneer/game-data</c>: definition tables the client caches at startup.</summary>
public sealed record GameDataResponse(
    [property: JsonPropertyName("quest-descriptions")] QuestDescriptionTable QuestDescriptions,
    [property: JsonPropertyName("offer-descriptions")] OfferDescriptionTable OfferDescriptions,
    [property: JsonPropertyName("items-description")] ItemDescriptionTable ItemDescriptions,
    [property: JsonPropertyName("level-groups")] LevelGroupTable LevelGroups);

public sealed record QuestDescriptionTable(
    [property: JsonPropertyName("questDescriptions")] IReadOnlyList<QuestDescription> QuestDescriptions);

public sealed record QuestDescription(
    [property: JsonPropertyName("gameAssetId")] int GameAssetId,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("questEdges")] IReadOnlyList<UnknownQuestEdge> QuestEdges,
    [property: JsonPropertyName("questObjectives")] IReadOnlyList<UnknownQuestObjective> QuestObjectives,
    [property: JsonPropertyName("questObjectiveNodes")] IReadOnlyList<UnknownQuestObjectiveNode> QuestObjectiveNodes,
    [property: JsonPropertyName("tags")] IReadOnlyList<string> Tags);

public sealed record OfferDescriptionTable(
    [property: JsonPropertyName("offerDescriptions")] IReadOnlyList<OfferDescription> OfferDescriptions);

public sealed record OfferDescription(
    [property: JsonPropertyName("offerId")] int OfferId,
    [property: JsonPropertyName("offerType")] string OfferType,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("localizedDescription")] string LocalizedDescription,
    [property: JsonPropertyName("isVisible")] bool IsVisible,
    [property: JsonPropertyName("requiredItems")] IReadOnlyList<UnknownRequiredItem> RequiredItems,
    [property: JsonPropertyName("costPackage")] UnknownItemPackage CostPackage,
    [property: JsonPropertyName("cost")] UnknownItemPackage Cost,
    [property: JsonPropertyName("rewardPackage")] UnknownItemPackage RewardPackage);

public sealed record ItemDescriptionTable(
    [property: JsonPropertyName("itemDescriptions")] IReadOnlyList<ItemDescription> ItemDescriptions);

/// <summary>Server-side definition of an item type.</summary>
public sealed record ItemDescription(
    [property: JsonPropertyName("gameAssetId")] int GameAssetId,
    [property: JsonPropertyName("itemType")] string ItemType,
    [property: JsonPropertyName("maxAmount")] int MaxAmount,
    [property: JsonPropertyName("stackable")] bool Stackable,
    [property: JsonPropertyName("unique")] bool Unique,
    [property: JsonPropertyName("tags")] IReadOnlyList<string> Tags,
    [property: JsonPropertyName("groupIDs")] IReadOnlyList<int> GroupIds);

public sealed record LevelGroupTable([property: JsonPropertyName("groups")] IReadOnlyList<LevelGroup> Groups);

/// <summary>A progression track (e.g. player level) and its levels.</summary>
public sealed record LevelGroup(
    [property: JsonPropertyName("groupId")] int GroupId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("xpItemGameAssetId")] int XpItemGameAssetId,
    [property: JsonPropertyName("trackingItemId")] int TrackingItemId,
    [property: JsonPropertyName("trackingPlayerStat")] UnknownTrackingPlayerStat TrackingPlayerStat,
    [property: JsonPropertyName("levels")] IReadOnlyList<LevelDefinition> Levels);

public sealed record LevelDefinition(
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("groupId")] int GroupId,
    [property: JsonPropertyName("absoluteAmountRequired")] long AbsoluteAmountRequired,
    [property: JsonPropertyName("relativeAmountRequired")] long RelativeAmountRequired,
    [property: JsonPropertyName("reward")] UnknownReward Reward);

/// <summary>
/// Tracking item asset ids the client looks up level groups by during user-data sync
/// (<c>TrySyncUserDataService</c> → <c>FindItemLevelGroupId</c>). Auto-claiming levels waits
/// until a group exists for each. Which progression each id tracks is not known yet.
/// </summary>
public static class SyncedLevelTrackingItems
{
    public const int UnknownTrackingItemA = -1869396984;
    public const int UnknownTrackingItemB = 1672550523;
}

public sealed record UnknownQuestEdge;

public sealed record UnknownQuestObjectiveNode;

public sealed record UnknownRequiredItem;

public sealed record UnknownItemPackage;

public sealed record UnknownTrackingPlayerStat;
