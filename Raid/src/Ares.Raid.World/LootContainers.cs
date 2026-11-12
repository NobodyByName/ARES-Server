using Ares.Common.Game.Inventory;
using Ares.Raid.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.World;

/// <summary>A loot container the server spawns.</summary>
/// <param name="Index">Index in <see cref="DamLoot.All"/>.</param>
/// <param name="Placement">Its class and transform.</param>
/// <param name="Guid">The container actor (dynamic id).</param>
/// <param name="Archetype">Exported id of its class default.</param>
/// <param name="Components">Its components (dynamic ids), in <see cref="DamLoot.Components"/> order.</param>
public sealed record LootContainer(int Index, LootPlacement Placement, ulong Guid, ulong Archetype, IReadOnlyList<ulong> Components)
{
    /// <summary>The search interaction.</summary>
    public ulong Interaction => Components[s_interaction];

    /// <summary>The item container.</summary>
    public ulong ItemContainer => Components[s_itemContainer];

    /// <summary>Where the container's prompt is when the container stands at a location.</summary>
    /// <param name="location">The container's location.</param>
    /// <returns>The prompt's world location.</returns>
    public Vector3D Prompt(Vector3D location)
    {
        Vector3D offset = Placement.Rotation.Rotate(DamLoot.PromptLocation);
        return new(location.X + offset.X, location.Y + offset.Y, location.Z + offset.Z);
    }

    private static readonly int s_itemContainer = DamLoot.Components.Select((name, index) => (name, index)).Single(entry => entry.name == DamLoot.ContainerComponent).index;

    private static readonly int s_interaction = DamLoot.Components.Select((name, index) => (name, index)).Single(entry => entry.name == DamLoot.InteractionComponent).index;
}

/// <summary>
/// The loot containers of a raid. A connection gets each one in four steps: the class default (loadable, as
/// the containers are not part of the map), the spawned actor at its transform, the names of its components,
/// then the search interaction's state.
/// </summary>
public sealed class LootCatalog
{
    // Static ids of the container classes and dynamic ids of the containers: clear of the other ranges.
    private const ulong FirstClassGuid = 0x2F0001;
    private const ulong FirstGuid = 0x380000;

    private readonly ReplicationContext _replication;
    private readonly ObjectExports _classExports;
    private readonly ObjectExports _componentExports;
    private readonly Dictionary<ulong, LootContainer> _interactions = [];

    /// <summary>Names the containers.</summary>
    /// <param name="replication">Replication context.</param>
    public LootCatalog(ReplicationContext replication)
    {
        _replication = replication ?? throw new ArgumentNullException(nameof(replication));
        List<StaticObjectExport> classes = [];
        Dictionary<string, ulong> archetypes = new(StringComparer.Ordinal);
        List<ObjectExport> components = [];
        List<LootContainer> containers = [];
        int stride = 1 + DamLoot.Components.Count;
        foreach (LootPlacement placement in DamLoot.All)
        {
            string archetype = placement.Container.Archetype;
            if (!archetypes.TryGetValue(archetype, out ulong archetypeGuid))
            {
                int dot = archetype.LastIndexOf('.');
                ulong package = FirstClassGuid + (2 * (ulong)classes.Count);
                classes.Add(new StaticObjectExport(package, 0, archetype[..dot], 0, false));
                archetypeGuid = FirstClassGuid + (2 * (ulong)classes.Count);
                classes.Add(new StaticObjectExport(archetypeGuid, package, archetype[(dot + 1)..], 0, false));
                archetypes[archetype] = archetypeGuid;
            }

            ulong guid = FirstGuid + (2 * (ulong)(containers.Count * stride));
            ulong[] parts = [.. DamLoot.Components.Select((_, index) => guid + (2 * (ulong)(index + 1)))];
            LootContainer container = new(containers.Count, placement, guid, archetypeGuid, parts);
            containers.Add(container);
            _interactions[container.Interaction] = container;
            components.Add(new DynamicActorExport(guid));
            components.AddRange(parts.Select((part, index) => new StableSubobjectExport(part, guid, DamLoot.Components[index], 0)));
        }

        // The animation set of the search, which the character's interactor state names.
        int name = DamLoot.SearchAnimation.LastIndexOf('.');
        ulong animationPackage = FirstClassGuid + (2 * (ulong)classes.Count);
        classes.Add(new StaticObjectExport(animationPackage, 0, DamLoot.SearchAnimation[..name], 0, false));
        SearchAnimation = FirstClassGuid + (2 * (ulong)classes.Count);
        classes.Add(new StaticObjectExport(SearchAnimation, animationPackage, DamLoot.SearchAnimation[(name + 1)..], 0, false));
        _classExports = new ObjectExports(classes);
        _componentExports = new ObjectExports(components);
        Containers = containers;
    }

