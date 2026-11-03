using System.Text.RegularExpressions;
using Ares.Raid.Replication;
using Ares.Raid.Transport;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
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

    /// <summary>Opening the world actors, one at a time.</summary>
    WorldOpensPending,

    /// <summary>Player actors can open.</summary>
    PlayerOpensReady,

    /// <summary>Squad channel sent; waiting for its acknowledgement.</summary>
    SquadOpenPending,

    /// <summary>Player actor channels sent; waiting for their acknowledgement.</summary>
    ActorOpensPending,

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

    /// <summary>Restart sent; waiting for the client to confirm possession.</summary>
    PossessionPending,

    /// <summary>The client possesses its character.</summary>
    Possessed,
}

/// <summary>
/// One player's actor channels after Join: exports and world actors first, then the player actors, their
/// components and the restart; after possession, input acknowledgements, the first blackboard frame,
/// level streaming requests, ability grants, and the doors and ladders of visible cells.
/// </summary>
public sealed partial class PlayerChannels : IPlayerChannels
{
    private const int ControllerChannel = 2;
    private const int PlayerStateChannel = 3;
    private const int PawnChannel = 6;
    private const int SquadChannel = 48;
    private const int FirstDoorChannel = 64;
    private const int MaxDoorsInFlight = 8;
    private const int MaxLaddersInFlight = 8;

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
    private readonly WorldPlayer _player;
    private readonly PlayerLogin _login;
    private readonly (int Channel, ulong Guid)[] _bindings;

    private ReliableKey[] _keys = [];
    private int _worldOpenIndex;
    private HashSet<ulong> _mapped = [];
    private HashSet<string> _visibleLevels = new(StringComparer.Ordinal);
    private double? _visibleLevelsChangedAt;
    private bool _possessionToCommit;
    private Dictionary<string, int> _ignoredCalls = new(StringComparer.Ordinal);

    private InputHistory _input = new();
    private ReliableKey[] _inputAckKeys = [];
    private bool _correctionExported;
    private uint? _inputAckSent;
    private ReliableKey[] _blackboardKeys = [];
    private uint _blackboardFrame;
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
    private double? _mapActorsAllowedAt;
    private Dictionary<int, (LadderChannel State, ReliableKey? Key)> _ladders = [];
    private HashSet<int> _failedLadders = [];

    /// <summary>Creates the channels of a joined player.</summary>
    /// <param name="raid">The raid.</param>
    /// <param name="player">The player's actors.</param>
    /// <param name="login">What the player's login gives its actors.</param>
    public PlayerChannels(RaidWorld raid, WorldPlayer player, PlayerLogin login)
    {
        _raid = raid ?? throw new ArgumentNullException(nameof(raid));
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

    /// <summary>The object the client focuses in its latest input frame (its prompt shows), or 0.</summary>
    public ulong InputFocus => _input.Focus;

    /// <summary>Pawn blackboard frames sent (the first frame and focus updates).</summary>
    public int BlackboardFrames { get; private set; }

    /// <summary>Door presses applied.</summary>
    public int DoorPresses { get; private set; }

    /// <summary>Door requests ignored (not a press, door not open on this connection, or still moving).</summary>
    public int IgnoredDoorRequests { get; private set; }

    /// <summary>Ladders whose climbable actor and interactions this client has.</summary>
    public int LaddersReady => _ladders.Values.Count(ladder => ladder.State == LadderChannel.Open);

    /// <summary>Ladders whose delivery started and has not finished.</summary>
    public int LaddersInProgress => _ladders.Values.Count(ladder => ladder.State is not (LadderChannel.Closed or LadderChannel.Open));

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
        copy._doors = new Dictionary<int, (DoorChannel, ReliableKey?)>(_doors);
        copy._doorRevisions = new Dictionary<int, int>(_doorRevisions);
        copy._doorUpdates = new Dictionary<int, ReliableKey>(_doorUpdates);
        copy._failedDoors = [.. _failedDoors];
        copy._requests = [.. _requests];
        copy._ladders = new Dictionary<int, (LadderChannel, ReliableKey?)>(_ladders);
        copy._failedLadders = [.. _failedLadders];
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
        if (bunch.Channel == SquadChannel || DoorIndex(bunch.Channel) is not null || LadderIndex(bunch.Channel) is not null)
        {
            // The client owns no squad, door or ladder; it only sends channel bookkeeping there.
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
            (uint? latest, IReadOnlyList<InteractionRequest> requests) = _input.Read(payload);
            if (latest is { } frame && (InputFrame is null || frame > InputFrame))
            {
                InputFrame = frame;
            }

            _requests.AddRange(requests);
        }
        catch (InvalidPacketException)
        {
            // Never let an undecoded input payload reject the channel traffic it rides with.
            InputDecodeFailures++;
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

        ServiceInputAcknowledgement(io.Sender);
        ServiceBlackboard(io.Sender, now);
        ServiceStreaming(io.Sender, now);
        ServiceAbilities(io.Sender);
        ServiceDoors(io.Sender, now);
        ServiceLadders(io.Sender, now);
    }

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
    }

