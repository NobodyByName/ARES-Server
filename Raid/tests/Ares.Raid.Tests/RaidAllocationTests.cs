using Ares.Common.Control;
using Ares.Common.Control.Server;
using Ares.Common.Game.Inventory;
using Ares.Common.Logging;
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
    private const string ThirdTicket = "2b7f6c3e-9d0a-4c1b-8e2f-3a4b5c6d7e8f";

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
        Assert.Equal("session_capacity_reached", allocations.Reserve(Request(OtherTicket, player: 43)).Error);
        Assert.Equal("session_allocation_conflict", allocations.Reserve(Request(player: 43)).Error);
        Assert.Equal(allocation.MatchId, allocations.Status(new AllocationStatusRequest(allocation.MatchId, Ticket)).Allocation!.MatchId);
        Assert.Equal(404, allocations.Status(new AllocationStatusRequest(Ticket, Ticket)).StatusCode);
    }

    [Fact]
    public void Other_parties_join_the_raid_while_it_has_room_and_its_fill_window_is_open()
    {
        RaidAllocations allocations = new(s_endpoint, _worlds, () => _now[0], maxPlayers: 3, fillWindow: 30);
        Allocation first = allocations.Reserve(Request()).Allocation!;
        _now[0] = 29;
        Allocation second = allocations.Reserve(Request(OtherTicket, player: 43)).Allocation!;
        Assert.Equal(first.MatchId, second.MatchId);
        Assert.Equal([first.MatchId], _worlds.Prepared);
        Assert.Equal([first.MatchId], _worlds.Joined);
        Assert.NotEqual(first.Gameserver.RoutingToken, second.Gameserver.RoutingToken);
        Assert.Equal(new SessionPrincipal(42, first.MatchId), allocations.Route(Convert.FromBase64String(first.Gameserver.RoutingToken), renew: false));
        Assert.Equal(new SessionPrincipal(43, first.MatchId), allocations.Route(Convert.FromBase64String(second.Gameserver.RoutingToken), renew: false));
        Assert.Equal([43L], allocations.Status(new AllocationStatusRequest(first.MatchId, OtherTicket)).Allocation!.PartyMemberIds);

        Assert.Equal("session_allocation_conflict", allocations.Reserve(Request(ThirdTicket, player: 43)).Error);
        _now[0] = 30;
        Assert.Equal("session_capacity_reached", allocations.Reserve(Request(ThirdTicket, player: 44)).Error);
    }

    [Fact]
    public void A_full_raid_or_a_world_that_refuses_the_party_sends_it_elsewhere()
    {
        RaidAllocations allocations = new(s_endpoint, _worlds, () => _now[0], maxPlayers: 2);
        Allocation first = allocations.Reserve(Request() with { PartyMemberIds = [42, 43] }).Allocation!;
        Assert.Equal("session_capacity_reached", allocations.Reserve(Request(OtherTicket, player: 44)).Error);

        RaidAllocations refused = new(s_endpoint, _worlds, () => _now[0], maxPlayers: 2);
        _worlds.RefuseJoin = true;
        Allocation kept = refused.Reserve(Request()).Allocation!;
        Assert.Equal((409, "session_capacity_reached"), (refused.Reserve(Request(OtherTicket, player: 43)).StatusCode,
            refused.Reserve(Request(OtherTicket, player: 43)).Error));
        Assert.NotNull(refused.Route(Convert.FromBase64String(kept.Gameserver.RoutingToken), renew: false));
        Assert.DoesNotContain(kept.MatchId, _worlds.Released);
        Assert.DoesNotContain(first.MatchId, _worlds.Released);
    }

    [Fact]
    public void The_raid_closes_once_every_party_has_been_released()
    {
        RaidAllocations allocations = new(s_endpoint, _worlds, () => _now[0], maxPlayers: 2);
        Allocation first = allocations.Reserve(Request()).Allocation!;
        _now[0] = 50;
        Allocation second = allocations.Reserve(Request(OtherTicket, player: 43)).Allocation!;
        _now[0] = 121;
        Assert.Null(allocations.Route(Convert.FromBase64String(first.Gameserver.RoutingToken), renew: false));
        Assert.NotNull(allocations.Route(Convert.FromBase64String(second.Gameserver.RoutingToken), renew: false));
        Assert.Empty(_worlds.Released);
        Assert.Equal([42L], _worlds.Expired);
        Assert.False(allocations.Closed);

        _now[0] = 171;
        Assert.True(allocations.Closed);
        Assert.Equal([42L, 43L], _worlds.Expired);
        Assert.Equal([first.MatchId], _worlds.Released);
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
    public void Ticket_status_is_read_only_and_requires_a_valid_ticket()
    {
        RaidAllocations allocations = Allocations();
        Allocation allocation = allocations.Reserve(Request()).Allocation!;
        Assert.Equal(allocation.MatchId, allocations.Status(new AllocationStatusRequest(null, Ticket)).Allocation!.MatchId);
        Assert.Equal(404, allocations.Status(new AllocationStatusRequest(null, OtherTicket)).StatusCode);
        Assert.Equal(400, allocations.Status(new AllocationStatusRequest(null)).StatusCode);
        Assert.Equal(400, allocations.Status(new AllocationStatusRequest(null, "invalid")).StatusCode);
        Assert.Equal(400, allocations.Status(new AllocationStatusRequest(allocation.MatchId)).StatusCode);
        Assert.Equal(400, allocations.Status(new AllocationStatusRequest("invalid", Ticket)).StatusCode);
        Assert.Equal([allocation.MatchId], _worlds.Prepared);
        _now[0] = 121;
        Assert.Equal(404, allocations.Status(new AllocationStatusRequest(null, Ticket)).StatusCode);
        Assert.Equal([allocation.MatchId], _worlds.Released);
        Assert.Equal([allocation.MatchId], _worlds.Prepared);
    }

    [Fact]
    public void A_party_that_left_ends_its_ticket_at_once_and_the_last_one_closes_the_raid()
    {
        RaidAllocations allocations = new(s_endpoint, _worlds, () => _now[0], maxPlayers: 2);
        Allocation first = allocations.Reserve(Request()).Allocation!;
        Allocation second = allocations.Reserve(Request(OtherTicket, player: 43)).Allocation!;
        byte[] token = Convert.FromBase64String(first.Gameserver.RoutingToken);
        Assert.NotNull(allocations.Route(token, renew: true));
        _worlds.Left.Add(42);

        Assert.Equal(404, allocations.Status(new AllocationStatusRequest(null, Ticket)).StatusCode);
        Assert.Null(allocations.Route(token, renew: true));
        Assert.NotNull(allocations.Route(Convert.FromBase64String(second.Gameserver.RoutingToken), renew: false));
        Assert.Empty(_worlds.Released);
        Assert.False(allocations.Closed);

        _worlds.Left.Add(43);
        Assert.Equal(404, allocations.Status(new AllocationStatusRequest(null, OtherTicket)).StatusCode);
        Assert.Equal([first.MatchId], _worlds.Released);
        Assert.True(allocations.Closed);
        Assert.Equal((409, "session_capacity_reached"), (allocations.Reserve(Request(ThirdTicket, player: 44)).StatusCode, allocations.Reserve(Request(ThirdTicket, player: 44)).Error));
        Assert.Equal([first.MatchId], _worlds.Prepared);
        Assert.Empty(_worlds.Expired);
    }

    [Fact]
    public void A_raid_without_a_reservation_closes_after_the_initialization_timeout()
    {
        RaidAllocations allocations = Allocations();
        _now[0] = 119;
        Assert.False(allocations.Closed);
        _now[0] = 120;
        Assert.True(allocations.Closed);
        Assert.Equal(409, allocations.Reserve(Request()).StatusCode);
        Assert.Empty(_worlds.Prepared);
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
        Assert.True(allocations.Closed);
        Assert.Equal(409, allocations.Reserve(Request(OtherTicket)).StatusCode);
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
    public void Two_tickets_share_the_real_dam_world_and_an_expired_one_leaves_it_alone()
    {
        DamWorlds worlds = new(() => _now[0], "Lighting.GreyMorning", () => 123456789);
        RaidAllocations allocations = new(s_endpoint, worlds, () => _now[0], maxPlayers: 2);
        Allocation first = allocations.Reserve(Request()).Allocation!;
        Allocation second = allocations.Reserve(Request(OtherTicket, player: 43)).Allocation!;
        Assert.Equal(first.MatchId, second.MatchId);
        Assert.Equal([42L, 43L], worlds.Current!.Players.Select(player => player.Id));
        Assert.Equal("session_capacity_reached", allocations.Reserve(Request(ThirdTicket, player: 44)).Error);

        byte[] token = Convert.FromBase64String(second.Gameserver.RoutingToken);
        _now[0] = 100;
        Assert.NotNull(allocations.Route(token, renew: true));
        _now[0] = 121;
        Assert.Equal(404, allocations.Status(new AllocationStatusRequest(null, Ticket)).StatusCode);
        Assert.True(worlds.HasLeft(first.MatchId, 42));
        Assert.Null(worlds.WelcomeFor(new SessionPrincipal(42, first.MatchId)));
        Assert.NotNull(worlds.WelcomeFor(new SessionPrincipal(43, first.MatchId)));
        Assert.Equal(200, allocations.Status(new AllocationStatusRequest(null, OtherTicket)).StatusCode);
        Assert.False(allocations.Closed);

        _now[0] = 221;
        Assert.True(allocations.Closed);
        Assert.Null(worlds.Current);
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

        Assert.Equal(4, Assert.Single(worlds.Current!.Players).Loadout.Items[duck].Amount);
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
        Assert.Equal(profile, Assert.Single(Assert.Single(worlds.Current!.Players).Profiles));
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

    [Fact]
    public void A_finished_player_result_is_reported_until_settled_and_settling_ends_the_raid()
    {
        RaidAllocations allocations = new(s_endpoint, _worlds, () => _now[0], maxPlayers: 2);
        Allocation allocation = allocations.Reserve(Request() with { Loadouts = [new PlayerLoadout(42, 7, [])] }).Allocation!;
        Assert.Null(allocation.Results);
        _worlds.Ends[42] = new PlayerRaidEnd(RaidOutcomes.Extracted, []);
        _worlds.Left.Add(42);

        RaidResult result = Assert.Single(allocations.Status(new AllocationStatusRequest(null, Ticket)).Allocation!.Results!);
        Assert.Equal(new RaidResult(Ticket, 42, 7, RaidOutcomes.Extracted, []), result with { Items = [] });
        Assert.Null(allocations.Route(Convert.FromBase64String(allocation.Gameserver.RoutingToken), renew: true));
        Assert.Equal("session_capacity_reached", allocations.Reserve(Request(OtherTicket, player: 43)).Error);
        Assert.False(allocations.Closed);
        Assert.Empty(_worlds.Released);

        Assert.Equal(404, allocations.Settle(new AllocationSettleRequest(OtherTicket, allocation.MatchId, [42])).StatusCode);
        Assert.Equal(404, allocations.Settle(new AllocationSettleRequest(Ticket, OtherTicket, [42])).StatusCode);
        Assert.Equal(400, allocations.Settle(new AllocationSettleRequest("invalid", allocation.MatchId, [42])).StatusCode);
        Assert.False(allocations.Closed);

        Assert.Equal(204, allocations.Settle(new AllocationSettleRequest(Ticket, allocation.MatchId, [42])).StatusCode);
        Assert.Equal(404, allocations.Status(new AllocationStatusRequest(null, Ticket)).StatusCode);
        Assert.Equal([allocation.MatchId], _worlds.Released);
        Assert.True(allocations.Closed);
        Assert.Equal(404, allocations.Settle(new AllocationSettleRequest(Ticket, allocation.MatchId, [42])).StatusCode);
    }

    [Fact]
    public void Settling_is_idempotent_and_a_party_ends_only_when_every_player_left_and_was_settled()
    {
        RaidAllocations allocations = new(s_endpoint, _worlds, () => _now[0], maxPlayers: 2);
        Allocation allocation = allocations.Reserve(Request() with { PartyMemberIds = [42, 43] }).Allocation!;
        _worlds.Ends[42] = new PlayerRaidEnd(RaidOutcomes.Lost, []);
        _worlds.Ends[43] = new PlayerRaidEnd(RaidOutcomes.Extracted, []);
        _worlds.Left.Add(42);
        Assert.Equal(204, allocations.Settle(new AllocationSettleRequest(Ticket, allocation.MatchId, [43])).StatusCode);
        Assert.Equal([42L], allocations.Status(new AllocationStatusRequest(null, Ticket)).Allocation!.Results!.Select(result => result.UserId));

        Assert.Equal(204, allocations.Settle(new AllocationSettleRequest(Ticket, allocation.MatchId, [42, 99])).StatusCode);
        Assert.Equal(204, allocations.Settle(new AllocationSettleRequest(Ticket, allocation.MatchId, [42])).StatusCode);
        Assert.Null(allocations.Status(new AllocationStatusRequest(null, Ticket)).Allocation!.Results);
        Assert.False(allocations.Closed);

        _worlds.Left.Add(43);
        RaidResult result = Assert.Single(allocations.Status(new AllocationStatusRequest(null, Ticket)).Allocation!.Results!);
        Assert.Equal((43L, RaidOutcomes.Extracted), (result.UserId, result.Outcome));
        Assert.False(allocations.Closed);
        Assert.Equal(204, allocations.Settle(new AllocationSettleRequest(Ticket, allocation.MatchId, [43])).StatusCode);
        Assert.True(allocations.Closed);
        Assert.Equal([allocation.MatchId], _worlds.Released);
    }

    [Fact]
    public void Unsettled_results_are_dropped_and_logged_after_the_settle_timeout()
    {
        using StringWriter output = new();
        RaidAllocations allocations = new(s_endpoint, _worlds, () => _now[0], settleTimeout: 30, log: new JsonEventLog(output));
        Allocation allocation = allocations.Reserve(Request()).Allocation!;
        _worlds.Ends[42] = new PlayerRaidEnd(RaidOutcomes.Lost, []);
        _now[0] = 10;
        _worlds.Left.Add(42);
        Assert.False(allocations.Closed);
        _now[0] = 39;
        Assert.False(allocations.Closed);
        Assert.Empty(output.ToString());

        _now[0] = 40;
        Assert.True(allocations.Closed);
        Assert.Equal([allocation.MatchId], _worlds.Released);
        Assert.Contains("raid_results_unsettled", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void An_expired_reservation_keeps_its_results_until_settled_without_expiring_again()
    {
        RaidAllocations allocations = Allocations();
        Allocation allocation = allocations.Reserve(Request()).Allocation!;
        _worlds.Ends[42] = new PlayerRaidEnd(RaidOutcomes.Lost, []);
        _now[0] = 121;
        Assert.Null(allocations.Route(Convert.FromBase64String(allocation.Gameserver.RoutingToken), renew: true));
        Assert.Equal(RaidOutcomes.Lost, Assert.Single(allocations.Status(new AllocationStatusRequest(null, Ticket)).Allocation!.Results!).Outcome);
        _now[0] = 500;
        Assert.False(allocations.Closed);
        Assert.Equal([42L], _worlds.Expired);
        Assert.Empty(_worlds.Released);

        Assert.Equal(204, allocations.Settle(new AllocationSettleRequest(Ticket, allocation.MatchId, [42])).StatusCode);
        Assert.True(allocations.Closed);
        Assert.Equal([allocation.MatchId], _worlds.Released);
    }

    private sealed class FakeWorlds : IWorldLifecycle
    {
        public List<string> Prepared { get; } = [];

        public List<string> Released { get; } = [];

        public HashSet<long> Left { get; } = [];

        public List<long> Expired { get; } = [];

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

        public List<string> Joined { get; } = [];

        public bool RefuseJoin { get; set; }

        public void Join(string matchId, IReadOnlyList<long> players, IReadOnlyList<PlayerLoadout>? loadouts = null, IReadOnlyList<CompactPlayerProfile>? profiles = null)
        {
            if (RefuseJoin)
            {
                throw new WorldPreparationException("test");
            }

            Joined.Add(matchId);
        }

        public void Leave(string matchId, IReadOnlyList<long> players)
        {
            Expired.AddRange(players);
            Left.UnionWith(players);
        }

        public void Release(string matchId) => Released.Add(matchId);

        public bool AcceptingPlayers(string matchId) => Prepared.Contains(matchId) && !Released.Contains(matchId);

        public bool HasLeft(string matchId, long player) => Left.Contains(player);

        public Dictionary<long, PlayerRaidEnd> Ends { get; } = [];

        public PlayerRaidEnd? Result(string matchId, long player) => Left.Contains(player) ? Ends.GetValueOrDefault(player) : null;
    }
}
