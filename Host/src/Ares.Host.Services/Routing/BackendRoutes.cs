using System.Text.Json;
using System.Text.Json.Nodes;
using Ares.Common.Logging;
using Ares.Host.Services.Accounts;
using Ares.Host.Services.Content;
using Ares.Host.Services.Identity;
using Ares.Host.Services.Inventory;
using Ares.Host.Services.Players;
using Ares.Host.Services.Status;
using Ares.Host.Services.Telemetry;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Accounts;
using Ares.Structs.Backend.Bootstrap;
using Ares.Structs.Backend.Eos;
using Ares.Structs.Backend.Identity;
using Ares.Structs.Backend.Matchmaking;
using Ares.Structs.Backend.Platform;
using Ares.Structs.Backend.Profiles;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Backend.Social;
using Ares.Structs.Backend.Telemetry;

namespace Ares.Host.Services.Routing;

/// <summary>A backend route handler.</summary>
public delegate Task<ServiceReply> RouteHandler(BackendRequest request, CancellationToken cancellationToken);

/// <summary>One backend route.</summary>
/// <param name="Handler">Produces the reply.</param>
/// <param name="RequiresIdentity">
/// The caller must present a valid identity token of an unbanned account; the signed-in player is
/// resolved only for these routes. Only the game's sign-in and ARES's own endpoints are open; the game
/// calls nothing else before it.
/// </param>
/// <param name="Delay">Wait before replying. Some bootstrap replies arriving too early made client startup fail intermittently.</param>
public sealed record BackendRoute(RouteHandler Handler, bool RequiresIdentity = true, TimeSpan Delay = default);

/// <summary>
/// Every route Host serves, matched exactly on method and path: the game's backend API and ARES's own
/// endpoints. Anything else is HTTP 501.
/// </summary>
public sealed class BackendRoutes
{
    /// <summary>Path of the liveness check, answered for any method.</summary>
    private const string HealthPath = "/health";

    private const string Get = "GET";
    private const string Post = "POST";
    private const string Patch = "PATCH";
    private const string AnyMethod = "*";

    private readonly Dictionary<string, BackendRoute> _routes = new(StringComparer.Ordinal);

