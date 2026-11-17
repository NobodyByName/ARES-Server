using System.Globalization;
using System.Text.RegularExpressions;
using Ares.Raid.Transport;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Movement;
using Ares.Raid.World.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.GameplayTags;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.World.Players;

/// <summary>Where a player's actor delivery stands.</summary>
public enum DeliveryPhase
{
    /// <summary>Nothing sent yet.</summary>
    NotStarted,

    /// <summary>Static exports sent; waiting for their acknowledgement.</summary>
    ExportsPending,

    /// <summary>Controller and player-state channels sent; waiting for their acknowledgement.</summary>
    ActorOpensPending,

    /// <summary>HUD assignment sent; waiting for acknowledgement before starting world play.</summary>
    HudAssignmentPending,

    /// <summary>Opening the world actors, one at a time.</summary>
    WorldOpensPending,

    /// <summary>World actors delivered; the squad can open.</summary>
    PlayerOpensReady,

    /// <summary>Squad channel sent; waiting for its acknowledgement.</summary>
    SquadOpenPending,

    /// <summary>Squad member created; waiting for its acknowledgement.</summary>
    SquadMemberPending,

    /// <summary>Squad and game state roster sent; waiting for their acknowledgement.</summary>
    SquadPropertiesPending,

    /// <summary>Player actor properties sent.</summary>
    PlayerPropertiesPending,

    /// <summary>Player actor properties delivered.</summary>
    PlayerPropertiesDelivered,

    /// <summary>Appearance component exported.</summary>
    AppearanceExportsPending,

    /// <summary>Appearance content sent.</summary>
    AppearanceContentPending,

    /// <summary>Inventory component exported; waiting for acknowledgement.</summary>
    InventoryExportsPending,

    /// <summary>Inventory handles sent; waiting for acknowledgement.</summary>
    InventoryContentPending,

    /// <summary>Appearance delivered.</summary>
    PlayerLinksReady,

    /// <summary>Ability system component exported.</summary>
    AbilityExportsPending,

    /// <summary>Ability system actor info sent.</summary>
    AbilityComponentPending,

    /// <summary>Attribute sets created.</summary>
    AbilityCreationPending,

    /// <summary>Attribute sets registered.</summary>
    AbilityRegistrationPending,

    /// <summary>Ability system delivered.</summary>
    AbilityDelivered,

    /// <summary>Controller and player state delivered; waiting for the initial levels before creating the character.</summary>
    InitialLevelsPending,

    /// <summary>The character's channel opened after loading; waiting for acknowledgement.</summary>
    PawnOpenPending,

    /// <summary>The character's initial properties sent.</summary>
    PawnPropertiesPending,

    /// <summary>Restart sent; waiting for the client to confirm possession.</summary>
    PossessionPending,

