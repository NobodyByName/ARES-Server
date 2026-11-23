using System.Globalization;
using Ares.Raid.Transport;
using Ares.Raid.World.Movement;
using Ares.Raid.World.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Game.Replication;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.World.Players;

/// <summary>
/// One player's actor channels after Join: exports, controller/player state and HUD before world play,
/// then the squad, character, components and restart; after possession, input acknowledgements, the first blackboard frame,
/// level streaming requests, ability grants, the doors and ladders of visible cells, the loot containers, the items
/// lying in the world, the extraction elevators, and the other players of the raid with their movement; once an elevator leaves with the
/// player, the end of their round.
/// </summary>
public sealed partial class PlayerChannels : IPlayerChannels, IPlayerChannelsContext
{
    // Time after a breach succeeds during which no pawn frame is sent, so the client keeps its Succeeded
    // interaction event long enough to play the ending.
    internal const double BreachEndingSeconds = 2.0;

    internal const int MaxLootStartsPerCheck = 8;
    internal const double LootRelevanceInterval = 0.25;
    internal const int InteractionRequestLog = 40;

    private const double LevelStreamingSettleSeconds = 5.0;
    private const double AsyncLoadInterval = 2.0;
    private const int AsyncLoadLimit = 30;
    private const double WorldTimeInterval = 1.0;
    private const int AbilityExportsPerBunch = 3;
    private const ulong FirstAbilityGuid = 0x100001;

    private readonly RaidWorld _raid;
    private readonly RaidPlayer _member;
    private readonly WorldPlayer _player;
    private readonly PlayerLogin _login;
    private readonly (int Channel, ulong Guid)[] _bindings;

    private HashSet<ulong> _mapped = [];
    private HashSet<string> _visibleLevels = new(StringComparer.Ordinal);
    private double? _visibleLevelsChangedAt;
    private bool _surrenderToCommit;
    private bool _surrendered;
    private Dictionary<string, int> _ignoredCalls = new(StringComparer.Ordinal);

    private InputHistory _input = new();
    private ReliableKey[] _inputAckKeys = [];
    private bool _correctionExported;
    private uint? _inputAckSent;
    private double? _asyncLoadAt;
    private int _asyncLoads;
    private ReliableKey[] _abilityKeys = [];
    private ulong[]? _abilityDefaults;
    private bool _abilitiesGranted;
    private double _pawnSpawnedAt;
    private bool _spawnInEffectRemoved;
    private ReliableKey? _worldTimeKey;
    private double? _worldTimeSentAt;

    private List<InteractionRequest> _requests = [];
    private List<InventoryRequest> _inventoryRequests = [];
    private List<WeaponInput> _weaponRequests = [];
    private PlayerPawnFrames _frames = new();
    private BoundedLog _unknownRequests = new(InteractionRequestLog);
    private BoundedLog _rejectedInventory = new(InteractionRequestLog);
    private InteractionRequest? _lastLoggedRequest;
    private PlayerDoors _doors;
    private PlayerLadders _ladders;
    private PlayerLoot _loot;
    private PlayerPickups _pickups;
    private PlayerArcs _arcs;
    private PlayerElevators _elevators;
    private PlayerObserved _observed = new();
    private PlayerDeparture _departure = new();

    /// <summary>Creates the channels of a joined player.</summary>
    /// <param name="raid">The raid.</param>
    /// <param name="member">The player's raid state: inventory, weapons and profiles.</param>
    /// <param name="player">The player's actors.</param>
    /// <param name="login">What the player's login gives its actors.</param>
    public PlayerChannels(RaidWorld raid, RaidPlayer member, WorldPlayer player, PlayerLogin login)
    {
        _raid = raid ?? throw new ArgumentNullException(nameof(raid));
        _member = member ?? throw new ArgumentNullException(nameof(member));
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _login = login ?? throw new ArgumentNullException(nameof(login));
        _bindings = [(ChannelLayout.Controller, player.Controller), (ChannelLayout.PlayerState, player.PlayerState), (ChannelLayout.Pawn, player.Pawn)];
        _doors = new PlayerDoors(raid.Channels.Doors);
        _ladders = new PlayerLadders(raid.Channels.Ladders);
        _loot = new PlayerLoot(raid.Channels.Loot);
        _pickups = new PlayerPickups(raid.Channels.Pickups);
        _arcs = new PlayerArcs(raid.Channels.Arcs);
        _elevators = new PlayerElevators(raid.Channels.Elevators, raid.GameData.Map);
    }

