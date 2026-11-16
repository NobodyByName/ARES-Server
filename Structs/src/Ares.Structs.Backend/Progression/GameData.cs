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

/// <summary>
/// Something a player can buy, such as a trader's wares. A trader's screen lists the offers whose owner is
/// that trader's item asset, sorted by <see cref="OrderIndex"/>.
/// </summary>
/// <param name="OfferId">Key the purchase operation names the offer by.</param>
/// <param name="OfferType">Where the offer is sold, e.g. <c>NPC</c> for traders.</param>
/// <param name="Title">Internal title; the trade screen shows the reward item's own name.</param>
/// <param name="LocalizedDescription">Key into the client's offer description strings, or empty for none.</param>
/// <param name="OwnerGameAssetId">Item asset id of the trader or station selling the offer.</param>
/// <param name="DurationInSeconds">Time until bought items arrive; zero delivers them at once.</param>
/// <param name="IsVisible">Listed even when the player lacks <paramref name="RequiredItems"/>.</param>
/// <param name="RequiredItems">Items the player must own to buy the offer.</param>
/// <param name="CostPackage">Cost as read by stations and projects.</param>
/// <param name="Cost">Cost as read by the trade screen.</param>
/// <param name="RewardPackage">Items one purchase gives.</param>
/// <param name="OrderIndex">Position in the seller's list.</param>
/// <param name="StartDate">Unix seconds from which the offer is sold, or <see cref="NoDate"/>.</param>
/// <param name="EndDate">Unix seconds after which the offer expires, or <see cref="NoDate"/> for never.</param>
/// <param name="Limited">Stock per refresh period.</param>
public sealed record OfferDescription(
    [property: JsonPropertyName("offerId")] long OfferId,
    [property: JsonPropertyName("offerType")] string OfferType,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("localizedDescription")] string LocalizedDescription,
    [property: JsonPropertyName("ownerGameAssetId")] long OwnerGameAssetId,
    [property: JsonPropertyName("durationInSeconds")] long DurationInSeconds,
    [property: JsonPropertyName("isVisible")] bool IsVisible,
    [property: JsonPropertyName("requiredItems")] IReadOnlyList<OfferRequirement> RequiredItems,
    [property: JsonPropertyName("costPackage")] OfferCostPackage CostPackage,
    [property: JsonPropertyName("cost")] OfferCost Cost,
    [property: JsonPropertyName("rewardPackage")] OfferRewardPackage RewardPackage,
    [property: JsonPropertyName("orderIndex")] long OrderIndex,
    [property: JsonPropertyName("startDate")] long StartDate,
    [property: JsonPropertyName("endDate")] long EndDate,
    [property: JsonPropertyName("limited")] OfferLimit Limited)
{
    /// <summary>The client's empty date (its minimum date, 0001-01-01, in Unix seconds): no start and no end.</summary>
    public const long NoDate = -62135596800;
}

/// <summary>An item amount the player must own.</summary>
public sealed record OfferRequirement(
    [property: JsonPropertyName("itemGameAssetId")] long ItemGameAssetId,
    [property: JsonPropertyName("amount")] long Amount);

/// <summary>An offer's cost as a list of items.</summary>
public sealed record OfferCostPackage([property: JsonPropertyName("costItems")] IReadOnlyList<OfferCostItem> CostItems);

/// <summary>An item amount an offer costs, with the durability it is taken at.</summary>
public sealed record OfferCostItem(
    [property: JsonPropertyName("gameAssetId")] long GameAssetId,
    [property: JsonPropertyName("amount")] long Amount,
    [property: JsonPropertyName("durability")] double Durability);

/// <summary>What an offer costs per purchase. Other cost kinds are not decoded here.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "discriminant")]
[JsonDerivedType(typeof(ItemAmountsOfferCost), "itemAmounts")]
public abstract record OfferCost;

/// <summary>A cost of fixed item amounts, taken from any stacks of those items.</summary>
public sealed record ItemAmountsOfferCost(
    [property: JsonPropertyName("itemAmounts")] IReadOnlyList<ItemAssetAmount> ItemAmounts) : OfferCost;

/// <summary>An amount of one item asset.</summary>
public sealed record ItemAssetAmount(
    [property: JsonPropertyName("gameAssetId")] long GameAssetId,
    [property: JsonPropertyName("amount")] long Amount);

