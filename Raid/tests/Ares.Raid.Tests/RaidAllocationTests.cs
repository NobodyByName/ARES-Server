using Ares.Common.Control;
using Ares.Common.Control.Server;
using Ares.Common.Game.Inventory;
using Ares.Raid.Transport;
using Ares.Raid.World;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.Replication;
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
    public void Ticket_status_is_read_only_and_requires_one_valid_lookup_key()
    {
        RaidAllocations allocations = Allocations();
        Allocation allocation = allocations.Reserve(Request()).Allocation!;
        Assert.Equal(allocation.MatchId, allocations.Status(new AllocationStatusRequest(null, Ticket)).Allocation!.MatchId);
        Assert.Equal(404, allocations.Status(new AllocationStatusRequest(null, OtherTicket)).StatusCode);
        Assert.Equal(400, allocations.Status(new AllocationStatusRequest(null)).StatusCode);
        Assert.Equal(400, allocations.Status(new AllocationStatusRequest(null, "invalid")).StatusCode);
        Assert.Equal(400, allocations.Status(new AllocationStatusRequest(allocation.MatchId, Ticket)).StatusCode);
        Assert.Equal(400, allocations.Status(new AllocationStatusRequest("invalid")).StatusCode);
        Assert.Equal([allocation.MatchId], _worlds.Prepared);
        _now[0] = 121;
        Assert.Equal(404, allocations.Status(new AllocationStatusRequest(null, Ticket)).StatusCode);
        Assert.Equal([allocation.MatchId], _worlds.Released);
        Assert.Equal([allocation.MatchId], _worlds.Prepared);
    }

    [Fact]
    public void An_ended_world_releases_its_ticket_immediately_and_another_raid_can_be_reserved()
    {
        RaidAllocations allocations = Allocations();
        Allocation allocation = allocations.Reserve(Request()).Allocation!;
        byte[] token = Convert.FromBase64String(allocation.Gameserver.RoutingToken);
        Assert.NotNull(allocations.Route(token, renew: true));
        _worlds.Ended.Add(allocation.MatchId);

        Assert.Equal(404, allocations.Status(new AllocationStatusRequest(null, Ticket)).StatusCode);
        Assert.Null(allocations.Route(token, renew: true));
        Assert.Equal([allocation.MatchId], _worlds.Released);
        Allocation next = allocations.Reserve(Request(OtherTicket)).Allocation!;
        Assert.NotEqual(allocation.MatchId, next.MatchId);
        Assert.Equal(200, allocations.Status(new AllocationStatusRequest(next.MatchId)).StatusCode);
        Assert.Equal(404, allocations.Status(new AllocationStatusRequest(allocation.MatchId)).StatusCode);
        Assert.Equal([allocation.MatchId], _worlds.Released);
    }

    [Fact]
    public void A_reservation_holds_at_most_the_configured_party_size()
    {
        RaidAllocations single = Allocations();
        Assert.Equal((400, "unsupported_session_allocation"), (single.Reserve(Request() with { PartyMemberIds = [42, 43] }).StatusCode,
            single.Reserve(Request() with { PartyMemberIds = [42, 43] }).Error));

        RaidAllocations squad = new(s_endpoint, _worlds, () => _now[0], maxPlayers: 3);
        Assert.Equal(200, squad.Reserve(Request() with { PartyMemberIds = [42, 43, 44] }).StatusCode);
        Assert.Equal(400, new RaidAllocations(s_endpoint, _worlds, () => _now[0], maxPlayers: 3)
            .Reserve(Request() with { PartyMemberIds = [42, 43, 44, 45] }).StatusCode);
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
    public void Loadouts_are_frozen_per_reservation_and_changed_retries_are_rejected()
    {
        InventoryItem[] items = [new("carried", 42, 1, [], 0, 0, "", "v1", 0)];
        AllocationRequest request = Request() with { Loadouts = [new PlayerLoadout(42, 7, items)] };
        RaidAllocations allocations = Allocations();
        string match = allocations.Reserve(request).Allocation!.MatchId;
        items[0] = items[0] with { Amount = 2 };
        Assert.Equal(1, Assert.Single(Assert.Single(_worlds.Loadouts!).Items).Amount);
        Assert.Equal("session_allocation_conflict", allocations.Reserve(request).Error);
        Assert.Equal(match, allocations.Reserve(request with { Loadouts = _worlds.Loadouts }).Allocation!.MatchId);

        Assert.Equal("invalid_session_loadout", allocations.Reserve(request with { Loadouts = [new PlayerLoadout(43, 7, items)] }).Error);
        Assert.Equal("invalid_session_loadout", allocations.Reserve(request with { Loadouts = [] }).Error);
        Assert.Equal("session_allocation_conflict", allocations.Reserve(Request()).Error);
    }

    [Fact]
    public void A_ferro_and_heavy_ammo_loadout_can_be_reserved_against_the_real_dam_world()
    {
        DamWorlds worlds = new(() => 0, "Lighting.GreyMorning", () => 123456789);
        RaidAllocations allocations = new(s_endpoint, worlds, () => 0);
        AllocationRequest request = Request() with { Loadouts = [World.WeaponTests.WithWeapons()] };
        AllocationResult result = allocations.Reserve(request);
        Assert.Equal(200, result.StatusCode);
        Assert.NotNull(worlds.Current);
        Assert.Equal(200, allocations.Reserve(request).StatusCode);
    }

    [Fact]
    public void Prepared_worlds_retain_the_loadout_without_sharing_operator_mutations()
    {
        DamWorlds worlds = new(() => 0, "Lighting.GreyMorning", () => 123456789);
        PlayerLoadout loadout = World.InventoryTests.StandardLoadout();
        InventoryItem[] items = [.. loadout.Items];
        PlayerLoadout[] loadouts = [loadout with { Items = items }];
        worlds.Prepare(Ticket, [42], loadouts);
        int duck = Array.FindIndex(items, item => item.GameAssetId == StorageItems.RubberDuckAssetId);
        items[duck] = items[duck] with { Amount = 5 };

        Assert.Equal(4, Assert.Single(worlds.Current!.Loadouts).Items[duck].Amount);
        Assert.Throws<WorldPreparationException>(() => worlds.Prepare(Ticket, [42], loadouts));
    }

    [Fact]
    public void Profiles_are_frozen_and_conflicting_or_invalid_retries_are_rejected()
    {
        CompactPlayerProfile profile = new(new TenancyUserId(42), "Raider", "1234", "platform", "Raider");
        CompactPlayerProfile[] profiles = [profile];
        AllocationRequest request = Request() with { Profiles = profiles };
        RaidAllocations allocations = Allocations();
        Assert.Equal(200, allocations.Reserve(request).StatusCode);
        profiles[0] = profile with { DisplayName = "Changed" };
        Assert.Equal(profile, Assert.Single(_worlds.Profiles!));
        Assert.Equal("session_allocation_conflict", allocations.Reserve(request).Error);
        Assert.Equal(200, allocations.Reserve(request with { Profiles = [profile] }).StatusCode);
        Assert.Equal("invalid_session_profiles", allocations.Reserve(request with { Profiles = [] }).Error);
        Assert.Equal("invalid_session_profiles", allocations.Reserve(request with { Profiles = [null!] }).Error);
        Assert.Equal("invalid_session_profiles", allocations.Reserve(request with { Profiles = [profile with { TenancyUserId = null! }] }).Error);
        Assert.Equal("invalid_session_profiles", allocations.Reserve(request with { Profiles = [profile with { DisplayName = null! }] }).Error);
        Assert.Equal("invalid_session_profiles", allocations.Reserve(request with { Profiles = [profile with { TenancyUserId = new TenancyUserId(43) }] }).Error);
        Assert.Equal("invalid_session_profiles", allocations.Reserve(request with { Profiles = [profile with { DisplayName = "bad\0name" }] }).Error);

        DamWorlds worlds = new(() => 0, "Lighting.GreyMorning", () => 123456789);
        worlds.Prepare(Ticket, [42], profiles: [profile]);
        Assert.Equal(profile, Assert.Single(worlds.Current!.Profiles));
        Assert.Throws<WorldPreparationException>(() => worlds.Prepare(Ticket, [42], profiles: profiles));
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

        public HashSet<string> Ended { get; } = [];

        public bool Fail { get; set; }

        public IReadOnlyList<PlayerLoadout>? Loadouts { get; private set; }

        public IReadOnlyList<CompactPlayerProfile>? Profiles { get; private set; }

        public void Prepare(string matchId, IReadOnlyList<long> players, IReadOnlyList<PlayerLoadout>? loadouts = null, IReadOnlyList<CompactPlayerProfile>? profiles = null)
        {
            Prepared.Add(matchId);
            Loadouts = loadouts;
            Profiles = profiles;
            if (Fail)
            {
                throw new WorldPreparationException("test");
            }
        }

        public void Release(string matchId) => Released.Add(matchId);

        public bool AcceptingPlayers(string matchId) => Prepared.Contains(matchId) && !Released.Contains(matchId);

        public bool HasEnded(string matchId) => Ended.Contains(matchId);
    }
}
