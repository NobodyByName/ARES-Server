using Ares.Host.Services.Accounts;
using Ares.Host.Services.Content;
using Ares.Host.Services.Identity;
using Ares.Host.Services.Matchmaking;
using Ares.Host.Services.Social;
using Ares.Structs.Backend;
using Ares.Structs.Backend.Accounts;
using Ares.Structs.Backend.Bootstrap;
using Ares.Structs.Backend.Eos;
using Ares.Structs.Backend.Identity;
using Ares.Structs.Backend.Matchmaking;
using Ares.Structs.Backend.Platform;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Backend.Social;

namespace Ares.Host.Services.Routing;

/// <summary>A backend route handler.</summary>
public delegate Task<ServiceReply> RouteHandler(BackendRequest request, CancellationToken cancellationToken);

/// <summary>One backend route.</summary>
/// <param name="Handler">Produces the reply.</param>
/// <param name="RequiresIdentity">The caller must present the local player's identity token (when Host has an identity key).</param>
/// <param name="Delay">Wait before replying. Some bootstrap replies arriving too early made client startup fail intermittently.</param>
public sealed record BackendRoute(RouteHandler Handler, bool RequiresIdentity = false, TimeSpan Delay = default);

/// <summary>The backend routes the client uses, keyed by <c>"METHOD /path"</c>. Anything else is HTTP 501.</summary>
public sealed class BackendRoutes
{
    private readonly Dictionary<string, BackendRoute> _routes = new(StringComparer.Ordinal);

    /// <summary>Registers all routes against the services.</summary>
    public BackendRoutes(IdentityService identity, ProfileFactory profiles, AccountStore accounts, PartyService party, MatchmakingService matchmaking)
    {
        // Identity and bootstrap.
        Add("POST /oauth2/token", identity.SignIn());
        Add("GET /v1/shared/profile", _ => profiles.Create());
        Add("GET /v1/pioneer/raiders", IdentityService.Raiders());
        Add("POST /v1/shared/profile/by-thirdparty-userid", request => identity.PlatformProfiles(request.Read<PlatformProfilesRequest>()).Response);
        Add("GET /v1/shared/auth/third-party-access-token", _ => identity.ThirdPartyAccessToken());
        Add("POST /v1/shared/manifest", request => identity.Manifest(request.Read<ManifestRequest>()));
        Add("POST /v1/shared/build/v2/latest", request => IdentityService.LatestBuild(request.Read<LatestBuildRequest>()));
        Add("GET /sdk/v1/default", EosSdkDefaults.Configuration);
        Add("GET /sdk/v1/product/" + EosEndpoints.GameProductId, EosSdkDefaults.Configuration);
        Add("POST /auth/v1/oauth/token", request => identity.EosToken(
            new EosTokenRequest(request.FormValue("grant_type"), request.FormValue("deployment_id"), request.FormValue("nonce")),
            request.Authorization).Response);

        // Accounts.
        Add("POST /v1/shared/profile/display-name/validate", request =>
            new DisplayNameValidationResponse(AccountStore.IsValidDisplayName(request.Read<DisplayNameRequest>().DisplayName), []));
        _routes["PUT /v1/shared/profile/display-name"] = new BackendRoute((request, _) => Task.FromResult(
            accounts.SetDisplayName(request.Read<DisplayNameRequest>().DisplayName)
                ? ServiceReply.Ok(profiles.Create())
                : ServiceReply.Error(400, "invalid_local_display_name")));
        Add("POST /v1/shared/get-persistent-player-keys", _ => new PersistentPlayerKeysResponse(accounts.GetKeys()));
        _routes["POST /v1/shared/set-persistent-player-keys"] = new BackendRoute((request, _) =>
        {
            (IReadOnlyList<PersistentPlayerKey>? updated, string? error) = accounts.SetKeys(request.Read<SetPersistentPlayerKeysRequest>().PersistentPlayerKeys);
            return Task.FromResult(updated is null ? ServiceReply.Error(400, error!) : ServiceReply.Ok(new PersistentPlayerKeysResponse(updated)));
        });

        // Party and notifications.
        _routes["POST /v1/shared/social/party/v2/create"] = new BackendRoute((request, _) => party.CreateAsync(request.Read<CreatePartyRequest>()));
        _routes["PATCH /v1/shared/social/party/v2/update-party-attributes"] = new BackendRoute((request, _) => party.UpdateAttributesAsync(request.Read<UpdatePartyAttributesRequest>()));
        _routes["POST /v1/shared/social/party/v2/update-party-member-data"] = new BackendRoute((request, _) => party.UpdateMemberAsync(request.Read<UpdatePartyMemberDataRequest>()));
        _routes["POST /v1/shared/social/party/set-ready-for-matchmaking"] = new BackendRoute((request, _) => party.SetReadyAsync(request.Read<SetReadyForMatchmakingRequest>()));
        _routes["POST /v1/shared/notifications"] = new BackendRoute((request, token) => party.PollAsync(request.Read<NotificationsRequest>(), token));

        // Matchmaking.
        _routes["POST /v1/shared/scenarios"] = new BackendRoute((_, _) => Task.FromResult(ServiceReply.Ok(BackendContent.Scenarios)));
        _routes["POST /v1/shared/match/start"] = new BackendRoute((request, token) => matchmaking.StartAsync(request.Read<MatchStartRequest>(), token), RequiresIdentity: true);
        _routes["POST /v1/shared/match/status"] = new BackendRoute((request, token) => matchmaking.StatusAsync(request.Read<TicketRequest>(), token), RequiresIdentity: true);
        _routes["POST /v1/shared/match/v2/cancel"] = new BackendRoute((request, token) => matchmaking.CancelAsync(request.Read<TicketRequest>(), token), RequiresIdentity: true);
        _routes["POST /v1/shared/squad/layout"] = new BackendRoute((request, token) => matchmaking.SquadLayoutAsync(request.Read<TicketRequest>(), token), RequiresIdentity: true);
        _routes["POST /v1/shared/gameserver/status"] = new BackendRoute((request, token) => matchmaking.GameServerAsync(request.Read<GameServerStatusRequest>(), token), RequiresIdentity: true);
        _routes["POST /v1/shared/player/activity"] = new BackendRoute(async (_, token) => ServiceReply.Ok(await matchmaking.ActivityAsync(token).ConfigureAwait(false)), RequiresIdentity: true);

        // Progression: empty state for a fresh account.
        Add("GET /v1/pioneer/game-data", BackendContent.GameData);
        Add("GET /v1/pioneer/quests", new QuestsResponse([]));
        Add("GET /v1/pioneer/inventory", new InventoryResponse([]));
        Add("GET /v1/pioneer/codex/entries/list", new CodexEntriesResponse([]));
        Add("POST /v1/pioneer/projects/list", new ProjectsResponse([]));
        Add("GET /v1/pioneer/levels/list", new LevelsResponse(BackendContent.Levels));
        Add("POST /v1/pioneer/quests/reconcile", new ReconcileResponse(Changeset.Empty), TimeSpan.FromSeconds(20));
        Add("POST /v1/pioneer/store/reconcile", new StoreReconcileResponse(Changeset.Empty, []), TimeSpan.FromSeconds(2));
        Add("POST /v1/pioneer/battlepass/reconcile", new BattlePassReconcileResponse(Changeset.Empty, []), TimeSpan.FromSeconds(2));
        Add("POST /v1/pioneer/battlepass/autoclaim", new BattlePassReconcileResponse(Changeset.Empty, []), TimeSpan.FromSeconds(2));
        Add("POST /v1/pioneer/levels/auto-claim", new AutoClaimLevelsResponse(Changeset.Empty, BackendContent.Levels), TimeSpan.FromSeconds(2));

        // Platform services.
        Add("GET /v1/shared/anticheat/config", BackendContent.AnticheatConfig);
        Add("GET /v1/shared/anticheat/config/is-anybrain-eligible", new AnybrainEligibilityResponse(Eligible: false, IsEligible: false));
        Add("POST /v1/shared/anticheat/restrictions", new RestrictionsResponse([]));
        Add("GET /v1/shared/is-restricted", new RestrictionsResponse([]));
        Add("GET /v1/shared/video-encryption-keys", new VideoEncryptionKeysResponse(new UnknownVideoEncryptionKeys()));
        Add("GET /v1/shared/game-settings", new EntriesResponse([]));
        Add("POST /v1/shared/localizations", new EntriesResponse([]));

        // Accepted with an empty object; their reply formats are not decoded yet.
        foreach (string route in s_emptyReplies)
        {
            Add(route, EmptyResponse.Instance);
        }
    }

