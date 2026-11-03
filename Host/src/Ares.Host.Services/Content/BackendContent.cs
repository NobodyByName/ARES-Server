using Ares.Structs.Backend.Accounts;
using Ares.Structs.Backend.Matchmaking;
using Ares.Structs.Backend.Platform;
using Ares.Structs.Backend.Progression;

namespace Ares.Host.Services.Content;

/// <summary>
/// Synthetic backend content Host serves. Each item is the minimum the client needs to reach
/// the main menu and matchmaking; none of it is official data.
/// </summary>
public static class BackendContent
{
    /// <summary>Selectable scenarios: Dam Battlegrounds only.</summary>
    public static ScenariosResponse Scenarios { get; } = new(
    [
        new MatchmakingScenario(
            ScenarioNames.TheDam,
            new ScenarioParameters(ScenarioNames.RaidGameMode, ScenarioNames.TheDam, 0, 0, 3, new UnknownFeatureFlags()),
            new ScenarioUiSettings(Visible: true, Disabled: false, Active: true, [], []),
            new ScenarioGameSettings(0, new MapConditions(IsOptIn: false, []))),
    ]);

    /// <summary>
    /// Synthetic level groups. Their tracking items are the ones the client looks up during user-data
    /// sync; without a group per item, level auto-claim times out.
    /// </summary>
    private static IReadOnlyList<LevelGroup> LevelGroups { get; } =
    [
        Group(1000004, "Local progression 1", SyncedLevelTrackingItems.UnknownTrackingItemA),
        Group(1000005, "Local progression 2", SyncedLevelTrackingItems.UnknownTrackingItemB),
    ];

    /// <summary>Level 1 with no XP in every group.</summary>
    public static IReadOnlyList<LevelState> Levels { get; } =
        [.. LevelGroups.Select(group => new LevelState(group.GroupId, 1, 0, []))];

    /// <summary>
    /// Minimal definition tables. Startup needs at least one quest definition (else ARSQ0003) and one
    /// offer definition (else ARSO0003).
    /// </summary>
    public static GameDataResponse GameData { get; } = new(
        new QuestDescriptionTable(
        [
            new QuestDescription(1000001, "Local bootstrap", "Synthetic local quest definition", [], [], [], []),
        ]),
        new OfferDescriptionTable(
        [
            new OfferDescription(1000002, "local", "Local bootstrap definition", "Synthetic hidden offline offer", IsVisible: false, [],
                new UnknownItemPackage(), new UnknownItemPackage(), new UnknownItemPackage()),
        ]),
        new ItemDescriptionTable(
        [
            new ItemDescription(1000003, "local", 1, Stackable: false, Unique: false, [], []),
            .. LevelGroups.Select(group => new ItemDescription(group.TrackingItemId, "local_xp", int.MaxValue, Stackable: true, Unique: false, [], [group.GroupId])),
        ]),
        new LevelGroupTable(LevelGroups));

    /// <summary>Empty anti-cheat configuration: no data and no signature.</summary>
    public static AnticheatConfigResponse AnticheatConfig { get; } = new("", "");

    /// <summary>Persistent player keys preset to "true" so the client skips first-start flows.</summary>
    public static IReadOnlyList<string> CompletedFirstStartKeys { get; } =
    [
        PersistentPlayerKeyNames.AccountCreatedSeen,
        PersistentPlayerKeyNames.FirstStartupDone,
        PersistentPlayerKeyNames.CharacterCreatorSeen,
        PersistentPlayerKeyNames.SettingsSeen,
        PersistentPlayerKeyNames.Video1ASeen,
        PersistentPlayerKeyNames.Video1BSeen,
        PersistentPlayerKeyNames.OnboardingLevelFinished,
        PersistentPlayerKeyNames.FirstStartupExtracted,
        PersistentPlayerKeyNames.HasSeenUpdateEmailModal,
        PersistentPlayerKeyNames.Graduated,
    ];

    /// <summary>Synthetic fallback manifest id.</summary>
    public static class Manifest
    {
        public const long DefaultId = 1;
    }

    /// <summary>Synthetic EOS organisation and product identifiers.</summary>
    public static class Eos
    {
        public const string ProductUserId = "00000000000040008000000000000001";
        public const string OrganizationId = "o-00000000000040008000000000000001";
        public const string ProductId = "00000000000040008000000000000003";
        public const string SandboxId = "00000000000040008000000000000004";
        public const string DefaultClientId = "ares-host-client";
        public const string DefaultDeploymentId = "ares-host";
        public const string TokenKeyId = "ares-host-eos";
        public static IReadOnlyList<string> Features { get; } = ["Connect", "EpicConnect", "RTC", "Lobby", "Sessions", "Presence"];
    }

    private static LevelGroup Group(int id, string name, int trackingItem)
    {
        return new LevelGroup(id, name, trackingItem, trackingItem, new UnknownTrackingPlayerStat(),
            [new LevelDefinition(1, id, 0, 0, new UnknownReward())]);
    }
}