    /// <summary>Latest client input frame received.</summary>
    public uint? InputFrame { get; private set; }

    // The families of world actors on this connection, in the order they are serviced.
    private IPlayerFamily[] Families => [_doors, _ladders, _loot, _pickups, _arcs, _elevators, _observed];

    // The families in the order they take interaction requests: the doors come last and take whatever the others left.
    private IPlayerFamily[] RequestOrder => [_loot, _pickups, _ladders, _elevators, _doors];

    /// <inheritdoc />
    RaidWorld IPlayerChannelsContext.Raid => _raid;

    /// <inheritdoc />
    RaidPlayer IPlayerChannelsContext.Member => _member;

    /// <inheritdoc />
    WorldPlayer IPlayerChannelsContext.Player => _player;

    /// <inheritdoc />
    Vector3D IPlayerChannelsContext.RelevancePosition => RelevancePosition;

    /// <inheritdoc />
    bool IPlayerChannelsContext.InteractorExported => _frames.InteractorExported;

    // Where the player is for relevance: an operator teleport's destination, else the latest reported position, else
    // the spawn.
    private Vector3D RelevancePosition => _teleport is { } teleport ? teleport.Position
        : InputFrame is { } latest && _input.Position(latest) is { } position ? position : _member.Spawn.Location;

    /// <inheritdoc />
    public IPlayerChannels Copy()
    {
        PlayerChannels copy = (PlayerChannels)MemberwiseClone();
        copy._mapped = [.. _mapped];
        copy._visibleLevels = new HashSet<string>(_visibleLevels, StringComparer.Ordinal);
        copy._ignoredCalls = new Dictionary<string, int>(_ignoredCalls, StringComparer.Ordinal);
        copy._input = _input.Copy();
        copy._requests = [.. _requests];
        copy._inventoryRequests = [.. _inventoryRequests];
        copy._weaponRequests = [.. _weaponRequests];
        copy._unknownRequests = _unknownRequests.Copy();
        copy._rejectedInventory = _rejectedInventory.Copy();
        copy._doors = _doors.Copy();
        copy._ladders = _ladders.Copy();
        copy._loot = _loot.Copy();
        copy._pickups = _pickups.Copy();
        copy._arcs = _arcs.Copy();
        copy._elevators = _elevators.Copy();
        copy._observed = _observed.Copy();
        copy._frames = _frames.Copy();
        copy._departure = _departure.Copy();
        return copy;
    }

    /// <inheritdoc />
    public void Acknowledge(ChannelIo io, NotificationHeader header)
    {
        ArgumentNullException.ThrowIfNull(io);
        io.Sender.Acknowledge(header);
        while (Advance(io.Sender))
        {
        }

        // Accept client bunches on a channel once its own open is acknowledged.
        foreach ((int channel, _) in _bindings)
        {
            if (io.Sender.OpenAcknowledged.Contains(channel) && !io.Receiver.IsRegistered(channel))
            {
                io.Receiver.RegisterActor(channel);
            }
        }
    }

    /// <inheritdoc />
    public void Service(ChannelIo io, double now)
    {
        ArgumentNullException.ThrowIfNull(io);
        ServiceWorldTime(io.Sender, now);
        if (Phase < DeliveryPhase.PlayerOpensReady)
        {
            return;
        }

        if (Phase != DeliveryPhase.Possessed)
        {
            // Map actors (doors, loot, other players...) replicate while the client loads,
            // once the HUD is assigned: their BeginPlay sets up screen helpers. Using them waits for possession.
            foreach (IPlayerFamily family in Families)
            {
                family.Service(this, io.Sender, now);
            }

            return;
        }

        if (_departure.Departed)
        {
            _departure.Service(this, _elevators, io.Sender, now);
            return;
        }

        ServiceInputAcknowledgement(io.Sender);
        if (!ServiceTeleport(io.Sender, now))
        {
            _frames.Service(this, _input, io.Sender, now);
        }

        ServiceStreaming(io.Sender, now);
        ServiceSpawnInEffect(io.Sender, now);
        ServiceAbilities(io.Sender);
        foreach (IPlayerFamily family in Families)
        {
            family.Service(this, io.Sender, now);
        }

        _departure.Service(this, _elevators, io.Sender, now);
    }

    /// <inheritdoc />
    public double ServerSeconds(double now) => _raid.WorldSeconds(now);

    /// <inheritdoc />
    public void ActorChannelFailed(int channel)
    {
        // The family whose channel it is takes it.
        _ = Array.Exists(Families, family => family.ChannelFailed(this, channel));
    }

