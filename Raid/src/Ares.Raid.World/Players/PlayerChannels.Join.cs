using Ares.Raid.Transport;
using Ares.Raid.World.Replication;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Replication;
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
/// The join sequence of a player's channels: the static exports, the controller and player state, the HUD, the world
/// actors one at a time, the squad, the player's properties, the character once the initial levels are loaded, its
/// appearance, inventory and ability system, then the restart, until the client confirms possession.
/// </summary>
public sealed partial class PlayerChannels
{
    private ReliableKey[] _keys = [];
    private int _worldOpenIndex;
    private bool _initialLevelsLoaded;
    private bool _possessionToCommit;

    /// <summary>Where the actor delivery stands.</summary>
    public DeliveryPhase Phase { get; private set; }

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
            .Where(item => _player.HeldItems.ContainsKey(item.Id)).Select(item => _raid.Imports.Classes[_raid.Imports.HeldItemClasses[item.AssetIndex]]));

        _keys = [.. roots.Distinct().Select(root => io.Sender.Queue(Bunches.Exports(_raid.Imports.Exports, [root])))];
        Phase = DeliveryPhase.ExportsPending;
    }

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
}
