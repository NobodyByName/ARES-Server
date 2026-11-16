namespace Ares.Structs.Backend.Matchmaking;

/// <summary>Request paths of the matchmaking API.</summary>
public static class MatchmakingPaths
{
    /// <summary><c>POST</c>; reply <see cref="ScenariosResponse"/>.</summary>
    public const string Scenarios = "/v1/shared/scenarios";

    /// <summary><c>POST</c> <see cref="MatchStartRequest"/>; reply <see cref="MatchTicket"/>.</summary>
    public const string MatchStart = "/v1/shared/match/start";

    /// <summary><c>POST</c> <see cref="TicketRequest"/>; reply <see cref="MatchTicket"/>.</summary>
    public const string MatchStatus = "/v1/shared/match/status";

    /// <summary><c>POST</c> <see cref="TicketRequest"/>; reply <see cref="CancelMatchResponse"/>.</summary>
    public const string MatchCancel = "/v1/shared/match/v2/cancel";

    /// <summary><c>POST</c> <see cref="TicketRequest"/>; reply <see cref="SquadLayoutResponse"/>.</summary>
    public const string SquadLayout = "/v1/shared/squad/layout";

    /// <summary><c>POST</c> <see cref="GameServerStatusRequest"/>; reply <see cref="GameServerStatusResponse"/>.</summary>
    public const string GameServerStatus = "/v1/shared/gameserver/status";

    /// <summary><c>POST</c>; reply <see cref="PlayerActivityResponse"/>.</summary>
    public const string PlayerActivity = "/v1/shared/player/activity";
}