    /// <inheritdoc />
    public void Commit()
    {
        if (_surrenderToCommit)
        {
            _surrendered = true;
            _surrenderToCommit = false;
        }

        if (_possessionToCommit)
        {
            _raid.State.AcknowledgePossession(_player);
            _possessionToCommit = false;
        }

        double now = _raid.Clock();
        _raid.Pickups.Expire(now);
        if (_departure.Departed)
        {
            // The player left with an elevator: their input no longer acts or moves in the raid.
            _requests.Clear();
            _inventoryRequests.Clear();
            _weaponRequests.Clear();
            return;
        }

        foreach (InteractionRequest request in _requests)
        {
            HandleRequest(request, now);
        }

        _requests.Clear();
        _doors.CancelBreachOnMovement(this, now);
        _elevators.CancelDescentOnMovement(this);
        if (Phase == DeliveryPhase.Possessed)
        {
            foreach ((uint frame, InventoryCommand command) in _inventoryRequests)
            {
                if (_member.Inventory.Apply(command, out string? rejection))
                {
                    InventoryMoves++;
                    _pickups.PlaceDrops(this, now);
                }
                else
                {
                    RejectedInventoryRequests++;
                    _rejectedInventory.Add($"{command.Type} {Slot(command.From)}->{Slot(command.To)} x{command.Amount} {rejection}@{frame}");
                }

                _frames.InventoryChangedAt(frame);
            }
        }

        _inventoryRequests.Clear();
        if (Phase == DeliveryPhase.Possessed && _frames.Frame != 0 && InputFrame is { } latest)
        {
            // Late input frames revise the ticks they reach, even when no newer frame arrived. Each newly simulated frame
            // moves with the weapon its own input left in the character's hands, and an equip request waits while the
            // frame before it blocks equipping.
            _frames.Movement?.Revise(_input);
            Queue<WeaponInput> weapons = new(_weaponRequests.Where(input => input.Frame < latest).OrderBy(input => input.Frame));
            while (_frames.Movement is { } movement && movement.Frame + 1 < latest)
            {
                while (weapons.TryPeek(out WeaponInput input) && input.Frame <= movement.Frame + 1)
                {
                    _member.Weapons.Apply(weapons.Dequeue(), _frames.ItemsLinked ? _player.HeldItems : null, movement.EquipBlocked);
                }

                movement.Advance(_input, _member.Weapons.Armed, _member.Weapons.HeldItem);
            }

            foreach (WeaponInput input in weapons)
            {
                _member.Weapons.Apply(input, _frames.ItemsLinked ? _player.HeldItems : null, _frames.Movement?.EquipBlocked ?? false);
            }

            _weaponRequests.RemoveAll(input => input.Frame < latest);
        }
        else
        {
            _weaponRequests.Clear();
        }

        _frames.PublishMotion(this, _input, now);
    }

    /// <inheritdoc />
    public void Closed()
    {
        // Whatever the player searched or had open is free for the others again.
        _loot.Release(this);
        if (_surrendered || _departure.Departed)
        {
            // Keep the reservation valid while the reliable return-to-menu reply (or the end of round) drains.
            // Only an accepted surrender or a departure with an elevator, followed by connection removal, takes the
            // player out of the raid.
            _member.Leave();
        }
    }

    private void HandleRequest(InteractionRequest request, double now)
    {
        // The loot sees every request first: a looting-menu close ends there, before its repeat is counted.
        if (_loot.ClosesMenu(this, request))
        {
            return;
        }

        // A held press repeats on the following input frames; it is logged once.
        bool repeated = _lastLoggedRequest is { } logged && logged.Frame + 1 == request.Frame && logged.Target == request.Target && logged.Request == request.Request;
        _lastLoggedRequest = request;
        if (request.Target == 0)
        {
            if ((request.Request & PendulumInput.PressRequest) != 0 && !repeated)
            {
                _unknownRequests.Add($"0x0 {request.Request}@{request.Frame}");
            }

            return;
        }

        // The families take the request in turn until one ends it.
        foreach (IPlayerFamily family in RequestOrder)
        {
            if (family.TryHandle(this, request, repeated, now))
            {
                break;
            }
        }
    }

