namespace Ares.Structs.Backend.Progression;

/// <summary>Request paths of the progression API: definitions, inventory, quests, levels and seasonal content.</summary>
public static class ProgressionPaths
{
    /// <summary><c>GET</c>; reply <see cref="GameDataResponse"/>.</summary>
    public const string GameData = "/v1/pioneer/game-data";

    /// <summary><c>POST</c>; reply format not decoded yet.</summary>
    public const string GameDataStore = "/v1/pioneer/game-data/store/v2";

    /// <summary><c>GET</c>; reply <see cref="InventoryResponse"/>.</summary>
    public const string Inventory = "/v1/pioneer/inventory";

    /// <summary><c>POST</c> <see cref="InventoryOperationsRequest"/>; reply <see cref="InventoryOperationsResponse"/>.</summary>
    public const string BatchOperation = "/v1/pioneer/v1/batch-operation";

    /// <summary><c>POST</c> <see cref="UpgradeInventoryItemRequest"/>; reply <see cref="UpgradeInventoryItemResponse"/>.</summary>
    public const string UpgradeInventoryItem = "/v1/pioneer/inventory/upgrade";

    /// <summary><c>GET</c>; reply <see cref="QuestsResponse"/>.</summary>
    public const string Quests = "/v1/pioneer/quests";

    /// <summary><c>POST</c>; reply <see cref="ReconcileResponse"/>.</summary>
    public const string ReconcileQuests = "/v1/pioneer/quests/reconcile";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string QuestRerollCost = "/v1/pioneer/quests/reroll-cost";

    /// <summary><c>GET</c>; reply <see cref="CodexEntriesResponse"/>.</summary>
    public const string CodexEntries = "/v1/pioneer/codex/entries/list";

    /// <summary><c>POST</c>; reply <see cref="ProjectsResponse"/>.</summary>
    public const string Projects = "/v1/pioneer/projects/list";

    /// <summary><c>GET</c>; reply <see cref="LevelsResponse"/>.</summary>
    public const string Levels = "/v1/pioneer/levels/list";

    /// <summary><c>POST</c>; reply <see cref="AutoClaimLevelsResponse"/>.</summary>
    public const string AutoClaimLevels = "/v1/pioneer/levels/auto-claim";

    /// <summary><c>POST</c>; reply <see cref="StoreReconcileResponse"/>.</summary>
    public const string ReconcileStore = "/v1/pioneer/store/reconcile";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string BattlePasses = "/v1/pioneer/battlepass/list-battlepasses";

    /// <summary><c>POST</c>; reply <see cref="BattlePassReconcileResponse"/>.</summary>
    public const string ReconcileBattlePass = "/v1/pioneer/battlepass/reconcile";

    /// <summary><c>POST</c>; reply <see cref="BattlePassReconcileResponse"/>.</summary>
    public const string AutoClaimBattlePass = "/v1/pioneer/battlepass/autoclaim";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string Expeditions = "/v1/pioneer/expeditions";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string ExpeditionsCatchUp = "/v1/pioneer/expeditions/catch-up";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string Compensations = "/v1/pioneer/compensations";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string TimedOfferTransactions = "/v1/pioneer/timed-offers/transactions";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string MasteryObjectives = "/v1/pioneer/mastery/objectives";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string League = "/v1/pioneer/league";

    /// <summary><c>POST</c>; reply format not decoded yet.</summary>
    public const string Ranks = "/v1/pioneer/rank/list";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string SeasonalRewards = "/v1/pioneer/seasonal-rewards";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string Bonuses = "/v1/pioneer/bonuses";

    /// <summary><c>GET</c>; reply format not decoded yet.</summary>
    public const string CommunityEvent = "/v1/pioneer/community-event";

    /// <summary><c>POST</c>; reply format not decoded yet.</summary>
    public const string PlayerStats = "/v1/pioneer/stats/player-v2";
}
