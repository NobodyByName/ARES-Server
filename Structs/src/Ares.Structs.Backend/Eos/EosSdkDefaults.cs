namespace Ares.Structs.Backend.Eos;

/// <summary>The public EOS SDK default configuration served to the game at startup.</summary>
public static class EosSdkDefaults
{
    /// <summary>The configuration document.</summary>
    public static EosSdkConfiguration Configuration { get; } = new(
        new Dictionary<string, IReadOnlyDictionary<string, EosConfigValue>>
        {
            ["BaseService"] = new Dictionary<string, EosConfigValue>
            {
                ["HttpRetryLimit"] = 4L,
                ["HttpRetryResponseCodes"] = new[] { 429L, 503L, 504L },
            },
            ["RateLimiter.PresenceClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 3L,
                ["TimeIntervalInSeconds"] = 20.0,
                ["Operation"] = new[] { "SendUpdate", "SetPresence" },
            },
            ["RateLimiter.LeaderboardsClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.InventoryClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 1000L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.MatchmakingClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["PartiesClient"] = new Dictionary<string, EosConfigValue>
            {
                ["bIsUserPartyInfoQueryEnabled"] = true,
                ["UserPartyInfoQueryAlreadyConnectedCooldownSeconds"] = 2.0,
            },
            ["RateLimiter.PartiesInternalClient.KeepAliveOperations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 30L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "KeepAlive" },
            },
            ["RateLimiter.SDKConfigClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "RequestUpdate" },
            },
            ["RateLimiter.WorldInventoryClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 1000L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.TitleStorageClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.DataStorageClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "GetAccessLinks", "QueryFile", "QueryFileList", "CopyFile", "DeleteFile", "ReadFile", "WriteFile", "DownloadFileRedirected", "UploadFileRedirected" },
            },
            ["LeaderboardsClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MaxUserScoresQueryUserIds"] = 100L,
                ["MaxUserScoresQueryStats"] = 25L,
            },
            ["RateLimiter.CustomInvitesClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 50L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "SendCustomInvite" },
            },
            ["RateLimiter.PartiesInternalClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 60L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["HTTP"] = new Dictionary<string, EosConfigValue>
            {
                ["HttpReceiveTimeout"] = 30L,
                ["bEnableHttp"] = true,
                ["HttpTimeout"] = 30L,
                ["HttpConnectionTimeout"] = 60L,
                ["HttpSendTimeout"] = 30L,
                ["MaxFlushTimeSeconds"] = 2.0,
            },
            ["RateLimiter.FriendClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "QueryFriends", "SendFriendInvite", "DeleteFriend" },
            },
            ["RateLimiter.FTextToSpeechClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.RTCAdminClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 50L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.UserInfoClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.PartiesInternalClient.ThrottledOperations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 30L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "CreateParty", "JoinPartyByPartyId", "JoinPartyByGroup", "UpdateParty", "SendInvite", "DeclineInvite", "DeclineGroupInvite", "DeclineAllInvites", "SendJoinRequest", "DeclineJoinRequest", "LeaveParty", "DisconnectFromParty", "JoinRTCRoom", "LeaveRTCRoom", "RefreshRTCToken", "UpdateMemberState" },
            },
            ["RateLimiter.AuthClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.PartiesInternalClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 100L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "QueryUserPartyInfo", "GetParty" },
            },
            ["WebSockets.LibWebSockets"] = new Dictionary<string, EosConfigValue>
            {
                ["ThreadStackSize"] = 131072L,
                ["ThreadTargetFrameTimeInSeconds"] = 0.0333,
                ["ThreadMinimumSleepTimeInSeconds"] = 0.0,
            },
            ["StatsClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MaxQueryStatsStatNamesStrLength"] = 1900L,
            },
            ["EcomClient.EcomSalesMappings"] = new Dictionary<string, EosConfigValue>
            {
                ["defaultStore"] = "EGA",
                ["com.epicgames.portal"] = "EGA",
                ["com.epicgames.store"] = "EGA",
            },
            ["RateLimiter.MetricsClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "RegisterPlayerBackendSession" },
            },
            ["RateLimiter.DataStorageClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["WorldInventoryClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MaxNumberOfParallelUpdateRequestsInFlight"] = 3L,
                ["SessionLockTTLSeconds"] = 60L,
                ["MaxNumberOfParallelQueryRequestsInFlight"] = 10L,
                ["MaxNumberOfLockEntriesPerRequest"] = 10L,
                ["MaxNumberOfWriteEntriesPerRequest"] = 10L,
                ["SessionLockUpdateIntervalRandomAddSeconds"] = 20.0,
                ["MaxNumberOfParallelLockRequestsInFlight"] = 3L,
                ["SessionLockUpdateIntervalSeconds"] = 30.0,
                ["MaxNumberOfSubTasksPerParallelUpdateTask"] = 20L,
                ["MaxNumberOfSubTasksPerParallelLockTask"] = 20L,
                ["QueryInventoryPageLimit"] = 0L,
                ["MaxBinaryDataSizeBytes"] = 10485760L,
                ["HttpRetryResponseCodes"] = new[] { 429L, 500L, 502L, 503L, 504L },
                ["LockType"] = "Session",
            },
            ["SanitizerClient"] = new Dictionary<string, EosConfigValue>
            {
                ["ReplaceChar"] = "*",
                ["RequestLimit"] = 10L,
            },
            ["RateLimiter.ModsClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "InstallMod", "UninstallMod", "UpdateMod", "EnumerateMods" },
            },
            ["RateLimiter.ReportsClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 100L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "SendPlayerBehaviorReport" },
            },
            ["RateLimiter.RTCAdminClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 50L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "QueryJoinRoomToken", "Kick", "SetParticipantHardMute" },
            },
            ["RateLimiter.PublicKeyClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.FriendClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.AchievementsClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 100L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["LogFiles"] = new Dictionary<string, EosConfigValue>
            {
                ["PurgeLogsDays"] = 5L,
                ["MaxLogFilesOnDisk"] = 5L,
                ["LogTimes"] = "SinceStart",
            },
            ["RateLimiter.PartiesClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 60L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.AntiCheatClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 120L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["SessionsClient"] = new Dictionary<string, EosConfigValue>
            {
                ["HeartbeatIntervalSecs"] = 30L,
            },
            ["RateLimiter.WorldInventoryClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 1000L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "Open", "Close", "Write", "WriteTransaction", "QueryVersionMetadata" },
            },
            ["InventoryClient.WorldInventory"] = new Dictionary<string, EosConfigValue>
            {
                ["MaxNumberOfParallelUpdateRequestsInFlight"] = 3L,
                ["SessionLockTTLSeconds"] = 60L,
                ["MaxNumberOfParallelQueryRequestsInFlight"] = 10L,
                ["MaxNumberOfLockEntriesPerRequest"] = 10L,
                ["MaxNumberOfWriteEntriesPerRequest"] = 10L,
                ["SessionLockUpdateIntervalRandomAddSeconds"] = 20.0,
                ["MaxNumberOfParallelLockRequestsInFlight"] = 3L,
                ["SessionLockUpdateIntervalSeconds"] = 30.0,
                ["MaxNumberOfSubTasksPerParallelUpdateTask"] = 20L,
                ["MaxNumberOfSubTasksPerParallelLockTask"] = 20L,
                ["QueryInventoryPageLimit"] = 0L,
                ["MaxBinaryDataSizeBytes"] = 10485760L,
                ["LockType"] = "Session",
            },
            ["RateLimiter.UserInfoClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "QueryLocalUserInfo", "QueryUserInfo", "QueryUserInfoByDisplayName", "QueryUserInfoByExternalAccount" },
            },
            ["RateLimiter.EComClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["LobbyClient"] = new Dictionary<string, EosConfigValue>
            {
                ["InitialResendDelayMs"] = 100L,
                ["MaxConnectionRetries"] = 3L,
                ["LobbySocketURL"] = "wss://api.epicgames.dev/lobby/v1/`deploymentId/lobbies/connect",
                ["NumConsecutiveFailuresAllowed"] = 5L,
                ["MaxResendDelayMs"] = 2000L,
                ["WebSocketConnectTaskMaxNetworkWaitSeconds"] = 15.0,
                ["RecoveryWaitTimeSecs"] = 2L,
                ["InitialRetryDelaySeconds"] = 5L,
                ["bDisableRTCVoice"] = false,
                ["MaxSendRetries"] = 3L,
                ["SentMessageTimeout"] = 5L,
                ["HeartbeatIntervalSecs"] = 30L,
                ["MaxRetryIntervalSeconds"] = 15L,
            },
            ["RateLimiter.SanctionsClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "QueryActivePlayerSanctions", "CreatePlayerSanctionAppeal" },
            },
            ["UIClient.SocialURLQueryParamNames"] = new Dictionary<string, EosConfigValue>
            {
                ["OSName"] = "os_name",
                ["ProductId"] = "product_id",
                ["SDKCLNumber"] = "sdk_cl_number",
                ["DeploymentId"] = "deployment_id",
                ["ProductVersion"] = "product_version",
                ["IntegratedPlatformName"] = "integrated_platform_name",
                ["SDKVersion"] = "sdk_version",
                ["OSVersion"] = "os_version",
                ["UserId"] = "user_id",
                ["ExchangeCode"] = "exchange_code",
            },
            ["RateLimiter.LobbyClient.ThrottledOperations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 30L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "CreateLobby", "DestroyLobby", "JoinLobby", "LeaveLobby", "HeartbeatLobby", "PromoteMember", "KickLobbyMember", "SendLobbyInvite", "RejectLobbyInvite", "QueryInvites", "FindLobby", "RefreshRTCToken", "HardMuteMember" },
            },
            ["RateLimiter.SessionsClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.KWSClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 20L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.PresenceClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 60L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.KWSClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 20L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "CreateUser", "UpdateParentEmail", "QueryAgeGate", "QueryPermissions", "RequestPermissions" },
            },
            ["RateLimiter.InventoryClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 1000L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "Open", "Close", "Write", "GetTransaction", "BatchGetTransactions" },
            },
            ["RateLimiter.LeaderboardsClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "QueryLeaderboardDefinitions", "QueryLeaderboardRanks", "QueryLeaderboardUserScores" },
            },
            ["RateLimiter.SanctionsClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["Messaging.EpicConnect"] = new Dictionary<string, EosConfigValue>
            {
                ["DisconnectInBackgroundPlatforms"] = new[] { "AND", "IOS" },
                ["EpicGlobalSubscriptionCoordinates"] = new[] { "ec-coord-temporary-subscription=parties-internal" },
                ["FailedConnectionDelayMultiplier"] = 2.5,
                ["RetryBackoffExponentialExponentBias"] = 0.0,
                ["ServerHeartbeatIntervalMilliseconds"] = 0L,
                ["RetryBackoffMaxSeconds"] = 180L,
                ["RetryBackoffJitterCoefficientMin"] = 0.5,
                ["Urlv2"] = "wss://connect.epicgames.dev/v2",
                ["ClientHeartbeatIntervalMilliseconds"] = 30000L,
                ["RetryBackoffExponentialBase"] = 2.0,
                ["RetryBackoffJitterCoefficientMax"] = 1.0,
                ["FailedConnectionDelayMaxSeconds"] = 180L,
                ["FailedConnectionDelayIntervalSeconds"] = 5L,
                ["UseV2Connection"] = false,
                ["Url"] = "wss://connect.epicgames.dev",
            },
            ["MetricsClient"] = new Dictionary<string, EosConfigValue>
            {
                ["HttpRetryLimit"] = 2L,
            },
            ["RateLimiter.TitleStorageClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "QueryFile", "QueryFileList", "ReadFile" },
            },
            ["RateLimiter.PartiesClient.ThrottledOperations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 10L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "ConnectToCachedParty", "DisconnectFromCachedParty", "YieldLeadership" },
            },
            ["RateLimiter.AchievementsClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 100L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "QueryDefinitions", "QueryPlayerAchievements", "UnlockAchievements" },
            },
            ["Messaging.Stomp"] = new Dictionary<string, EosConfigValue>
            {
                ["ClientHeartbeatIntervalMs"] = 30000L,
                ["RequestedServerHeartbeatIntervalMs"] = 0L,
                ["Url"] = "wss://api.epicgames.dev/notifications/v1/`deploymentid`/connect",
                ["BlocklistMessageTypeFilters"] = new[] { "lobbyinvite", "matchmakinginvite" },
            },
            ["TitleStorageClient"] = new Dictionary<string, EosConfigValue>
            {
                ["AccessLinkDurationSeconds"] = 300L,
                ["UnusedCachedFileDaysToLive"] = 7L,
                ["ClearInvalidFileCacheFrequencyDays"] = 2L,
                ["MaxSimultaneousReads"] = 10L,
            },
            ["RateLimiter.PartiesInternalClient.PromoteOperations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 10L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "Promote" },
            },
            ["RateLimiter.ReceiptValidatorClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["Overlay"] = new Dictionary<string, EosConfigValue>
            {
                ["ValidDomains"] = new[] { ".epicgames.com", ".epicgames.dev" },
            },
            ["ConnectClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MaxProductUserIdMappingsQueryUserIds"] = 128L,
                ["MinProductUserIdMappingsUpdateTimeInSeconds"] = 900.0,
            },
            ["RateLimiter.LobbyClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 100L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "GetByLobbyId", "UpdateLobby" },
            },
            ["RateLimiter.AntiCheatClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 120L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "QueryServiceStatus", "SendClientMessage" },
            },
            ["EcomClient"] = new Dictionary<string, EosConfigValue>
            {
                ["PurchaseCookieName"] = "EPIC_BEARER_TOKEN",
                ["PurchaseEpicIdMobileLandscapeUrl"] = "https://www.epicgames.com/ecom/payment/v2/purchase",
                ["CTCCountries"] = "AT,AUT,BE,BEL,BG,BGR,CY,CYP,CZ,CZE,DE,DEU,DK,DNK,EE,EST,ES,ESP,FI,FIN,FR,FRA,GR,GRC,EL,HR,HRV,HU,HUN,IE,IRL,IT,ITA,LT,LTU,LU,LUX,LV,LVA,MT,MLT,NL,NLD,PL,POL,PT,PRT,RO,ROU,SE,SWE,SI,SVN,SK,SVK",
                ["PurchaseEpicIdMobilePortraitUrl"] = "https://www.epicgames.com/ecom/payment/v2/purchase",
                ["PurchaseEpicIdUrl"] = "https://www.epicgames.com/ecom/payment/v1/purchase",
                ["PurchaseUrl"] = "https://launcher-website-prod07.ol.epicgames.com/purchase",
            },
            ["RateLimiter.SessionsClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "UpdateSession", "JoinSession", "StartSession", "EndSession", "RegisterPlayers", "SendInvite", "RejectInvite", "QueryInvites", "FindSession", "DestroySession" },
            },
            ["RateLimiter.StatsClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "IngestStat", "QueryStats" },
            },
            ["RateLimiter.WeblinksClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.ReceiptValidatorClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "VerifyPurchase" },
            },
            ["DataStorageClient"] = new Dictionary<string, EosConfigValue>
            {
                ["AccessLinkDurationSeconds"] = 300L,
                ["MaxSimultaneousReads"] = 10L,
                ["MaxSimultaneousWrites"] = 10L,
                ["HttpRetryVerbs"] = new[] { "GET", "HEAD", "PUT", "POST" },
            },
            ["RateLimiter.AuthClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "VerifyAuth", "DeletePersistentAuth", "GenerateClientAuth", "LinkAccount", "QueryIdToken", "VerifyIdToken" },
            },
            ["InventoryClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MaxNumberOfParallelUpdateRequestsInFlight"] = 3L,
                ["SessionLockTTLSeconds"] = 60L,
                ["MaxNumberOfLockEntriesPerRequest"] = 10L,
                ["MaxNumberOfWriteEntriesPerRequest"] = 10L,
                ["SessionLockUpdateIntervalRandomAddSeconds"] = 20.0,
                ["MaxNumberOfParallelLockRequestsInFlight"] = 3L,
                ["SessionLockUpdateIntervalSeconds"] = 30.0,
                ["MaxNumberOfSubTasksPerParallelLockTask"] = 20L,
                ["HttpRetryResponseCodes"] = new[] { 429L, 500L, 502L, 503L, 504L },
                ["LockType"] = "Session",
                ["BaseUrl"] = "https://api.epicgames.dev/inventory",
                ["MaxNumberOfSubTasksPerParallelUpdateTask"] = 20L,
                ["QueryInventoryPageLimit"] = 0L,
                ["HttpRetryVerbs"] = new[] { "GET", "HEAD", "POST" },
            },
            ["P2PClient"] = new Dictionary<string, EosConfigValue>
            {
                ["IceServers"] = new[] { "stun:stun.l.google.com:19302", "stun:turn.rtcp.on.epicgames.com:3478", "turn:turn.rtcp.on.epicgames.com:3478" },
                ["P2PMinPort"] = 7777L,
                ["P2PMaxPort"] = 7876L,
            },
            ["RateLimiter.LobbyClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 30L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["SDKAnalytics"] = new Dictionary<string, EosConfigValue>
            {
                ["BaseUrl"] = "https://api.epicgames.dev/telemetry/data/",
                ["DevPhase"] = 2L,
                ["AppEnvironment"] = "Production",
                ["UploadType"] = "sdkevents",
            },
            ["RateLimiter.ConnectClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.PartiesInternalClient.KickMemberOperations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 75L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "KickMember" },
            },
            ["RateLimiter.MetricsClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["GameClient"] = new Dictionary<string, EosConfigValue>
            {
                ["UseGzipEncoding"] = false,
            },
            ["RateLimiter.PartiesClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 100L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "QueryUserPartyInfo" },
            },
            ["RateLimiter.StatsClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["UIClient.PartyFeature"] = new Dictionary<string, EosConfigValue>
            {
                ["RoomSettings.EnableRNNoise"] = true,
                ["RoomSettings.AudioVolumeMode"] = "PostApm",
                ["RoomSettings.AutoGainControlMode"] = "AGC2",
                ["RoomSettings.AGC2AdaptiveDigitalEnabled"] = true,
            },
            ["AntiCheat.GameplayData"] = new Dictionary<string, EosConfigValue>
            {
                ["Url"] = "wss://api.epicgames.dev/cerberus-edge/v1/",
            },
            ["AuthClient"] = new Dictionary<string, EosConfigValue>
            {
                ["AccountPortalURLLocaleSuffix"] = "lang=`code",
                ["PollInterval"] = 5L,
                ["RefreshTokenThreshold"] = 100.0,
                ["VPCRegisterURL"] = "https://epicgames.com/id/register/quick/minor/await?code=`challenge_id&display=embedded",
                ["AuthorizeContinuationEndpoint"] = "https://epicgames.com/id/login?continuation=`continuation&prompt=skip_merge%20skip_upgrade",
                ["AuthorizeCodeEndpoint"] = "https://epicgames.com/id/authorize?client_id=`client_id&response_type=code&scope=`scope&redirect_uri=`redirect_uri&display=popup&prompt=login",
                ["VerifyTokenInterval"] = 60.0,
                ["PollExpiresIn"] = 300L,
                ["IdTokenCacheMinExpirySeconds"] = 300L,
                ["AuthorizeEndpoint"] = "https://epicgames.com/id/authorize?exchange_code=`exchange_code&scope=`scope&prompt=skip_merge%20skip_upgrade",
                ["AccountPortalScheme"] = "eos.`client_id://epic/auth",
                ["bOfflineAccountToken"] = true,
                ["bFailLoginIfExternalAccountIdDoesNotMatchAuthToken"] = false,
                ["AuthorizeContinuationEmbeddedEndpoint"] = "https://epicgames.com/id/embedded/login?continuation=`continuation&prompt=skip_merge%20skip_upgrade",
            },
            ["RateLimiter.EComClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "QueryOwnership", "QueryOwnershipToken", "QueryEntitlement", "QueryOffer", "RedeemEntitlements", "Checkout", "QueryEntitlementToken" },
            },
            ["XMPP"] = new Dictionary<string, EosConfigValue>
            {
                ["bEnabled"] = true,
                ["bEnableWebsockets"] = true,
                ["ThreadStackSize"] = 131072L,
            },
            ["RateLimiter.AntiCheatServer.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 100000L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "QueryServiceStatus", "SendClientMessage" },
            },
            ["RateLimiter.ProgressionSnapshotClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.MatchmakingClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "SessionRegister", "SessionDeregister", "SessionAcknowledgeAssignment", "TicketJoin", "TicketClose", "SessionHeartbeat", "TicketHeartbeat", "SessionBackfillRegister", "SessionBackfillUpdate", "PlayerTicketPopQueue", "PlayerTicketPauseQueue", "PlayerTicketResumeQueue" },
            },
            ["Core.Log"] = new Dictionary<string, EosConfigValue>
            {
                ["LogEOS"] = "verbose",
                ["LogEOSMessaging"] = "verbose",
                ["LogEOSConnect"] = "verbose",
                ["LogEOSAuth"] = "verbose",
                ["LogHttpSerialization"] = "verbose",
                ["LogCore"] = "verbose",
                ["LogHttp"] = "warning",
                ["LogStomp"] = "verbose",
                ["LogXmpp"] = "verbose",
                ["LogEOSSessions"] = "verbose",
            },
            ["UIClient"] = new Dictionary<string, EosConfigValue>
            {
                ["FriendsURL"] = "https://api.epicgames.dev/overlay-override/v1/",
                ["DisableSocialOverlayRefreshOnForegroundPlatforms"] = Array.Empty<string>(),
                ["SocialSPAClientId"] = "cf27c69fe66441e8a8a4e8faf396ee4c",
                ["VPCURLLocaleSuffix"] = "&lang=`code",
                ["FriendsURLExchangeCodeSuffix"] = "?exchange_code=`exchange_code",
                ["VPCURL"] = "https://epicgames.com/id/overlay/quick/minor/verify?code=`challenge_id",
            },
            ["RateLimiter.AntiCheatServer"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 100000L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.InventoryClient.WorldInventory.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 1000L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "WorldOpen", "WorldClose", "WorldWrite", "WriteTransaction", "QueryVersionMetadata" },
            },
            ["Messaging.XMPP"] = new Dictionary<string, EosConfigValue>
            {
                ["ReconnectionDelayJitter"] = 1.5,
                ["PingTimeout"] = 30L,
                ["ReconnectionDelayBase"] = 4.0,
                ["ServerPort"] = 443L,
                ["bPrivateChatFriendsOnly"] = true,
                ["ReconnectionDelayMax"] = 300.0,
                ["Domain"] = "prod.ol.epicgames.com",
                ["ReconnectionDelayBackoffExponent"] = 2.0,
                ["ServerAddr"] = "wss://xmpp-service-prod.ol.epicgames.com",
                ["PingInterval"] = 60L,
            },
            ["RateLimiter.WeblinksClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "CreateWeblinkSession" },
            },
            ["RateLimiter.CustomInvitesClient.IncomingOperations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 30L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "HandleCustomInviteMessageReceived", "HandleSignalMessageReceived" },
            },
            ["RateLimiter.SDKConfigClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.ProgressionSnapshotClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "SubmitSnapshot", "DeleteSnapshot" },
            },
            ["PresenceClient"] = new Dictionary<string, EosConfigValue>
            {
                ["EpicConnectNotificationWaitTime"] = 5.0,
                ["PresenceQueryTimeoutSeconds"] = 60.0,
                ["bSetOfflineOnLogoutEnabled"] = true,
                ["PresenceAutoUpdateInSeconds"] = 600.0,
                ["bSetOfflineOnShutdownEnabled"] = true,
            },
            ["RateLimiter.CustomInvitesClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 50L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.ModsClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
            ["RateLimiter.PublicKeyClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "RegisterPublicKey" },
            },
            ["RateLimiter.FTextToSpeechClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "SynthesizeSpeech", "RequestSpeechSynthesis", "GetSpeechChunk" },
            },
            ["RateLimiter.ConnectClient.Operations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 300L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "LoginAccount", "CreateAccount", "LinkAccount", "UnlinkAccount", "CreateDeviceId", "DeleteDeviceId", "TransferDeviceIdAccount", "QueryExternalAccountMappings", "QueryProductUserIdMappings", "VerifyIdToken", "RequestExchangeToken" },
            },
            ["RateLimiter.AuthClient.SensitiveOperations"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 12L,
                ["TimeIntervalInSeconds"] = 60.0,
                ["Operation"] = new[] { "GenerateUserAuth" },
            },
            ["RateLimiter.ReportsClient"] = new Dictionary<string, EosConfigValue>
            {
                ["MessageCount"] = 100L,
                ["TimeIntervalInSeconds"] = 60.0,
            },
        },
        new Dictionary<string, EosService>
        {
            ["DataStorageService"] = new("https://api.epicgames.dev/datastorage"),
            ["AccountsEpicIdService"] = new("https://api.epicgames.dev"),
            ["AntiCheatService"] = new("https://api.epicgames.dev/anticheat"),
            ["PartiesInternalService"] = new("https://api.epicgames.dev/epic/party/internal"),
            ["LobbyService"] = new("https://api.epicgames.dev/lobby"),
            ["StatsAchievementsService"] = new("https://api.epicgames.dev/stats"),
            ["PriceEngineService"] = new("https://priceengine-public-service-ecomprod01.ol.epicgames.com/priceengine"),
            ["MatchmakingService"] = new("https://api.epicgames.dev/epic/matchmaking"),
            ["AccountsService"] = new("https://egp-idsoc-proxy-prod.ol.epicgames.com/account", "accounts.epicgames.com"),
            ["PartiesService"] = new("https://api.epicgames.dev/epic/party"),
            ["EcommerceEpicIdService"] = new("https://api.epicgames.dev/epic/ecom"),
            ["PaymentEpicIdService"] = new("https://api.epicgames.dev/epic/payment"),
            ["SanctionsService"] = new("https://api.epicgames.dev/sanctions"),
            ["FriendService"] = new("https://egp-idsoc-proxy-prod.ol.epicgames.com/friends"),
            ["TextChatTrustedServerFNService"] = new("https://api.epicgames.dev/epic/chat"),
            ["ReceiptValidatorService"] = new("https://api.epicgames.dev/receipt-validator"),
            ["RTCService"] = new("https://api.epicgames.dev/rtc"),
            ["WeblinksService"] = new("https://api.epicgames.dev/external-session-sync"),
            ["MetricsService"] = new("https://api.epicgames.dev/datarouter"),
            ["PostPartyService"] = new("https://global-postparty.game-social.epicgames.com"),
            ["EcommerceService"] = new("https://ecommerceintegration-public-service-ecomprod02.ol.epicgames.com/ecommerceintegration"),
            ["EULATrackingService"] = new("https://eulatracking-public-service-prod06.ol.epicgames.com/eulatracking"),
            ["FriendEpicIdService"] = new("https://api.epicgames.dev/epic/friends"),
            ["CatalogService"] = new("https://catalog-public-service-prod06.ol.epicgames.com/catalog"),
            ["KWSService"] = new("https://api.epicgames.dev/kws"),
            ["TextChatFNService"] = new("https://api.epicgames.dev/epic/chat"),
            ["GalleryService"] = new("https://gallery-service-prod.social.live.on.epicgames.com"),
            ["EOSAuthService"] = new("https://api.epicgames.dev"),
            ["SessionsService"] = new("https://api.epicgames.dev/matchmaking"),
            ["ModsService"] = new("https://api.epicgames.dev/mods"),
            ["ReportsService"] = new("https://api.epicgames.dev/player-reports"),
            ["ProgressionSnapshotService"] = new("https://api.epicgames.dev/snapshots"),
            ["CustomInvitesService"] = new("https://api.epicgames.dev/notifications"),
            ["PresenceService"] = new("https://api.epicgames.dev/epic/presence"),
            ["TitleStorageService"] = new("https://api.epicgames.dev/titlestorage"),
            ["StatsIngestService"] = new("https://api.epicgames.dev/ingestion/stats"),
            ["TextChatGatewayServerFNService"] = new("https://fngw-svc-ds-livefn.ol.epicgames.com/chat"),
            ["LeaderboardsService"] = new("https://api.epicgames.dev/leaderboards"),
            ["InventoryService"] = new("https://api.epicgames.dev/inventory"),
        },
        -1267564831);
}