    private void ServiceInputAcknowledgement(ReliableSender sender)
    {
        if (InputFrame is not { } frame || _inputAckKeys.Any(sender.Pending))
        {
            return;
        }

        if (!_correctionExported)
        {
            (ObjectExports exports, ulong correction) = WorldState.CorrectionExports(_player);
            _inputAckKeys = [sender.Queue(Bunches.Exports(exports, [correction]))];
            _correctionExported = true;
            return;
        }

        if (_inputAckSent == frame)
        {
            return;
        }

        StableSubobjectContent content = _raid.State.CorrectionContent(_player, frame, _inputAckSent is null);
        _inputAckKeys = [sender.Queue(Bunches.Actor(ChannelLayout.Controller, content.Write))];
        _inputAckSent = frame;
    }

    // The game state's world time, once its channel is open and then every second: the client's server clock
    // (raid timers, cooldowns) follows it.
    private void ServiceWorldTime(ReliableSender sender, double now)
    {
        if (Phase < DeliveryPhase.PlayerOpensReady || (_worldTimeKey is { } key && sender.Pending(key))
            || (_worldTimeSentAt is { } sent && now - sent < WorldTimeInterval))
        {
            return;
        }

        _worldTimeKey = sender.Queue(Bunches.Actor(ChannelLayout.GameState, _raid.State.WorldTime(_raid.WorldSeconds(now))));
        _worldTimeSentAt = now;
    }

    // Asks the client to finish loading the levels around it, while streaming still changes and before the
    // character simulation starts; the client decides what it loads.
    private void ServiceStreaming(ReliableSender sender, double now)
    {
        if (_frames.Frame != 0 || _asyncLoads >= AsyncLoadLimit
            || (_asyncLoadAt is { } last && (now - last < AsyncLoadInterval || (_visibleLevelsChangedAt is { } changed && now - changed >= LevelStreamingSettleSeconds))))
        {
            return;
        }

        RpcCodec calls = _raid.State.Replication.Calls(_raid.State.Objects(_mapped));
        sender.Queue(Bunches.Actor(ChannelLayout.Controller, calls.Call(PlayerClasses.PlayerController.Class, FunctionNames.ClientSetBlockOnAsyncLoading, new Dictionary<string, object?>(), RpcDirection.Client)));
        _asyncLoadAt = now;
        _asyncLoads++;
    }

    // Ends the spawn-in effect once it has lasted its time, which returns the character's input. The client has
    // possessed the character by then, so its body stays turned to the view direction it possessed with.
    private void ServiceSpawnInEffect(ReliableSender sender, double now)
    {
        if (_spawnInEffectRemoved || now - _pawnSpawnedAt < SpawnInEffect.Seconds)
        {
            return;
        }

        sender.Queue(Bunches.Actor(ChannelLayout.Pawn, _raid.State.SpawnInEffectRemoved(_player).Write));
        _spawnInEffectRemoved = true;
    }

    // Exports the class defaults of the Raider's abilities, then grants them.
    private void ServiceAbilities(ReliableSender sender)
    {
        if (_abilitiesGranted || _abilityKeys.Any(sender.Pending))
        {
            return;
        }

        if (_abilityDefaults is null)
        {
            (ObjectExports exports, ulong[] defaults, _) = ClassDefaultExports();
            _abilityKeys = [.. defaults.Chunk(AbilityExportsPerBunch).Select(group => sender.Queue(Bunches.Exports(exports, group)))];
            _abilityDefaults = defaults;
            return;
        }

        GrantedAbility[] abilities = [.. _abilityDefaults.Select((guid, index) => new GrantedAbility(index + 1, index + 1, guid))];
        _abilityKeys = [sender.Queue(Bunches.Actor(ChannelLayout.Pawn, _raid.State.AbilityGrants(_player, abilities).Write))];
        _abilitiesGranted = true;
    }

