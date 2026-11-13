using System.Globalization;
using System.Text.RegularExpressions;
using Ares.Raid.Replication;
using Ares.Raid.Transport;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.GameplayTags;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.World;

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
public sealed partial class PlayerChannels : IPlayerChannels
{
    private const int ControllerChannel = 2;
    private const int PlayerStateChannel = 3;
    private const int PawnChannel = 6;
    private const int SquadChannel = 48;
    private const int FirstDoorChannel = 64;
    private const int MaxDoorsInFlight = 8;

    // Time after a breach succeeds during which no pawn frame is sent, so the client keeps its Succeeded
    // interaction event long enough to play the ending.
    private const double BreachEndingSeconds = 2.0;
    private const int MaxLaddersInFlight = 8;

    // Loot containers are dormant: a channel opens to send a container's state and closes again, so a few channels
    // serve them all. New deliveries start only while most of them are free; the rest stay free for state changes.
    private const int MaxLootDeliveries = 24;
    private const int MaxLootStartsPerCheck = 8;
    private const double LootRelevanceInterval = 0.25;
    private const int LootRequestLog = 40;
    private const int LootFailureLog = 16;
    private const int InteractionRequestLog = 40;

    // Longest time to put the held item away and draw the raider tool.
    private const double ToolDrawSeconds = FerroDefaults.UnequipSeconds + HandItems.RaiderToolEquipSeconds;

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

    private Dictionary<int, (DoorChannel State, ReliableKey? Key)> _doors = [];
    private Dictionary<int, int> _doorRevisions = [];
    private Dictionary<int, ReliableKey> _doorUpdates = [];
    private HashSet<int> _failedDoors = [];
    private List<InteractionRequest> _requests = [];
    private Dictionary<int, int> _breachRequests = [];
    private (int Door, uint Checked)? _breach;
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
    private Dictionary<int, (LadderChannel State, ReliableKey? Key)> _ladders = [];
    private HashSet<int> _failedLadders = [];
    private byte[] _lootKnown = [];
    private Dictionary<int, LootLink> _lootLinks = [];
    private Dictionary<ulong, ReliableKey> _lootClassExports = [];
    private HashSet<int> _lootDirty = [];
    private HashSet<int> _failedLoot = [];
    private List<string> _lootFailures = [];
    private int _lootOpenings;
    private double _lootRelevantAt;
    private List<string> _lootRequests = [];
    private (LootContainer Container, bool DrawsTool)? _lootPress;
    private bool _lootCloseRequested;
    private (LootContainer Container, uint Frame, double RequestedAt, double HoldStartsAt, double OpensAt)? _search;

    // The interaction the server runs for the character (a container search), times in raid world seconds.
    private PawnInteraction? _interaction;
    private uint _interactionCount;
    private InteractionRequest? _lastLootRequest;
    private (LootContainer Container, int ItemContainer)? _openLoot;
    private ReliableKey? _interactorExport;
    private bool _interactorExported;
    private Dictionary<long, ObservedPlayer> _observed = [];
    private Dictionary<int, (ElevatorChannel State, ReliableKey[] Keys)> _elevators = [];
    private Dictionary<int, ExtractionPointState> _elevatorStates = [];
    private HashSet<int> _failedElevators = [];
    private List<string> _elevatorRequests = [];
    private List<string> _elevatorChanges = [];

