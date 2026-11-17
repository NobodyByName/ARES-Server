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

    // Map actors sent before the character simulation settles crashed the client.
    private const double MapActorStartDelay = 15.0;
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

    // Other players' movement is sent at most this often.
    private const double ObservedFrameInterval = 0.05;
    private const int MaxObservedFramesInFlight = 32;

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
    private ReliableKey[] _blackboardKeys = [];
    private uint _blackboardFrame;
    private PlayerMovement? _movement;
    private IReadOnlyDictionary<int, PendulumSlot> _blackboardState = new Dictionary<int, PendulumSlot>();
    private ulong _blackboardFocus;
    private double? _asyncLoadAt;
    private int _asyncLoads;
    private ReliableKey[] _abilityKeys = [];
    private ulong[]? _abilityDefaults;
    private bool _abilitiesGranted;
    private ReliableKey? _worldTimeKey;
    private double? _worldTimeSentAt;

    private List<InteractionRequest> _requests = [];
    private double _framesHeldUntil;
    private List<InventoryRequest> _inventoryRequests = [];
    private List<WeaponInput> _weaponRequests = [];
    private uint _weaponVersion;
    private bool _populatedInventorySent;
    private bool _weaponCreationSent;
    private bool _weaponItemsCreated;
    private ReliableKey[] _weaponCreationKeys = [];
    private bool _weaponReferencesSent;
    private bool _weaponReferencesMapped;
    private ReliableKey[] _weaponReferenceKeys = [];
    private uint _inventoryFrame;
    private uint _inventoryVersion = 1;
    private ContainerItem? _shield;
    private double? _mapActorsAllowedAt;

    // The interaction the server runs for the character (a container search, a descend hold), times in raid world
    // seconds.
    private PawnInteraction? _interaction;
    private uint _interactionCount;
    private ReliableKey? _interactorExport;
    private bool _interactorExported;
    private Dictionary<long, ObservedPlayer> _observed = [];
    private BoundedLog _unknownRequests = new(InteractionRequestLog);
    private BoundedLog _rejectedInventory = new(InteractionRequestLog);
    private InteractionRequest? _lastLoggedRequest;
    private PlayerDoors _doors;
    private PlayerLadders _ladders;
    private PlayerLoot _loot;
    private PlayerPickups _pickups;
    private PlayerArcs _arcs;
    private PlayerElevators _elevators;

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

    private enum ObservedStage
    {
        Hidden,
        StateOpening,
        PawnOpening,
        Linking,
        Visible,
        Gone,
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
    public int BlackboardFrames { get; private set; }

    /// <summary>
    /// The character's time offset in its newest frame (see <see cref="PawnBlackboard.TimeOffsetField"/>), or null
    /// before its first frame.
    /// </summary>
    public long? TimeOffset { get; private set; }

    /// <summary>The character's simulated state after the latest simulated input frame, or null before the reset frame.</summary>
    public CharacterState? SimulatedCharacter => _movement?.Latest;

    /// <summary>Input frames the character simulation ran without the client's input.</summary>
    public int MissingInputFrames => _movement?.MissingFrames ?? 0;

    /// <summary>Simulated frames after which the character continued from the position its client reported.</summary>
    public int PositionReseeds => _movement?.Reseeds ?? 0;

    /// <summary>The latest such frame: its number, how far the simulation was off and whether it had the character in the air.</summary>
    public (uint Frame, double Distance, bool Airborne)? LastPositionReseed => _movement?.LastReseed;

    /// <summary>Largest distance the simulation was off on such a frame.</summary>
    public double WorstPositionReseed => _movement?.WorstReseed ?? 0;

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
    public int QuickItemHolds { get; private set; }

    /// <summary>The latest such hold: the first and the last withheld frame tick (none while it lasts).</summary>
    public (uint From, uint? To)? LastQuickItemHold { get; private set; }

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
    private IPlayerFamily[] Families => [_doors, _ladders, _loot, _pickups, _arcs, _elevators];

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
    bool IPlayerChannelsContext.InteractorExported => _interactorExported;

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
        copy._movement = _movement?.Copy();
        copy._requests = [.. _requests];
        copy._inventoryRequests = [.. _inventoryRequests];
        copy._weaponRequests = [.. _weaponRequests];
        copy._observed = new Dictionary<long, ObservedPlayer>(_observed);
        copy._unknownRequests = _unknownRequests.Copy();
        copy._rejectedInventory = _rejectedInventory.Copy();
        copy._doors = _doors.Copy();
        copy._ladders = _ladders.Copy();
        copy._loot = _loot.Copy();
        copy._pickups = _pickups.Copy();
        copy._arcs = _arcs.Copy();
        copy._elevators = _elevators.Copy();
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
            ServiceBlackboard(io.Sender, now);
        }

        ServiceStreaming(io.Sender, now);
        ServiceAbilities(io.Sender);
        foreach (IPlayerFamily family in Families)
        {
            family.Service(this, io.Sender, now);
        }

        ServiceObserved(io.Sender, now);
        ServiceDeparture(io.Sender, now);
    }

    /// <inheritdoc />
    public double ServerSeconds(double now) => _raid.WorldSeconds(now);

    /// <inheritdoc />
    public void ActorChannelFailed(int channel)
    {
        if (Array.Exists(Families, family => family.ChannelFailed(this, channel)))
        {
            return;
        }

        if (_raid.Channels.ObservedPlayer(channel) is { } observed && observed < _raid.Players.Count)
        {
            _observed[_raid.Players[observed].Id] = new ObservedPlayer(ObservedStage.Gone);
        }
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

                _inventoryFrame = Math.Max(_inventoryFrame, frame);
            }
        }

        _inventoryRequests.Clear();
        if (Phase == DeliveryPhase.Possessed && _blackboardFrame != 0 && InputFrame is { } latest)
        {
            // Late input frames revise the ticks they reach, even when no newer frame arrived. Each newly simulated frame
            // moves with the weapon its own input left in the character's hands.
            _movement?.Revise(_input);
            Queue<WeaponInput> weapons = new(_weaponRequests.Where(input => input.Frame < latest).OrderBy(input => input.Frame));
            while (_movement is { } movement && movement.Frame + 1 < latest)
            {
                while (weapons.TryPeek(out WeaponInput input) && input.Frame <= movement.Frame + 1)
                {
                    _member.Weapons.Apply(weapons.Dequeue(), _weaponReferencesMapped ? _player.HeldItems : null);
                }

                movement.Advance(_input, _member.Weapons.Armed, _member.Weapons.HeldItem);
            }

            foreach (WeaponInput input in weapons)
            {
                _member.Weapons.Apply(input, _weaponReferencesMapped ? _player.HeldItems : null);
            }

            _weaponRequests.RemoveAll(input => input.Frame < latest);
        }
        else
        {
            _weaponRequests.Clear();
        }

        PublishMotion(now);
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
                _keys = [sender.Queue(new ActorInitialization(pawnSpawn, null, [SpawnFrame()]).ToBunch(ChannelLayout.Pawn, sender.NextSequence(ChannelLayout.Pawn), []))];
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
                _shield = _member.Inventory.Shield;
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

    // The client's character simulation turns the character to the facing of the state it starts from, so the
    // character opens with the spawn state. The first frame built from the client's input then follows as an
    // ordinary update.
    private ActorContentBlock SpawnFrame()
    {
        WorldActor pawn = Actor(_player.Pawn);
        IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(PawnBlackboardFrames.FirstTick,
            new PawnMotion(pawn.Location, Yaw: pawn.Rotation.Yaw * Math.PI / 180), 0, _member.Inventory.InitialBlackboard,
            _member.Weapons.Variables(PawnBlackboardFrames.FirstTick), TimeOffsetAt(PawnBlackboardFrames.FirstTick, _raid.Clock()));
        uint delivery = checked((uint)BlackboardFrames);
        ActorContentBlock frame = _raid.State.BlackboardFrame(delivery, delivery + 1, state, _blackboardState);
        _blackboardState = state;
        BlackboardFrames++;
        return frame;
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

    // The first frame starts the client's character simulation at the spawn after the client confirmed its
    // initial levels loaded and possession completed, with the slot-1 gun already in hand (a raid never starts
    // unarmed; its item link follows once the item exists). The client applies each later
    // frame as its full character state, so later frames carry the movement the client itself predicted
    // (accepted as is): input frame N reports the position before tick N, so a frame describes the tick before
    // the latest input. They are sent when the usable focused button changes: the client's interaction (and
    // its animation) only starts once the server says it may use the button.
    private void ServiceBlackboard(ReliableSender sender, double now)
    {
        if (InputFrame is not { } latest || latest is < PawnBlackboardFrames.FirstTick or > PawnBlackboardFrames.LastTick || _blackboardKeys.Any(sender.Pending))
        {
            return;
        }

        // Item initialization caches its inventory owner even when FindSlotOf fails. Registering
        // before populated containers arrive leaves the item permanently assigned to an invalid slot.
        if (_populatedInventorySent && _player.HeldItems.Count > 0 && !_weaponReferencesMapped)
        {
            if (!_weaponCreationSent)
            {
                _weaponCreationKeys = [.. _raid.State.WeaponItemCreation(_player, _member.Inventory)
                    .Select(item => sender.Queue(Bunches.Actor(ChannelLayout.Pawn, item.Write)))];
                _weaponCreationSent = true;
                return;
            }

            if (!_weaponItemsCreated)
            {
                if (_weaponCreationKeys.Any(sender.Pending))
                {
                    return;
                }

                _weaponItemsCreated = true;
            }

            if (!_weaponReferencesSent)
            {
                _weaponReferenceKeys = [sender.Queue(Bunches.Actor(ChannelLayout.Pawn, _raid.State.WeaponItemReferences(_player).Write))];
                _weaponReferencesSent = true;
                return;
            }

            if (_weaponReferenceKeys.Any(sender.Pending))
            {
                return;
            }

            _weaponReferencesMapped = true;
            _weaponVersion = uint.MaxValue;
        }

        IReadOnlyDictionary<Guid, ulong>? weaponObjects = _weaponReferencesMapped ? _player.HeldItems : null;
        IReadOnlyDictionary<int, PendulumSlot> state;
        uint frame = latest;
        ulong focus = 0;
        bool initializingContainers = _blackboardFrame == 0;
        if (_blackboardFrame == 0)
        {
            WorldActor pawn = Actor(_player.Pawn);
            double yaw = pawn.Rotation.Yaw * Math.PI / 180;
            state = PawnBlackboardFrames.State(frame, new PawnMotion(pawn.Location, Yaw: yaw), focus,
                _member.Inventory.InitialBlackboard, _member.Weapons.Variables(frame, weaponObjects), TimeOffsetAt(frame, now),
                emptySlots: _member.Weapons.EmptyItemSlots(frame));
            _movement = new PlayerMovement(frame, pawn.Location, yaw, _raid.Collision);
        }
        else
        {
            if (InteractionFramesHeld(now))
            {
                return;
            }

            if (QuickItemHeld(latest - 1))
            {
                return;
            }

            focus = Array.Exists(Families, family => family.Usable(this, _input.Focus)) ? _input.Focus : 0;
            frame = latest - 1;
            bool inventoryChanged = _inventoryVersion != _member.Inventory.Version;
            bool weaponsChanged = _weaponVersion != _member.Weapons.Version;
            if ((!inventoryChanged && !weaponsChanged && focus == _blackboardFocus) || frame <= _blackboardFrame || frame < _inventoryFrame
                || _input.Position(latest) is not { } position || _input.Position(frame) is not { } before || _input.Yaw(frame) is not { } yaw
                || _movement?.State(frame) is not { } character || character.Stance == CharacterStance.Slide || character.SlideExitTicks > 0
                || character.Roll is not null || character.Vaulting || character.Unsettled || character.SnapHook.Busy)
            {
                // A frame lacks the state of the client's own slide, roll, vault, landing and Snap Hook nodes: it would end a
                // slide, a roll, a vault, a landing recovery or a hook's flight and pull, and drop a jumping or falling character
                // to the ground. Wait until the character is back in ordinary ground movement.
                return;
            }

            // The character's interactor shows the server-run interaction (an open container is still its search).
            state = PawnBlackboardFrames.State(frame, Motion(character, position, before, yaw, _input.ActionRotation(frame)), focus, _member.Inventory.Blackboard,
                _member.Weapons.Variables(frame, weaponObjects), TimeOffsetAt(frame, now),
                _interaction is { } interaction ? OnCharacterClock(interaction) : null, _member.Weapons.EmptyItemSlots(frame));
        }

        // Delivery numbers address the client's snapshot ring; the graph inside carries the input tick.
        uint delivery = checked((uint)BlackboardFrames);
        List<ReliableKey> keys = [sender.Queue(Bunches.Actor(ChannelLayout.Pawn, _raid.State.BlackboardFrame(delivery, delivery + 1, state, _blackboardState)))];
        if (_shield != _member.Inventory.Shield)
        {
            keys.Add(sender.Queue(Bunches.Actor(ChannelLayout.Pawn, _raid.State.ShieldContent(_player, _member.Inventory).Write)));
            _shield = _member.Inventory.Shield;
        }

        _blackboardKeys = [.. keys];
        (_blackboardFrame, _blackboardState, _blackboardFocus) = (frame, state, focus);
        _inventoryVersion = initializingContainers ? 1 : _member.Inventory.Version;
        _weaponVersion = _member.Weapons.Version;
        _populatedInventorySent = !initializingContainers;
        BlackboardFrames++;
    }

    // While a thrown Snap Hook is out (its flight and pull, until the client may use it again) the client keeps its own
    // character state: a frame would end them. Raid keeps following its movement reports.
    // Inventory and focus changes go out with the first frame after the hold.
    private bool QuickItemHeld(uint frame)
    {
        bool held = _member.Weapons.HoldsFrames(frame);
        switch (held)
        {
            case true when LastQuickItemHold is not { To: null }:
                QuickItemHolds++;
                LastQuickItemHold = (frame, null);
                break;
            case false when LastQuickItemHold is { To: null } hold:
                LastQuickItemHold = (hold.From, frame - 1);
                break;
        }

        return held;
    }

    // Pawn frames carry no interaction state, so the client's own one (requested, holding, succeeded) is reset by
    // each frame; none are sent while this player breaches and just after it succeeds.
    private bool InteractionFramesHeld(double now) => _doors.Breaching || _loot.Searching || _elevators.Descending || now < _framesHeldUntil;

    // Asks the client to finish loading the levels around it, while streaming still changes and before the
    // character simulation starts; the client decides what it loads.
    private void ServiceStreaming(ReliableSender sender, double now)
    {
        if (_blackboardFrame != 0 || _asyncLoads >= AsyncLoadLimit
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

    // A server-run interaction with its times moved from world time to the character's clock.
    private PawnInteraction OnCharacterClock(PawnInteraction interaction)
    {
        double offset = (double)(TimeOffset ?? 0) / PawnBlackboard.TimePerSecond;
        return interaction with
        {
            RequestedAt = interaction.RequestedAt - offset,
            AlignedAt = interaction.AlignedAt - offset,
            StartedAt = interaction.StartedAt - offset,
        };
    }

    private static string Slot(ItemContainerSlot slot) => $"{slot.Parent.Id}r{slot.Parent.Revision}/{slot.Index}";

    // Whether an id names an interaction the server gave the client: a door, ladder, loot, elevator button or pickup.
    private bool KnownInteraction(ulong target) =>
        _raid.Doors.ByButton(target) is not null || _raid.Doors.ByBreach(target) is not null || _raid.Loot.ByInteraction(target) is not null
        || _raid.Extraction.ByButton(target) is not null || _raid.Ladders.Ladders.Any(ladder => ladder.Interactions.Contains(target))
        || _raid.Pickups.ByTarget(target) is not null;

    // Gives other players this character's movement: the input tick before the newest input, at the position the
    // client reported, with the simulated velocity, stance and locomotion (as the character's own frames carry it),
    // the held weapons and the aim pitch. Other clients have no item objects of this player, so the weapons go
    // without item links: the client draws an equipped gun or gadget from its item type. Other clients only show the
    // character, so its movement goes out through a Snap Hook's throw and pull too, while this player's own frames are
    // held: the Snap Hook's node carries the hook (other clients draw it and its cable from it), and the move target and
    // pull node the pulled pose.
    private void PublishMotion(double now)
    {
        if (Phase != DeliveryPhase.Possessed || _blackboardFrame == 0 || InputFrame is not { } latest || latest <= PawnBlackboardFrames.FirstTick)
        {
            return;
        }

        uint tick = latest - 1;
        if ((_member.Motion is { } published && published.Tick >= tick) || !_member.Weapons.Describable(tick) || _input.Position(latest) is not { } position
            || _input.Position(tick) is not { } before || _input.Yaw(tick) is not { } yaw || _movement?.State(tick) is not { } character
            || _input.Pitch(tick) is not { } pitch)
        {
            return;
        }

        IReadOnlyList<PendulumVariable> weapons = _member.Weapons.ObservedVariables(tick, character.SnapHook);
        _member.Publish(new PlayerMotion(tick, Motion(character, position, before, yaw, _input.ActionRotation(tick)),
            [.. weapons, .. character.SnapHook.ObservedNodes(tick, weapons)], pitch, TimeOffsetAt(tick, now)));
    }

    // A tick's movement for a frame: the simulated body when the simulation moves it, otherwise the position the
    // client reported after the tick with the height change it shows.
    private static PawnMotion Motion(CharacterState character, Vector3D reported, Vector3D before, double yaw, PendulumQuaternion? actionRotation) =>
        character.Body is null
            ? new PawnMotion(reported, character.Velocity with { Z = (reported.Z - before.Z) / PawnBlackboard.TickSeconds }, yaw, character, actionRotation)
            : new PawnMotion(character.Position, character.Velocity, yaw, character, actionRotation);

    // The character's time offset: a tick's simulation time plus the offset is the raid's world time, which is
    // also the server time on this raid's packets. The client's ticks drift against the raid's clock; like the
    // game, the offset follows in whole steps, so it stays put while the drift is under one step.
    private long TimeOffsetAt(uint tick, double now)
    {
        long ideal = (long)Math.Round(_raid.WorldSeconds(now) * PawnBlackboard.TimePerSecond) - ((long)tick * PawnBlackboard.TimePerTick);
        long offset = TimeOffset is { } current
            ? current + (PawnBlackboard.TimeOffsetStep * ((ideal - current) / PawnBlackboard.TimeOffsetStep))
            : ideal - (ideal % PawnBlackboard.TimePerTick);
        TimeOffset = offset;
        return offset;
    }

    // Shows the client the other players of the raid once their characters move: opens each one's player state
    // as a simulated actor, then its character at its latest movement, links them and gives the character its
    // appearance; then sends that character's movement as it changes, unreliably, each frame coded
    // against the newest frame the client confirmed. A player who leaves the raid, or leaves it with an elevator, is
    // removed.
    // Everyone is shown everywhere; far-away players are not filtered out yet.
    private void ServiceObserved(ReliableSender sender, double now)
    {
        if (!MapActorsAllowed(now))
        {
            return;
        }

        IReadOnlyList<RaidPlayer> players = _raid.Players;
        for (int index = 0; index < players.Count; index++)
        {
            RaidPlayer member = players[index];
            ObservedPlayer observed = _observed.GetValueOrDefault(member.Id, new ObservedPlayer(ObservedStage.Hidden));
            if (member.Id == _member.Id || observed.Stage == ObservedStage.Gone || observed.Keys.Any(sender.Pending)
                || _raid.State.FindPlayer(member.Id) is not { } other)
            {
                continue;
            }

            int stateChannel = _raid.Channels.ObservedState(index);
            int pawnChannel = stateChannel + 1;
            if (member.Left || member.Departed)
            {
                if (observed.Stage >= ObservedStage.PawnOpening)
                {
                    sender.Queue(Bunches.Close(pawnChannel, Bunches.Destroyed));
                }

                if (observed.Stage >= ObservedStage.StateOpening)
                {
                    sender.Queue(Bunches.Close(stateChannel, Bunches.Destroyed));
                }

                _observed[member.Id] = new ObservedPlayer(ObservedStage.Gone);
                continue;
            }

            if (member.Motion is not { } motion)
            {
                continue;
            }

            _observed[member.Id] = observed.Stage switch
            {
                ObservedStage.Hidden => OpenObservedState(sender, other, stateChannel),
                ObservedStage.StateOpening => OpenObservedPawn(sender, other, pawnChannel, motion, now),
                ObservedStage.PawnOpening => LinkObservedPawn(sender, other, stateChannel, observed),
                ObservedStage.Linking => observed with
                {
                    Stage = ObservedStage.Visible,
                    Keys = [sender.Queue(Bunches.Actor(pawnChannel, _raid.State.AppearanceContent(other).Write))],
                },
                _ when motion.Tick > observed.SentTick && now - observed.SentAt >= ObservedFrameInterval =>
                    SendObservedFrame(sender, pawnChannel, observed, motion, now),
                _ => observed,
            };
        }
    }

    private ObservedPlayer OpenObservedState(ReliableSender sender, WorldPlayer other, int channel)
    {
        ActorSpawn spawn = new(other.PlayerState, _raid.Imports.Archetypes[Actor(other.PlayerState).ClassPath], 0, default);
        ActorInitialization open = new(spawn, null, [_raid.State.ObservedPlayerStateProperties(other)]);
        return new ObservedPlayer(ObservedStage.StateOpening) { Keys = [sender.Queue(open.ToBunch(channel, sender.NextSequence(channel), []))] };
    }

    // The character opens as a simulated proxy with its movement's first (reset) frame.
    // Other players' frames carry only the fields the game sends to other clients (PendulumCodec.Others).
    // Frames are numbered by the character's simulation tick (about 30 per second).
    private ObservedPlayer OpenObservedPawn(ReliableSender sender, WorldPlayer other, int channel, PlayerMotion motion, double now)
    {
        IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(motion.Tick, motion.Motion, 0, weapons: motion.Weapons, timeOffset: motion.TimeOffset);
        Rotator facing = new(0, (motion.Motion.Yaw ?? 0) * 180 / Math.PI, 0);
        ActorSpawn spawn = new(other.Pawn, _raid.Imports.Archetypes[Actor(other.Pawn).ClassPath], 0, motion.Motion.Position, facing);
        ushort pitch = WorldState.CompressedPitch(motion.Pitch);
        ActorInitialization open = new(spawn, null,
            [WorldState.Merge(_raid.State.ObservedPawnProperties(other, pitch), _raid.State.BlackboardFrame(0, motion.Tick, state, new Dictionary<int, PendulumSlot>(), PendulumCodec.Others))]);
        return new ObservedPlayer(ObservedStage.PawnOpening, motion.Tick, state, now, pitch, motion.Tick)
        {
            Keys = [sender.Queue(open.ToBunch(channel, sender.NextSequence(channel), []))],
        };
    }

    private ObservedPlayer LinkObservedPawn(ReliableSender sender, WorldPlayer other, int stateChannel, ObservedPlayer observed)
    {
        _mapped.UnionWith([other.PlayerState, other.Pawn]);
        (ObjectExports exports, ulong appearance) = _raid.State.AppearanceExports(other);
        return observed with
        {
            Stage = ObservedStage.Linking,
            Keys =
            [
                sender.Queue(Bunches.Actor(stateChannel, _raid.State.ObservedPawnLinks(other))),
                sender.Queue(Bunches.Exports(exports, [appearance])),
            ],
        };
    }

    private ObservedPlayer SendObservedFrame(ReliableSender sender, int channel, ObservedPlayer observed, PlayerMotion motion, double now)
    {
        observed = Confirmed(sender, observed);
        // A reset clears the receiver's history. Wait for its outcome before sending another frame.
        if (observed.State is null && observed.InFlight.Length != 0)
        {
            return observed;
        }

        IReadOnlyDictionary<int, PendulumSlot> state = PawnBlackboardFrames.State(motion.Tick, motion.Motion, 0, weapons: motion.Weapons, timeOffset: motion.TimeOffset);
        // The client may already have overwritten a baseline this far back; a reset frame starts from the defaults.
        bool reset = observed.State is null || motion.Tick - observed.Tick >= PendulumFrames.History;
        if (!reset && WithEndedSlots(state, observed.State!) is { } kept)
        {
            state = kept;
        }
        else
        {
            reset = true;
        }

        ActorContentBlock frame = reset
            ? _raid.State.BlackboardFrame(0, motion.Tick, state, new Dictionary<int, PendulumSlot>(), PendulumCodec.Others)
            : _raid.State.BlackboardFrame(observed.Tick, motion.Tick, state, observed.State!, PendulumCodec.Others);
        // Aim pitch rides with the frame in one block when it changed.
        ushort pitch = WorldState.CompressedPitch(motion.Pitch);
        if (pitch != observed.Pitch)
        {
            frame = WorldState.Merge(_raid.State.ObservedPitch(pitch), frame);
        }

        ObservedFrame sent = new(sender.QueueUnreliable(Bunches.Actor(channel, frame)), motion.Tick, state, pitch);
        return observed with
        {
            Stage = ObservedStage.Visible,
            State = reset ? null : observed.State,
            SentAt = now,
            SentTick = motion.Tick,
            InFlight = reset ? [sent] : [.. observed.InFlight.TakeLast(MaxObservedFramesInFlight - 1), sent],
            Keys = [],
        };
    }

    // A frame cannot take a slot out of the client's layout: the client keeps what its baseline has in a slot the frame
    // does not name. A slot whose variable ended (the Snap Hook's pull node) stays laid out without a variable, as the
    // game keeps it. Null when such a slot shares pages with the frame's own slots, which only a reset frame can clear.
    private static IReadOnlyDictionary<int, PendulumSlot>? WithEndedSlots(IReadOnlyDictionary<int, PendulumSlot> state,
        IReadOnlyDictionary<int, PendulumSlot> baseline)
    {
        PendulumSlot[] ended = [.. baseline.Values.Where(slot => !state.ContainsKey(slot.Slot))];
        if (ended.Length == 0)
        {
            return state;
        }

        HashSet<int> pages = [.. state.Values.SelectMany(slot => Enumerable.Range(slot.Position, slot.Size))];
        if (ended.Any(slot => Enumerable.Range(slot.Position, slot.Size).Any(pages.Contains)))
        {
            return null;
        }

        Dictionary<int, PendulumSlot> kept = new(state);
        foreach (PendulumSlot slot in ended)
        {
            kept[slot.Slot] = slot with { DescriptorId = null, Value = [] };
        }

        return kept;
    }

    // The observed player with its baseline moved to the newest frame the client confirmed; lost or forgotten
    // frames are dropped.
    private static ObservedPlayer Confirmed(ReliableSender sender, ObservedPlayer observed)
    {
        ObservedPlayer result = observed;
        List<ObservedFrame> waiting = [];
        foreach (ObservedFrame frame in observed.InFlight)
        {
            if (sender.Arrived(frame.Key) is true && frame.Tick > result.Tick)
            {
                result = result with { Tick = frame.Tick, State = frame.State, Pitch = frame.Pitch };
            }
            else if (sender.Pending(frame.Key))
            {
                waiting.Add(frame);
            }
        }

        return result with { InFlight = [.. waiting.Where(frame => frame.Tick > result.Tick)] };
    }

    /// <inheritdoc />
    void IPlayerChannelsContext.InventoryChangedAt(uint frame) => _inventoryFrame = Math.Max(_inventoryFrame, frame);

    /// <inheritdoc />
    bool IPlayerChannelsContext.MapActorsAllowed(double now) => MapActorsAllowed(now);

    /// <inheritdoc />
    bool IPlayerChannelsContext.LevelVisible(string level) => _visibleLevels.Contains(level);

    /// <inheritdoc />
    bool IPlayerChannelsContext.MovedSince(uint frame) => MovedSince(frame);

    /// <inheritdoc />
    void IPlayerChannelsContext.HoldFrames(double until) => _framesHeldUntil = until;

    /// <inheritdoc />
    void IPlayerChannelsContext.ExportInteractor(ReliableSender sender)
    {
        if (_interactorExported)
        {
            return;
        }

        if (_interactorExport is null)
        {
            _interactorExport = sender.Queue(Bunches.Exports(_raid.State.InteractorExports(_player), [_player.Interactor]));
        }
        else if (!sender.Pending(_interactorExport.Value))
        {
            _interactorExported = true;
        }
    }

    /// <inheritdoc />
    void IPlayerChannelsContext.RunInteraction(PawnInteraction interaction) => _interaction = interaction;

    /// <inheritdoc />
    void IPlayerChannelsContext.SucceedInteraction() =>
        _interaction = _interaction is { } ended ? ended with { Event = LastInteractionEvent.Succeeded } : null;

    /// <inheritdoc />
    uint IPlayerChannelsContext.NextInteractionSequence() => ++_interactionCount;

    /// <inheritdoc />
    void IPlayerChannelsContext.ExtendFrameHold(double until) => _framesHeldUntil = Math.Max(_framesHeldUntil, until);

    /// <inheritdoc />
    void IPlayerChannelsContext.Climb(uint frame, int ladder, bool topEntry) => _movement?.Climb(frame, ladder, topEntry);

    /// <inheritdoc />
    void IPlayerChannelsContext.LogUnhandled(InteractionRequest request)
    {
        if (!KnownInteraction(request.Target))
        {
            _unknownRequests.Add($"0x{request.Target:x} {request.Request}@{request.Frame}");
        }
    }

    // Doors and ladders wait until the character simulation has started and settled.
    private bool MapActorsAllowed(double now)
    {
        if (_blackboardFrame == 0)
        {
            return false;
        }

        _mapActorsAllowedAt ??= now + MapActorStartDelay;
        return now >= _mapActorsAllowedAt;
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

    // What this client has of another player: how far its actors' delivery got, the reliable bunches it waits
    // for, the newest movement frame it confirmed (tick, state, aim pitch), when and which tick was last sent, and
    // the unreliable frames whose delivery is not known yet.
    private readonly record struct ObservedPlayer(ObservedStage Stage, uint Tick = 0,
        IReadOnlyDictionary<int, PendulumSlot>? State = null, double SentAt = 0, ushort Pitch = 0, uint SentTick = 0)
    {
        public ReliableKey[] Keys { get; init; } = [];

        public ObservedFrame[] InFlight { get; init; } = [];
    }

    // An unreliable movement frame sent to this client.
    private readonly record struct ObservedFrame(ReliableKey Key, uint Tick, IReadOnlyDictionary<int, PendulumSlot> State, ushort Pitch);
}