    /// <summary>All containers.</summary>
    public IReadOnlyList<LootContainer> Containers { get; }

    /// <summary>Id of the animation set of a search.</summary>
    public ulong SearchAnimation { get; }

    /// <summary>The container a search interaction belongs to.</summary>
    /// <param name="interaction">Interaction id.</param>
    /// <returns>The container, or null.</returns>
    public LootContainer? ByInteraction(ulong interaction) => _interactions.GetValueOrDefault(interaction);

    /// <summary>Exports the container's class default and the animation set of its search.</summary>
    /// <param name="container">The container.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch ExportBunch(LootContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return Bunches.Exports(_classExports, [container.Archetype, SearchAnimation]);
    }

    /// <summary>Spawns the container actor at its transform.</summary>
    /// <param name="container">The container.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <param name="location">Where to spawn it instead of its placement (testing).</param>
    /// <param name="openedAt">Server time it was opened, once it has been (it then shows open).</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch OpenBunch(LootContainer container, int channel, int sequence, Vector3D? location = null, double? openedAt = null)
    {
        ArgumentNullException.ThrowIfNull(container);
        Dictionary<string, object?> values = new()
        {
            [PropertyNames.Role] = (ulong)NetRole.Authority,
            [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
        };
        if (openedAt is { } opened)
        {
            values[PropertyNames.CurrentOpenTime] = opened;
        }

        ActorContentBlock actor = _replication.Encode(DamLoot.ActorClass, values);
        ActorSpawn spawn = new(container.Guid, container.Archetype, 0, location ?? container.Placement.Location, container.Placement.Rotation);
        return new ActorInitialization(spawn, null, [actor]).ToBunch(channel, sequence, []);
    }

    /// <summary>Names the container's components.</summary>
    /// <param name="container">The container.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch ComponentExportBunch(LootContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);
        return Bunches.Exports(_componentExports, container.Components);
    }

    /// <summary>Sets the search interaction's state.</summary>
    /// <param name="container">The container.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="state">Interaction state.</param>
    /// <param name="holdStartedAt">Server time the search hold starts, while it runs (no hold otherwise).</param>
    /// <param name="firstOpenedAt">Server time the container was first opened, once it has been.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch InteractionBunch(LootContainer container, int channel, CurrentInteractionState state, double? holdStartedAt = null, double? firstOpenedAt = null)
    {
        ArgumentNullException.ThrowIfNull(container);
        Dictionary<string, object?> values = new()
        {
            [PropertyNames.CurrentInteractionState] = (ulong)state,
            [PropertyNames.InteractionServerHoldStartedTime] = holdStartedAt ?? DamLoot.NoHoldStartedTime,
        };

        if (firstOpenedAt is { } opened)
        {
            Opened(values, opened);
        }

        return Bunches.Actor(channel, new StableSubobjectContent(container.Interaction, _replication.Encode(DamLoot.InteractionClass, values)).Write);
    }

    // What changes on a search interaction once its container has been opened: the breach is over for the whole
    // raid, so the next search needs no raider tool, no hold and no breach animation (the prompt becomes "Search").
    private static void Opened(Dictionary<string, object?> values, double firstOpenedAt)
    {
        values[PropertyNames.HasBeenOpened] = true;
        values[PropertyNames.FirstOpenedServerTime] = firstOpenedAt;
        values[PropertyNames.EquipRaiderToolDuringInitialization] = false;
        values[PropertyNames.HoldDuration] = DamLoot.OpenedHoldSeconds;
        values[PropertyNames.UseAnimationContainer] = false;
        values[PropertyNames.UseWaitDelayForAnimationNonLocal] = false;
    }

    /// <summary>
    /// Opens the container to a player: the container shows open (its open time also ends the breach), the search
    /// goes on as opened, and the item container names the player's view of it.
    /// </summary>
    /// <param name="container">The container.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="itemContainer">Id of the container among the player's containers.</param>
    /// <param name="firstOpenedAt">Server time the container was first opened.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch OpenedBunch(LootContainer container, int channel, int itemContainer, double firstOpenedAt)
    {
        ArgumentNullException.ThrowIfNull(container);
        Dictionary<string, object?> values = new() { [PropertyNames.CurrentInteractionState] = (ulong)CurrentInteractionState.Ongoing };
        Opened(values, firstOpenedAt);
        ActorContentBlock search = _replication.Encode(DamLoot.InteractionClass, values);
        ActorContentBlock items = _replication.Encode(DamLoot.ContainerClass, new Dictionary<string, object?>
        {
            [$"{PropertyNames.Content}.{PropertyNames.Id}"] = itemContainer,
            [$"{PropertyNames.Content}.{PropertyNames.Revision}"] = (ushort)0,
        });
        ActorContentBlock actor = _replication.Encode(DamLoot.ActorClass, new Dictionary<string, object?> { [PropertyNames.CurrentOpenTime] = firstOpenedAt });
        return Bunches.Actor(channel, writer =>
        {
            actor.Write(writer);
            new StableSubobjectContent(container.Interaction, search).Write(writer);
            new StableSubobjectContent(container.ItemContainer, items).Write(writer);
        });
    }
}

