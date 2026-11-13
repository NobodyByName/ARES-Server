using System.Globalization;
using Ares.Raid.Replication;
using Ares.Raid.Transport;
using Ares.Raid.World.Inventory;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Control;
using Ares.Structs.Net.GameplayTags;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.World;

/// <summary>A server actor.</summary>
/// <param name="Guid">Its dynamic id.</param>
/// <param name="ClassPath">Its class.</param>
/// <param name="Location">Spawn location.</param>
/// <param name="Properties">Initial property values by member path.</param>
/// <param name="Rotation">Spawn rotation in degrees.</param>
public sealed record WorldActor(ulong Guid, string ClassPath, Vector3D Location, IReadOnlyDictionary<string, object?> Properties, Rotator Rotation = default);

/// <summary>A player's server actors and components.</summary>
/// <param name="Principal">The player.</param>
/// <param name="Controller">Player controller.</param>
/// <param name="PlayerState">Player state.</param>
/// <param name="Pawn">Character.</param>
/// <param name="Appearance">The character's appearance component.</param>
/// <param name="AbilitySystem">The character's ability system component.</param>
/// <param name="AttributeSets">Attribute sets the ability system owns, in <see cref="CharacterDefaults.AttributeSets"/> order.</param>
/// <param name="Correction">The controller's input-correction component.</param>
public sealed record WorldPlayer(
    SessionPrincipal Principal,
    ulong Controller,
    ulong PlayerState,
    ulong Pawn,
    ulong Appearance,
    ulong AbilitySystem,
    IReadOnlyList<ulong> AttributeSets,
    ulong Correction)
{
    /// <summary>The player's squad actor.</summary>
    public ulong Squad { get; init; }

    /// <summary>The replicated member inside the squad.</summary>
    public ulong SquadMember { get; init; }

    /// <summary>The character's inventory component.</summary>
    public ulong Inventory { get; init; }

    /// <summary>The character's health and shield component.</summary>
    public ulong Health { get; init; }

    /// <summary>The character's interactor component.</summary>
    public ulong Interactor { get; init; }

    /// <summary>The controller's end-of-round component.</summary>
    public ulong EndOfRound { get; init; }

    /// <summary>Replicated objects of the items the character can hold, keyed by their inventory identities.</summary>
    public IReadOnlyDictionary<Guid, ulong> HeldItems { get; init; } = new Dictionary<Guid, ulong>();

    /// <summary>Whether the client confirmed possessing its character.</summary>
    public bool PossessionAcknowledged { get; init; }
}

/// <summary>
/// The server actors of one raid: the game state and each player's controller, player state and character
/// with their components. Builds the content each actor and component replicates.
/// </summary>
public sealed class WorldState
{
    private readonly Lock _lock = new();
    private readonly WorldImports _imports;
    private readonly Dictionary<ulong, WorldActor> _actors = [];
    private readonly Dictionary<long, WorldPlayer> _players = [];
    private ulong _nextGuid = 2;

