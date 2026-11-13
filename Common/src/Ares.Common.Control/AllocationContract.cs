using System.Text.Json;
using System.Text.Json.Serialization;
using Ares.Common.Game.Inventory;
using Ares.Structs.Net.Replication;

namespace Ares.Common.Control;

/// <summary>Host → Raid: reserve a raid for a matchmaking ticket.</summary>
/// <param name="TicketId">The matchmaking ticket (canonical UUID).</param>
/// <param name="ScenarioName">Matchmaking scenario, e.g. the Dam scenario.</param>
/// <param name="PartyMemberIds">Tenancy user ids of the players in the party.</param>
/// <param name="Loadouts">Optional inventory snapshot per player; omitted requests use empty loadouts.</param>
/// <param name="Profiles">Optional public squad profiles, one per player.</param>
public sealed record AllocationRequest(string TicketId, string ScenarioName, IReadOnlyList<long> PartyMemberIds,
    IReadOnlyList<PlayerLoadout>? Loadouts = null, IReadOnlyList<CompactPlayerProfile>? Profiles = null);

/// <summary>Host → Raid: poll a reservation. Several tickets can share one raid, so the ticket is the key.</summary>
/// <param name="MatchId">The match id returned by the reservation, or null to look up a persisted ticket.</param>
/// <param name="TicketId">The ticket the reservation was made for.</param>
public sealed record AllocationStatusRequest(string? MatchId, string? TicketId = null);

/// <summary>Raid → Host: the state of one reservation.</summary>
/// <param name="TicketId">The ticket the reservation was made for.</param>
/// <param name="MatchId">Raid-assigned match id (canonical UUID).</param>
/// <param name="ScenarioName">The reserved scenario.</param>
/// <param name="PartyMemberIds">Players admitted to the raid.</param>
/// <param name="State">See <see cref="AllocationStates"/>.</param>
/// <param name="ServerInitializationState">See <see cref="ServerInitializationStates"/>.</param>
/// <param name="PollingInterval">Seconds the client should wait between status polls.</param>
/// <param name="Gameserver">Where players connect.</param>
/// <param name="Loadouts">Receipts for the loadouts accepted by Raid, absent for older allocation services.</param>
/// <param name="Results">Finished players' results awaiting settlement; absent while none.</param>
public sealed record Allocation(
    string TicketId,
    string MatchId,
    string ScenarioName,
    IReadOnlyList<long> PartyMemberIds,
    string State,
    string ServerInitializationState,
    double PollingInterval,
    GameServerEndpoint Gameserver,
    IReadOnlyList<LoadoutReceipt>? Loadouts = null,
    IReadOnlyList<RaidResult>? Results = null);

/// <summary>Host → Raid: confirms the results for these players are saved. Idempotent.</summary>
/// <param name="TicketId">The ticket the reservation was made for.</param>
/// <param name="MatchId">The match id of the reservation.</param>
/// <param name="UserIds">Players whose results Host has saved.</param>
public sealed record AllocationSettleRequest(string TicketId, string MatchId, IReadOnlyList<long> UserIds);

/// <summary>The player-reachable raid endpoint of an allocation.</summary>
/// <param name="Name">Raid server name.</param>
/// <param name="Host">IPv4 address or DNS name players connect to.</param>
/// <param name="Port">UDP port.</param>
/// <param name="SecretKey">Unused; always empty.</param>
/// <param name="RoutingToken">Base64 of the 16-byte routing credential, or empty.</param>
/// <param name="ProxyEnabled">Proxied transport; not implemented, always false.</param>
/// <param name="DatacenterIcaoCode">Datacenter label shown to the client.</param>
public sealed record GameServerEndpoint(
    string Name,
    string Host,
    int Port,
    string SecretKey,
    string RoutingToken,
    bool ProxyEnabled,
    string DatacenterIcaoCode);

/// <summary>Values of <see cref="Allocation.State"/>.</summary>
public static class AllocationStates
{
    /// <summary>Reserved; the raid is still preparing.</summary>
    public const string Waiting = "WAITING";

    /// <summary>The raid accepts its players.</summary>
    public const string Available = "AVAILABLE";

    /// <summary>The reservation was lost (Host-side state; never sent by Raid).</summary>
    public const string Expired = "EXPIRED";
}

/// <summary>Values of <see cref="Allocation.ServerInitializationState"/>.</summary>
public static class ServerInitializationStates
{
    /// <summary>Paired with <see cref="AllocationStates.Waiting"/>.</summary>
    public const string Initializing = "INITIALIZING";

    /// <summary>Paired with <see cref="AllocationStates.Available"/>.</summary>
    public const string Ready = "READY";

    /// <summary>Paired with <see cref="AllocationStates.Expired"/>.</summary>
    public const string Failure = "FAILURE";
}

/// <summary>Error body of a rejected allocation API call.</summary>
/// <param name="Error">Machine-readable reason.</param>
public sealed record AllocationError(string Error);

/// <summary>Paths and JSON settings of the allocation API.</summary>
public static class AllocationApi
{
    /// <summary>Reserve endpoint path.</summary>
    public const string ReservePath = "/allocations/reserve";

    /// <summary>Status endpoint path.</summary>
    public const string StatusPath = "/allocations/status";

    /// <summary>Settle endpoint path.</summary>
    public const string SettlePath = "/allocations/settle";

    /// <summary>Largest request or response body, in bytes.</summary>
    public const int MaximumBodyBytes = 262144;

    /// <summary>camelCase JSON; missing or null required members are rejected.</summary>
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        NumberHandling = JsonNumberHandling.Strict,
    };
}
