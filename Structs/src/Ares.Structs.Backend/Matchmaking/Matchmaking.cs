using System.Text.Json;
using System.Text.Json.Serialization;
using Ares.Structs.Backend.Profiles;

namespace Ares.Structs.Backend.Matchmaking;

/// <summary>Request of <c>POST /v1/shared/match/start</c>.</summary>
public sealed record MatchStartRequest(
    [property: JsonPropertyName("matchmakingScenario")] string? MatchmakingScenario,
    [property: JsonPropertyName("preferredSquadSize")] JsonElement? PreferredSquadSize,
    [property: JsonPropertyName("preferredRegion")] string? PreferredRegion,
    [property: JsonPropertyName("region")] string? Region,
    [property: JsonPropertyName("gameMode")] string? GameMode,
    [property: JsonPropertyName("partyMemberIds")] IReadOnlyList<JsonElement>? PartyMemberIds,
    [property: JsonPropertyName("datacenters")] IReadOnlyList<JsonElement>? Datacenters,
    [property: JsonPropertyName("platforms")] IReadOnlyList<JsonElement>? Platforms,
    [property: JsonPropertyName("isLonewolf")] bool? IsLonewolf,
    [property: JsonPropertyName("streamerMode")] bool? StreamerMode,
    [property: JsonPropertyName("tags")] JsonElement? Tags);

/// <summary>Requests that only carry a ticket id: match status, cancel and squad layout.</summary>
public sealed record TicketRequest([property: JsonPropertyName("ticketId")] string? TicketId);

/// <summary>A matchmaking ticket (start and status responses).</summary>
public sealed record MatchTicket(
    [property: JsonPropertyName("ticketId")] string TicketId,
    [property: JsonPropertyName("pollingInterval")] double PollingInterval,
    [property: JsonPropertyName("selectedRegion")] string SelectedRegion,
    [property: JsonPropertyName("matchState")] string MatchState,
    [property: JsonPropertyName("matchTimeEstimate")] int MatchTimeEstimate,
    [property: JsonPropertyName("partyMemberIds")] IReadOnlyList<long> PartyMemberIds,
    [property: JsonPropertyName("confirmedPartyMemberIds")] IReadOnlyList<long> ConfirmedPartyMemberIds,
    [property: JsonPropertyName("scenarioName")] string ScenarioName,
    [property: JsonPropertyName("redirectInfo"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] RedirectInfo? RedirectInfo = null);

/// <summary>Where a completed ticket sends the client.</summary>
public sealed record RedirectInfo(
    [property: JsonPropertyName("target")] string Target,
    [property: JsonPropertyName("matchId")] string MatchId);

/// <summary>Values of <see cref="MatchTicket.MatchState"/>.</summary>
public static class MatchStates
{
    /// <summary>Searching.</summary>
    public const string Matching = "MATCHING";

    /// <summary>A match was assigned; see <see cref="MatchTicket.RedirectInfo"/>.</summary>
    public const string Completed = "COMPLETED";
}

/// <summary>Values of <see cref="RedirectInfo.Target"/>.</summary>
public static class RedirectTargets
{
    /// <summary>Connect to a game server.</summary>
    public const string GameServer = "GAMESERVER";
}

/// <summary>Response of <c>POST /v1/shared/match/v2/cancel</c>.</summary>
public sealed record CancelMatchResponse(
    [property: JsonPropertyName("ticketId")] string TicketId,
    [property: JsonPropertyName("hasAssignment")] bool HasAssignment);

/// <summary>Request of <c>POST /v1/shared/gameserver/status</c>.</summary>
public sealed record GameServerStatusRequest([property: JsonPropertyName("matchId")] string? MatchId);

/// <summary>Response of <c>POST /v1/shared/gameserver/status</c>.</summary>
public sealed record GameServerStatusResponse(
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("matchId")] string MatchId,
    [property: JsonPropertyName("pollingInterval")] double PollingInterval,
    [property: JsonPropertyName("serverInitializationState")] string ServerInitializationState,
    [property: JsonPropertyName("gameserver"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] GameServer? GameServer = null);

/// <summary>
/// The game server a client travels to. <see cref="RoutingToken"/> is the base64 credential the
/// client appends to its datagrams, or empty.
/// </summary>
public sealed record GameServer(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("host")] string Host,
    [property: JsonPropertyName("port")] int Port,
    [property: JsonPropertyName("secretKey")] string SecretKey,
    [property: JsonPropertyName("routingToken")] string RoutingToken,
    [property: JsonPropertyName("proxyEnabled")] bool ProxyEnabled,
    [property: JsonPropertyName("datacenterIcaoCode")] string DatacenterIcaoCode);

/// <summary>Values of <see cref="GameServerStatusResponse.State"/>.</summary>
public static class GameServerStates
{
    public const string Waiting = "WAITING";
    public const string Available = "AVAILABLE";

    /// <summary>The client treats this as a failed allocation.</summary>
    public const string Expired = "EXPIRED";
}

/// <summary>Values of <see cref="GameServerStatusResponse.ServerInitializationState"/>.</summary>
public static class ServerInitializationStates
{
    public const string Initializing = "INITIALIZING";
    public const string Ready = "READY";
    public const string Failure = "FAILURE";
}

/// <summary>Response of <c>POST /v1/shared/squad/layout</c>.</summary>
public sealed record SquadLayoutResponse(
    [property: JsonPropertyName("squads")] IReadOnlyList<Squad> Squads,
    [property: JsonPropertyName("maxPlayers")] int MaxPlayers,
    [property: JsonPropertyName("maxSquadSize")] int MaxSquadSize,
    [property: JsonPropertyName("maxSquads")] int MaxSquads,
    [property: JsonPropertyName("currentPlayerCount")] int CurrentPlayerCount);

/// <summary>One squad of a match.</summary>
public sealed record Squad(
    [property: JsonPropertyName("squadId")] string SquadId,
    [property: JsonPropertyName("squadMembers")] IReadOnlyList<SquadMember> SquadMembers);

/// <summary>One squad member.</summary>
public sealed record SquadMember([property: JsonPropertyName("profile")] EmbarkProfile Profile);

/// <summary>Response of <c>POST /v1/shared/player/activity</c>.</summary>
public sealed record PlayerActivityResponse(
    [property: JsonPropertyName("state")] int State,
    [property: JsonPropertyName("currentState")] string CurrentState,
    [property: JsonPropertyName("etag")] string ETag,
    [property: JsonPropertyName("ticket_id")] string TicketId,
    [property: JsonPropertyName("match_id")] string MatchId,
    [property: JsonPropertyName("tournament_id")] string TournamentId,
    [property: JsonPropertyName("lobby_id")] string LobbyId,
    [property: JsonPropertyName("scenarioMetadata")] ScenarioMetadata ScenarioMetadata);

/// <summary>Scenario of the current activity.</summary>
public sealed record ScenarioMetadata(
    [property: JsonPropertyName("scenarioName")] string ScenarioName,
    [property: JsonPropertyName("allowReconnects")] bool AllowReconnects);

/// <summary>Values of <see cref="PlayerActivityResponse.CurrentState"/>.</summary>
public static class PlayerActivityStates
{
    public const string Idle = "IDLE";
    public const string Matchmaking = "MATCHMAKING";
    public const string InMatch = "IN_MATCH";
}
