using Ares.Common.Control;
using Ares.Raid.Transport;
using Ares.Raid.World;
using Ares.Structs.Net.Control;
using ScenarioNames = Ares.Structs.Backend.Matchmaking.ScenarioNames;

namespace Ares.Raid.Tests;

public sealed class RaidAllocationTests
{
    private const string Ticket = "0b7f6c3e-9d0a-4c1b-8e2f-3a4b5c6d7e8f";
    private const string OtherTicket = "1b7f6c3e-9d0a-4c1b-8e2f-3a4b5c6d7e8f";

    private static readonly GameServerEndpoint s_endpoint = new("ares-raid", "127.0.0.1", 7777, "", "", false, "LOCAL");

    private readonly double[] _now = [0];
    private readonly FakeWorlds _worlds = new();

    private RaidAllocations Allocations() => new(s_endpoint, _worlds, () => _now[0]);

    private static AllocationRequest Request(string ticket = Ticket, long player = 42) => new(ticket, ScenarioNames.TheDam, [player]);

    [Fact]
    public void A_reservation_prepares_the_world_and_issues_a_routing_credential()
    {
        RaidAllocations allocations = Allocations();
        Allocation allocation = allocations.Reserve(Request()).Allocation!;
        Assert.Equal((AllocationStates.Available, ServerInitializationStates.Ready), (allocation.State, allocation.ServerInitializationState));
        Assert.True(SessionAllocatorClient.IsCanonicalUuid(allocation.MatchId));
        byte[] token = Convert.FromBase64String(allocation.Gameserver.RoutingToken);
        Assert.Equal(16, token.Length);
        Assert.Equal([allocation.MatchId], _worlds.Prepared);
        Assert.Equal(new SessionPrincipal(42, allocation.MatchId), allocations.Route(token, renew: false));
        Assert.Null(allocations.Route(new byte[16], renew: false));

        Assert.Equal(allocation.MatchId, allocations.Reserve(Request()).Allocation!.MatchId);
        Assert.Equal("session_capacity_reached", allocations.Reserve(Request(OtherTicket)).Error);
        Assert.Equal("session_allocation_conflict", allocations.Reserve(Request(player: 43)).Error);
        Assert.Equal(allocation.MatchId, allocations.Status(new AllocationStatusRequest(allocation.MatchId)).Allocation!.MatchId);
        Assert.Equal(404, allocations.Status(new AllocationStatusRequest(Ticket)).StatusCode);
    }

    [Fact]
    public void Unsupported_requests_are_rejected()
    {
        RaidAllocations allocations = Allocations();
        AllocationRequest[] requests =
        [
            Request("not-a-uuid"),
            Request() with { ScenarioName = "Other" },
            Request() with { PartyMemberIds = [1, 2] },
            Request(player: 0),
        ];
        Assert.All(requests, request => Assert.Equal("unsupported_session_allocation", allocations.Reserve(request).Error));
        Assert.Empty(_worlds.Prepared);
    }

    [Fact]
    public void Reservations_expire_unless_the_player_is_active()
    {
        RaidAllocations allocations = Allocations();
        Allocation allocation = allocations.Reserve(Request()).Allocation!;
        byte[] token = Convert.FromBase64String(allocation.Gameserver.RoutingToken);
        _now[0] = 100;
        Assert.NotNull(allocations.Route(token, renew: true));
        _now[0] = 219;
        Assert.NotNull(allocations.Route(token, renew: false));
        _now[0] = 221;
        Assert.Null(allocations.Route(token, renew: false));
        Assert.Equal([allocation.MatchId], _worlds.Released);
        Assert.Equal(200, allocations.Reserve(Request(OtherTicket)).StatusCode);
    }

    [Fact]
    public void A_world_that_cannot_be_prepared_fails_the_reservation()
    {
        _worlds.Fail = true;
        RaidAllocations allocations = Allocations();
        Assert.Equal((503, "world_initialization_failed"), (allocations.Reserve(Request()).StatusCode, allocations.Reserve(Request()).Error));
        Assert.Equal(_worlds.Prepared, _worlds.Released);
    }

    [Fact]
    public void Players_are_admitted_by_their_routing_credential_and_identity()
    {
        RaidAllocations allocations = Allocations();
        byte[] token = Convert.FromBase64String(allocations.Reserve(Request()).Allocation!.Gameserver.RoutingToken);
        RoutingAuthenticator authenticator = new(allocations);

        (byte[] packet, byte[] routed) = authenticator.Unwrap([1, 2, 3, .. token, 0]);
        Assert.Equal([1, 2, 3], packet);
        Assert.Equal(token, routed);
        Assert.Throws<AuthenticationRejectedException>(() => authenticator.Unwrap([1, .. token, 1]));
        Assert.Throws<AuthenticationRejectedException>(() => authenticator.Unwrap([1, .. new byte[16], 0]));
        Assert.Throws<AuthenticationRejectedException>(() => authenticator.BeginHello("43", token));

        PendingLogin login = authenticator.BeginHello("42", token);
        Assert.Equal(8, login.Challenge.Length);
        ControlLogin valid = new("0", "/Game/Map?Name=x?EncryptionToken=42", new UniqueNetId(5, "42", "", false, false), "WIN");
        Assert.Throws<AuthenticationRejectedException>(() => authenticator.Complete(login, valid));
        login.ChallengeSent = true;
        Assert.Throws<AuthenticationRejectedException>(() => authenticator.Complete(login.Copy(), valid with { Response = "1" }));
        Assert.Throws<AuthenticationRejectedException>(() => authenticator.Complete(login.Copy(), valid with { Url = "?EncryptionToken=43" }));
        Assert.Throws<AuthenticationRejectedException>(() => authenticator.Complete(login.Copy(), valid with { Url = "?EncryptionToken=42?encryptiontoken=42" }));
        Assert.Throws<AuthenticationRejectedException>(() => authenticator.Complete(login.Copy(), valid with { UniqueId = valid.UniqueId with { Contents = "43" } }));
        authenticator.Complete(login, valid);
        Assert.True(login.Authenticated);

        _now[0] = 121;
        Assert.Throws<AuthenticationRejectedException>(() => authenticator.Validate(login));
    }

    private sealed class FakeWorlds : IWorldLifecycle
    {
        public List<string> Prepared { get; } = [];

        public List<string> Released { get; } = [];

        public bool Fail { get; set; }

        public void Prepare(string matchId, IReadOnlyList<long> players)
        {
            Prepared.Add(matchId);
            if (Fail)
            {
                throw new WorldPreparationException("test");
            }
        }

        public void Release(string matchId) => Released.Add(matchId);

        public bool AcceptingPlayers(string matchId) => Prepared.Contains(matchId) && !Released.Contains(matchId);
    }
}
