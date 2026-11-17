using System.Text.RegularExpressions;
using Ares.Raid.Transport;
using Ares.Raid.World.Movement;
using Ares.Raid.World.Replication;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Game.Replication;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.World.Players;

/// <summary>
/// What the client sends on its own actors: the input stream, the controller's start-up calls and reports (possession,
/// level visibility, squad profiles, surrender), the calls on the pawn and its components, and the spectator manager's
/// ready for the end of the round. Calls the server does not handle yet are counted by name.
/// </summary>
public sealed partial class PlayerChannels
{
    private const int TelemetryBitLimit = 256;
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

    [GeneratedRegex("^/Game/[A-Za-z_0-9/]+$")]
    private static partial Regex PackagePattern();

    [GeneratedRegex("^[A-Za-z_0-9]{1,128}$")]
    private static partial Regex ComponentNamePattern();

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
            if (name == FunctionNames.ServerReadyToSpectate && _departure.Transitioning)
            {
                _departure.Ready(_raid);
            }
            else
            {
                Count($"spectator:{name}");
            }
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

    private void Count(string name) => _ignoredCalls[name] = _ignoredCalls.GetValueOrDefault(name) + 1;
}