/// <summary>The items one purchase of an offer gives.</summary>
public sealed record OfferRewardPackage([property: JsonPropertyName("rewardedItems")] IReadOnlyList<OfferRewardedItem> RewardedItems);

/// <summary>
/// An item an offer gives. An item whose <paramref name="AliasInstanceId"/> appears in another rewarded item's
/// <paramref name="Slots"/> is given inside that item.
/// </summary>
/// <param name="GameAssetId">Rewarded item asset.</param>
/// <param name="Amount">Amount per purchase.</param>
/// <param name="Durability">Durability of the given item.</param>
/// <param name="Slots">Alias ids of rewarded items placed in this item's slots.</param>
/// <param name="AliasInstanceId">Id other rewarded items name this one by.</param>
/// <param name="MaxAmount">Limit on the rewarded amount, or zero for none.</param>
/// <param name="HideInUi">Left out of the offer's reward list.</param>
public sealed record OfferRewardedItem(
    [property: JsonPropertyName("gameAssetId")] long GameAssetId,
    [property: JsonPropertyName("amount")] long Amount,
    [property: JsonPropertyName("durability")] double Durability,
    [property: JsonPropertyName("slots")] IReadOnlyList<string> Slots,
    [property: JsonPropertyName("aliasInstanceId")] string AliasInstanceId,
    [property: JsonPropertyName("maxAmount")] long MaxAmount,
    [property: JsonPropertyName("hideInUi")] bool HideInUi);

/// <summary>Stock of an offer: <paramref name="MaxAmount"/> purchases per <paramref name="RefreshInterval"/> seconds.</summary>
/// <param name="RefreshInterval">Seconds until the stock refills; zero never refills.</param>
/// <param name="MaxAmount">Purchases per period; zero for unlimited.</param>
public sealed record OfferLimit(
    [property: JsonPropertyName("refreshInterval")] long RefreshInterval,
    [property: JsonPropertyName("maxAmount")] long MaxAmount);

public sealed record ItemDescriptionTable(
    [property: JsonPropertyName("itemDescriptions")] IReadOnlyList<ItemDescription> ItemDescriptions);

/// <summary>Server-side definition of an item type.</summary>
public sealed record ItemDescription(
    [property: JsonPropertyName("gameAssetId")] long GameAssetId,
    [property: JsonPropertyName("itemType")] string ItemType,
    [property: JsonPropertyName("maxAmount")] long MaxAmount,
    [property: JsonPropertyName("stackable")] bool Stackable,
    [property: JsonPropertyName("unique")] bool Unique,
    [property: JsonPropertyName("tags")] IReadOnlyList<string> Tags,
    [property: JsonPropertyName("groupIDs")] IReadOnlyList<long> GroupIds)
{
    /// <summary>Asset id of the base item, or zero when this definition has no base.</summary>
    [JsonPropertyName("baseGameAssetId")]
    public long BaseGameAssetId { get; init; }

    /// <summary>The item's quality level.</summary>
    [JsonPropertyName("qualityLevel")]
    public long QualityLevel { get; init; }

    /// <summary>Child-slot layout, when available.</summary>
    [JsonPropertyName("slotConfig"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ItemSlotConfiguration? SlotConfig { get; init; }

    /// <summary>What selling one item gives, or null when the item cannot be sold.</summary>
    [JsonPropertyName("scrap"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ItemScrapDescription? Scrap { get; init; }

    /// <summary>
    /// What recycling one item gives, or null when the item cannot be recycled. The menu offers to recycle an item only
    /// when the package has rewarded items, and shows each one times the recycled amount.
    /// </summary>
    [JsonPropertyName("recycle"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OfferRewardPackage? Recycle { get; init; }
}

/// <summary>
/// What selling an item gives. The menu shows the coins of <paramref name="RewardPackage"/> as the item's value and
/// offers to sell it only when the package has rewarded items.
/// </summary>
/// <param name="RewardPackage">Items one sold item gives at full durability.</param>
public sealed record ItemScrapDescription([property: JsonPropertyName("rewardPackage")] OfferRewardPackage RewardPackage);

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

public sealed record UnknownTrackingPlayerStat;
