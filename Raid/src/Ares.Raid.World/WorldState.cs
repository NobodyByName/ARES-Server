using Ares.Raid.Replication;
using Ares.Raid.Transport;
using Ares.Raid.World.Movement;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Characters;
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
            [PropertyNames.MaxPlayers] = 1,
            [PropertyNames.MaxSquads] = 1,
            [PropertyNames.MaxSquadSize] = 1,
            [PropertyNames.NumExpectedPlayersToJoin] = 1,
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
    /// <returns>The world actor.</returns>
    public SpawnedWorldActor GameStateActor(int channel) => new(
        channel,
        GameState.Guid,
        _imports.Archetypes[GameState.ClassPath],
        [.. _imports.Classes.Values],
        [Replication.Encode(GameState.ClassPath, GameState.Properties, Objects([GameState.Guid]))]);

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
    /// <returns>The player.</returns>
    public WorldPlayer PlayerFor(SessionPrincipal principal, UniqueNetId uniqueId, Vector3D spawn, Rotator rotation = default)
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
                [PropertyNames.SquadId] = MatchId,
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
            };
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

    /// <summary>
    /// The controller and player-state properties and HUD assignment. Pawn creation follows loading; its
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
        string controllerClass = PlayerClasses.PlayerController.Class;
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
            if (guid == player.Controller)
            {
                contents.Add((guid, Replication.Calls(objects).Call(controllerClass, FunctionNames.ClientSetHud, new Dictionary<string, object?>
                {
                    [PropertyNames.NewHudClass] = _imports.Classes[PlayerClasses.Hud],
                }, RpcDirection.Client)));
            }
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
    /// notice, the pawn and ready flag together, the player-state links, a restart if needed, and the pawn's
    /// walking movement mode at its location.
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
        WorldActor pawn = Actor(player.Pawn);
        return
        [
            (player.Pawn, Replication.Encode(pawnClass, new Dictionary<string, object?> { [PropertyNames.Controller] = player.Controller }, objects)),
            (player.Pawn, calls.Call(pawnClass, FunctionNames.ClientPostInitializeAbilitySystem, new Dictionary<string, object?>(), RpcDirection.Client)),
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
            (player.Pawn, calls.Call(pawnClass, FunctionNames.ClientAdjustPosition, new Dictionary<string, object?>
            {
                [PropertyNames.Timestamp] = 1.0f,
                [PropertyNames.NewLoc] = pawn.Location,
                [PropertyNames.NewVel] = default(Vector3D),
                [PropertyNames.NewBase] = 0UL,
                [PropertyNames.NewBaseBoneName] = NetworkName.None,
                [PropertyNames.HasBase] = false,
                [PropertyNames.BaseRelativePosition] = false,
                [PropertyNames.ServerMovementMode] = (ulong)MovementMode.Walking,
            }, RpcDirection.Client)),
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

    /// <summary>Creates the attribute sets (at their class defaults) inside the character.</summary>
    /// <param name="player">The player.</param>
    /// <returns>The construction blocks.</returns>
    public IReadOnlyList<DynamicSubobjectContent> AttributeSetCreation(WorldPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return
        [
            .. player.AttributeSets.Select((guid, index) => new DynamicSubobjectContent(
                guid,
                _imports.Classes[CharacterDefaults.AttributeSets[index].Class],
                new ActorContentBlock([], 0, false),
                null)),
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
    /// <param name="baseline">Frame number of the previous frame (0 for a reset).</param>
    /// <param name="frame">The frame number (a client input frame).</param>
    /// <param name="state">The frame's state.</param>
    /// <param name="previous">The previous frame's state (empty for a reset).</param>
    /// <returns>The pawn content.</returns>
    public ActorContentBlock BlackboardFrame(uint baseline, uint frame, IReadOnlyDictionary<int, PendulumSlot> state, IReadOnlyDictionary<int, PendulumSlot> previous) =>
        Field(PlayerClasses.Pawn.Class, PropertyNames.BlackBoardReplicationPtr, writer =>
        {
            writer.WriteBit(true); // The server supports the delta-struct form.
            BitWriter body = new();
            PawnBlackboardFrames.Write(body, state, previous);
            BlackboardUpdate.Write(writer, baseline, frame, baseline == 0, BlackboardUpdate.DefaultFlags, body.ToArray(), body.Length);
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