    /// <summary>Registers all routes against the services.</summary>
    public BackendRoutes(HostSettings settings, IdentityService identity, PlayerRegistry players, SignInService signIn, JsonEventLog log, TimeProvider time)
    {
        // ARES endpoints.
        Open(Get, PublicStatus.Path, (_, _) => Task.FromResult(ServiceReply.Ok(PublicStatus.Current(settings, players))));
        Open(Get, ArchiveKeysEndpoint.Path, (_, _) => Task.FromResult(ServiceReply.Ok(settings.ArchiveKeys.ToDocument(settings.GameBuild))));
        Open(Post, SignInService.StartPath, (request, _) => Task.FromResult(signIn.Start(request.Read<SignInStartRequest>())));
        Open(Post, SignInService.FinishPath, (request, _) => Task.FromResult(signIn.Finish(request.Read<SignInFinishRequest>())));
        Open(AnyMethod, HealthPath, (_, _) => Task.FromResult(ServiceReply.Ok(new JsonObject { ["service"] = "ARES-Host" })));
        Open(AnyMethod, UiTelemetry.TransferBatchedPath, (request, _) =>
        {
            LogUiTelemetry(request, log, time);
            return Task.FromResult(ServiceReply.Ok(new RawReply(GrpcWeb.ContentType, GrpcWeb.EmptyOkResponse())));
        });

        // Identity and bootstrap.
        Open(Post, IdentityPaths.Token, (request, _) =>
        {
            (OAuthTokenResponse? signedIn, Account? account) = identity.SignIn(request);
            log.Write("game_sign_in", new JsonObject
            {
                ["name"] = account?.Name,
                ["grant"] = request.FormValue(OAuthTokenForm.GrantType),
                ["result"] = signedIn is null ? "rejected" : "signed_in",
            });
            return Task.FromResult(signedIn is null ? ServiceReply.Error(401, "invalid_launch_key") : ServiceReply.Ok(signedIn));
        });
        Player(Get, ProfilePaths.Profile, (_, player) => player.Profiles.Create());
        Player(Get, ProfilePaths.Raiders, (_, player) => IdentityService.Raiders(player.Account.UserId));
        Player(Post, IdentityPaths.PlatformProfiles, (request, player) =>
            IdentityService.PlatformProfiles(player.Profiles.Create(), request.Read<PlatformProfilesRequest>()).Response);
        Player(Get, IdentityPaths.ThirdPartyAccessToken, (_, player) => identity.ThirdPartyAccessToken(player.Account));
        Player(Post, BootstrapPaths.Manifest, (request, player) => identity.Manifest(request.Read<ManifestRequest>(), player.Inventory.DefinitionHash()));
        Add(Post, BootstrapPaths.LatestBuild, request => IdentityService.LatestBuild(request.Read<LatestBuildRequest>()));
        Add(Get, EosEndpoints.DefaultSdkConfigurationPath, EosSdkDefaults.Configuration);
        Add(Get, EosEndpoints.GameProductSdkConfigurationPath, EosSdkDefaults.Configuration);
        Add(Post, EosEndpoints.TokenPath, request => identity.EosToken(
            new EosTokenRequest(request.FormValue("grant_type"), request.FormValue("deployment_id"), request.FormValue("nonce")),
            request.Authorization).Response);

        // Accounts.
        Player(Post, AccountPaths.GetPersistentPlayerKeys, (_, player) => new PersistentPlayerKeysResponse(player.Settings.GetKeys()));
        Player(Post, AccountPaths.SetPersistentPlayerKeys, (request, player, _) =>
        {
            (IReadOnlyList<PersistentPlayerKey>? updated, string? error) = player.Settings.SetKeys(request.Read<SetPersistentPlayerKeysRequest>().PersistentPlayerKeys);
            return Task.FromResult(updated is null ? ServiceReply.Error(400, error!) : ServiceReply.Ok(new PersistentPlayerKeysResponse(updated)));
        });

        // Party and notifications.
        Player(Post, SocialPaths.CreateParty, (request, player, _) => player.Party.CreateAsync(request.Read<CreatePartyRequest>()));
        Player(Patch, SocialPaths.UpdatePartyAttributes, (request, player, _) => player.Party.UpdateAttributesAsync(request.Read<UpdatePartyAttributesRequest>()));
        Player(Post, SocialPaths.UpdatePartyMemberData, (request, player, _) => player.Party.UpdateMemberAsync(request.Read<UpdatePartyMemberDataRequest>()));
        Player(Post, SocialPaths.SetReadyForMatchmaking, (request, player, _) => player.Party.SetReadyAsync(request.Read<SetReadyForMatchmakingRequest>()));
        Player(Post, SocialPaths.Notifications, (request, player, token) => player.Party.PollAsync(request.Read<NotificationsRequest>(), token));

        // Matchmaking.
        Add(Post, MatchmakingPaths.Scenarios, _ => BackendContent.Scenarios);
        Player(Post, MatchmakingPaths.MatchStart, (request, player, token) => player.Matchmaking.StartAsync(request.Read<MatchStartRequest>(), token));
        Player(Post, MatchmakingPaths.MatchStatus, (request, player, token) => player.Matchmaking.StatusAsync(request.Read<TicketRequest>(), token));
        Player(Post, MatchmakingPaths.MatchCancel, (request, player, token) => player.Matchmaking.CancelAsync(request.Read<TicketRequest>(), token));
        Player(Post, MatchmakingPaths.SquadLayout, (request, player, token) => player.Matchmaking.SquadLayoutAsync(request.Read<TicketRequest>(), token));
        Player(Post, MatchmakingPaths.GameServerStatus, (request, player, token) => player.Matchmaking.GameServerAsync(request.Read<GameServerStatusRequest>(), token));
        Player(Post, MatchmakingPaths.PlayerActivity, async (_, player, token) => ServiceReply.Ok(await player.Matchmaking.ActivityAsync(token).ConfigureAwait(false)));

        // Progression: empty state for a fresh account.
        Player(Get, ProgressionPaths.GameData, (_, player) => player.Inventory.GameData());
        Add(Get, ProgressionPaths.Quests, new QuestsResponse([]));
        Player(Get, ProgressionPaths.Inventory, async (_, player, token) => ServiceReply.Ok(await player.Matchmaking.InventoryAsync(token).ConfigureAwait(false)));
        Player(Post, ProgressionPaths.BatchOperation, (request, player, token) =>
        {
            LogUndecodedOperations(request, log);
            return player.Matchmaking.CommitInventoryAsync(request.Read<InventoryOperationsRequest>(), time.GetUtcNow(), token);
        });
        Player(Post, ProgressionPaths.UpgradeInventoryItem, async (request, player, token) =>
        {
            UpgradeInventoryItemRequest upgrade = request.Read<UpgradeInventoryItemRequest>();
            ServiceReply reply = await player.Matchmaking.CommitInventoryAsync(InventoryStore.UpgradeOperations(upgrade), time.GetUtcNow(), token)
                .ConfigureAwait(false);
            return InventoryStore.UpgradeReply(reply, upgrade.InstanceId ?? "");
        });
        Add(Get, ProgressionPaths.CodexEntries, new CodexEntriesResponse([]));
        Add(Post, ProgressionPaths.Projects, new ProjectsResponse([]));
        Add(Get, ProgressionPaths.Levels, new LevelsResponse(BackendContent.Levels));
        Add(Post, ProgressionPaths.ReconcileQuests, new ReconcileResponse(Changeset.Empty), TimeSpan.FromSeconds(20));
        Add(Post, ProgressionPaths.ReconcileStore, new StoreReconcileResponse(Changeset.Empty, []), TimeSpan.FromSeconds(2));
        Add(Post, ProgressionPaths.ReconcileBattlePass, new BattlePassReconcileResponse(Changeset.Empty, []), TimeSpan.FromSeconds(2));
        Add(Post, ProgressionPaths.AutoClaimBattlePass, new BattlePassReconcileResponse(Changeset.Empty, []), TimeSpan.FromSeconds(2));
        Add(Post, ProgressionPaths.AutoClaimLevels, new AutoClaimLevelsResponse(Changeset.Empty, BackendContent.Levels), TimeSpan.FromSeconds(2));

        // Platform services.
        Add(Get, PlatformPaths.AnticheatConfig, BackendContent.AnticheatConfig);
        Add(Get, PlatformPaths.AnybrainEligibility, new AnybrainEligibilityResponse(Eligible: false, IsEligible: false));
        Add(Post, PlatformPaths.AnticheatRestrictions, new RestrictionsResponse([]));
        Add(Get, PlatformPaths.IsRestricted, new RestrictionsResponse([]));
        Add(Get, PlatformPaths.VideoEncryptionKeys, new VideoEncryptionKeysResponse(new UnknownVideoEncryptionKeys()));
        Add(Get, PlatformPaths.GameSettings, new EntriesResponse([]));
        Add(Post, PlatformPaths.Localizations, new EntriesResponse([]));

        // Accepted with an empty object; their reply formats are not decoded yet.
        foreach ((string method, string path) in s_emptyReplies)
        {
            Add(method, path, EmptyResponse.Instance);
        }
    }