    /// <summary>The route for <paramref name="method"/> and <paramref name="path"/>, or null.</summary>
    public BackendRoute? Find(string method, string path)
    {
        return _routes.GetValueOrDefault(method + " " + path);
    }

    private static readonly string[] s_emptyReplies =
    [
        "POST /api/v3/update/",
        "POST /api/v1/notify/",
        "POST /v1/shared/heartbeat",
        "POST /v1/pioneer/tenancy-user/sync",
        "POST /v1/pioneer/game-data/store/v2",
        "GET /v1/shared/proxy",
        "GET /v1/shared/quilkin",
        "POST /v1/shared/social/presence/set-rich-presence",
        "POST /v1/shared/social/presence/get-rich-presence",
        "POST /v1/shared/social/presence/allowlist",
        "POST /v1/shared/social/party/leave",
        "GET /v1/pioneer/expeditions",
        "GET /v1/pioneer/expeditions/catch-up",
        "GET /v1/pioneer/quests/reroll-cost",
        "GET /v1/pioneer/compensations",
        "GET /v1/shared/social/block-players/blocked-players",
        "GET /v1/shared/social/friends/get-friends",
        "GET /v1/shared/social/recently-played-with",
        "POST /v1/shared/inbox/get-messages",
        "GET /v1/pioneer/battlepass/list-battlepasses",
        "GET /v1/pioneer/timed-offers/transactions",
        "GET /v1/pioneer/mastery/objectives",
        "GET /v1/pioneer/league",
        "POST /v1/pioneer/rank/list",
        "GET /v1/pioneer/seasonal-rewards",
        "GET /v1/pioneer/bonuses",
        "GET /v1/pioneer/community-event",
        "POST /v1/shared/announcements",
        "POST /v1/pioneer/stats/player-v2",
    ];

    private void Add(string route, object reply, TimeSpan delay = default)
    {
        _routes.Add(route, new BackendRoute((_, _) => Task.FromResult(ServiceReply.Ok(reply)), Delay: delay));
    }

    private void Add(string route, Func<BackendRequest, object> reply)
    {
        _routes.Add(route, new BackendRoute((request, _) => Task.FromResult(ServiceReply.Ok(reply(request)))));
    }
}
