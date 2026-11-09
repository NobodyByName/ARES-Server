using System.Text.Json.Nodes;
using Ares.Common.Logging;
using Ares.Host.Services.Accounts;
using Ares.Host.Services.Content;
using Ares.Host.Services.Identity;
using Ares.Host.Services.Players;
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
/// <param name="RequiresIdentity">
/// The caller must present a valid identity token of an unbanned account. Only the game's sign-in
/// itself is open; the game calls nothing else before it.
/// </param>
/// <param name="Delay">Wait before replying. Some bootstrap replies arriving too early made client startup fail intermittently.</param>
public sealed record BackendRoute(RouteHandler Handler, bool RequiresIdentity = true, TimeSpan Delay = default);

/// <summary>The backend routes the client uses, keyed by <c>"METHOD /path"</c>. Anything else is HTTP 501.</summary>
public sealed class BackendRoutes
{
    private readonly Dictionary<string, BackendRoute> _routes = new(StringComparer.Ordinal);

    /// <summary>Registers all routes against the services.</summary>
    public BackendRoutes(IdentityService identity, JsonEventLog log, TimeProvider time)
    {
        // Identity and bootstrap.
        _routes["POST /oauth2/token"] = new BackendRoute((request, _) =>
        {
            (OAuthTokenResponse? signedIn, Account? account) = identity.SignIn(request);
            log.Write("game_sign_in", new JsonObject
            {
                ["name"] = account?.Name,
                ["grant"] = request.FormValue(OAuthTokenForm.GrantType),
                ["result"] = signedIn is null ? "rejected" : "signed_in",
            });
            return Task.FromResult(signedIn is null ? ServiceReply.Error(401, "invalid_launch_key") : ServiceReply.Ok(signedIn));
        }, RequiresIdentity: false);
        Player("GET /v1/shared/profile", (_, player) => player.Profiles.Create());
        Player("GET /v1/pioneer/raiders", (_, player) => IdentityService.Raiders(player.Account.UserId));
        Player("POST /v1/shared/profile/by-thirdparty-userid", (request, player) =>
            IdentityService.PlatformProfiles(player.Profiles.Create(), request.Read<PlatformProfilesRequest>()).Response);
        Player("GET /v1/shared/auth/third-party-access-token", (_, player) => identity.ThirdPartyAccessToken(player.Account));
        Player("POST /v1/shared/manifest", (request, player) => identity.Manifest(request.Read<ManifestRequest>(), player.Inventory.DefinitionHash()));
        Add("POST /v1/shared/build/v2/latest", request => IdentityService.LatestBuild(request.Read<LatestBuildRequest>()));
        Add("GET /sdk/v1/default", EosSdkDefaults.Configuration);
        Add("GET /sdk/v1/product/" + EosEndpoints.GameProductId, EosSdkDefaults.Configuration);
        Add("POST /auth/v1/oauth/token", request => identity.EosToken(
            new EosTokenRequest(request.FormValue("grant_type"), request.FormValue("deployment_id"), request.FormValue("nonce")),
            request.Authorization).Response);

        // Accounts.
        Player("POST /v1/shared/get-persistent-player-keys", (_, player) => new PersistentPlayerKeysResponse(player.Settings.GetKeys()));
        Player("POST /v1/shared/set-persistent-player-keys", (request, player, _) =>
        {
            (IReadOnlyList<PersistentPlayerKey>? updated, string? error) = player.Settings.SetKeys(request.Read<SetPersistentPlayerKeysRequest>().PersistentPlayerKeys);
            return Task.FromResult(updated is null ? ServiceReply.Error(400, error!) : ServiceReply.Ok(new PersistentPlayerKeysResponse(updated)));
        });

        // Party and notifications.
        Player("POST /v1/shared/social/party/v2/create", (request, player, _) => player.Party.CreateAsync(request.Read<CreatePartyRequest>()));
        Player("PATCH /v1/shared/social/party/v2/update-party-attributes", (request, player, _) => player.Party.UpdateAttributesAsync(request.Read<UpdatePartyAttributesRequest>()));
        Player("POST /v1/shared/social/party/v2/update-party-member-data", (request, player, _) => player.Party.UpdateMemberAsync(request.Read<UpdatePartyMemberDataRequest>()));
        Player("POST /v1/shared/social/party/set-ready-for-matchmaking", (request, player, _) => player.Party.SetReadyAsync(request.Read<SetReadyForMatchmakingRequest>()));
        Player("POST /v1/shared/notifications", (request, player, token) => player.Party.PollAsync(request.Read<NotificationsRequest>(), token));

        // Matchmaking.
        _routes["POST /v1/shared/scenarios"] = new BackendRoute((_, _) => Task.FromResult(ServiceReply.Ok(BackendContent.Scenarios)));
        Player("POST /v1/shared/match/start", (request, player, token) => player.Matchmaking.StartAsync(request.Read<MatchStartRequest>(), token));
        Player("POST /v1/shared/match/status", (request, player, token) => player.Matchmaking.StatusAsync(request.Read<TicketRequest>(), token));
        Player("POST /v1/shared/match/v2/cancel", (request, player, token) => player.Matchmaking.CancelAsync(request.Read<TicketRequest>(), token));
        Player("POST /v1/shared/squad/layout", (request, player, token) => player.Matchmaking.SquadLayoutAsync(request.Read<TicketRequest>(), token));
        Player("POST /v1/shared/gameserver/status", (request, player, token) => player.Matchmaking.GameServerAsync(request.Read<GameServerStatusRequest>(), token));
        Player("POST /v1/shared/player/activity", async (_, player, token) => ServiceReply.Ok(await player.Matchmaking.ActivityAsync(token).ConfigureAwait(false)));

        // Progression: empty state for a fresh account.
        Player("GET /v1/pioneer/game-data", (_, player) => player.Inventory.GameData());
        Add("GET /v1/pioneer/quests", new QuestsResponse([]));
        Player("GET /v1/pioneer/inventory", (_, player) => new InventoryResponse(player.Inventory.Snapshot.Items));
        Player("POST /v1/pioneer/v1/batch-operation", (request, player, token) =>
            player.Matchmaking.CommitInventoryAsync(request.Read<InventoryOperationsRequest>(), time.GetUtcNow(), token));
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

    private void Player(string route, Func<BackendRequest, PlayerServices, object> reply)
    {
        Player(route, (request, player, _) => Task.FromResult(ServiceReply.Ok(reply(request, player))));
    }

    private void Player(string route, Func<BackendRequest, PlayerServices, CancellationToken, Task<ServiceReply>> handler)
    {
        _routes.Add(route, new BackendRoute((request, token) => handler(request, request.Player!, token)));
    }
}