    /// <summary>The client possesses its character.</summary>
    Possessed,
}

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
    private const int TelemetryBitLimit = 256;
    private const double WorldTimeInterval = 1.0;
    private const int AbilityExportsPerBunch = 3;
    private const ulong FirstAbilityGuid = 0x100001;
    private const ulong PathReference = 1;

    // ARES's own choice, not a game value: how long the server waits after the end-of-round transition for the client
    // to say it is ready for the end of the round before sending it back to the main menu instead.
    private const double DepartureFallbackSeconds = 20.0;

    private static readonly HashSet<string> s_startupCalls = new(StringComparer.Ordinal)
    {
        FunctionNames.ServerAcknowledgePossession,
        FunctionNames.ServerUpdateLevelVisibility,
        FunctionNames.ServerUpdateMultipleLevelsVisibility,
        FunctionNames.ServerShortTimeout,
        FunctionNames.ServerNotifyHasReplicatedPlayerState,
        FunctionNames.ServerNotifyHasReplicatedGameModeInfo,
        FunctionNames.ServerNotifyHasLoadedInitialLevels,
        FunctionNames.ServerSetPlayerInitAllAttributeSetsReplicated,
        FunctionNames.ServerSetPlayerInitClientIsListeningToAttributeSetRegistration,
        FunctionNames.ServerUpdateLoginResult,
        FunctionNames.ServerSendClientTelemetryInfo,
        FunctionNames.ServerSurrender,
        FunctionNames.ServerRequestSquadPlayersProfiles,
    };

    private readonly RaidWorld _raid;
    private readonly RaidPlayer _member;
    private readonly WorldPlayer _player;
    private readonly PlayerLogin _login;
    private readonly (int Channel, ulong Guid)[] _bindings;

    private ReliableKey[] _keys = [];
    private int _worldOpenIndex;
    private HashSet<ulong> _mapped = [];
    private HashSet<string> _visibleLevels = new(StringComparer.Ordinal);
    private double? _visibleLevelsChangedAt;
    private bool _possessionToCommit;
    private bool _surrenderToCommit;
    private bool _surrendered;
    private bool _initialLevelsLoaded;
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

    // This player's departure with an elevator (its index and the server world time it left), the export naming the
    // controller's end-of-round component, and the server world times of each end-of-round step.
    private (int Elevator, double At)? _departure;
    private ReliableKey? _endOfRoundExport;
    private double? _transitionAt;
    private double? _readyAt;
    private double? _endAt;
    private double? _fallbackAt;

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
        _elevators = new PlayerElevators(raid.Channels.Elevators);
    }

    /// <summary>Where the actor delivery stands.</summary>
    public DeliveryPhase Phase { get; private set; }

    /// <summary>Latest client input frame received.</summary>
    public uint? InputFrame { get; private set; }

    /// <summary>Input payloads that could not be decoded (they never reject the packet carrying them).</summary>
    public int InputDecodeFailures { get; private set; }

    /// <summary>Reason the latest undecoded input payload was rejected, if any.</summary>
    public string? LastInputDecodeError { get; private set; }

    /// <summary>The newest weapon controls the client sent: what it holds or wants to equip.</summary>
    public WeaponInput? LatestWeaponInput { get; private set; }

    /// <summary>The object the client focuses in its latest input frame (its prompt shows), or 0.</summary>
    public ulong InputFocus => _input.Focus;

    /// <summary>The character position the client predicted in its latest input frame, if any.</summary>
    public Vector3D? InputPosition => InputFrame is { } frame ? _input.Position(frame) : null;

    /// <summary>The heading of the client's control rotation in its latest input frame, in radians, if any.</summary>
    public double? InputYaw => InputFrame is { } frame ? _input.Yaw(frame) : null;

    /// <summary>Pawn blackboard frames sent (the first frame and focus updates).</summary>
    public int BlackboardFrames => _frames.BlackboardFrames;

    /// <summary>
    /// The character's time offset in its newest frame (see <see cref="PawnBlackboard.TimeOffsetField"/>), or null
    /// before its first frame.
    /// </summary>
    public long? TimeOffset => _frames.TimeOffset;

    /// <summary>The character's simulated state after the latest simulated input frame, or null before the reset frame.</summary>
    public CharacterState? SimulatedCharacter => _frames.Movement?.Latest;

    /// <summary>Input frames the character simulation ran without the client's input.</summary>
    public int MissingInputFrames => _frames.Movement?.MissingFrames ?? 0;

    /// <summary>Simulated frames after which the character continued from the position its client reported.</summary>
    public int PositionReseeds => _frames.Movement?.Reseeds ?? 0;

    /// <summary>The latest such frame: its number, how far the simulation was off and whether it had the character in the air.</summary>
    public (uint Frame, double Distance, bool Airborne)? LastPositionReseed => _frames.Movement?.LastReseed;

    /// <summary>Largest distance the simulation was off on such a frame.</summary>
    public double WorstPositionReseed => _frames.Movement?.WorstReseed ?? 0;

    /// <summary>Door presses applied.</summary>
    public int DoorPresses => _doors.Presses;

    /// <summary>Door requests ignored (not a press, door not open on this connection, or still moving).</summary>
    public int IgnoredDoorRequests => _doors.IgnoredRequests;

    /// <summary>Door breaches started.</summary>
    public int DoorBreaches => _doors.Breaches;

    /// <summary>Door breaches this player stopped by moving.</summary>
    public int CancelledBreaches => _doors.CancelledBreaches;

    /// <summary>Request values received on breach buttons, with their counts (a hold sends more than a press).</summary>
    public IReadOnlyDictionary<int, int> BreachRequests => _doors.BreachRequests;

    /// <summary>Whole-stack inventory swaps accepted.</summary>
    public int InventoryMoves { get; private set; }

    public int ShotsFired => _member.Weapons.ShotsFired;
    public int ReloadsCompleted => _member.Weapons.ReloadsCompleted;

    /// <summary>Accepted requests to draw a quick-use item.</summary>
    public int QuickItemRequests => _member.Weapons.QuickItemRequests;

    /// <summary>Snap Hook throws (each costs gadget power).</summary>
    public int HookThrows => _member.Weapons.HookThrows;

    /// <summary>Times pawn frames started being withheld because a thrown Snap Hook was out.</summary>
    public int QuickItemHolds => _frames.QuickItemHolds;

    /// <summary>The latest such hold: the first and the last withheld frame tick (none while it lasts).</summary>
    public (uint From, uint? To)? LastQuickItemHold => _frames.LastQuickItemHold;

    /// <summary>Inventory requests rejected and corrected.</summary>
    public int RejectedInventoryRequests { get; private set; }

    /// <summary>
    /// The latest rejected inventory requests, oldest first, as type from->to (container r revision/slot) xamount
    /// reason@input frame.
    /// </summary>
    public IReadOnlyList<string> RejectedInventoryLog => _rejectedInventory;

    /// <summary>Ladders whose climbable actor and interactions this client has.</summary>
    public int LaddersReady => _ladders.Ready;

    /// <summary>Ladders whose delivery started and has not finished.</summary>
    public int LaddersInProgress => _ladders.InProgress;

    /// <summary>Loot containers whose actor and search interaction this client has.</summary>
    public int LootReady => _loot.Ready;

    /// <summary>Whether this client has a loot container (it keeps it while the container is dormant).</summary>
    /// <param name="container">Container index.</param>
    /// <returns>True once the container's state reached the client.</returns>
    public bool HasLoot(int container) => _loot.Has(container);

    /// <summary>Loot channels open now (or on the way): containers being delivered or updated.</summary>
    public int LootChannelsInUse => _loot.ChannelsInUse;

    /// <summary>Loot channel opens so far: first deliveries and state changes.</summary>
    public int LootChannelOpens => _loot.ChannelOpens;

    /// <summary>Loot containers whose channel the client closed: they are not delivered again to this player.</summary>
    public int LootFailed => _loot.Failed;

    /// <summary>
    /// The first loot containers whose channel the client closed, oldest first, as source:class@stage (the step whose
    /// bunch the channel was on: Spawning = the open, Naming = the component names, Open = a state).
    /// </summary>
    public IReadOnlyList<string> LootFailures => _loot.Failures;

    /// <summary>Loot containers relevant to the player that the client does not have yet.</summary>
    public int LootWaiting => _loot.Waiting(this);

    /// <summary>First openings of the raid's containers with what they held, oldest first.</summary>
    public IReadOnlyList<string> LootEvents => _raid.LootStates.Events;

    /// <summary>Describes a container of the raid and what it holds now.</summary>
    /// <param name="index">Container index.</param>
    /// <returns>One line.</returns>
    public string DescribeLoot(int index) => _raid.LootStates.Describe(index);

    /// <summary>Match id of the raid.</summary>
    public string MatchId => _raid.MatchId;

    /// <summary>Searches started on loot containers.</summary>
    public int LootSearches => _loot.Searches;

    /// <summary>Searches the player stopped by moving.</summary>
    public int CancelledSearches => _loot.CancelledSearches;

    /// <summary>Loot containers opened by this player.</summary>
    public int LootOpens => _loot.Opens;

    /// <summary>Requests received on loot search interactions, oldest first, as request@input frame.</summary>
    public IReadOnlyList<string> LootRequests => _loot.Requests;

    /// <summary>Items this player dropped into the world.</summary>
    public int ItemsDropped => _pickups.ItemsDropped;

    /// <summary>Pickup requests of this player that took items.</summary>
    public int PickupsTaken => _pickups.Taken;

    /// <summary>Pickups whose actor and interaction this client has.</summary>
    public int PickupsReady => _pickups.Ready;

    /// <summary>Whether this client has a pickup (it keeps it while the pickup is dormant).</summary>
    /// <param name="pickup">Pickup index.</param>
    /// <returns>True once the pickup reached the client and until it is destroyed there.</returns>
    public bool HasPickup(int pickup) => _pickups.Has(pickup);

    /// <summary>Pickup channels open now (or on the way).</summary>
    public int PickupChannelsInUse => _pickups.ChannelsInUse;

    /// <summary>Requests received on pickup interactions, oldest first, as pickup:request@input frame.</summary>
    public IReadOnlyList<string> PickupRequests => _pickups.Requests;

    /// <summary>What happened to the raid's pickups, oldest first.</summary>
    public IReadOnlyList<string> PickupEvents => _raid.Pickups.Events;

    /// <summary>ARCs whose actor and ability system this client has.</summary>
    public int ArcsReady => _arcs.Ready;

    /// <summary>Whether this client has an ARC.</summary>
    /// <param name="arc">ARC index.</param>
    /// <returns>True from the ability system's content until the channel closes.</returns>
    public bool HasArc(int arc) => _arcs.Has(arc);

    /// <summary>ARC channels open now (or on the way).</summary>
    public int ArcChannelsInUse => _arcs.ChannelsInUse;

    /// <summary>ARCs whose channel the client refused, as index@stage.</summary>
    public IReadOnlyList<string> ArcFailures => _arcs.Failures;

    /// <summary>What happened to the raid's ARCs, oldest first.</summary>
    public IReadOnlyList<string> ArcEvents => _raid.Arcs.Events;

    /// <summary>Extraction elevators whose state and buttons this client has.</summary>
    public int ElevatorsReady => _elevators.Ready;

    /// <summary>
    /// Elevator states this client has, by elevator index: the state (Called while the call press runs) and the
    /// server world time it started, with the descend switch held (+switch@press, ~hold@hold start once the hold runs)
    /// or, once the elevator left, the one that sent it (^switch).
    /// </summary>
    public string ElevatorStates => _elevators.States;

    /// <summary>Calls of an elevator by this player.</summary>
    public int ElevatorCalls => _elevators.Calls;

    /// <summary>Elevator state changes this client got, oldest first, as elevator:state@server time (see <see cref="ElevatorStates"/>).</summary>
    public IReadOnlyList<string> ElevatorChanges => _elevators.Changes;

    /// <summary>Descend holds started by this player.</summary>
    public int ElevatorDescents => _elevators.Descents;

    /// <summary>Descend holds this player stopped by moving.</summary>
    public int CancelledDescents => _elevators.CancelledDescents;

    /// <summary>Descend holds of this player that sent an elevator off.</summary>
    public int CompletedDescents => _elevators.CompletedDescents;

    /// <summary>
    /// Players inside each elevator when it last left (its doors closed), as elevator@server time the doors started
    /// closing:user ids.
    /// </summary>
    public string ElevatorDepartures => string.Join(", ", _raid.Extraction.Points
        .Select(point => (point.Index, Departure: _raid.ExtractionStates.Departure(point.Index)))
        .Where(entry => entry.Departure is not null)
        .Select(entry => $"{entry.Index}@{entry.Departure!.At.ToString("0.000", CultureInfo.InvariantCulture)}:{string.Join(' ', entry.Departure.Players)}"));

    /// <summary>
    /// This player's departure with an elevator, as elevator@server time it left, then each end-of-round step with its
    /// server time: transition (the client starts its end of round), ready (the client is ready for it), end (the end
    /// of round sent) or fallback (no ready in time: sent back to the main menu). Empty before a departure.
    /// </summary>
    public string Departure => _departure is not { } departure ? string.Empty : string.Join(' ', ((string?[])[
        $"{departure.Elevator}@{Seconds(departure.At)}",
        _transitionAt is { } transition ? $"transition@{Seconds(transition)}" : null,
        _readyAt is { } ready ? $"ready@{Seconds(ready)}" : null,
        _endAt is { } end ? $"end@{Seconds(end)}" : null,
        _fallbackAt is { } fallback ? $"fallback@{Seconds(fallback)}" : null,
    ]).OfType<string>());

    /// <summary>Requests received on elevator buttons, oldest first, as elevator/button request@input frame.</summary>
    public IReadOnlyList<string> ElevatorRequests => _elevators.Requests;

    /// <summary>
    /// Presses on no target or on an object the server does not know, oldest first, as target request@input frame.
    /// </summary>
    public IReadOnlyList<string> UnknownInteractionRequests => _unknownRequests;

    /// <summary>Client calls received and not handled yet, by name.</summary>
    public IReadOnlyDictionary<string, int> IgnoredCalls => _ignoredCalls;

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

    /// <summary>Queues the static exports.</summary>
    /// <param name="io">The connection's channels.</param>
    public void Start(ChannelIo io)
    {
        ArgumentNullException.ThrowIfNull(io);
        if (Phase != DeliveryPhase.NotStarted)
        {
            throw new InvalidPacketException("Player delivery already started");
        }

        IEnumerable<ulong> roots = _raid.StaticActors.Select(actor => actor.Guid)
            .Concat(_raid.SpawnedActors.Select(actor => actor.Archetype))
            .Concat(_raid.SpawnedActors.SelectMany(actor => actor.RequiredGuids))
            .Concat(_bindings.Select(binding => _raid.Imports.Archetypes[_raid.State.Actor(binding.Guid).ClassPath]))
            .Append(_raid.Imports.Archetypes[PlayerClasses.Squad.Class]);
        roots = roots.Concat(_member.Inventory.Containers.SelectMany(container => container.Items).OfType<ContainerItem>()
            .Where(item => _player.HeldItems.ContainsKey(item.Id)).Select(item => _raid.Imports.Classes[WorldImports.HeldItemClasses[item.AssetIndex]]));

        _keys = [.. roots.Distinct().Select(root => io.Sender.Queue(Bunches.Exports(_raid.Imports.Exports, [root])))];
        Phase = DeliveryPhase.ExportsPending;
    }

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
    public void Receive(ChannelIo io, ChannelBunch bunch)
    {
        ArgumentNullException.ThrowIfNull(io);
        ArgumentNullException.ThrowIfNull(bunch);
        ChannelLayout layout = _raid.Channels;
        if (bunch.Channel == ChannelLayout.Squad || layout.Doors.Contains(bunch.Channel) || layout.Ladders.Contains(bunch.Channel)
            || layout.Loot.Contains(bunch.Channel) || layout.Elevators.Contains(bunch.Channel)
            || (layout.ObservedPlayer(bunch.Channel) is { } observed && observed < _raid.Players.Count) || layout.Pickups.Contains(bunch.Channel))
        {
            // The client owns no squad, door, ladder, container, elevator, other player or pickup; it only sends channel bookkeeping there.
            return;
        }

        if (Array.FindIndex(_bindings, binding => binding.Channel == bunch.Channel) < 0
            || bunch.Opened || bunch.CloseReason is not null || bunch.PackageExports || bunch.MustBeMapped || bunch.ReplicationPaused)
        {
            throw new InvalidPacketException("Unsupported or unowned actor channel traffic");
        }

        List<(string Function, Dictionary<string, object?> Parameters)> replies = [];
        foreach (ChannelBunch item in io.Receiver.Receive(bunch))
        {
            if (Phase < DeliveryPhase.ActorOpensPending)
            {
                throw new InvalidPacketException("Actor traffic before the player actors opened");
            }

            BitReader reader = new(item.Payload, item.PayloadBits);
            ulong actor = Array.Find(_bindings, binding => binding.Channel == item.Channel).Guid;
            if (actor == _player.Controller)
            {
                ReceiveControllerCalls(item, reader, replies);
            }
            else if (actor == _player.Pawn && Phase == DeliveryPhase.Possessed)
            {
                ReceivePawnCalls(item, reader);
            }
            else
            {
                throw new InvalidPacketException($"Unsupported calls on an owned actor: {string.Join(", ", Calls(Actor(actor).ClassPath, ActorContentBlock.Read(reader)).Take(8))}");
            }
        }

        RpcCodec calls = _raid.State.Replication.Calls(_raid.State.Objects(_mapped));
        foreach ((string function, Dictionary<string, object?> parameters) in replies)
        {
            io.Sender.Queue(Bunches.Actor(ChannelLayout.Controller, calls.Call(PlayerClasses.PlayerController.Class, function, parameters, RpcDirection.Client)));
        }

        while (Advance(io.Sender))
        {
        }
    }

    /// <inheritdoc />
    public bool ObserveDirect(int kind, byte[] payload)
    {
        if (kind != PendulumInput.DirectStreamKind)
        {
            return false;
        }

        try
        {
            (uint? latest, IReadOnlyList<InteractionRequest> requests, IReadOnlyList<InventoryRequest> inventory) = _input.Read(payload);
            if (latest is { } frame && (InputFrame is null || frame > InputFrame))
            {
                InputFrame = frame;
            }

            _requests.AddRange(requests);
            _inventoryRequests.AddRange(inventory);
            _weaponRequests.AddRange(_input.Weapons);
            foreach (WeaponInput weapon in _input.Weapons)
            {
                if (LatestWeaponInput is not { } current || weapon.Frame > current.Frame)
                {
                    LatestWeaponInput = weapon;
                }
            }
        }
        catch (InvalidPacketException error)
        {
            // Never let an undecoded input payload reject the channel traffic it rides with.
            InputDecodeFailures++;
            LastInputDecodeError = error.Message;
        }

        return true;
    }

    /// <inheritdoc />
    public void Service(ChannelIo io, double now)
    {
        ArgumentNullException.ThrowIfNull(io);
        ServiceWorldTime(io.Sender, now);
        if (Phase != DeliveryPhase.Possessed)
        {
            return;
        }

        if (_departure is not null)
        {
            ServiceDeparture(io.Sender, now);
            return;
        }

        ServiceInputAcknowledgement(io.Sender);
        if (!ServiceTeleport(io.Sender, now))
        {
            _frames.Service(this, _input, io.Sender, now);
        }

        ServiceStreaming(io.Sender, now);
        ServiceAbilities(io.Sender);
        foreach (IPlayerFamily family in Families)
        {
            family.Service(this, io.Sender, now);
        }

        ServiceDeparture(io.Sender, now);
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
        if (_departure is not null)
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
            // moves with the weapon its own input left in the character's hands.
            _frames.Movement?.Revise(_input);
            Queue<WeaponInput> weapons = new(_weaponRequests.Where(input => input.Frame < latest).OrderBy(input => input.Frame));
            while (_frames.Movement is { } movement && movement.Frame + 1 < latest)
            {
                while (weapons.TryPeek(out WeaponInput input) && input.Frame <= movement.Frame + 1)
                {
                    _member.Weapons.Apply(weapons.Dequeue(), _frames.ItemsLinked ? _player.HeldItems : null);
                }

                movement.Advance(_input, _member.Weapons.Armed, _member.Weapons.HeldItem);
            }

            foreach (WeaponInput input in weapons)
            {
                _member.Weapons.Apply(input, _frames.ItemsLinked ? _player.HeldItems : null);
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
        if (_surrendered || _departure is not null)
        {
            // Keep the reservation valid while the reliable return-to-menu reply (or the end of round) drains.
            // Only an accepted surrender or a departure with an elevator, followed by connection removal, takes the
            // player out of the raid.
            _member.Leave();
        }
    }

    [GeneratedRegex("^/Game/[A-Za-z_0-9/]+$")]
    private static partial Regex PackagePattern();

    [GeneratedRegex("^[A-Za-z_0-9]{1,128}$")]
    private static partial Regex ComponentNamePattern();

    // One delivery step; returns whether the phase moved on.
    private bool Advance(ReliableSender sender)
    {
        bool delivered = _keys.All(key => !sender.Pending(key));
        switch (Phase)
        {
            case DeliveryPhase.ExportsPending when delivered:
                _keys =
                [
                    .. _bindings.Where(binding => binding.Guid != _player.Pawn).Select(binding =>
                    {
                        WorldActor actor = Actor(binding.Guid);
                        ActorSpawn spawn = new(binding.Guid, _raid.Imports.Archetypes[actor.ClassPath], 0, actor.Location, actor.Rotation);
                        PlayerControllerOpen? open = binding.Guid == _player.Controller ? _login.ControllerOpen() : null;
                        IReadOnlyList<ActorContentBlock> content = binding.Guid == _player.Controller ? [_raid.State.ControllerOpeningProperties(_player)] : [];
                        return sender.Queue(new ActorInitialization(spawn, open, content).ToBunch(binding.Channel, sender.NextSequence(binding.Channel), []));
                    }),
                ];
                Phase = DeliveryPhase.ActorOpensPending;
                return true;
            case DeliveryPhase.ActorOpensPending when delivered:
                _mapped = [.. _bindings.Where(binding => binding.Guid != _player.Pawn).Select(binding => binding.Guid)];
                // Spawn must finish assigning the HUD before world BeginPlay initializes its screen helpers.
                _keys = [sender.Queue(Bunches.Actor(ChannelLayout.Controller, _raid.State.HudAssignment(_player)))];
                Phase = DeliveryPhase.HudAssignmentPending;
                return true;
            case DeliveryPhase.HudAssignmentPending when delivered:
                _keys = [];
                Phase = DeliveryPhase.WorldOpensPending;
                return true;
            case DeliveryPhase.WorldOpensPending when delivered:
                // World actors open one at a time, after HUD assignment has completed.
                if (_worldOpenIndex < _raid.StaticActors.Count + _raid.SpawnedActors.Count)
                {
                    int index = _worldOpenIndex++;
                    ChannelBunch open = index < _raid.StaticActors.Count
                        ? _raid.StaticActors[index].Open(sender.NextSequence(_raid.StaticActors[index].Channel))
                        : _raid.SpawnedActors[index - _raid.StaticActors.Count].Open(sender.NextSequence(_raid.SpawnedActors[index - _raid.StaticActors.Count].Channel));
                    _keys = [sender.Queue(open)];
                    return false;
                }

                Phase = DeliveryPhase.PlayerOpensReady;
                return true;
            case DeliveryPhase.PlayerOpensReady:
                WorldActor squad = Actor(_player.Squad);
                ActorSpawn squadSpawn = new(squad.Guid, _raid.Imports.Archetypes[squad.ClassPath], 0, default);
                _keys = [sender.Queue(new ActorInitialization(squadSpawn, null, []).ToBunch(ChannelLayout.Squad, sender.NextSequence(ChannelLayout.Squad), []))];
                Phase = DeliveryPhase.SquadOpenPending;
                return true;
            case DeliveryPhase.SquadOpenPending when delivered:
                _mapped.UnionWith([_player.Squad, .. _raid.SpawnedActors.Select(actor => actor.Guid)]);
                _keys = [sender.Queue(Bunches.Actor(ChannelLayout.Squad, _raid.State.SquadMemberContent(_player, _mapped).Write))];
                Phase = DeliveryPhase.SquadMemberPending;
                return true;
            case DeliveryPhase.SquadMemberPending when delivered:
                _keys =
                [
                    sender.Queue(Bunches.Actor(ChannelLayout.Squad, _raid.State.SquadContent(_player, _mapped))),
                    sender.Queue(Bunches.Actor(ChannelLayout.GameState, _raid.State.SquadRoster(_player, _mapped))),
                ];
                Phase = DeliveryPhase.SquadPropertiesPending;
                return true;
            case DeliveryPhase.SquadPropertiesPending when delivered:
                _keys = [.. _raid.State.InitialProperties(_player, _mapped).Select(content => sender.Queue(Bunches.Actor(ChannelOf(content.Actor), content.Content)))];
                Phase = DeliveryPhase.PlayerPropertiesPending;
                return true;
            case DeliveryPhase.PlayerPropertiesPending when delivered:
                if (_member.Profiles.Count > 0)
                {
                    sender.Queue(Bunches.Actor(ChannelLayout.Controller, _raid.State.Replication.Calls(_raid.State.Objects(_mapped)).Call(
                        PlayerClasses.PlayerController.Class, FunctionNames.ClientReceiveSquadPlayersProfiles,
                        ProfileParameters(_member.Profiles), RpcDirection.Client)));
                }

                Phase = DeliveryPhase.InitialLevelsPending;
                return true;
            case DeliveryPhase.InitialLevelsPending when _initialLevelsLoaded:
                // Starting the character earlier lets its spawn animation finish behind the loading screen.
                WorldActor pawn = Actor(_player.Pawn);
                ActorSpawn pawnSpawn = new(pawn.Guid, _raid.Imports.Archetypes[pawn.ClassPath], 0, pawn.Location, pawn.Rotation);
                _keys = [sender.Queue(new ActorInitialization(pawnSpawn, null, [_frames.SpawnFrame(this)]).ToBunch(ChannelLayout.Pawn, sender.NextSequence(ChannelLayout.Pawn), []))];
                Phase = DeliveryPhase.PawnOpenPending;
                return true;
            case DeliveryPhase.PawnOpenPending when delivered:
                _mapped.Add(_player.Pawn);
                _keys = [sender.Queue(Bunches.Actor(ChannelLayout.Pawn, _raid.State.PawnInitialProperties(_player, _mapped)))];
                Phase = DeliveryPhase.PawnPropertiesPending;
                return true;
            case DeliveryPhase.PawnPropertiesPending when delivered:
                Phase = DeliveryPhase.PlayerPropertiesDelivered;
                return true;
            case DeliveryPhase.PlayerPropertiesDelivered:
                (ObjectExports appearanceExports, ulong appearance) = _raid.State.AppearanceExports(_player);
                _keys = [sender.Queue(Bunches.Exports(appearanceExports, [appearance]))];
                Phase = DeliveryPhase.AppearanceExportsPending;
                return true;
            case DeliveryPhase.AppearanceExportsPending when delivered:
                _keys = [sender.Queue(Bunches.Actor(ChannelLayout.Pawn, _raid.State.AppearanceContent(_player).Write))];
                Phase = DeliveryPhase.AppearanceContentPending;
                return true;
            case DeliveryPhase.AppearanceContentPending when delivered:
                _keys = [sender.Queue(Bunches.Exports(_raid.State.InventoryExports(_player), [_player.Inventory, _player.Health]))];
                Phase = DeliveryPhase.InventoryExportsPending;
                return true;
            case DeliveryPhase.InventoryExportsPending when delivered:
                _keys =
                [
                    sender.Queue(Bunches.Actor(ChannelLayout.Pawn, _raid.State.InventoryContent(_player, _member.Inventory).Write)),
                    sender.Queue(Bunches.Actor(ChannelLayout.Pawn, _raid.State.ShieldContent(_player, _member.Inventory).Write)),
                ];
                _frames.ShieldSent(_member.Inventory.Shield);
                Phase = DeliveryPhase.InventoryContentPending;
                return true;
            case DeliveryPhase.InventoryContentPending when delivered:
                Phase = DeliveryPhase.PlayerLinksReady;
                return true;
            case DeliveryPhase.PlayerLinksReady:
                (ObjectExports abilityExports, ulong abilitySystem) = WorldState.AbilityExports(_player);
                _keys = [sender.Queue(Bunches.Exports(abilityExports, [abilitySystem]))];
                Phase = DeliveryPhase.AbilityExportsPending;
                return true;
            case DeliveryPhase.AbilityExportsPending when delivered:
                _keys = [sender.Queue(Bunches.Actor(ChannelLayout.Pawn, _raid.State.AbilityComponentContent(_player).Write))];
                Phase = DeliveryPhase.AbilityComponentPending;
                return true;
            case DeliveryPhase.AbilityComponentPending when delivered:
                IReadOnlyList<DynamicSubobjectContent> sets = _raid.State.AttributeSetCreation(_player, _member.Inventory);
                _keys = [sender.Queue(Bunches.Actor(ChannelLayout.Pawn, writer =>
                {
                    foreach (DynamicSubobjectContent set in sets)
                    {
                        set.Write(writer);
                    }
                }))];
                Phase = DeliveryPhase.AbilityCreationPending;
                return true;
            case DeliveryPhase.AbilityCreationPending when delivered:
                _keys = [sender.Queue(Bunches.Actor(ChannelLayout.Pawn, _raid.State.AttributeSetRegistration(_player).Write))];
                Phase = DeliveryPhase.AbilityRegistrationPending;
                return true;
            case DeliveryPhase.AbilityRegistrationPending when delivered:
                Phase = DeliveryPhase.AbilityDelivered;
                return true;
            case DeliveryPhase.AbilityDelivered:
                foreach ((ulong actor, ActorContentBlock content) in _raid.State.InitialCalls(_player, _mapped))
                {
                    sender.Queue(Bunches.Actor(ChannelOf(actor), content));
                }

                _keys = [];
                Phase = DeliveryPhase.PossessionPending;
                return true;
            default:
                return false;
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

    private void ReceiveControllerCalls(ChannelBunch item, BitReader reader, List<(string, Dictionary<string, object?>)> replies)
    {
        RpcCodec calls = _raid.State.Replication.Calls(_raid.State.Objects(_mapped));
        string controllerClass = PlayerClasses.PlayerController.Class;
        bool reliable = item.Sequence is not null;
        while (reader.Remaining > 0)
        {
            if (Phase == DeliveryPhase.Possessed && SkipComponentCalls(item, reader, _player.Controller))
            {
                continue;
            }

            foreach ((NetField field, ActorFieldPayload payload) in calls.ReadFields(controllerClass, ActorContentBlock.Read(reader), RpcDirection.Server))
            {
                string name = field.Name;
                if (name is FunctionNames.ServerUpdateCamera or FunctionNames.ServerSetSpectatorLocation)
                {
                    // View telemetry: never applied to the server's character or camera.
                    if (payload.PayloadBits is < 1 or > TelemetryBitLimit)
                    {
                        throw new InvalidPacketException("Oversized view telemetry");
                    }

                    continue;
                }

                if (!reliable)
                {
                    throw new InvalidPacketException($"Reliable call sent unreliably: {name}");
                }

                if (name == FunctionNames.SendInputDeviceChanged)
                {
                    // An optional input-device hint (a presence bit and the device); not used.
                    bool present = new BitReader(payload.Payload, payload.PayloadBits).ReadBit();
                    if (payload.PayloadBits != (present ? 3 : 1))
                    {
                        throw new InvalidPacketException("Unexpected input-device report");
                    }

                    continue;
                }

                if (!s_startupCalls.Contains(name))
                {
                    if (Phase != DeliveryPhase.Possessed)
                    {
                        throw new InvalidPacketException($"Unsupported controller call: {name}");
                    }

                    Count($"controller:{name}");
                    continue;
                }

                // The other start-up calls are client reports and telemetry: accepted, not stored.
                Dictionary<string, object?> parameters = calls.ReadParameters(field, payload);
                switch (name)
                {
                    case FunctionNames.ServerRequestSquadPlayersProfiles:
                        IReadOnlyList<object?> requested = parameters[PropertyNames.TenancyUserIds] as IReadOnlyList<object?> ?? [];
                        replies.Add((FunctionNames.ClientReceiveSquadPlayersProfiles, ProfileParameters(_member.Profiles.IntersectBy(requested.OfType<TenancyUserId>(), profile => profile.TenancyUserId))));
                        break;
                    case FunctionNames.ServerNotifyHasLoadedInitialLevels:
                        _initialLevelsLoaded = true;
                        break;
                    case FunctionNames.ServerAcknowledgePossession:
                        ReceivePossession(parameters);
                        break;
                    case FunctionNames.ServerUpdateLevelVisibility:
                        ReceiveVisibility([parameters[PropertyNames.LevelVisibility]], replies);
                        break;
                    case FunctionNames.ServerUpdateMultipleLevelsVisibility:
                        ReceiveVisibility(parameters[PropertyNames.LevelVisibilities] as IReadOnlyList<object?> ?? [], replies);
                        break;
                    case FunctionNames.ServerSurrender when Phase == DeliveryPhase.Possessed:
                        // Leaving the raid returns the player to the main menu; no raid result is recorded.
                        _surrenderToCommit = true;
                        replies.Add((FunctionNames.ClientReturnToMainMenuWithTextReason, new Dictionary<string, object?> { [PropertyNames.ReturnReason] = "" }));
                        break;
                    case FunctionNames.ServerSurrender:
                        throw new InvalidPacketException("Surrender before possession");
                }
            }
        }
    }

    private static Dictionary<string, object?> ProfileParameters(IEnumerable<CompactPlayerProfile> profiles) => new()
    {
        [PropertyNames.Profiles] = profiles.Cast<object?>().ToArray(),
    };

    private void ReceivePossession(Dictionary<string, object?> parameters)
    {
        // The client may confirm while the ability system is still being delivered.
        if (Phase < DeliveryPhase.PlayerPropertiesPending)
        {
            throw new InvalidPacketException("Possession before the player actors were linked");
        }

        // The client confirms the server's choice; it cannot pick another pawn.
        if (parameters.Count != 1 || parameters.GetValueOrDefault(PropertyNames.P) is not ulong pawn || pawn != _player.Pawn)
        {
            throw new InvalidPacketException("Possession of another pawn");
        }

        _possessionToCommit = true;
        if (Phase == DeliveryPhase.PossessionPending)
        {
            Phase = DeliveryPhase.Possessed;
        }
    }

    private void ReceiveVisibility(IReadOnlyList<object?> reports, List<(string, Dictionary<string, object?>)> replies)
    {
        HashSet<string> visible = new(_visibleLevels, StringComparer.Ordinal);
        foreach (object? item in reports)
        {
            if (item is not LevelVisibility report || !PackagePattern().IsMatch(report.Package) || report.File != report.Package
                || !RaidWorld.LevelPackages.Contains(report.Package))
            {
                throw new InvalidPacketException("Visibility report outside the map");
            }

            if (report.Transaction.Client)
            {
                // The client may show known map packages without server-synchronized streaming.
                replies.Add((FunctionNames.ClientAckUpdateLevelVisibility, new Dictionary<string, object?>
                {
                    [PropertyNames.PackageName] = report.Package,
                    [PropertyNames.TransactionId] = report.Transaction,
                    [PropertyNames.ClientAckCanMakeVisible] = true,
                }));
            }

            if (report.Visible)
            {
                visible.Add(report.Package);
            }
            else
            {
                visible.Remove(report.Package);
            }
        }

        if (!visible.SetEquals(_visibleLevels))
        {
            _visibleLevelsChangedAt = _raid.Clock();
        }

        _visibleLevels = visible;
    }

    // Calls on the possessed pawn and its components (predicted ability requests, component events) are
    // decoded and counted, not applied yet.
    private void ReceivePawnCalls(ChannelBunch item, BitReader reader)
    {
        while (reader.Remaining > 0)
        {
            if (SkipComponentCalls(item, reader, _player.Pawn))
            {
                continue;
            }

            foreach (string name in Calls(PlayerClasses.Pawn.Class, ActorContentBlock.Read(reader)))
            {
                Count(name);
            }
        }
    }

    // Reads the next block when it targets one of the actor's components rather than the actor: calls on the
    // ability system are decoded and counted; components the server does not simulate yet (named by path,
    // such as the controller's ping manager) are counted by name. Returns false for the actor's own content.
    private bool SkipComponentCalls(ChannelBunch item, BitReader reader, ulong outer)
    {
        if (At(item, reader.Position + 1).ReadBit())
        {
            return false;
        }

        if (NetGuid.Read(At(item, reader.Position + 2)) == PathReference)
        {
            ClientSubobjectReference reference = ClientSubobjectReference.Read(reader);
            string name = reference.Name.TrimEnd('\0');
            if (reference.Outer != outer || !ComponentNamePattern().IsMatch(name))
            {
                throw new InvalidPacketException("Unsupported client component reference");
            }

            if (outer == _player.Controller && name == PlayerClasses.SpectatorManagerComponent)
            {
                ReceiveSpectatorCalls(reference.Content);
                return true;
            }

            Count($"path:{name}");
            return true;
        }

        StableSubobjectContent content = StableSubobjectContent.ReadFromClient(reader);
        if (content.Guid != _player.AbilitySystem || outer != _player.Pawn)
        {
            Count($"subobject:{content.Guid}");
            return true;
        }

        foreach (string name in Calls(CharacterDefaults.AbilitySystem.Class, content.Content))
        {
            Count(name);
        }

        return true;
    }

    // Calls on the controller's spectator manager: the client says it is ready for the end of the round once its
    // end-of-round data is collected. Other calls are counted.
    private void ReceiveSpectatorCalls(ActorContentBlock content)
    {
        foreach (string name in Calls(ClassPaths.PioneerSpectatorManagerComponent, content))
        {
            if (name == FunctionNames.ServerReadyToSpectate && _transitionAt is not null)
            {
                _readyAt ??= ServerSeconds(_raid.Clock());
            }
            else
            {
                Count($"spectator:{name}");
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

    // Exports the class defaults of the Raider's abilities, then grants them.
    private void ServiceAbilities(ReliableSender sender)
    {
        if (_abilitiesGranted || _abilityKeys.Any(sender.Pending))
        {
            return;
        }

        if (_abilityDefaults is null)
        {
            (ObjectExports exports, ulong[] defaults) = AbilityExports();
            _abilityKeys = [.. defaults.Chunk(AbilityExportsPerBunch).Select(group => sender.Queue(Bunches.Exports(exports, group)))];
            _abilityDefaults = defaults;
            return;
        }

        GrantedAbility[] abilities = [.. _abilityDefaults.Select((guid, index) => new GrantedAbility(index + 1, index + 1, guid))];
        _abilityKeys = [sender.Queue(Bunches.Actor(ChannelLayout.Pawn, _raid.State.AbilityGrants(_player, abilities).Write))];
        _abilitiesGranted = true;
    }

    private (ObjectExports Exports, ulong[] Defaults) AbilityExports()
    {
        IReadOnlyCollection<ObjectExport> existing = _raid.Imports.Exports.Records.Values as IReadOnlyCollection<ObjectExport> ?? [.. _raid.Imports.Exports.Records.Values];
        Dictionary<string, StaticObjectExport> packages = existing.OfType<StaticObjectExport>().Where(record => record.OuterGuid == 0)
            .GroupBy(record => record.Path, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        uint packageChecksum = _raid.State.Replication.Catalog.Class(ClassPaths.Package).Checksum;
        List<StaticObjectExport> records = [];
        List<ulong> defaults = [];
        ulong guid = FirstAbilityGuid;
        foreach (AbilityGrant grant in DefaultAbilities.Raider)
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

        return (new ObjectExports(records), [.. defaults]);
    }

    // Ends this player's round once an elevator left with them inside (its doors closed): the extraction, the
    // controller's end-of-round component and its quest progress, the end-of-round data (no experience, no journey),
    // the transition to the end-of-round screen and the end of the data. The client then collects its data and says
    // when it is ready; the end of the round follows, and the client returns to the main menu by itself. A client that
    // never says it is ready is sent back to the main menu.
    private void ServiceDeparture(ReliableSender sender, double now)
    {
        double at = ServerSeconds(now);
        if (_departure is null)
        {
            if (LeftWithElevator(at) is not { } departure)
            {
                return;
            }

            _departure = departure;
            _elevators.Depart();
            _member.Depart();
            QueueControllerCall(sender, FunctionNames.ClientNotifyExtracted, new Dictionary<string, object?>
            {
                [PropertyNames.ExtractionType] = new GameplayTag(DamExtraction.ExtractionType),
            });
            _endOfRoundExport = sender.Queue(Bunches.Exports(_raid.State.EndOfRoundExports(_player), [_player.EndOfRound]));
            return;
        }

        if (_transitionAt is null)
        {
            if (_endOfRoundExport is { } export && sender.Pending(export))
            {
                return;
            }

            sender.Queue(Bunches.Actor(ChannelLayout.Controller, _raid.State.EndOfRoundCall(_player, FunctionNames.ClientFinalizeQuestProgress,
                new Dictionary<string, object?> { [PropertyNames.PlayerWon] = true }).Write));
            QueueControllerCall(sender, FunctionNames.ReceiveXpData, new Dictionary<string, object?>
            {
                [PropertyNames.XpEventLog] = null,
                [PropertyNames.XpBasedRewardsLog] = null,
            });
            QueueControllerCall(sender, FunctionNames.ClientClearReconnectData, []);
            QueueControllerCall(sender, FunctionNames.ClientStartEndOfRoundTransition, new Dictionary<string, object?> { [PropertyNames.PlayerWon] = true });
            QueueControllerCall(sender, FunctionNames.ReceiveEndOfRoundJourneySummary, new Dictionary<string, object?> { [PropertyNames.Summary] = null });
            QueueControllerCall(sender, FunctionNames.EndOfRoundJourneyDataTransferComplete, []);
            _transitionAt = at;
            return;
        }

        if (_endAt is not null || _fallbackAt is not null)
        {
            return;
        }

        if (_readyAt is not null)
        {
            QueueControllerCall(sender, FunctionNames.ClientStartEndOfRound, new Dictionary<string, object?> { [PropertyNames.PlayerWon] = true });
            _endAt = at;
        }
        else if (at >= _transitionAt + DepartureFallbackSeconds)
        {
            // As after a surrender: an empty return reason.
            QueueControllerCall(sender, FunctionNames.ClientReturnToMainMenuWithTextReason, new Dictionary<string, object?> { [PropertyNames.ReturnReason] = "" });
            _fallbackAt = at;
        }
    }

    // The elevator that left with this player inside and when it left (its doors closed), once it has.
    private (int Elevator, double At)? LeftWithElevator(double at)
    {
        foreach (ExtractionPoint point in _raid.Extraction.Points)
        {
            if (_raid.ExtractionStates.Departure(point.Index) is { } departure && departure.Players.Contains(_member.Id)
                && at >= departure.At + DamExtraction.DoorsSeconds)
            {
                return (point.Index, departure.At + DamExtraction.DoorsSeconds);
            }
        }

        return null;
    }

    private void QueueControllerCall(ReliableSender sender, string function, Dictionary<string, object?> parameters) =>
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
    bool IPlayerChannelsContext.MapActorsAllowed(double now) => _frames.MapActorsAllowed(now);

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

    private static BitReader At(ChannelBunch item, long position)
    {
        BitReader reader = new(item.Payload, item.PayloadBits);
        reader.ReadBitArray(position);
        return reader;
    }

    private IEnumerable<string> Calls(string classPath, ActorContentBlock block) =>
        _raid.State.Replication.Calls(_raid.State.Objects(_mapped)).ReadFields(classPath, block, RpcDirection.Server).Select(call => call.Field.Name);

    private WorldActor Actor(ulong guid) => _raid.State.Actor(guid);

    private int ChannelOf(ulong actor) => Array.Find(_bindings, binding => binding.Guid == actor).Channel;

    private void Count(string name) => _ignoredCalls[name] = _ignoredCalls.GetValueOrDefault(name) + 1;
}