    // The class defaults of the Raider's abilities and of the spawn-in effect, with the packages they are in.
    private (ObjectExports Exports, ulong[] Abilities, ulong SpawnInEffect) ClassDefaultExports()
    {
        IReadOnlyCollection<ObjectExport> existing = _raid.Imports.Exports.Records.Values as IReadOnlyCollection<ObjectExport> ?? [.. _raid.Imports.Exports.Records.Values];
        Dictionary<string, StaticObjectExport> packages = existing.OfType<StaticObjectExport>().Where(record => record.OuterGuid == 0)
            .GroupBy(record => record.Path, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        uint packageChecksum = _raid.State.Replication.Catalog.Class(ClassPaths.Package).Checksum;
        List<StaticObjectExport> records = [];
        List<ulong> defaults = [];
        ulong guid = FirstAbilityGuid;
        foreach (AbilityGrant grant in DefaultAbilities.Raider.Append(SpawnInEffect.ClassDefault))
        {
            if (!packages.TryGetValue(grant.Package, out StaticObjectExport? package))
            {
                package = new StaticObjectExport(guid, 0, grant.Package, packageChecksum, false);
                packages[grant.Package] = package;
                guid += 2;
            }

            records.Add(package);
            StaticObjectExport classDefault = new(guid, package.Guid, $"Default__{grant.ClassName}", grant.ExportChecksum, false);
            guid += 2;
            records.Add(classDefault);
            defaults.Add(classDefault.Guid);
        }

        return (new ObjectExports(records), [.. defaults[..^1]], defaults[^1]);
    }

    /// <inheritdoc />
    void IPlayerChannelsContext.QueueControllerCall(ReliableSender sender, string function, Dictionary<string, object?> parameters) =>
        sender.Queue(Bunches.Actor(ChannelLayout.Controller, _raid.State.Replication.Calls(_raid.State.Objects(_mapped))
            .Call(PlayerClasses.PlayerController.Class, function, parameters, RpcDirection.Client)));

    internal static string Seconds(double value) => value.ToString("0.000", CultureInfo.InvariantCulture);

    // Whether the player gave movement input after an input frame.
    private bool MovedSince(uint frame)
    {
        for (uint next = frame + 1; next <= (InputFrame ?? frame); next++)
        {
            if (_input.Movement(next) is { Forward: not 0 } or { Right: not 0 })
            {
                return true;
            }
        }

        return false;
    }

    private static string Slot(ItemContainerSlot slot) => $"{slot.Parent.Id}r{slot.Parent.Revision}/{slot.Index}";

    // Whether an id names an interaction the server gave the client: a door, ladder, loot, elevator button or pickup.
    private bool KnownInteraction(ulong target) =>
        _raid.Doors.ByButton(target) is not null || _raid.Doors.ByBreach(target) is not null || _raid.Loot.ByInteraction(target) is not null
        || _raid.Extraction.ByButton(target) is not null || _raid.Ladders.Ladders.Any(ladder => ladder.Interactions.Contains(target))
        || _raid.Pickups.ByTarget(target) is not null;

    /// <inheritdoc />
    void IPlayerChannelsContext.InventoryChangedAt(uint frame) => _frames.InventoryChangedAt(frame);

    /// <inheritdoc />
    bool IPlayerChannelsContext.LevelVisible(string level) => _visibleLevels.Contains(level);

    /// <inheritdoc />
    bool IPlayerChannelsContext.MovedSince(uint frame) => MovedSince(frame);

    /// <inheritdoc />
    void IPlayerChannelsContext.MapObjects(IEnumerable<ulong> objects) => _mapped.UnionWith(objects);

    /// <inheritdoc />
    void IPlayerChannelsContext.HoldFrames(double until) => _frames.HoldFrames(until);

    /// <inheritdoc />
    void IPlayerChannelsContext.ExportInteractor(ReliableSender sender) => _frames.ExportInteractor(this, sender);

    /// <inheritdoc />
    void IPlayerChannelsContext.RunInteraction(PawnInteraction interaction) => _frames.RunInteraction(interaction);

    /// <inheritdoc />
    void IPlayerChannelsContext.SucceedInteraction() => _frames.SucceedInteraction();

    /// <inheritdoc />
    uint IPlayerChannelsContext.NextInteractionSequence() => _frames.NextInteractionSequence();

    /// <inheritdoc />
    void IPlayerChannelsContext.ExtendFrameHold(double until) => _frames.ExtendFrameHold(until);

    /// <inheritdoc />
    void IPlayerChannelsContext.Climb(uint frame, int ladder, bool topEntry) => _frames.Movement?.Climb(frame, ladder, topEntry);

    /// <inheritdoc />
    bool IPlayerChannelsContext.FocusUsable(ulong target) => Array.Exists(Families, family => family.Usable(this, target));

    /// <inheritdoc />
    bool IPlayerChannelsContext.InteractionRunning => _doors.Breaching || _loot.Searching || _elevators.Descending;

    /// <inheritdoc />
    void IPlayerChannelsContext.LogUnhandled(InteractionRequest request)
    {
        if (!KnownInteraction(request.Target))
        {
            _unknownRequests.Add($"0x{request.Target:x} {request.Request}@{request.Frame}");
        }
    }

    private WorldActor Actor(ulong guid) => _raid.State.Actor(guid);

    private int ChannelOf(ulong actor) => Array.Find(_bindings, binding => binding.Guid == actor).Channel;
}