    /// <summary>
    /// The route for <paramref name="method"/> and <paramref name="path"/>, or one registered for any
    /// method on that path, or null.
    /// </summary>
    public BackendRoute? Find(string method, string path)
    {
        return _routes.GetValueOrDefault(Key(method, path)) ?? _routes.GetValueOrDefault(Key(AnyMethod, path));
    }

    private static readonly (string Method, string Path)[] s_emptyReplies =
    [
        (Post, PlatformPaths.UnknownUpdate),
        (Post, PlatformPaths.UnknownNotify),
        (Post, PlatformPaths.Heartbeat),
        (Post, IdentityPaths.TenancyUserSync),
        (Post, ProgressionPaths.GameDataStore),
        (Get, PlatformPaths.Proxy),
        (Get, PlatformPaths.Quilkin),
        (Post, SocialPaths.SetRichPresence),
        (Post, SocialPaths.GetRichPresence),
        (Post, SocialPaths.PresenceAllowlist),
        (Post, SocialPaths.LeaveParty),
        (Get, ProgressionPaths.Expeditions),
        (Get, ProgressionPaths.ExpeditionsCatchUp),
        (Get, ProgressionPaths.QuestRerollCost),
        (Get, ProgressionPaths.Compensations),
        (Get, SocialPaths.BlockedPlayers),
        (Get, SocialPaths.Friends),
        (Get, SocialPaths.RecentlyPlayedWith),
        (Post, SocialPaths.InboxMessages),
        (Get, ProgressionPaths.BattlePasses),
        (Get, ProgressionPaths.TimedOfferTransactions),
        (Get, ProgressionPaths.MasteryObjectives),
        (Get, ProgressionPaths.League),
        (Post, ProgressionPaths.Ranks),
        (Get, ProgressionPaths.SeasonalRewards),
        (Get, ProgressionPaths.Bonuses),
        (Get, ProgressionPaths.CommunityEvent),
        (Post, PlatformPaths.Announcements),
        (Post, ProgressionPaths.PlayerStats),
    ];