    /// <inheritdoc />
    public void Commit()
    {
        if (_possessionToCommit)
        {
            _raid.State.AcknowledgePossession(_player);
            _possessionToCommit = false;
        }

        double now = _raid.Clock();
        foreach (InteractionRequest request in _requests)
        {
            Door? door = UsableDoor(request.Target);
            if (request.Request == PendulumInput.PressRequest && door is not null && _raid.DoorStates.Press(door.Index, now))
            {
                DoorPresses++;
            }
            else
            {
                IgnoredDoorRequests++;
            }
        }

        _requests.Clear();
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
                _keys = [];
                Phase = DeliveryPhase.WorldOpensPending;
                return true;
            case DeliveryPhase.WorldOpensPending when delivered:
                // World actors open one at a time, before the player actors.
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
                _keys =
                [
                    .. _bindings.Select(binding =>
                    {
                        WorldActor actor = Actor(binding.Guid);
                        ActorSpawn spawn = new(binding.Guid, _raid.Imports.Archetypes[actor.ClassPath], 0, actor.Location, actor.Rotation);
                        PlayerControllerOpen? open = binding.Guid == _player.Controller ? _login.ControllerOpen() : null;
                        return sender.Queue(new ActorInitialization(spawn, open, []).ToBunch(binding.Channel, sender.NextSequence(binding.Channel), []));
                    }),
                ];
                Phase = DeliveryPhase.ActorOpensPending;
                return true;
            case DeliveryPhase.ActorOpensPending when delivered:
                // Each open completed (with retries): the client can resolve links between the actors.
                _mapped = [.. _bindings.Select(binding => binding.Guid), _player.Squad, .. _raid.SpawnedActors.Select(actor => actor.Guid)];
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
                if (_raid.Profiles.Count > 0)
                {
                    sender.Queue(Bunches.Actor(ControllerChannel, _raid.State.Replication.Calls(_raid.State.Objects(_mapped)).Call(
                        PlayerClasses.PlayerController.Class, FunctionNames.ClientReceiveSquadPlayersProfiles,
                        ProfileParameters(_raid.Profiles), RpcDirection.Client)));
                }

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
                IReadOnlyList<DynamicSubobjectContent> sets = _raid.State.AttributeSetCreation(_player);
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
                        replies.Add((FunctionNames.ClientReceiveSquadPlayersProfiles, ProfileParameters(_raid.Profiles.IntersectBy(requested.OfType<TenancyUserId>(), profile => profile.TenancyUserId))));
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

    // The first frame starts the client's character simulation at the spawn, once level streaming has settled
    // (otherwise the character falls through floors that are not loaded yet). The client applies each later
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

        IReadOnlyDictionary<int, PendulumSlot> state;
        uint frame = latest;
        ulong focus = 0;
        if (_blackboardFrame == 0)
        {
            if (_visibleLevelsChangedAt is not { } changed || now - changed < LevelStreamingSettleSeconds)
            {
                return;
            }

            WorldActor pawn = Actor(_player.Pawn);
            state = PawnBlackboardFrames.State(frame, new PawnMotion(pawn.Location, Yaw: pawn.Rotation.Yaw * Math.PI / 180), focus);
        }
        else
        {
            focus = UsableDoor(_input.Focus) is null ? 0 : _input.Focus;
            frame = latest - 1;
            if (focus == _blackboardFocus || frame <= _blackboardFrame
                || _input.Position(latest) is not { } position || _input.Position(frame) is not { } before || _input.Yaw(frame) is not { } yaw)
            {
                return;
            }

            Vector3D step = new(position.X - before.X, position.Y - before.Y, position.Z - before.Z);
            state = PawnBlackboardFrames.State(frame, new PawnMotion(position, step, yaw), focus);
        }

        _blackboardKeys = [sender.Queue(Bunches.Actor(PawnChannel, _raid.State.BlackboardFrame(_blackboardFrame, frame, state, _blackboardState)))];
        (_blackboardFrame, _blackboardState, _blackboardFocus) = (frame, state, focus);
        BlackboardFrames++;
    }

    // The door of a button the client may use: possessed, door replicated on this connection, cell visible,
    // and the door not barricaded, locked or without power.
    private Door? UsableDoor(ulong button) =>
        _raid.Doors.ByButton(button) is { } door && Phase == DeliveryPhase.Possessed && !_failedDoors.Contains(door.Index)
        && _doors.GetValueOrDefault(door.Index).State == DoorChannel.Open && _visibleLevels.Contains(door.Cell)
        && _raid.DoorStates.Usable(door.Index)
            ? door
            : null;

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

    private int? LadderIndex(int channel) =>
        channel - FirstDoorChannel - _raid.Doors.Doors.Count is var index && index >= 0 && index < _raid.Ladders.Ladders.Count ? index : null;

    private IEnumerable<string> Calls(string classPath, ActorContentBlock block) =>
        _raid.State.Replication.Calls(_raid.State.Objects(_mapped)).ReadFields(classPath, block, RpcDirection.Server).Select(call => call.Field.Name);

    private WorldActor Actor(ulong guid) => _raid.State.Actor(guid);

    private int ChannelOf(ulong actor) => Array.Find(_bindings, binding => binding.Guid == actor).Channel;

    private void Count(string name) => _ignoredCalls[name] = _ignoredCalls.GetValueOrDefault(name) + 1;
}