    /// <summary>Creates the world with its game state: begun play, match in progress, raid in its first phase.</summary>
    /// <param name="replication">Replication context of the world.</param>
    /// <param name="imports">The world's static exports.</param>
    /// <param name="matchId">Match id of the reservation.</param>
    /// <param name="sessionSeed">Session seed (0 to 2^31 - 1).</param>
    public WorldState(ReplicationContext replication, WorldImports imports, string matchId, int sessionSeed)
    {
        Replication = replication ?? throw new ArgumentNullException(nameof(replication));
        _imports = imports ?? throw new ArgumentNullException(nameof(imports));
        MatchId = matchId ?? throw new ArgumentNullException(nameof(matchId));
        ArgumentOutOfRangeException.ThrowIfNegative(sessionSeed);
        WorldActor state = Add(DamMap.GameState.Class, default, new Dictionary<string, object?>
        {
            [PropertyNames.Role] = (ulong)NetRole.Authority,
            [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
            [PropertyNames.GameModeClass] = imports.Classes[DamMap.GameModeClass],
            [PropertyNames.ReplicatedHasBegunPlay] = true,
            [PropertyNames.SessionSeed] = sessionSeed,
            [PropertyNames.LocationTag] = new GameplayTag(DamMap.LocationTag),
            [PropertyNames.MatchState] = new NetworkName(MatchStates.InProgress, 0),
            [PropertyNames.GamePhase] = (ulong)GamePhase.PreGame,
        });
        Replication.Encode(state.ClassPath, state.Properties, Objects([]));
        GameState = state;
    }

    /// <summary>Replication context of the world.</summary>
    public ReplicationContext Replication { get; }

    /// <summary>Match id of the reservation.</summary>
    private string MatchId { get; }

    /// <summary>The game state actor.</summary>
    public WorldActor GameState { get; }

    /// <summary>The game state's channel opening.</summary>
    /// <param name="channel">Channel index.</param>
    /// <param name="worldItemClassBits">World item classes the client should load (<see cref="LootCatalog.WorldItemClassBits"/>).</param>
    /// <returns>The world actor.</returns>
    public SpawnedWorldActor GameStateActor(int channel, IReadOnlyList<ulong> worldItemClassBits)
    {
        ArgumentNullException.ThrowIfNull(worldItemClassBits);
        Dictionary<string, object?> properties = new(GameState.Properties)
        {
            [PropertyNames.PossibleWorldItemClassIndexBitFields] = worldItemClassBits.Select(word => (object?)word).ToArray(),
        };
        return new SpawnedWorldActor(
            channel,
            GameState.Guid,
            _imports.Archetypes[GameState.ClassPath],
            [.. _imports.Classes.Where(item => !WorldImports.HeldItemClasses.Values.Contains(item.Key)).Select(item => item.Value)],
            [Replication.Encode(GameState.ClassPath, properties, Objects([GameState.Guid]))]);
    }

    /// <summary>The game state's world time: clients derive the server's clock from it.</summary>
    /// <param name="seconds">The raid's world time in seconds.</param>
    /// <returns>The game state content.</returns>
    public ActorContentBlock WorldTime(double seconds) =>
        Replication.Encode(GameState.ClassPath, new Dictionary<string, object?> { [PropertyNames.ReplicatedWorldTimeSecondsDouble] = seconds }, Objects([]));

    /// <summary>A player's actors, created at <paramref name="spawn"/> on first use.</summary>
    /// <param name="principal">The player.</param>
    /// <param name="uniqueId">The unique id the player logged in with; its player state carries it.</param>
    /// <param name="spawn">Spawn location for a new player.</param>
    /// <param name="rotation">Authored spawn rotation for a new player.</param>
    /// <param name="inventory">Carried items that need replicated gameplay objects.</param>
    /// <returns>The player.</returns>
    public WorldPlayer PlayerFor(SessionPrincipal principal, UniqueNetId uniqueId, Vector3D spawn, Rotator rotation = default, RaidInventory? inventory = null)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(uniqueId);
        lock (_lock)
        {
            if (principal.MatchId != MatchId)
            {
                throw new InvalidPacketException("Player does not belong to this world");
            }

            if (_players.TryGetValue(principal.UserId, out WorldPlayer? existing))
            {
                return existing;
            }

            ulong controller = _nextGuid;
            ulong playerState = controller + 2;
            ulong pawn = controller + 4;
            Dictionary<string, object?> controllerValues = new()
            {
                [PropertyNames.Role] = (ulong)NetRole.Authority,
                [PropertyNames.RemoteRole] = (ulong)NetRole.AutonomousProxy,
                [PropertyNames.Pawn] = pawn,
                [PropertyNames.PlayerState] = playerState,
                [PropertyNames.SpawnLocation] = spawn,
                [$"{PropertyNames.GameModeInfo}.{PropertyNames.Identifier}"] = new GameplayTag(DamMap.GameModeTag),
                [$"{PropertyNames.GameModeInfo}.{PropertyNames.Location}"] = new GameplayTag(DamMap.LocationTag),
                [PropertyNames.IsServerReady] = true,
            };
            Dictionary<string, object?> stateValues = new()
            {
                [PropertyNames.Role] = (ulong)NetRole.Authority,
                [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
                [PropertyNames.Owner] = controller,
                [PropertyNames.PlayerId] = _players.Count,
                [PropertyNames.UniqueId] = uniqueId,
                [PropertyNames.PioneerCharacter] = pawn,
                [PropertyNames.CurrentPawn] = pawn,
            };
            Dictionary<string, object?> pawnValues = new()
            {
                [PropertyNames.Role] = (ulong)NetRole.Authority,
                [PropertyNames.RemoteRole] = (ulong)NetRole.AutonomousProxy,
                [PropertyNames.Owner] = controller,
                [PropertyNames.Controller] = controller,
                [PropertyNames.PlayerState] = playerState,
                [PropertyNames.ReplicateMovement] = false,
                [PropertyNames.Hidden] = false,
            };

            Add(PlayerClasses.PlayerController.Class, default, controllerValues);
            Add(PlayerClasses.PlayerState.Class, default, stateValues);
            Add(PlayerClasses.Pawn.Class, spawn, pawnValues, rotation);
            ulong appearance = Next();
            ulong abilitySystem = Next();
            ulong[] sets = [.. CharacterDefaults.AttributeSets.Select(_ => Next())];
            ulong correction = Next();
            Dictionary<string, object?> squadValues = new()
            {
                [PropertyNames.Role] = (ulong)NetRole.Authority,
                [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
                [PropertyNames.SquadId] = $"{MatchId}-{_players.Count}",
                [PropertyNames.IsGeneratedSquad] = false,
            };
            WorldActor squad = Add(PlayerClasses.Squad.Class, default, squadValues);
            ulong member = Next();
            squadValues[PropertyNames.Members] = new object?[] { member };
            stateValues[PropertyNames.Squad] = squad.Guid;
            stateValues[PropertyNames.SquadSlotIndex] = 0;
            foreach (ulong guid in (ulong[])[controller, playerState, pawn])
            {
                WorldActor actor = _actors[guid];
                Replication.Encode(actor.ClassPath, actor.Properties, Objects([controller, playerState, pawn, squad.Guid]));
            }

            WorldPlayer player = new(principal, controller, playerState, pawn, appearance, abilitySystem, sets, correction)
            {
                Squad = squad.Guid,
                SquadMember = member,
                Inventory = Next(),
                Health = Next(),
                HeldItems = inventory?.Containers.SelectMany(container => container.Items)
                    .OfType<ContainerItem>().Where(item => WorldImports.HeldItemClasses.ContainsKey(item.AssetIndex))
                    .ToDictionary(item => item.Id, _ => Next()) ?? [],
            };
            player = player with { Interactor = Next(), EndOfRound = Next() };
            _players[principal.UserId] = player;
            return player;
        }
    }

    /// <summary>A player's current actors.</summary>
    /// <param name="principal">The player.</param>
    /// <returns>The player.</returns>
    public WorldPlayer Player(SessionPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        lock (_lock)
        {
            return _players.TryGetValue(principal.UserId, out WorldPlayer? player) && player.Principal == principal
                ? player
                : throw new InvalidPacketException("Connection has no player in this world");
        }
    }

    /// <summary>A player's actors, if that player has joined the world.</summary>
    /// <param name="userId">The player's user id.</param>
    /// <returns>The player, or null.</returns>
    public WorldPlayer? FindPlayer(long userId)
    {
        lock (_lock)
        {
            return _players.GetValueOrDefault(userId);
        }
    }

    /// <summary>Another player's player state as an observer sees it: simulated, with its id and unique id.</summary>
    /// <param name="other">The observed player.</param>
    /// <returns>The opening properties.</returns>
    public ActorContentBlock ObservedPlayerStateProperties(WorldPlayer other)
    {
        ArgumentNullException.ThrowIfNull(other);
        WorldActor state = Actor(other.PlayerState);
        return Replication.Encode(state.ClassPath, new Dictionary<string, object?>
        {
            [PropertyNames.Role] = (ulong)NetRole.Authority,
            [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
            [PropertyNames.PlayerId] = state.Properties[PropertyNames.PlayerId],
            [PropertyNames.UniqueId] = state.Properties[PropertyNames.UniqueId],
        }, Objects([]));
    }

    /// <summary>
    /// Another player's character as an observer sees it: a simulated proxy linked to its player state, with its
    /// aim pitch; its movement arrives in blackboard frames.
    /// </summary>
    /// <param name="other">The observed player.</param>
    /// <param name="pitch">Aim pitch, compressed (see <see cref="CompressedPitch"/>).</param>
    /// <returns>The opening properties.</returns>
    public ActorContentBlock ObservedPawnProperties(WorldPlayer other, ushort pitch = 0)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Replication.Encode(PlayerClasses.Pawn.Class, new Dictionary<string, object?>
        {
            [PropertyNames.Role] = (ulong)NetRole.Authority,
            [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
            [PropertyNames.PlayerState] = other.PlayerState,
            [PropertyNames.ReplicateMovement] = false,
            [PropertyNames.Hidden] = false,
            [PropertyNames.RemoteViewPitch16] = pitch,
        }, Objects([other.PlayerState]));
    }

    /// <summary>Another player's aim pitch, as an observer's copy of the character receives it.</summary>
    /// <param name="pitch">Aim pitch, compressed (see <see cref="CompressedPitch"/>).</param>
    /// <returns>The character's properties.</returns>
    public ActorContentBlock ObservedPitch(ushort pitch) =>
        Replication.Encode(PlayerClasses.Pawn.Class, new Dictionary<string, object?> { [PropertyNames.RemoteViewPitch16] = pitch }, Objects([]));

    /// <summary>An aim pitch as the pawn replicates it: 65536 steps per turn, negative angles wrapped.</summary>
    /// <param name="radians">Pitch in radians, positive up.</param>
    /// <returns>The compressed pitch.</returns>
    public static ushort CompressedPitch(double radians) => (ushort)((int)Math.Round(radians * 32768 / Math.PI) & 0xFFFF);

    /// <summary>One content block with properties followed by field payloads (a blackboard frame).</summary>
    /// <param name="properties">Property content (with its layout flag).</param>
    /// <param name="fields">Field content (without layout).</param>
    /// <returns>The merged block.</returns>
    public static ActorContentBlock Merge(ActorContentBlock properties, ActorContentBlock fields)
    {
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(fields);
        if (!properties.HasRepLayout || fields.HasRepLayout)
        {
            throw new InvalidPacketException("Merging needs a property block and a field block");
        }

        BitWriter writer = new();
        writer.WriteBits(properties.Payload, properties.PayloadBits);
        writer.WriteBits(fields.Payload, fields.PayloadBits);
        return new ActorContentBlock(writer.ToArray(), writer.Length, true);
    }

    /// <summary>Links another player's player state to its character once the observer has both.</summary>
    /// <param name="other">The observed player.</param>
    /// <returns>The player-state properties.</returns>
    public ActorContentBlock ObservedPawnLinks(WorldPlayer other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Replication.Encode(PlayerClasses.PlayerState.Class, new Dictionary<string, object?>
        {
            [PropertyNames.PioneerCharacter] = other.Pawn,
            [PropertyNames.CurrentPawn] = other.Pawn,
        }, Objects([other.PlayerState, other.Pawn]));
    }

    /// <summary>An actor.</summary>
    /// <param name="guid">Its id.</param>
    /// <returns>The actor.</returns>
    public WorldActor Actor(ulong guid)
    {
        lock (_lock)
        {
            return _actors.TryGetValue(guid, out WorldActor? actor) ? actor : throw new InvalidPacketException("Unknown world actor");
        }
    }

    /// <summary>Object ids with their classes, as a codec needs them.</summary>
    /// <param name="guids">Mapped actor ids.</param>
    /// <returns>The mapping.</returns>
    public Dictionary<ulong, string> Objects(IEnumerable<ulong> guids)
    {
        ArgumentNullException.ThrowIfNull(guids);
        lock (_lock)
        {
            return guids.ToDictionary(guid => guid, guid => Actor(guid).ClassPath);
        }
    }

    /// <summary>The controller's roles and planned spawn location, applied before its BeginPlay.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The opening properties.</returns>
    public ActorContentBlock ControllerOpeningProperties(WorldPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        WorldActor controller = Actor(player.Controller);
        return Replication.Encode(controller.ClassPath, new Dictionary<string, object?>
        {
            [PropertyNames.Role] = controller.Properties[PropertyNames.Role],
            [PropertyNames.RemoteRole] = controller.Properties[PropertyNames.RemoteRole],
            [PropertyNames.SpawnLocation] = controller.Properties[PropertyNames.SpawnLocation],
        }, Objects([]));
    }

    /// <summary>Assigns the controller's HUD before world play initializes its widgets.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The HUD assignment.</returns>
    public ActorContentBlock HudAssignment(WorldPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return Replication.Calls(Objects([player.Controller])).Call(PlayerClasses.PlayerController.Class, FunctionNames.ClientSetHud, new Dictionary<string, object?>
        {
            [PropertyNames.NewHudClass] = _imports.Classes[PlayerClasses.Hud],
        }, RpcDirection.Client);
    }

    /// <summary>
    /// The controller and player-state properties. Pawn creation follows loading; its
    /// links and readiness follow component delivery
    /// and the client's confirmation that its initial levels are loaded.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="mapped">Actor ids the client has mapped.</param>
    /// <returns>Content by actor, in order.</returns>
    public IReadOnlyList<(ulong Actor, ActorContentBlock Content)> InitialProperties(WorldPlayer player, IReadOnlyCollection<ulong> mapped)
    {
        ArgumentNullException.ThrowIfNull(player);
        Dictionary<ulong, string> objects = Objects(mapped);
        List<(ulong Actor, ActorContentBlock Content)> contents = [];
        foreach (ulong guid in (ulong[])[player.Controller, player.PlayerState])
        {
            WorldActor actor = Actor(guid);
            Dictionary<string, object?> values = new(actor.Properties);
            if (guid == player.Controller)
            {
                values.Remove(PropertyNames.Role);
                values.Remove(PropertyNames.RemoteRole);
                values.Remove(PropertyNames.SpawnLocation);
                values.Remove(PropertyNames.Pawn);
                values.Remove(PropertyNames.IsServerReady);
            }

            if (guid == player.PlayerState)
            {
                values.Remove(PropertyNames.PioneerCharacter);
                values.Remove(PropertyNames.CurrentPawn);
            }

            contents.Add((guid, Replication.Encode(actor.ClassPath, values, objects)));
        }

        return contents;
    }

    /// <summary>The character's initial properties, before its controller link can establish possession.</summary>
    /// <param name="player">The player.</param>
    /// <param name="mapped">Actor ids the client has mapped.</param>
    /// <returns>The character's properties.</returns>
    public ActorContentBlock PawnInitialProperties(WorldPlayer player, IReadOnlyCollection<ulong> mapped)
    {
        ArgumentNullException.ThrowIfNull(player);
        WorldActor pawn = Actor(player.Pawn);
        Dictionary<string, object?> values = new(pawn.Properties);
        values.Remove(PropertyNames.Controller);
        return Replication.Encode(pawn.ClassPath, values, Objects(mapped));
    }

    /// <summary>
    /// The calls and the last property that complete the player's start: the ability system's initialization
    /// notice, the view of the pawn, the pawn and ready flag together, the player-state links and a restart if needed.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="mapped">Actor ids the client has mapped.</param>
    /// <returns>Content by actor, in order.</returns>
    public IReadOnlyList<(ulong Actor, ActorContentBlock Content)> InitialCalls(WorldPlayer player, IReadOnlyCollection<ulong> mapped)
    {
        ArgumentNullException.ThrowIfNull(player);
        Dictionary<ulong, string> objects = Objects(mapped);
        RpcCodec calls = Replication.Calls(objects);
        string controllerClass = PlayerClasses.PlayerController.Class;
        string pawnClass = PlayerClasses.Pawn.Class;
        return
        [
            (player.Pawn, Replication.Encode(pawnClass, new Dictionary<string, object?> { [PropertyNames.Controller] = player.Controller }, objects)),
            (player.Pawn, calls.Call(pawnClass, FunctionNames.ClientPostInitializeAbilitySystem, new Dictionary<string, object?>(), RpcDirection.Client)),
            // Viewing the pawn turns the camera to the pawn's facing.
            (player.Controller, calls.Call(controllerClass, FunctionNames.ClientSetViewTarget, new Dictionary<string, object?>
            {
                [PropertyNames.A] = player.Pawn,
                [PropertyNames.TransitionParams] = new Dictionary<string, object?>
                {
                    [PropertyNames.BlendTime] = 0f,
                    [PropertyNames.BlendFunction] = (ulong)ViewTargetBlendFunction.Cubic,
                    [PropertyNames.BlendExp] = 2f,
                    [PropertyNames.LockOutgoing] = false,
                },
            }, RpcDirection.Client)),
            (player.Controller, Replication.Encode(controllerClass, new Dictionary<string, object?>
            {
                [PropertyNames.Pawn] = player.Pawn,
                [PropertyNames.IsServerReady] = true,
            }, objects)),
            (player.PlayerState, Replication.Encode(PlayerClasses.PlayerState.Class, new Dictionary<string, object?>
            {
                [PropertyNames.PioneerCharacter] = player.Pawn,
                [PropertyNames.CurrentPawn] = player.Pawn,
            }, objects)),
            (player.Controller, calls.Call(controllerClass, FunctionNames.ClientRetryClientRestart, new Dictionary<string, object?> { [PropertyNames.NewPawn] = player.Pawn }, RpcDirection.Client)),
        ];
    }

    /// <summary>Creates the member inside the player's squad after its player state is mapped.</summary>
    /// <param name="player">The player.</param>
    /// <param name="mapped">Mapped actors.</param>
    /// <returns>The member's construction and initial properties.</returns>
    public DynamicSubobjectContent SquadMemberContent(WorldPlayer player, IReadOnlyCollection<ulong> mapped)
    {
        ArgumentNullException.ThrowIfNull(player);
        return new DynamicSubobjectContent(player.SquadMember, _imports.Classes[PlayerClasses.SquadMember],
            Replication.Encode(PlayerClasses.SquadMember, new Dictionary<string, object?>
            {
                [PropertyNames.UniqueId] = Actor(player.PlayerState).Properties[PropertyNames.UniqueId],
                [PropertyNames.PlayerState] = player.PlayerState,
            }, Objects(mapped)), null);
    }

    /// <summary>Links the squad's member after the member's construction is acknowledged.</summary>
    /// <param name="player">The player.</param>
    /// <param name="mapped">Mapped actors.</param>
    /// <returns>The squad's initial properties.</returns>
    public ActorContentBlock SquadContent(WorldPlayer player, IReadOnlyCollection<ulong> mapped)
    {
        ArgumentNullException.ThrowIfNull(player);
        Dictionary<ulong, string> objects = Objects(mapped);
        objects[player.SquadMember] = PlayerClasses.SquadMember;
        WorldActor squad = Actor(player.Squad);
        return Replication.Encode(squad.ClassPath, squad.Properties, objects);
    }

    /// <summary>Lists the player's squad and player state in the game state.</summary>
    /// <param name="player">The player.</param>
    /// <param name="mapped">Mapped actors.</param>
    /// <returns>The game state roster.</returns>
    public ActorContentBlock SquadRoster(WorldPlayer player, IReadOnlyCollection<ulong> mapped)
    {
        ArgumentNullException.ThrowIfNull(player);
        return Replication.Encode(GameState.ClassPath, new Dictionary<string, object?>
        {
            [PropertyNames.AllSquads] = new object?[] { player.Squad },
            [PropertyNames.PlayerStates] = new object?[] { player.PlayerState },
        }, Objects(mapped));
    }

    /// <summary>Exports naming the character's appearance component.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The exports and the component id.</returns>
    public (ObjectExports Exports, ulong Root) AppearanceExports(WorldPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        DefaultAppearance appearance = CharacterDefaults.Appearance;
        return (new ObjectExports([
            new DynamicActorExport(player.Pawn),
            new StableSubobjectExport(player.Appearance, player.Pawn, appearance.ComponentName, Replication.Catalog.Class(appearance.ComponentClass).Checksum),
        ]), player.Appearance);
    }

    /// <summary>The appearance component's content: the default skin.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The content.</returns>
    public StableSubobjectContent AppearanceContent(WorldPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        DefaultAppearance appearance = CharacterDefaults.Appearance;
        const string Skin = PropertyNames.SkinSelection;
        return new StableSubobjectContent(player.Appearance, Replication.Encode(appearance.ComponentClass, new Dictionary<string, object?>
        {
            [PropertyNames.Replicates] = true,
            [PropertyNames.IsActive] = true,
            [$"{Skin}.{PropertyNames.RegistryHash}"] = appearance.RegistryHash,
            [$"{Skin}.{PropertyNames.ChosenSkinNetIndices}"] = appearance.SlotSelections.Select(index => (object?)index).ToArray(),
            [PropertyNames.IsOnboardingCharacter] = false,
        }, Objects([])));
    }

    /// <summary>Exports naming the character's inventory component.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The component exports.</returns>
    public ObjectExports InventoryExports(WorldPlayer player) => new([
        new DynamicActorExport(player.Pawn),
        new StableSubobjectExport(player.Inventory, player.Pawn, InventoryBlackboard.ComponentName, Replication.Catalog.Class(ClassPaths.InventoryComponent).Checksum),
        new StableSubobjectExport(player.Health, player.Pawn, ShieldDefaults.ComponentName, Replication.Catalog.Class(ClassPaths.HealthComponent).Checksum),
    ]);

    /// <summary>Names the character's interactor component.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The exports.</returns>
    public ObjectExports InteractorExports(WorldPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return new ObjectExports([
            new DynamicActorExport(player.Pawn),
            new StableSubobjectExport(player.Interactor, player.Pawn, PawnBlackboard.InteractorComponentName, Replication.Catalog.Class(ClassPaths.InteractorComponent).Checksum),
        ]);
    }

    /// <summary>A call on the character's interactor component about one interaction.</summary>
    /// <param name="player">The player.</param>
    /// <param name="function">Client function of the interactor component.</param>
    /// <param name="interaction">The interaction component.</param>
    /// <param name="interactionClass">Its class.</param>
    /// <returns>The content.</returns>
    public StableSubobjectContent InteractorCall(WorldPlayer player, string function, ulong interaction, string interactionClass)
    {
        ArgumentNullException.ThrowIfNull(player);
        ActorContentBlock call = Replication.Calls(new Dictionary<ulong, string> { [interaction] = interactionClass })
            .Call(ClassPaths.InteractorComponent, function, new Dictionary<string, object?> { [PropertyNames.Interaction] = interaction }, RpcDirection.Client);
        return new StableSubobjectContent(player.Interactor, call);
    }

    /// <summary>Names the controller's end-of-round component.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The exports.</returns>
    public ObjectExports EndOfRoundExports(WorldPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return new ObjectExports([
            new DynamicActorExport(player.Controller),
            new StableSubobjectExport(player.EndOfRound, player.Controller, PlayerClasses.EndOfRoundComponent, Replication.Catalog.Class(ClassPaths.EndOfRoundComponent).Checksum),
        ]);
    }

    /// <summary>A call on the controller's end-of-round component.</summary>
    /// <param name="player">The player.</param>
    /// <param name="function">Client function of the end-of-round component.</param>
    /// <param name="parameters">Parameter values by name.</param>
    /// <returns>The content.</returns>
    public StableSubobjectContent EndOfRoundCall(WorldPlayer player, string function, IReadOnlyDictionary<string, object?> parameters)
    {
        ArgumentNullException.ThrowIfNull(player);
        return new StableSubobjectContent(player.EndOfRound, Replication.Calls().Call(ClassPaths.EndOfRoundComponent, function, parameters, RpcDirection.Client));
    }

    /// <summary>Replicates the equipped Light Shield's initial health for the Raid HUD.</summary>
    /// <param name="player">The player.</param>
    /// <param name="inventory">The current loadout.</param>
    /// <returns>The health component properties.</returns>
    public StableSubobjectContent ShieldContent(WorldPlayer player, RaidInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(inventory);
        // TODO: Apply shield damage mitigation and drain shield health when server-side damage is
        // implemented. These values only initialize/display the shield; no damage is processed yet.
        // Medium/Heavy shields, movement penalties, equip charge-up and repair are future work.
        bool equipped = inventory.Shield is not null;
        return new StableSubobjectContent(player.Health, Replication.Encode(ClassPaths.HealthComponent, new Dictionary<string, object?>
        {
            [PropertyNames.Replicates] = true,
            [PropertyNames.IsActive] = true,
            [$"{PropertyNames.Armor}.{PropertyNames.Health}"] = inventory.ShieldHealth,
            [$"{PropertyNames.Armor}.{PropertyNames.CurrentMaxArmor}"] = inventory.ShieldHealth,
            [$"{PropertyNames.Armor}.{PropertyNames.HealthPerPlate}"] = equipped ? ShieldDefaults.HealthPerPlate : 0.0,
        }, Objects([])));
    }

    /// <summary>The inventory handles that enable the client's container-state receiver.</summary>
    /// <param name="player">The player.</param>
    /// <param name="inventory">The player's containers.</param>
    /// <returns>The component properties.</returns>
    public StableSubobjectContent InventoryContent(WorldPlayer player, RaidInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(inventory);
        Dictionary<string, object?> values = new()
        {
            [PropertyNames.Replicates] = true,
            [PropertyNames.IsActive] = true,
            [PropertyNames.EquippedLoadoutFrameItemAssetId] = inventory.FrameAssetId,
        };
        foreach ((string name, int id) in ((string, int)[])[
            (PropertyNames.Loadout, inventory.Loadout),
            (PropertyNames.Backpack, inventory.Backpack),
            (PropertyNames.Belt, inventory.Belt),
            (PropertyNames.SafePouch, inventory.SafePouch),
            (PropertyNames.Auxiliary, inventory.Auxiliary),
        ])
        {
            values[$"{name}.{PropertyNames.Id}"] = id;
            values[$"{name}.{PropertyNames.Revision}"] = (ushort)0;
        }

        return new StableSubobjectContent(player.Inventory, Replication.Encode(ClassPaths.InventoryComponent, values, Objects([])));
    }

    /// <summary>Creates the held items' gameplay objects before the inventory references them.</summary>
    public IReadOnlyList<DynamicSubobjectContent> WeaponItemCreation(WorldPlayer player, RaidInventory inventory)
    {
        return [.. inventory.Containers.SelectMany(container => container.Items).OfType<ContainerItem>()
            .Where(item => player.HeldItems.ContainsKey(item.Id)).Select(item =>
            {
                Dictionary<string, object?> values = new()
                {
                    [PropertyNames.QualityLevel] = 0,
                    [PropertyNames.DurabilityVal] = item.Durability,
                    [$"{PropertyNames.SubItemContainer}.{PropertyNames.Id}"] = item.ModContainerId ?? -1,
                    [$"{PropertyNames.SubItemContainer}.{PropertyNames.Revision}"] = item.ModContainerId is null ? ushort.MaxValue : ushort.MinValue,
                };
                string id = item.Id.ToString("N");
                foreach ((string name, int index) in ((string, int)[])
                    [(PropertyNames.A, 0), (PropertyNames.B, 1), (PropertyNames.C, 2), (PropertyNames.D, 3)])
                {
                    values[$"{PropertyNames.Id}.{name}"] = unchecked((int)uint.Parse(id.AsSpan(index * 8, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                }

                return new DynamicSubobjectContent(player.HeldItems[item.Id], _imports.Classes[WorldImports.HeldItemClasses[item.AssetIndex]],
                    Replication.Encode(ClassPaths.ItemBase, values, Objects([])), player.Inventory);
            })];
    }

    /// <summary>Publishes acknowledged item objects so the client recognizes the items it can hold.</summary>
    public StableSubobjectContent WeaponItemReferences(WorldPlayer player)
    {
        Dictionary<ulong, string> objects = Objects([]);
        foreach (ulong guid in player.HeldItems.Values)
        {
            objects[guid] = ClassPaths.ItemBase;
        }

        return new StableSubobjectContent(player.Inventory, Replication.Encode(ClassPaths.InventoryComponent,
            new Dictionary<string, object?> { [PropertyNames.KnownReplicatedItems] = player.HeldItems.Values.Select(guid => (object?)guid).ToArray() }, objects));
    }

    /// <summary>Exports naming the character's ability system component.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The exports and the component id.</returns>
    public static (ObjectExports Exports, ulong Root) AbilityExports(WorldPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        AbilitySystemComponentInfo component = CharacterDefaults.AbilitySystem;
        return (new ObjectExports([
            new DynamicActorExport(player.Pawn),
            new StableSubobjectExport(player.AbilitySystem, player.Pawn, component.ComponentName, component.ExportChecksum),
        ]), player.AbilitySystem);
    }

    /// <summary>The ability system's actor info, before its attribute sets exist.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The content.</returns>
    public StableSubobjectContent AbilityComponentContent(WorldPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return new StableSubobjectContent(player.AbilitySystem, Replication.Encode(CharacterDefaults.AbilitySystem.Class, AbilityValues(player), Objects([player.Pawn])));
    }

    /// <summary>Creates the character's attribute sets with the selected loadout's carrying limit.</summary>
    /// <param name="player">The player.</param>
    /// <param name="inventory">The selected loadout.</param>
    /// <returns>The construction blocks.</returns>
    public IReadOnlyList<DynamicSubobjectContent> AttributeSetCreation(WorldPlayer player, RaidInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(inventory);
        return
        [
            .. player.AttributeSets.Select((guid, index) =>
            {
                string classPath = CharacterDefaults.AttributeSets[index].Class;
                if (classPath != ClassPaths.PioneerCharacterAttributeSet)
                {
                    return new DynamicSubobjectContent(guid, _imports.Classes[classPath], new ActorContentBlock([], 0, false), null);
                }

                Dictionary<string, object?> values = new()
                {
                    [$"{PropertyNames.MaxWeight}.{PropertyNames.BaseValue}"] = (float)inventory.MaxWeight,
                    [$"{PropertyNames.MaxWeight}.{PropertyNames.CurrentValue}"] = (float)inventory.MaxWeight,
                };
                if (inventory.Weight != 0)
                {
                    values[$"{PropertyNames.Weight}.{PropertyNames.BaseValue}"] = (float)inventory.Weight;
                    values[$"{PropertyNames.Weight}.{PropertyNames.CurrentValue}"] = (float)inventory.Weight;
                }

                ActorContentBlock content = Replication.Encode(classPath, values, Objects([]));
                return new DynamicSubobjectContent(guid, _imports.Classes[classPath], content, null);
            }),
        ];
    }

    /// <summary>Registers the attribute sets on the ability system.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The content.</returns>
    public StableSubobjectContent AttributeSetRegistration(WorldPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        Dictionary<ulong, string> objects = Objects([player.Controller, player.PlayerState, player.Pawn]);
        objects[player.AbilitySystem] = CharacterDefaults.AbilitySystem.Class;
        for (int index = 0; index < player.AttributeSets.Count; index++)
        {
            objects[player.AttributeSets[index]] = CharacterDefaults.AttributeSets[index].Class;
        }

        Dictionary<string, object?> values = AbilityValues(player);
        values[PropertyNames.SpawnedAttributes] = player.AttributeSets.Select(guid => (object?)guid).ToArray();
        return new StableSubobjectContent(player.AbilitySystem, Replication.Encode(CharacterDefaults.AbilitySystem.Class, values, objects));
    }

    /// <summary>The ability system's granted abilities.</summary>
    /// <param name="player">The player.</param>
    /// <param name="abilities">The abilities.</param>
    /// <returns>The content.</returns>
    public StableSubobjectContent AbilityGrants(WorldPlayer player, IReadOnlyList<GrantedAbility> abilities)
    {
        ArgumentNullException.ThrowIfNull(player);
        return new StableSubobjectContent(player.AbilitySystem, Field(CharacterDefaults.AbilitySystem.Class, PropertyNames.ActivatableAbilities, writer => ActivatableAbilities.WriteInitial(writer, abilities)));
    }

    /// <summary>Exports naming the controller's input-correction component.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The exports and the component id.</returns>
    public static (ObjectExports Exports, ulong Root) CorrectionExports(WorldPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return (new ObjectExports([
            new DynamicActorExport(player.Controller),
            new StableSubobjectExport(player.Correction, player.Controller, PlayerClasses.CorrectionComponent, 0),
        ]), player.Correction);
    }

    /// <summary>Acknowledges the latest client input frame the server received (no correction of its state).</summary>
    /// <param name="player">The player.</param>
    /// <param name="frame">Latest input frame.</param>
    /// <param name="initial">Whether this is the component's first content.</param>
    /// <returns>The content.</returns>
    public StableSubobjectContent CorrectionContent(WorldPlayer player, uint frame, bool initial)
    {
        ArgumentNullException.ThrowIfNull(player);
        Dictionary<string, object?> values = [];
        if (initial)
        {
            values[PropertyNames.Replicates] = true;
            values[PropertyNames.IsActive] = false;
        }

        values[PropertyNames.RepServerProcessedLatestInputFrame] = frame <= int.MaxValue ? (int)frame : throw new InvalidPacketException("Input frame out of range");
        return new StableSubobjectContent(player.Correction, Replication.Encode(PlayerClasses.CorrectionComponentClass, values, Objects([])));
    }

    /// <summary>A pawn blackboard frame: a reset when there is no baseline, otherwise a delta from it.</summary>
    /// <param name="baseline">Delivery number of the previous snapshot (0 for a reset).</param>
    /// <param name="frame">The next delivery number, independent of the simulation tick in the state.</param>
    /// <param name="state">The frame's state.</param>
    /// <param name="previous">The previous frame's state (empty for a reset).</param>
    /// <param name="codec">The codec: the owner's (default) or <see cref="PendulumCodec.Others"/> for another player's character.</param>
    /// <returns>The pawn content.</returns>
    public ActorContentBlock BlackboardFrame(uint baseline, uint frame, IReadOnlyDictionary<int, PendulumSlot> state, IReadOnlyDictionary<int, PendulumSlot> previous,
        PendulumCodec? codec = null) =>
        Field(PlayerClasses.Pawn.Class, PropertyNames.BlackBoardReplicationPtr, writer =>
        {
            PendulumCodec encoder = codec ?? PendulumCodec.Default;
            writer.WriteBit(true); // The server supports the delta-struct form.
            BitWriter body = new();
            PawnBlackboardFrames.Write(body, state, previous, encoder);
            // The header names the body's field set: the client reads the body with the set the header names.
            BlackboardUpdate.Write(writer, baseline, frame, baseline == 0, encoder.Fields, body.ToArray(), body.Length,
                preferShortHeader: encoder == PendulumCodec.Others);
        });

    /// <summary>Records that the client confirmed possessing its character.</summary>
    /// <param name="player">The player.</param>
    public void AcknowledgePossession(WorldPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        lock (_lock)
        {
            _players[player.Principal.UserId] = Player(player.Principal) with { PossessionAcknowledged = true };
        }
    }

    private static Dictionary<string, object?> AbilityValues(WorldPlayer player) => new()
    {
        [PropertyNames.Replicates] = true,
        [PropertyNames.IsActive] = false,
        [PropertyNames.OwnerActor] = player.Pawn,
        [PropertyNames.AvatarActor] = player.Pawn,
    };

    // A field with its own serializer, sent as a field payload of the class.
    private ActorContentBlock Field(string classPath, string property, Action<BitWriter> write)
    {
        NetFieldTable table = Replication.Calls().Fields(classPath);
        NetField field = table.Field(property);
        BitWriter payload = new();
        write(payload);
        BitWriter writer = new();
        new ActorFieldPayload(field.Index, payload.ToArray(), payload.Length).Write(writer, table.Count);
        return new ActorContentBlock(writer.ToArray(), writer.Length, false);
    }

    private WorldActor Add(string classPath, Vector3D location, Dictionary<string, object?> properties, Rotator rotation = default)
    {
        WorldActor actor = new(Next(), classPath, location, properties, rotation);
        _actors[actor.Guid] = actor;
        return actor;
    }

    private ulong Next()
    {
        ulong guid = _nextGuid;
        _nextGuid += 2;
        return guid;
    }
}