/// <summary>
/// What each loot container of a raid holds, shared by its players: a container keeps what the players who
/// opened it left inside, and the time it was first opened.
/// </summary>
/// <param name="count">Number of containers.</param>
public sealed class LootStates(int count)
{
    private readonly Dictionary<int, IReadOnlyList<ContainerItem?>> _items = [];
    private readonly Dictionary<int, double> _openedAt = [];

    private static readonly ushort s_duck = checked((ushort)LoadoutDefinitions.UsableIndex(StorageItems.RubberDuckAssetId));

    /// <summary>Unit weight of each item type a container may hold.</summary>
    public static IReadOnlyDictionary<ushort, double> UnitWeights { get; } = new Dictionary<ushort, double>
    {
        [s_duck] = LoadoutDefinitions.ItemWeight(StorageItems.RubberDuckAssetId),
    };

    /// <summary>A container's slots: one rubber duck until real loot tables are supported.</summary>
    /// <param name="index">Container index.</param>
    /// <returns>Its slots.</returns>
    public IReadOnlyList<ContainerItem?> Items(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, count);
        if (!_items.TryGetValue(index, out IReadOnlyList<ContainerItem?>? items))
        {
            ContainerItem?[] slots = new ContainerItem?[DamLoot.SlotCount];
            slots[0] = new ContainerItem(s_duck, Guid.NewGuid(), 1, 1);
            items = _items[index] = Array.AsReadOnly(slots);
        }

        return items;
    }

    /// <summary>Keeps what a player left in a container.</summary>
    /// <param name="index">Container index.</param>
    /// <param name="items">Its slots.</param>
    public void Store(int index, IReadOnlyList<ContainerItem?> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        _ = Items(index);
        _items[index] = Array.AsReadOnly(items.ToArray());
    }

    /// <summary>When a container was first opened, if it has been.</summary>
    /// <param name="index">Container index.</param>
    /// <returns>Server time, or null.</returns>
    public double? OpenedAt(int index) => _openedAt.TryGetValue(index, out double at) ? at : null;

    /// <summary>Marks a container opened.</summary>
    /// <param name="index">Container index.</param>
    /// <param name="at">Server time.</param>
    /// <returns>When it was first opened.</returns>
    public double Open(int index, double at) => _openedAt.TryGetValue(index, out double first) ? first : _openedAt[index] = at;
}
