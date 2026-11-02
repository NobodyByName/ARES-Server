using System.Text.Json.Serialization;

namespace Ares.Structs.Backend.Progression;

/// <summary>
/// Changes to a player's server-side state, returned by every reconcile/claim route.
/// Element types are not decoded yet; ARES currently always sends empty changesets.
/// </summary>
public sealed record Changeset(
    [property: JsonPropertyName("items")] ItemChanges Items,
    [property: JsonPropertyName("quests")] QuestChanges Quests,
    [property: JsonPropertyName("stats")] StatChanges Stats,
    [property: JsonPropertyName("offers")] OfferChanges Offers,
    [property: JsonPropertyName("projects")] ProjectChanges Projects,
    [property: JsonPropertyName("notifications")] IReadOnlyList<UnknownChangesetNotification> Notifications)
{
    /// <summary>A changeset with no changes.</summary>
    public static Changeset Empty { get; } = new(
        new ItemChanges([], [], []),
        new QuestChanges([], [], [], []),
        new StatChanges([]),
        new OfferChanges([], [], []),
        new ProjectChanges([], [], [], []),
        []);
}

public sealed record ItemChanges(
    [property: JsonPropertyName("created")] IReadOnlyList<UnknownItem> Created,
    [property: JsonPropertyName("updated")] IReadOnlyList<UnknownItem> Updated,
    [property: JsonPropertyName("deleted")] IReadOnlyList<UnknownItem> Deleted);

public sealed record QuestChanges(
    [property: JsonPropertyName("created")] IReadOnlyList<UnknownQuest> Created,
    [property: JsonPropertyName("updated")] IReadOnlyList<UnknownQuest> Updated,
    [property: JsonPropertyName("createdObjectives")] IReadOnlyList<UnknownQuestObjective> CreatedObjectives,
    [property: JsonPropertyName("updatedObjectives")] IReadOnlyList<UnknownQuestObjective> UpdatedObjectives);

public sealed record StatChanges(
    [property: JsonPropertyName("scopedPlayerStats")] IReadOnlyList<UnknownScopedPlayerStat> ScopedPlayerStats);

public sealed record OfferChanges(
    [property: JsonPropertyName("transactionsCreated")] IReadOnlyList<UnknownOfferTransaction> TransactionsCreated,
    [property: JsonPropertyName("transactionsUpdated")] IReadOnlyList<UnknownOfferTransaction> TransactionsUpdated,
    [property: JsonPropertyName("transactionsDeleted")] IReadOnlyList<UnknownOfferTransaction> TransactionsDeleted);

public sealed record ProjectChanges(
    [property: JsonPropertyName("createdProjects")] IReadOnlyList<UnknownProject> CreatedProjects,
    [property: JsonPropertyName("updatedProjects")] IReadOnlyList<UnknownProject> UpdatedProjects,
    [property: JsonPropertyName("createdGoals")] IReadOnlyList<UnknownProjectGoal> CreatedGoals,
    [property: JsonPropertyName("updatedGoals")] IReadOnlyList<UnknownProjectGoal> UpdatedGoals);

/// <summary>Response of <c>POST /v1/pioneer/quests/reconcile</c>.</summary>
public sealed record ReconcileResponse([property: JsonPropertyName("changeset")] Changeset Changeset);

/// <summary>Response of <c>POST /v1/pioneer/store/reconcile</c>.</summary>
public sealed record StoreReconcileResponse(
    [property: JsonPropertyName("changeset")] Changeset Changeset,
    [property: JsonPropertyName("productIds")] IReadOnlyList<string> ProductIds);

/// <summary>Response of <c>POST /v1/pioneer/battlepass/reconcile</c> and <c>/autoclaim</c>.</summary>
public sealed record BattlePassReconcileResponse(
    [property: JsonPropertyName("changeset")] Changeset Changeset,
    [property: JsonPropertyName("battlePassStates")] IReadOnlyList<UnknownBattlePassState> BattlePassStates);

/// <summary>Response of <c>POST /v1/pioneer/levels/auto-claim</c>.</summary>
public sealed record AutoClaimLevelsResponse(
    [property: JsonPropertyName("changeset")] Changeset Changeset,
    [property: JsonPropertyName("levels")] IReadOnlyList<LevelState> Levels);

/// <summary>Response of <c>GET /v1/pioneer/levels/list</c>.</summary>
public sealed record LevelsResponse([property: JsonPropertyName("levels")] IReadOnlyList<LevelState> Levels);

/// <summary>A player's progress in one level group.</summary>
public sealed record LevelState(
    [property: JsonPropertyName("groupId")] int GroupId,
    [property: JsonPropertyName("currentLevel")] int CurrentLevel,
    [property: JsonPropertyName("currentXp")] long CurrentXp,
    [property: JsonPropertyName("unclaimedRewards")] IReadOnlyList<UnknownReward> UnclaimedRewards);

/// <summary>Response of <c>GET /v1/pioneer/quests</c>.</summary>
public sealed record QuestsResponse([property: JsonPropertyName("quests")] IReadOnlyList<UnknownQuest> Quests);

/// <summary>Response of <c>GET /v1/pioneer/inventory</c>.</summary>
public sealed record InventoryResponse([property: JsonPropertyName("items")] IReadOnlyList<UnknownItem> Items);

/// <summary>Response of <c>GET /v1/pioneer/codex/entries/list</c>.</summary>
public sealed record CodexEntriesResponse([property: JsonPropertyName("entries")] IReadOnlyList<UnknownCodexEntry> Entries);

/// <summary>Response of <c>POST /v1/pioneer/projects/list</c>.</summary>
public sealed record ProjectsResponse([property: JsonPropertyName("projects")] IReadOnlyList<UnknownProject> Projects);

public sealed record UnknownItem;

public sealed record UnknownQuest;

public sealed record UnknownQuestObjective;

public sealed record UnknownScopedPlayerStat;

public sealed record UnknownOfferTransaction;

public sealed record UnknownProject;

public sealed record UnknownProjectGoal;

public sealed record UnknownChangesetNotification;

public sealed record UnknownBattlePassState;

public sealed record UnknownReward;

public sealed record UnknownCodexEntry;