    /// <summary>Logs each UI event of a telemetry batch.</summary>
    private static void LogUiTelemetry(BackendRequest request, JsonEventLog log, TimeProvider time)
    {
        foreach (JsonObject item in UiTelemetryLog.Project(request.Body))
        {
            log.Write(new JsonObject { ["time"] = time.GetUtcNow().ToUnixTimeMilliseconds() / 1000.0, ["client_ui_event"] = item });
        }
    }

    /// <summary>
    /// Logs, as received, each batch operation Host cannot decode. Batch operations hold only item and
    /// offer ids.
    /// </summary>
    private static void LogUndecodedOperations(BackendRequest request, JsonEventLog log)
    {
        if (request.Json is not { } body || !body.TryGetProperty("operations", out JsonElement operations) || operations.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement operation in operations.EnumerateArray())
        {
            InventoryOperation? decoded;
            try
            {
                decoded = operation.Deserialize<InventoryOperation>(BackendJson.Options);
            }
            catch (Exception error) when (error is JsonException or NotSupportedException)
            {
                decoded = null;
            }

            if (decoded is null)
            {
                log.Write(new JsonObject { ["undecoded_batch_operation"] = JsonNode.Parse(operation.GetRawText()) });
            }
        }
    }

    private static string Key(string method, string path)
    {
        return method + " " + path;
    }

    private void Register(string method, string path, BackendRoute route)
    {
        _routes.Add(Key(method, path), route);
    }

    private void Open(string method, string path, RouteHandler handler)
    {
        Register(method, path, new BackendRoute(handler, RequiresIdentity: false));
    }

    private void Add(string method, string path, object reply, TimeSpan delay = default)
    {
        Register(method, path, new BackendRoute((_, _) => Task.FromResult(ServiceReply.Ok(reply)), Delay: delay));
    }

    private void Add(string method, string path, Func<BackendRequest, object> reply)
    {
        Register(method, path, new BackendRoute((request, _) => Task.FromResult(ServiceReply.Ok(reply(request)))));
    }

    private void Player(string method, string path, Func<BackendRequest, PlayerServices, object> reply)
    {
        Player(method, path, (request, player, _) => Task.FromResult(ServiceReply.Ok(reply(request, player))));
    }

    private void Player(string method, string path, Func<BackendRequest, PlayerServices, CancellationToken, Task<ServiceReply>> handler)
    {
        Register(method, path, new BackendRoute((request, token) => handler(request, request.Player!, token)));
    }
}