    // This player's descend hold: the elevator, the switch (index in the elevator's buttons), the input frame of the
    // press and its server world time.
    private (ExtractionPoint Point, int Button, uint Frame, double PressedAt)? _descent;
    private List<string> _unknownRequests = [];
    private List<string> _rejectedInventory = [];
    private InteractionRequest? _lastLoggedRequest;

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
        _bindings = [(ControllerChannel, player.Controller), (PlayerStateChannel, player.PlayerState), (PawnChannel, player.Pawn)];
    }

    private enum DoorChannel
    {
        Closed,
        Exporting,
        Opening,
        Open,
        Closing,
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

    // Where a loot container's channel stands: a container the client does not have yet is exported, spawned and
    // named first; an open channel sends the container's state until the client has the latest, then closes dormant.
    private enum LootStage
    {
        Exporting,
        Spawning,
        Naming,
        Open,
        Closing,
    }

    // What the client knows of a loot container: nothing yet, or it keeps the actor showing it closed or opened.
    private enum LootKnowledge : byte
    {
        None,
        Closed,
        Opened,
    }

    private enum ElevatorChannel
    {
        Closed,
        Exporting,
        Opening,
        Open,
    }

    private enum LadderChannel
    {
        Closed,
        Exporting,
        Spawning,
        NamingInteractions,
        EnablingInteractions,
        Open,
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

    /// <summary>Door presses applied.</summary>
    public int DoorPresses { get; private set; }

    /// <summary>Door requests ignored (not a press, door not open on this connection, or still moving).</summary>
    public int IgnoredDoorRequests { get; private set; }

    /// <summary>Door breaches started.</summary>
    public int DoorBreaches { get; private set; }

    /// <summary>Door breaches this player stopped by moving.</summary>
    public int CancelledBreaches { get; private set; }

    /// <summary>Request values received on breach buttons, with their counts (a hold sends more than a press).</summary>
    public IReadOnlyDictionary<int, int> BreachRequests => _breachRequests;

    /// <summary>Whole-stack inventory swaps accepted.</summary>
    public int InventoryMoves { get; private set; }

    public int ShotsFired => _member.Weapons.ShotsFired;
    public int ReloadsCompleted => _member.Weapons.ReloadsCompleted;

    /// <summary>Inventory requests rejected and corrected.</summary>
    public int RejectedInventoryRequests { get; private set; }

    /// <summary>
    /// The latest rejected inventory requests, oldest first, as type from->to (container r revision/slot) xamount
    /// reason@input frame.
    /// </summary>
    public IReadOnlyList<string> RejectedInventoryLog => _rejectedInventory;

    /// <summary>Ladders whose climbable actor and interactions this client has.</summary>
    public int LaddersReady => _ladders.Values.Count(ladder => ladder.State == LadderChannel.Open);

    /// <summary>Ladders whose delivery started and has not finished.</summary>
    public int LaddersInProgress => _ladders.Values.Count(ladder => ladder.State is not (LadderChannel.Closed or LadderChannel.Open));

    /// <summary>Number of loot channels this client uses.</summary>
    public const int LootChannels = 32;

    /// <summary>Loot containers whose actor and search interaction this client has.</summary>
    public int LootReady => _lootKnown.Count(known => known != (byte)LootKnowledge.None);

    /// <summary>Whether this client has a loot container (it keeps it while the container is dormant).</summary>
    /// <param name="container">Container index.</param>
    /// <returns>True once the container's state reached the client.</returns>
    public bool HasLoot(int container) => Known(container) != LootKnowledge.None;

    /// <summary>Loot channels open now (or on the way): containers being delivered or updated.</summary>
    public int LootChannelsInUse => _lootLinks.Count;

    /// <summary>Loot channel opens so far: first deliveries and state changes.</summary>
    public int LootChannelOpens { get; private set; }

    /// <summary>Loot containers whose channel the client closed: they are not delivered again to this player.</summary>
    public int LootFailed => _failedLoot.Count;

    /// <summary>
    /// The first loot containers whose channel the client closed, oldest first, as source:class@stage (the step whose
    /// bunch the channel was on: Spawning = the open, Naming = the component names, Open = a state).
    /// </summary>
    public IReadOnlyList<string> LootFailures => _lootFailures;

    /// <summary>Loot containers relevant to the player that the client does not have yet.</summary>
    public int LootWaiting =>
        _raid.Loot.Relevant(LootPosition).Count(container => Known(container.Index) == LootKnowledge.None && !_failedLoot.Contains(container.Index));

    /// <summary>How the raid's container sockets turned into containers.</summary>
    public LootSpawnCounts LootSpawn => _raid.Loot.Counts;

    /// <summary>The seed the raid's loot was rolled with.</summary>
    public int LootSeed => _raid.Loot.Seed;

    /// <summary>The raid's containers that do not stand on prop sockets, with their areas.</summary>
    public IReadOnlyList<FreeStandingContainer> LootFreeStanding => _raid.Loot.FreeStanding;

    /// <summary>Match id of the raid.</summary>
    public string MatchId => _raid.MatchId;

    /// <summary>Searches started on loot containers.</summary>
    public int LootSearches { get; private set; }

    /// <summary>Searches the player stopped by moving.</summary>
    public int CancelledSearches { get; private set; }

    /// <summary>Loot containers opened by this player.</summary>
    public int LootOpens { get; private set; }

    /// <summary>Requests received on loot search interactions, oldest first, as request@input frame.</summary>
    public IReadOnlyList<string> LootRequests => _lootRequests;

    /// <summary>Extraction elevators whose state and buttons this client has.</summary>
    public int ElevatorsReady => _elevators.Values.Count(elevator => elevator.State == ElevatorChannel.Open);

    /// <summary>
    /// Elevator states this client has, by elevator index: the state (Called while the call press runs) and the
    /// server world time it started, with the descend switch held (+switch@press, ~hold@hold start once the hold runs)
    /// or, once the elevator left, the one that sent it (^switch).
    /// </summary>
    public string ElevatorStates => string.Join(' ', _elevatorStates.OrderBy(entry => entry.Key).Select(entry => Describe(entry.Key, entry.Value)));

    /// <summary>Calls of an elevator by this player.</summary>
    public int ElevatorCalls { get; private set; }

    /// <summary>Elevator state changes this client got, oldest first, as elevator:state@server time (see <see cref="ElevatorStates"/>).</summary>
    public IReadOnlyList<string> ElevatorChanges => _elevatorChanges;

    /// <summary>Descend holds started by this player.</summary>
    public int ElevatorDescents { get; private set; }

    /// <summary>Descend holds this player stopped by moving.</summary>
    public int CancelledDescents { get; private set; }

    /// <summary>Descend holds of this player that sent an elevator off.</summary>
    public int CompletedDescents { get; private set; }

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
    public IReadOnlyList<string> ElevatorRequests => _elevatorRequests;

    /// <summary>
    /// Presses on no target or on an object the server does not know, oldest first, as target request@input frame.
    /// </summary>
    public IReadOnlyList<string> UnknownInteractionRequests => _unknownRequests;

    /// <summary>Client calls received and not handled yet, by name.</summary>
    public IReadOnlyDictionary<string, int> IgnoredCalls => _ignoredCalls;

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
        copy._doors = new Dictionary<int, (DoorChannel, ReliableKey?)>(_doors);
        copy._doorRevisions = new Dictionary<int, int>(_doorRevisions);
        copy._doorUpdates = new Dictionary<int, ReliableKey>(_doorUpdates);
        copy._failedDoors = [.. _failedDoors];
        copy._breachRequests = new Dictionary<int, int>(_breachRequests);
        copy._requests = [.. _requests];
        copy._inventoryRequests = [.. _inventoryRequests];
        copy._weaponRequests = [.. _weaponRequests];
        copy._ladders = new Dictionary<int, (LadderChannel, ReliableKey?)>(_ladders);
        copy._failedLadders = [.. _failedLadders];
        copy._lootKnown = (byte[])_lootKnown.Clone();
        copy._lootLinks = new Dictionary<int, LootLink>(_lootLinks);
        copy._lootClassExports = new Dictionary<ulong, ReliableKey>(_lootClassExports);
        copy._lootDirty = [.. _lootDirty];
        copy._failedLoot = [.. _failedLoot];
        copy._lootFailures = [.. _lootFailures];
        copy._lootRequests = [.. _lootRequests];
        copy._observed = new Dictionary<long, ObservedPlayer>(_observed);
        copy._elevators = new Dictionary<int, (ElevatorChannel, ReliableKey[])>(_elevators);
        copy._elevatorStates = new Dictionary<int, ExtractionPointState>(_elevatorStates);
        copy._failedElevators = [.. _failedElevators];
        copy._elevatorRequests = [.. _elevatorRequests];
        copy._elevatorChanges = [.. _elevatorChanges];
        copy._unknownRequests = [.. _unknownRequests];
        copy._rejectedInventory = [.. _rejectedInventory];
        CopyPickups(copy);
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
        if (bunch.Channel == SquadChannel || DoorIndex(bunch.Channel) is not null || LadderIndex(bunch.Channel) is not null
            || LootIndex(bunch.Channel) is not null || ElevatorIndex(bunch.Channel) is not null || ObservedIndex(bunch.Channel) is not null
            || PickupIndex(bunch.Channel) is not null)
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
            io.Sender.Queue(Bunches.Actor(ControllerChannel, calls.Call(PlayerClasses.PlayerController.Class, function, parameters, RpcDirection.Client)));
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
        ServiceDoors(io.Sender, now);
        ServiceLadders(io.Sender, now);
        ServiceLoot(io.Sender, now);
        ServicePickups(io.Sender, now);
        ServiceElevators(io.Sender, now);
        ServiceObserved(io.Sender, now);
        ServiceDeparture(io.Sender, now);
    }

    /// <inheritdoc />
    public double ServerSeconds(double now) => _raid.WorldSeconds(now);

    /// <inheritdoc />
    public void ActorChannelFailed(int channel)
    {
        if (DoorIndex(channel) is { } index)
        {
            _failedDoors.Add(index);
        }
        else if (LadderIndex(channel) is { } ladder)
        {
            _failedLadders.Add(ladder);
        }
        else if (LootIndex(channel) is not null)
        {
            foreach ((int container, LootLink link) in _lootLinks)
            {
                if (link.Channel != channel || !_failedLoot.Add(container) || _lootFailures.Count >= LootFailureLog)
                {
                    continue;
                }

                LootContainer failed = _raid.Loot.Containers[container];
                _lootFailures.Add($"{failed.Source}:{FreeStandingAreas.ShortName(failed.Class.Container.Class)}@{link.Stage}");
            }
        }
        else if (ElevatorIndex(channel) is { } elevator)
        {
            _failedElevators.Add(elevator);
        }
        else if (ObservedIndex(channel) is { } observed && observed < _raid.Players.Count)
        {
            _observed[_raid.Players[observed].Id] = new ObservedPlayer(ObservedStage.Gone);
        }
        else if (PickupIndex(channel) is not null)
        {
            PickupChannelFailed(channel);
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
            LootContainer? loot = SearchableLoot(request.Target);
            bool repeated = _lastLootRequest is { } last && last.Frame + 1 == request.Frame && last.Target == request.Target && last.Request == request.Request;
            if (loot is not null || request.Target == 0 || _openLoot is not null)
            {
                _lastLootRequest = request;
            }

            // An open container closes when the client says its looting menu closed.
            if (_openLoot is not null && (request.Request & PendulumInput.LootingMenuClosedRequest) != 0)
            {
                if (!repeated)
                {
                    LogLootRequest($"{request.Request}@{request.Frame}");
                }

                _lootCloseRequested = true;
                continue;
            }

            // A held press repeats on the following input frames; it is logged once.
            bool repeatedLog = _lastLoggedRequest is { } logged && logged.Frame + 1 == request.Frame && logged.Target == request.Target && logged.Request == request.Request;
            _lastLoggedRequest = request;
            if (request.Target == 0 || (_openLoot is not null && loot is not null))
            {
                if (request.Target == 0 && (request.Request & PendulumInput.PressRequest) != 0 && !repeatedLog)
                {
                    LogRequest(_unknownRequests, $"0x0 {request.Request}@{request.Frame}");
                }

                continue;
            }

            if (loot is not null)
            {
                // Like a breach, one press starts the search, also when it takes the raider tool out first.
                if (!repeated)
                {
                    LogLootRequest($"{request.Request}@{request.Frame}");
                    if ((request.Request & PendulumInput.PressRequest) != 0 && _search is null)
                    {
                        _lootPress = (loot, (request.Request & PendulumInput.EquipToolRequestFlag) != 0);
                    }
                }

                continue;
            }

            if (PickableUp(request.Target) is { } pickup)
            {
                RequestPickup(pickup, request, repeatedLog, now);
                continue;
            }

            if (_raid.Extraction.ByButton(request.Target) is { } elevator)
            {
                if (!repeatedLog)
                {
                    LogRequest(_elevatorRequests, $"{elevator.Point.Name}/{elevator.Button.Component} {request.Request}@{request.Frame}");
                }

                // A press on a closed elevator's call button calls it. The client runs the press itself (the button
                // is focused and usable in its pawn frames); no frame may reset it until its ending has played.
                // A press on a descend switch of a ready elevator starts its hold, which the client runs the same way
                // (lining up, then holding); the server ends it.
                if ((request.Request & PendulumInput.PressRequest) != 0 && CallableElevator(request.Target) is { } called
                    && _raid.ExtractionStates.Call(called.Index, _member.Id, ServerSeconds(now)))
                {
                    ElevatorCalls++;
                    _framesHeldUntil = Math.Max(_framesHeldUntil, now + ExtractionStates.CallSeconds + BreachEndingSeconds);
                }
                else if ((request.Request & PendulumInput.PressRequest) != 0 && _descent is null && DescendableElevator(request.Target) is { } descend
                    && _raid.ExtractionStates.Descend(descend.Point.Index, descend.Button, _member.Id, ServerSeconds(now)))
                {
                    ElevatorDescents++;
                    _descent = (descend.Point, descend.Button, request.Frame, ServerSeconds(now));
                }

                continue;
            }

            if (_raid.Doors.ByBreach(request.Target) is not null)
            {
                _breachRequests[request.Request] = _breachRequests.GetValueOrDefault(request.Request) + 1;
            }

            // The request is a set of flags; a press has the press flag, with the take-out-raider-tool flag when
            // the breach is pressed with the tool holstered. Other flags are not decoded yet.
            if ((request.Request & PendulumInput.PressRequest) == 0)
            {
                IgnoredDoorRequests++;
            }
            else if (UsableDoor(request.Target) is { } door && _raid.DoorStates.Press(door.Index, now))
            {
                DoorPresses++;
            }
            else if (BreachableDoor(request.Target) is { } barricaded && _raid.DoorStates.StartBreach(barricaded.Index, now))
            {
                DoorBreaches++;
                _breach = (barricaded.Index, request.Frame);
            }
            else
            {
                IgnoredDoorRequests++;
                if (!repeatedLog && !KnownInteraction(request.Target))
                {
                    LogRequest(_unknownRequests, $"0x{request.Target:x} {request.Request}@{request.Frame}");
                }
            }
        }

        _requests.Clear();
        CancelBreachOnMovement(now);
        CancelDescentOnMovement();
        if (Phase == DeliveryPhase.Possessed)
        {
            foreach ((uint frame, InventoryCommand command) in _inventoryRequests)
            {
                if (_member.Inventory.Apply(command, out string? rejection))
                {
                    InventoryMoves++;
                    PlaceDrops(now);
                }
                else
                {
                    RejectedInventoryRequests++;
                    LogRequest(_rejectedInventory, $"{command.Type} {Slot(command.From)}->{Slot(command.To)} x{command.Amount} {rejection}@{frame}");
                }

                _inventoryFrame = Math.Max(_inventoryFrame, frame);
            }
        }

        _inventoryRequests.Clear();
        if (Phase == DeliveryPhase.Possessed && _blackboardFrame != 0 && InputFrame is { } latest)
        {
            // Each simulated frame moves with the weapon its own input left in the character's hands.
            Queue<WeaponInput> weapons = new(_weaponRequests.Where(input => input.Frame < latest).OrderBy(input => input.Frame));
            while (_movement is { } movement && movement.Frame + 1 < latest)
            {
                while (weapons.TryPeek(out WeaponInput input) && input.Frame <= movement.Frame + 1)
                {
                    _member.Weapons.Apply(weapons.Dequeue(), _weaponReferencesMapped ? _player.HeldItems : null);
                }

                movement.Advance(_input, _member.Weapons.Armed);
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
                _keys = [sender.Queue(Bunches.Actor(ControllerChannel, _raid.State.HudAssignment(_player)))];
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
                _keys = [sender.Queue(new ActorInitialization(squadSpawn, null, []).ToBunch(SquadChannel, sender.NextSequence(SquadChannel), []))];
                Phase = DeliveryPhase.SquadOpenPending;
                return true;
            case DeliveryPhase.SquadOpenPending when delivered:
                _mapped.UnionWith([_player.Squad, .. _raid.SpawnedActors.Select(actor => actor.Guid)]);
                _keys = [sender.Queue(Bunches.Actor(SquadChannel, _raid.State.SquadMemberContent(_player, _mapped).Write))];
                Phase = DeliveryPhase.SquadMemberPending;
                return true;
            case DeliveryPhase.SquadMemberPending when delivered:
                _keys =
                [
                    sender.Queue(Bunches.Actor(SquadChannel, _raid.State.SquadContent(_player, _mapped))),
                    sender.Queue(Bunches.Actor(RaidWorld.GameStateChannel, _raid.State.SquadRoster(_player, _mapped))),
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
                    sender.Queue(Bunches.Actor(ControllerChannel, _raid.State.Replication.Calls(_raid.State.Objects(_mapped)).Call(
                        PlayerClasses.PlayerController.Class, FunctionNames.ClientReceiveSquadPlayersProfiles,
                        ProfileParameters(_member.Profiles), RpcDirection.Client)));
                }

                Phase = DeliveryPhase.InitialLevelsPending;
                return true;
            case DeliveryPhase.InitialLevelsPending when _initialLevelsLoaded:
                // Starting the character earlier lets its spawn animation finish behind the loading screen.
                WorldActor pawn = Actor(_player.Pawn);
                ActorSpawn pawnSpawn = new(pawn.Guid, _raid.Imports.Archetypes[pawn.ClassPath], 0, pawn.Location, pawn.Rotation);
                _keys = [sender.Queue(new ActorInitialization(pawnSpawn, null, [SpawnFrame()]).ToBunch(PawnChannel, sender.NextSequence(PawnChannel), []))];
                Phase = DeliveryPhase.PawnOpenPending;
                return true;
            case DeliveryPhase.PawnOpenPending when delivered:
                _mapped.Add(_player.Pawn);
                _keys = [sender.Queue(Bunches.Actor(PawnChannel, _raid.State.PawnInitialProperties(_player, _mapped)))];
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
                _keys = [sender.Queue(Bunches.Actor(PawnChannel, _raid.State.AppearanceContent(_player).Write))];
                Phase = DeliveryPhase.AppearanceContentPending;
                return true;
            case DeliveryPhase.AppearanceContentPending when delivered:
                _keys = [sender.Queue(Bunches.Exports(_raid.State.InventoryExports(_player), [_player.Inventory, _player.Health]))];
                Phase = DeliveryPhase.InventoryExportsPending;
                return true;
            case DeliveryPhase.InventoryExportsPending when delivered:
                _keys =
                [
                    sender.Queue(Bunches.Actor(PawnChannel, _raid.State.InventoryContent(_player, _member.Inventory).Write)),
                    sender.Queue(Bunches.Actor(PawnChannel, _raid.State.ShieldContent(_player, _member.Inventory).Write)),
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
                _keys = [sender.Queue(Bunches.Actor(PawnChannel, _raid.State.AbilityComponentContent(_player).Write))];
                Phase = DeliveryPhase.AbilityComponentPending;
                return true;
            case DeliveryPhase.AbilityComponentPending when delivered:
                IReadOnlyList<DynamicSubobjectContent> sets = _raid.State.AttributeSetCreation(_player, _member.Inventory);
                _keys = [sender.Queue(Bunches.Actor(PawnChannel, writer =>
                {
                    foreach (DynamicSubobjectContent set in sets)
                    {
                        set.Write(writer);
                    }
                }))];
                Phase = DeliveryPhase.AbilityCreationPending;
                return true;
            case DeliveryPhase.AbilityCreationPending when delivered:
                _keys = [sender.Queue(Bunches.Actor(PawnChannel, _raid.State.AttributeSetRegistration(_player).Write))];
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
        _inputAckKeys = [sender.Queue(Bunches.Actor(ControllerChannel, content.Write))];
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

        _worldTimeKey = sender.Queue(Bunches.Actor(RaidWorld.GameStateChannel, _raid.State.WorldTime(_raid.WorldSeconds(now))));
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
                    .Select(item => sender.Queue(Bunches.Actor(PawnChannel, item.Write)))];
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
                _weaponReferenceKeys = [sender.Queue(Bunches.Actor(PawnChannel, _raid.State.WeaponItemReferences(_player).Write))];
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
                _member.Inventory.InitialBlackboard, _member.Weapons.Variables(frame, weaponObjects), TimeOffsetAt(frame, now));
            _movement = new PlayerMovement(frame, pawn.Location, yaw);
        }
        else
        {
            if (InteractionFramesHeld(now))
            {
                return;
            }

            focus = UsableDoor(_input.Focus) is null && BreachableDoor(_input.Focus) is null && SearchableLoot(_input.Focus) is null
                && CallableElevator(_input.Focus) is null && DescendableElevator(_input.Focus) is null && PickableUp(_input.Focus) is null ? 0 : _input.Focus;
            frame = latest - 1;
            bool inventoryChanged = _inventoryVersion != _member.Inventory.Version;
            bool weaponsChanged = _weaponVersion != _member.Weapons.Version;
            if ((!inventoryChanged && !weaponsChanged && focus == _blackboardFocus) || frame <= _blackboardFrame || frame < _inventoryFrame
                || _input.Position(latest) is not { } position || _input.Position(frame) is not { } before || _input.Yaw(frame) is not { } yaw
                || _movement?.State(frame) is not { } character || character.Stance == CharacterStance.Slide || character.SlideExitTicks > 0)
            {
                // The slide's own state is not written yet, so a frame would end it; wait until it is over.
                return;
            }

            // Vertical motion is not simulated yet; it stays the reported position change.
            Vector3D velocity = character.Velocity with { Z = (position.Z - before.Z) / PawnBlackboard.TickSeconds };
            // The character's interactor shows the server-run interaction (an open container is still its search).
            state = PawnBlackboardFrames.State(frame, new PawnMotion(position, velocity, yaw, character), focus, _member.Inventory.Blackboard,
                _member.Weapons.Variables(frame, weaponObjects), TimeOffsetAt(frame, now),
                _interaction is { } interaction ? OnCharacterClock(interaction) : null);
        }

        // Delivery numbers address the client's snapshot ring; the graph inside carries the input tick.
        uint delivery = checked((uint)BlackboardFrames);
        List<ReliableKey> keys = [sender.Queue(Bunches.Actor(PawnChannel, _raid.State.BlackboardFrame(delivery, delivery + 1, state, _blackboardState)))];
        if (_shield != _member.Inventory.Shield)
        {
            keys.Add(sender.Queue(Bunches.Actor(PawnChannel, _raid.State.ShieldContent(_player, _member.Inventory).Write)));
            _shield = _member.Inventory.Shield;
        }

        _blackboardKeys = [.. keys];
        (_blackboardFrame, _blackboardState, _blackboardFocus) = (frame, state, focus);
        _inventoryVersion = initializingContainers ? 1 : _member.Inventory.Version;
        _weaponVersion = _member.Weapons.Version;
        _populatedInventorySent = !initializingContainers;
        BlackboardFrames++;
    }

    // The door of a button the client may use: possessed, door replicated on this connection, cell visible,
    // and the door not barricaded, locked or without power.
    private Door? UsableDoor(ulong button) =>
        _raid.Doors.ByButton(button) is { } door && Replicated(door) && _raid.DoorStates.Usable(door.Index) ? door : null;

    // Pawn frames carry no interaction state, so the client's own one (requested, holding, succeeded) is reset by
    // each frame; none are sent while this player breaches and just after it succeeds.
    private bool InteractionFramesHeld(double now) => _breach is not null || _search is not null || _descent is not null || now < _framesHeldUntil;

    // The breach is a hold that movement input cancels (the client stops it on its side). The client sends no
    // request when it does, so the server watches the breaching player's movement axes after the start frame.
    private void CancelBreachOnMovement(double now)
    {
        if (_breach is not { } breach)
        {
            return;
        }

        if (!_raid.DoorStates.Breaching(breach.Door))
        {
            _breach = null;
            _framesHeldUntil = now + BreachEndingSeconds;
            return;
        }

        uint latest = InputFrame ?? breach.Checked;
        for (uint frame = breach.Checked + 1; frame <= latest; frame++)
        {
            if (_input.Movement(frame) is null or { Forward: 0, Right: 0 })
            {
                continue;
            }

            _raid.DoorStates.CancelBreach(breach.Door);
            CancelledBreaches++;
            _breach = null;
            return;
        }

        _breach = breach with { Checked = Math.Max(breach.Checked, latest) };
    }

    // The door of a breach button the client may use: replicated as above and still barricaded.
    private Door? BreachableDoor(ulong button) =>
        _raid.Doors.ByBreach(button) is { } door && Replicated(door) && _raid.DoorStates.Breachable(door.Index) ? door : null;

    private bool Replicated(Door door) =>
        Phase == DeliveryPhase.Possessed && !_failedDoors.Contains(door.Index)
        && _doors.GetValueOrDefault(door.Index).State == DoorChannel.Open && _visibleLevels.Contains(door.Cell);

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
        sender.Queue(Bunches.Actor(ControllerChannel, calls.Call(PlayerClasses.PlayerController.Class, FunctionNames.ClientSetBlockOnAsyncLoading, new Dictionary<string, object?>(), RpcDirection.Client)));
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
        _abilityKeys = [sender.Queue(Bunches.Actor(PawnChannel, _raid.State.AbilityGrants(_player, abilities).Write))];
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

    // Replicates the doors of the cells the client reports visible: exports each door's path, opens its
    // channel with usable buttons, sends state changes, and closes the channel when the cell unloads.
    private void ServiceDoors(ReliableSender sender, double now)
    {
        if (!MapActorsAllowed(now))
        {
            return;
        }

        _raid.DoorStates.Advance(now);
        int inFlight = _doors.Values.Count(door => door.Key is { } key && sender.Pending(key));
        foreach (Door door in _raid.Doors.Doors)
        {
            (DoorChannel state, ReliableKey? key) = _doors.GetValueOrDefault(door.Index, (DoorChannel.Closed, null));
            if (key is { } waiting && sender.Pending(waiting))
            {
                continue;
            }

            int channel = FirstDoorChannel + door.Index;
            (DoorState value, int revision, _) = _raid.DoorStates.Get(door.Index);
            switch (state)
            {
                case DoorChannel.Exporting:
                    (state, key) = (DoorChannel.Opening, sender.Queue(_raid.Doors.OpenBunch(door, channel, sender.NextSequence(channel), _raid.DoorStates.Buttons(door.Index), value)));
                    _doorRevisions[door.Index] = revision;
                    break;
                case DoorChannel.Opening:
                    (state, key) = (DoorChannel.Open, null);
                    break;
                case DoorChannel.Closing:
                    (state, key) = (DoorChannel.Closed, null);
                    break;
            }

            bool wanted = _visibleLevels.Contains(door.Cell) && !_failedDoors.Contains(door.Index);
            switch (state)
            {
                case DoorChannel.Closed when wanted && inFlight < MaxDoorsInFlight:
                    (state, key) = (DoorChannel.Exporting, sender.Queue(_raid.Doors.ExportBunch(door)));
                    inFlight++;
                    break;
                case DoorChannel.Open when !wanted:
                    (state, key) = (DoorChannel.Closing, sender.Queue(Bunches.Close(channel, Bunches.LevelUnloaded)));
                    _doorUpdates.Remove(door.Index);
                    break;
                case DoorChannel.Open when revision != _doorRevisions.GetValueOrDefault(door.Index)
                    && (!_doorUpdates.TryGetValue(door.Index, out ReliableKey update) || !sender.Pending(update)):
                    _doorUpdates[door.Index] = sender.Queue(_raid.Doors.UpdateBunch(door, channel, value, _raid.DoorStates.Buttons(door.Index)));
                    _doorRevisions[door.Index] = revision;
                    break;
            }

            _doors[door.Index] = (state, key);
        }
    }

    // Gives the client each extraction elevator: exports its buttons by path, then opens its channel with the
    // raid's elevator state and the button states. The channel stays open and gets every later change.
    private void ServiceElevators(ReliableSender sender, double now)
    {
        _raid.ExtractionStates.Advance(ServerSeconds(now), _raid.PlayersInside);
        ServiceDescent(sender, now);
        if (!MapActorsAllowed(now))
        {
            return;
        }

        foreach (ExtractionPoint point in _raid.Extraction.Points)
        {
            (ElevatorChannel state, ReliableKey[] keys) = _elevators.GetValueOrDefault(point.Index, (ElevatorChannel.Closed, []));
            if (Array.Exists(keys, sender.Pending))
            {
                continue;
            }

            int channel = FirstElevatorChannel + point.Index;
            ExtractionPointState value = _raid.ExtractionStates.Get(point.Index);
            switch (state)
            {
                case ElevatorChannel.Closed when !_failedElevators.Contains(point.Index):
                    (state, keys) = (ElevatorChannel.Exporting, [.. _raid.Extraction.ExportBunches(point).Select(sender.Queue)]);
                    break;
                case ElevatorChannel.Exporting:
                    (state, keys) = (ElevatorChannel.Opening, [sender.Queue(_raid.Extraction.OpenBunch(point, channel, sender.NextSequence(channel), value,
                        ServerSeconds(now)))]);
                    _elevatorStates[point.Index] = value;
                    LogElevatorChange(point.Index, value);
                    break;
                case ElevatorChannel.Opening:
                    (state, keys) = (ElevatorChannel.Open, []);
                    break;
                case ElevatorChannel.Open when value != _elevatorStates[point.Index]:
                    keys = _raid.Extraction.UpdateBunch(point, channel, _elevatorStates[point.Index], value) is { } update ? [sender.Queue(update)] : [];
                    _elevatorStates[point.Index] = value;
                    LogElevatorChange(point.Index, value);
                    break;
            }

            _elevators[point.Index] = (state, keys);
        }
    }

    // Ends this player's descend hold once the elevator moved on. When the hold sent the elevator off, the character's
    // interactor gets the succeeded hold (the switch's animation set) and the end of the interaction, and no pawn frame
    // is sent until its ending has played; otherwise (the elevator left on its own) the hold just stops.
    private void ServiceDescent(ReliableSender sender, double now)
    {
        if (_descent is not { } descent)
        {
            return;
        }

        ExtractionPointState state = _raid.ExtractionStates.Get(descent.Point.Index);
        if (state.Descent?.Player == _member.Id)
        {
            return;
        }

        if (state.Departed is { } departed && departed.Player == _member.Id && departed.PressedAt.Equals(descent.PressedAt))
        {
            if (!_interactorExported)
            {
                return;
            }

            ulong button = descent.Point.Buttons[descent.Button];
            _interaction = new PawnInteraction(button, LastInteractionEvent.Succeeded, departed.PressedAt, departed.PressedAt, departed.HoldStartsAt,
                ++_interactionCount, Animation: _raid.Extraction.Animation(descent.Button));
            sender.Queue(Bunches.Actor(PawnChannel, _raid.State.InteractorCall(_player, FunctionNames.ClientEndInteractionSuccessfullyImplementation, button,
                DamExtraction.Buttons[descent.Button].Class).Write));
            CompletedDescents++;
        }

        _descent = null;
        _framesHeldUntil = Math.Max(_framesHeldUntil, now + BreachEndingSeconds);
    }

    // A descend hold is a hold that movement input cancels, like a breach (the client stops it on its side and sends
    // no request): the server watches the player's movement axes after the press frame.
    private void CancelDescentOnMovement()
    {
        if (_descent is not { } descent || !MovedSince(descent.Frame) || !_raid.ExtractionStates.CancelDescent(descent.Point.Index, _member.Id))
        {
            return;
        }

        CancelledDescents++;
        _descent = null;
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
            _descent = null;
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

            sender.Queue(Bunches.Actor(ControllerChannel, _raid.State.EndOfRoundCall(_player, FunctionNames.ClientFinalizeQuestProgress,
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
        sender.Queue(Bunches.Actor(ControllerChannel, _raid.State.Replication.Calls(_raid.State.Objects(_mapped))
            .Call(PlayerClasses.PlayerController.Class, function, parameters, RpcDirection.Client)));

    private void LogElevatorChange(int index, ExtractionPointState state)
    {
        string entry = Describe(index, state);
        if (_elevatorChanges.Count == 0 || _elevatorChanges[^1] != entry)
        {
            LogRequest(_elevatorChanges, entry);
        }
    }

    private static string Describe(int index, ExtractionPointState state)
    {
        string text = $"{index}:{(state.CalledAt is null ? state.State.ToString() : "Called")}" + (state.ChangedAt is { } at ? $"@{Seconds(at)}" : string.Empty);
        if (state.Descent is { } descent)
        {
            text += $"+{DamExtraction.Buttons[descent.Button].Component}@{Seconds(descent.PressedAt)}"
                + (descent.Holding ? $"~hold@{Seconds(descent.HoldStartsAt)}" : string.Empty);
        }
        else if (state.Departed is { } departed && state.State != ExtractionState.Ready)
        {
            text += $"^{DamExtraction.Buttons[departed.Button].Component}";
        }

        return text;
    }

    private static string Seconds(double value) => value.ToString("0.000", CultureInfo.InvariantCulture);

    // Gives the client the climbable actor of each ladder in the cells it reports visible: exports the class
    // default and the placed ladder, spawns the actor attached to it, then names and enables its climb
    // interactions. The actors stay when their cell unloads, as the game's own ladder actors do.
    private void ServiceLadders(ReliableSender sender, double now)
    {
        if (!MapActorsAllowed(now))
        {
            return;
        }

        int firstChannel = FirstDoorChannel + _raid.Doors.Doors.Count;
        int inFlight = _ladders.Values.Count(ladder => ladder.Key is { } key && sender.Pending(key));
        foreach (Ladder ladder in _raid.Ladders.Ladders)
        {
            (LadderChannel state, ReliableKey? key) = _ladders.GetValueOrDefault(ladder.Index, (LadderChannel.Closed, null));
            if (key is { } waiting && sender.Pending(waiting))
            {
                continue;
            }

            int channel = firstChannel + ladder.Index;
            switch (state)
            {
                case LadderChannel.Closed when inFlight < MaxLaddersInFlight && _visibleLevels.Contains(ladder.Cell) && !_failedLadders.Contains(ladder.Index):
                    (state, key) = (LadderChannel.Exporting, sender.Queue(_raid.Ladders.ExportBunch(ladder)));
                    inFlight++;
                    break;
                case LadderChannel.Exporting:
                    (state, key) = (LadderChannel.Spawning, sender.Queue(_raid.Ladders.OpenBunch(ladder, channel, sender.NextSequence(channel))));
                    break;
                case LadderChannel.Spawning:
                    (state, key) = (LadderChannel.NamingInteractions, sender.Queue(_raid.Ladders.InteractionExportBunch(ladder)));
                    break;
                case LadderChannel.NamingInteractions:
                    (state, key) = (LadderChannel.EnablingInteractions, sender.Queue(_raid.Ladders.InteractionBunch(ladder, channel)));
                    break;
                case LadderChannel.EnablingInteractions:
                    (state, key) = (LadderChannel.Open, null);
                    break;
            }

            _ladders[ladder.Index] = (state, key);
        }
    }

    // Gives the client the loot containers once map actors are allowed. Containers are dormant: a channel opens
    // for a container that becomes relevant (within the net cull distance; nearest first, a few at a time) to export
    // its class, spawn it at its socket, name its components and send its state, then closes dormant while the
    // client keeps the actor. A later change (a first opening by anyone, this player's search) opens a channel
    // again for a client that has the container, wherever the player is, and sends the new state.
    private void ServiceLoot(ReliableSender sender, double now)
    {
        if (!MapActorsAllowed(now))
        {
            return;
        }

        // Containers spawned during the raid extend the list; what the player knows of the others stays.
        if (_lootKnown.Length != _raid.Loot.Containers.Count)
        {
            Array.Resize(ref _lootKnown, _raid.Loot.Containers.Count);
        }

        IReadOnlyList<int> openings = _raid.LootStates.Openings;
        for (; _lootOpenings < openings.Count; _lootOpenings++)
        {
            if (Known(openings[_lootOpenings]) != LootKnowledge.None)
            {
                _lootDirty.Add(openings[_lootOpenings]);
            }
        }

        ServiceLootLinks(sender);
        ServiceSearch(sender, now);
        foreach (int index in _lootDirty.ToArray())
        {
            if (_lootLinks.ContainsKey(index))
            {
                continue;
            }

            // A dormant container shows the raid's state of it; it needs its channel again when that changed or this
            // player searches it.
            LootContainer container = _raid.Loot.Containers[index];
            bool current = !LootHeld(container) && DefaultLootKnowledge(container) == Known(index);
            if (current || _failedLoot.Contains(index) || PushLoot(sender, container))
            {
                _lootDirty.Remove(index);
            }
        }

        if (now < _lootRelevantAt)
        {
            return;
        }

        _lootRelevantAt = now + LootRelevanceInterval;
        int started = 0;
        foreach (LootContainer container in _raid.Loot.Relevant(LootPosition))
        {
            if (_lootLinks.Count >= MaxLootDeliveries || started >= MaxLootStartsPerCheck)
            {
                break;
            }

            if (Known(container.Index) != LootKnowledge.None || _lootLinks.ContainsKey(container.Index) || _failedLoot.Contains(container.Index)
                || FreeLootChannel() is not { } channel)
            {
                continue;
            }

            // The class default goes once per client; containers of a class wait for its export.
            ReliableKey? export = _lootClassExports.TryGetValue(container.Archetype, out ReliableKey key) && sender.Pending(key) ? key : null;
            if (!_lootClassExports.ContainsKey(container.Archetype))
            {
                export = _lootClassExports[container.Archetype] = sender.Queue(_raid.Loot.ExportBunch(container));
            }

            _lootLinks[container.Index] = new LootLink(LootStage.Exporting, channel, export, default);
            started++;
        }

        ServiceLootLinks(sender);
    }

    // Moves each loot channel on by the steps whose bunches the client has.
    private void ServiceLootLinks(ReliableSender sender)
    {
        foreach ((int index, LootLink current) in _lootLinks.ToArray())
        {
            LootLink link = current;
            bool waiting = link.Key is { } key && sender.Pending(key);
            if (waiting && link.Stage != LootStage.Open)
            {
                continue;
            }

            LootContainer container = _raid.Loot.Containers[index];
            switch (link.Stage)
            {
                case LootStage.Exporting:
                    link = link with { Stage = LootStage.Spawning, Key = sender.Queue(OpenLootBunch(sender, container, link.Channel)) };
                    break;
                case LootStage.Spawning:
                    link = link with { Stage = LootStage.Naming, Key = sender.Queue(LootCatalog.ComponentExportBunch(container)) };
                    break;
                case LootStage.Naming:
                    LootView first = LootViewOf(container);
                    link = link with { Stage = LootStage.Open, Key = sender.Queue(_raid.Loot.StateBunch(container, link.Channel, first)), Sent = first };
                    _lootKnown[index] = (byte)Knowledge(first);
                    break;
                case LootStage.Open:
                    link = SendLootState(sender, container, link);
                    if (link.Key is { } sent && sender.Pending(sent))
                    {
                        break;
                    }

                    // Nothing left to send: the container goes dormant, unless this player is searching it.
                    if (!LootHeld(container))
                    {
                        link = link with { Stage = LootStage.Closing, Key = sender.Queue(Bunches.Close(link.Channel, Bunches.Dormancy)) };
                    }

                    break;
                case LootStage.Closing:
                    _lootLinks.Remove(index);
                    if (_failedLoot.Contains(index))
                    {
                        _lootKnown[index] = (byte)LootKnowledge.None;
                    }
                    else if (LootViewOf(container) != link.Sent)
                    {
                        _lootDirty.Add(index);
                    }

                    continue;
            }

            _lootLinks[index] = link;
        }
    }

    // Brings the client's copy of a container up to date now: on its open channel, or by opening a channel again
    // for a dormant container. Returns false when no channel is free yet.
    private bool PushLoot(ReliableSender sender, LootContainer container)
    {
        if (_lootLinks.TryGetValue(container.Index, out LootLink link))
        {
            if (link.Stage == LootStage.Open)
            {
                _lootLinks[container.Index] = SendLootState(sender, container, link);
            }
            else
            {
                _lootDirty.Add(container.Index);
            }

            return true;
        }

        if (Known(container.Index) == LootKnowledge.None || FreeLootChannel() is not { } channel)
        {
            _lootDirty.Add(container.Index);
            return false;
        }

        sender.Queue(OpenLootBunch(sender, container, channel));
        LootView view = LootViewOf(container);
        _lootLinks[container.Index] = new LootLink(LootStage.Open, channel, sender.Queue(_raid.Loot.StateBunch(container, channel, view)), view);
        _lootKnown[container.Index] = (byte)Knowledge(view);
        return true;
    }

    // Sends a container's state on its open channel when the client's copy differs.
    private LootLink SendLootState(ReliableSender sender, LootContainer container, LootLink link)
    {
        LootView view = LootViewOf(container);
        if (view == link.Sent)
        {
            return link;
        }

        _lootKnown[container.Index] = (byte)Knowledge(view);
        return link with { Key = sender.Queue(_raid.Loot.StateBunch(container, link.Channel, view)), Sent = view };
    }

    private ChannelBunch OpenLootBunch(ReliableSender sender, LootContainer container, int channel)
    {
        LootChannelOpens++;
        return _raid.Loot.OpenBunch(container, channel, sender.NextSequence(channel), _raid.LootStates.OpenedAt(container.Index));
    }

    // What this player's client should show of a container: this player's search or open container, else the
    // raid's state of it.
    private LootView LootViewOf(LootContainer container)
    {
        double? opened = _raid.LootStates.OpenedAt(container.Index);
        if (_openLoot is { } open && open.Container == container)
        {
            return new LootView(CurrentInteractionState.Ongoing, null, opened, open.ItemContainer);
        }

        if (_search is { } search && search.Container == container)
        {
            return new LootView(CurrentInteractionState.Ongoing, search.HoldStartsAt, opened, null);
        }

        return new LootView(opened is null ? CurrentInteractionState.Idle : CurrentInteractionState.SuccessIdle, null, opened, null);
    }

    // The client's copy of a container after it gets this view.
    private static LootKnowledge Knowledge(LootView view) => view.OpenedAt is null ? LootKnowledge.Closed : LootKnowledge.Opened;

    // What a dormant container shows when nobody on this connection searches it.
    private LootKnowledge DefaultLootKnowledge(LootContainer container) =>
        _raid.LootStates.OpenedAt(container.Index) is null ? LootKnowledge.Closed : LootKnowledge.Opened;

    private LootKnowledge Known(int index) => index < _lootKnown.Length ? (LootKnowledge)_lootKnown[index] : LootKnowledge.None;

    // Whether this player searches the container or has it open: its channel stays open meanwhile.
    private bool LootHeld(LootContainer container) => _search?.Container == container || _openLoot?.Container == container;

    // A loot channel no container uses.
    private int? FreeLootChannel()
    {
        for (int channel = FirstLootChannel; channel < FirstLootChannel + LootChannels; channel++)
        {
            if (_lootLinks.Values.All(link => link.Channel != channel))
            {
                return channel;
            }
        }

        return null;
    }

    // Where the player is for loot relevance: an operator teleport's destination, else the latest reported position, else
    // the spawn.
    private Vector3D LootPosition => _teleport is { } teleport ? teleport.Position
        : InputFrame is { } latest && _input.Position(latest) is { } position ? position : _member.Spawn.Location;

    // Runs this player's search: names the character's interactor once, starts a requested search (the client
    // lines up, then holds), cancels it on movement, opens the container when the hold ends, and closes it when
    // the player closes the looting menu or walks away. What the player leaves inside stays in the raid's loot state.
    private void ServiceSearch(ReliableSender sender, double now)
    {
        if (!_interactorExported)
        {
            if (_interactorExport is null)
            {
                _interactorExport = sender.Queue(Bunches.Exports(_raid.State.InteractorExports(_player), [_player.Interactor]));
            }
            else if (!sender.Pending(_interactorExport.Value))
            {
                _interactorExported = true;
            }
        }

        if (_lootPress is { } lootPress && _search is null && _openLoot is null)
        {
            (LootContainer pressed, bool drawsTool) = lootPress;
            LootContainerClass type = pressed.Class;

            // An opened container has no breach left: it opens again once the character is lined up. A press
            // that takes the raider tool out first lines up only once the held item is put away and the tool drawn.
            double? opened = _raid.LootStates.OpenedAt(pressed.Index);
            double draw = drawsTool && type.EquipsRaiderTool && opened is null ? ToolDrawSeconds : 0;
            double requested = ServerSeconds(now) + draw;
            _search = (pressed, InputFrame ?? 0, requested, requested + type.AlignmentSeconds,
                now + draw + type.AlignmentSeconds + (opened is null ? type.HoldSeconds : LootInteractions.OpenedHoldSeconds));
            PushLoot(sender, pressed);
            LootSearches++;
        }

        _lootPress = null;
        if (_search is { } search)
        {
            (LootContainer container, uint frame, double requestedAt, _, double opensAt) = search;
            if (MovedSince(frame))
            {
                _search = null;
                PushLoot(sender, container);
                CancelledSearches++;
            }
            else if (now >= opensAt && _interactorExported)
            {
                int id = _member.Inventory.OpenWorldContainer(_raid.LootStates.Items(container.Index), LootStates.UnitWeights);
                _raid.LootStates.Open(container.Index, ServerSeconds(now));
                _search = null;
                _openLoot = (container, id);
                PushLoot(sender, container);
                sender.Queue(Bunches.Actor(PawnChannel, _raid.State.InteractorCall(_player, FunctionNames.ClientLootInteractionHoldCompleted, container.Interaction, container.InteractionClass).Write));
                _interaction = new PawnInteraction(container.Interaction, LastInteractionEvent.Restaged, requestedAt, requestedAt,
                    requestedAt + container.Class.AlignmentSeconds, ++_interactionCount, container.Class.SearchLocation, container.SearchAnimation);
                LootOpens++;
            }
        }

        if (_openLoot is { } open)
        {
            Vector3D prompt = open.Container.Prompt;
            bool away = InputFrame is { } latest && _input.Position(latest) is { } position
                && Math.Sqrt(Math.Pow(position.X - prompt.X, 2) + Math.Pow(position.Y - prompt.Y, 2) + Math.Pow(position.Z - prompt.Z, 2))
                    > open.Container.CloseDistance;
            if (away || _lootCloseRequested)
            {
                _raid.LootStates.Store(open.Container.Index, _member.Inventory.CloseWorldContainer());
                _openLoot = null;
                PushLoot(sender, open.Container);
                _interaction = _interaction is { } ended ? ended with { Event = LastInteractionEvent.Succeeded } : null;
                sender.Queue(Bunches.Actor(PawnChannel, _raid.State.InteractorCall(_player, FunctionNames.ClientEndInteractionSuccessfullyImplementation, open.Container.Interaction, open.Container.InteractionClass).Write));
            }
        }

        _lootCloseRequested = false;
    }

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

    private void LogLootRequest(string entry)
    {
        if (_lootRequests.Count == LootRequestLog)
        {
            _lootRequests.RemoveAt(0);
        }

        _lootRequests.Add(entry);
    }

    private static string Slot(ItemContainerSlot slot) => $"{slot.Parent.Id}r{slot.Parent.Revision}/{slot.Index}";

    private static void LogRequest(List<string> log, string entry)
    {
        if (log.Count == InteractionRequestLog)
        {
            log.RemoveAt(0);
        }

        log.Add(entry);
    }

    // The elevator of a call button this player may press: possessed, the elevator on this connection, closed and
    // not called yet.
    private ExtractionPoint? CallableElevator(ulong button) =>
        Phase == DeliveryPhase.Possessed && _raid.Extraction.ByButton(button) is ({ } point, { Role: ExtractionButtonRole.Summon })
        && _elevators.GetValueOrDefault(point.Index).State == ElevatorChannel.Open
        && _raid.ExtractionStates.Get(point.Index) is { State: ExtractionState.Closed, CalledAt: null } ? point : null;

    // The elevator and switch of a descend switch this player may press: possessed, the elevator on this connection,
    // ready and nobody holding a switch.
    private (ExtractionPoint Point, int Button)? DescendableElevator(ulong button) =>
        Phase == DeliveryPhase.Possessed && _raid.Extraction.ByButton(button) is ({ } point, { Role: ExtractionButtonRole.Descend })
        && _elevators.GetValueOrDefault(point.Index).State == ElevatorChannel.Open
        && _raid.ExtractionStates.Get(point.Index) is { State: ExtractionState.Ready, Descent: null } ? (point, point.ButtonIndex(button)) : null;

    // Whether an id names an interaction the server gave the client: a door, ladder, loot, elevator button or pickup.
    private bool KnownInteraction(ulong target) =>
        _raid.Doors.ByButton(target) is not null || _raid.Doors.ByBreach(target) is not null || _raid.Loot.ByInteraction(target) is not null
        || _raid.Extraction.ByButton(target) is not null || _raid.Ladders.Ladders.Any(ladder => ladder.Interactions.Contains(target))
        || _raid.Pickups.ByTarget(target) is not null;

    // The loot container of a search interaction the client may use: the client has the container.
    private LootContainer? SearchableLoot(ulong interaction) =>
        _raid.Loot.ByInteraction(interaction) is { } container && Known(container.Index) != LootKnowledge.None && !_failedLoot.Contains(container.Index) ? container : null;

    // Gives other players this character's movement: the input tick before the newest input, at the position the
    // client reported, with the simulated velocity, stance and locomotion (as the character's own frames carry it),
    // the held weapons and the aim pitch. Other clients have no item objects of this player, so the weapons go
    // without item links: the client draws an equipped gun from its item type.
    private void PublishMotion(double now)
    {
        if (Phase != DeliveryPhase.Possessed || _blackboardFrame == 0 || InputFrame is not { } latest || latest <= PawnBlackboardFrames.FirstTick)
        {
            return;
        }

        uint tick = latest - 1;
        if ((_member.Motion is { } published && published.Tick >= tick) || _input.Position(latest) is not { } position
            || _input.Position(tick) is not { } before || _input.Yaw(tick) is not { } yaw || _movement?.State(tick) is not { } character
            || _input.Pitch(tick) is not { } pitch)
        {
            return;
        }

        Vector3D velocity = character.Velocity with { Z = (position.Z - before.Z) / PawnBlackboard.TickSeconds };
        _member.Publish(new PlayerMotion(tick, new PawnMotion(position, velocity, yaw, character), _member.Weapons.Variables(tick), pitch, TimeOffsetAt(tick, now)));
    }

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

            int stateChannel = FirstObservedChannel + (2 * index);
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

    private int? DoorIndex(int channel) =>
        channel - FirstDoorChannel is var index && index >= 0 && index < _raid.Doors.Doors.Count ? index : null;

    // Index in the raid's player list of the player an observed-player channel belongs to; each player has a
    // player-state channel and a character channel after the loot containers.
    private int? ObservedIndex(int channel) =>
        (channel - FirstObservedChannel) / 2 is var index && channel >= FirstObservedChannel && index < _raid.Players.Count ? index : null;

    private int FirstLootChannel => FirstDoorChannel + _raid.Doors.Doors.Count + _raid.Ladders.Ladders.Count;

    private int FirstElevatorChannel => FirstLootChannel + LootChannels;

    private int FirstObservedChannel => FirstElevatorChannel + _raid.Extraction.Points.Count;

    private int? ElevatorIndex(int channel) =>
        channel - FirstElevatorChannel is var index && index >= 0 && index < _raid.Extraction.Points.Count ? index : null;

    private int? LootIndex(int channel) =>
        channel - FirstLootChannel is var index and >= 0 and < LootChannels ? index : null;

    private int? LadderIndex(int channel) =>
        channel - FirstDoorChannel - _raid.Doors.Doors.Count is var index && index >= 0 && index < _raid.Ladders.Ladders.Count ? index : null;

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

    // A loot container's channel on this connection: its stage, the bunch it waits for and the state last sent.
    private readonly record struct LootLink(LootStage Stage, int Channel, ReliableKey? Key, LootView Sent);
}
