using System.Globalization;
using Ares.Common.Game.Inventory;
using Ares.Raid.Replication;
using Ares.Raid.World.Inventory;
using Ares.Structs.Backend.Progression;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.World;

/// <summary>An item stack lying in the raid: a pickup actor.</summary>
/// <param name="Index">Index in <see cref="WorldPickups.All"/>.</param>
/// <param name="Guid">The pickup actor (dynamic id); its interaction and collider components take the next two ids.</param>
/// <param name="DataAsset">Static id of the item's data asset.</param>
/// <param name="Item">The stack lying there; its amount is 0 once the whole stack was picked up.</param>
/// <param name="Location">Where the actor stands.</param>
/// <param name="Rotation">The actor's rotation.</param>
/// <param name="DroppedBy">User id of the player who dropped the item.</param>
/// <param name="DroppingPlayerId">Player id (of the player state) of the player who dropped the item.</param>
/// <param name="DroppedAt">Raid world time of the drop, in seconds.</param>
public sealed record WorldPickup(int Index, ulong Guid, ulong DataAsset, ContainerItem Item, Vector3D Location, Rotator Rotation, long DroppedBy, int DroppingPlayerId, double DroppedAt)
{
    /// <summary>The pickup's interaction component (the "Pickup" prompt).</summary>
    public ulong Interaction => Guid + 2;

    /// <summary>The pickup's collision mesh component.</summary>
    public ulong RootCollider => Guid + 4;

    /// <summary>Changes since the spawn (merges and pickups); clients that have an older revision get the pickup again.</summary>
    public int Revision { get; init; }

    /// <summary>The character that picked the whole stack up, once one has.</summary>
    public ulong? PickedUpBy { get; init; }

    /// <summary>When the whole stack was picked up, on the raid clock.</summary>
    public double? PickedUpAt { get; init; }

    /// <summary>The actor is gone: every client that has it destroys it.</summary>
    public bool Destroyed { get; init; }
}

/// <summary>
/// The item stacks lying in a raid. A drop joins the same player's nearby pickup of the item when it can, or spawns a
/// pickup actor; a pickup is taken whole or in part, first come first served; a pickup taken whole floats to its
/// picker on the clients and is destroyed shortly after.
/// </summary>
public sealed class WorldPickups
{
    /// <summary>Seconds between a pickup taken whole and the destruction of its actor; clients float the item to the picker meanwhile.</summary>
    public const double DestroySeconds = 1.0;

    // Static ids of the pickup class default and the item data assets: clear of the other static ranges.
    private const ulong FirstExportGuid = 0x3F0001;

    // Dynamic ids start here, or above the raid's loot containers when they reach further: clear of the players'
    // actors, the ladders and the loot containers. Each pickup takes its actor id and the two after it.
    private const ulong FirstGuid = 0x400000;
    private const ulong GuidStride = 6;

    // Data asset of every item a player can carry (and so drop), by usable index.
    private static readonly Dictionary<ushort, string> s_dataAssets = ItemTechnicalNames.AssetIds
        .Where(pair => LoadoutDefinitions.SupportsCarriedItem(pair.Value))
        .ToDictionary(pair => checked((ushort)LoadoutDefinitions.UsableIndex(pair.Value)), pair => ItemTechnicalNames.DataAssets[pair.Key]);

    private readonly ReplicationContext _replication;
    private readonly WorldState _state;
    private readonly ulong _firstGuid;
    private readonly ObjectExports _exports;
    private readonly Dictionary<ushort, ulong> _dataAssets = [];
    private readonly List<WorldPickup> _pickups = [];
    private readonly List<string> _events = [];

    /// <summary>Creates the raid's empty set of pickups.</summary>
    /// <param name="replication">Replication context.</param>
    /// <param name="state">The raid's actors (the characters that pick items up).</param>
    /// <param name="firstFreeGuid">First dynamic id no other actor of the raid uses.</param>
    public WorldPickups(ReplicationContext replication, WorldState state, ulong firstFreeGuid)
    {
        _replication = replication ?? throw new ArgumentNullException(nameof(replication));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        if (NetGuid.IsStatic(firstFreeGuid))
        {
            throw new ArgumentOutOfRangeException(nameof(firstFreeGuid), "Dynamic ids are even.");
        }

        _firstGuid = Math.Max(FirstGuid, firstFreeGuid);
        List<StaticObjectExport> records = [];
        Archetype = Export(Pickups.Class.Archetype);
        foreach ((ushort index, string path) in s_dataAssets)
        {
            _dataAssets[index] = Export(path);
        }

        _exports = new ObjectExports(records);
        return;

        // An object path exported by name: its package, then the object in it.
        ulong Export(string path)
        {
            int dot = path.LastIndexOf('.');
            ulong package = FirstExportGuid + (2 * (ulong)records.Count);
            records.Add(new StaticObjectExport(package, 0, path[..dot], 0, false));
            ulong exported = FirstExportGuid + (2 * (ulong)records.Count);
            records.Add(new StaticObjectExport(exported, package, path[(dot + 1)..], 0, false));
            return exported;
        }
    }

    /// <summary>Static id of the pickup class default, the archetype of every pickup.</summary>
    public ulong Archetype { get; }

    /// <summary>Every pickup of the raid, also those picked up and destroyed, by index.</summary>
    public IReadOnlyList<WorldPickup> All => _pickups;

    /// <summary>What happened to the pickups, oldest first, one line each.</summary>
    public IReadOnlyList<string> Events => _events;

    /// <summary>Whether an item can lie in the world: it has a data asset.</summary>
    /// <param name="assetIndex">Index of the item in the usable item table.</param>
    /// <returns>True when the item can be dropped.</returns>
    public static bool Supports(ushort assetIndex) => s_dataAssets.ContainsKey(assetIndex);

    /// <summary>The pickup an interaction target names: the pickup's interaction component or the actor itself.</summary>
    /// <param name="target">Object id.</param>
    /// <returns>The pickup, or null.</returns>
    public WorldPickup? ByTarget(ulong target)
    {
        if (target < _firstGuid || NetGuid.IsStatic(target))
        {
            return null;
        }

        ulong offset = (target - _firstGuid) % GuidStride;
        ulong index = (target - _firstGuid) / GuidStride;
        return offset is 0 or 2 && index < (ulong)_pickups.Count ? _pickups[(int)index] : null;
    }

    /// <summary>
    /// Puts a dropped item into the world. It joins the dropping player's pickup of the same item when that lies within
    /// the merge distance, the item stacks above the merge threshold and the whole amount fits; otherwise it spawns a
    /// new pickup.
    /// </summary>
    /// <param name="player">User id of the dropping player.</param>
    /// <param name="playerId">Player id of the dropping player's player state.</param>
    /// <param name="item">The dropped item.</param>
    /// <param name="location">Where a new pickup stands.</param>
    /// <param name="rotation">A new pickup's rotation.</param>
    /// <param name="worldSeconds">Raid world time.</param>
    /// <returns>The pickup that now holds the item.</returns>
    public WorldPickup Drop(long player, int playerId, ContainerItem item, Vector3D location, Rotator rotation, double worldSeconds)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!_dataAssets.TryGetValue(item.AssetIndex, out ulong dataAsset))
        {
            throw new InvalidOperationException("The item has no data asset to show in the world");
        }

        int maximum = RaidInventory.MaxAmount(item.AssetIndex);
        if (maximum > Pickups.MergeStackSizeThreshold
            && _pickups.Find(pickup => pickup is { PickedUpBy: null, Destroyed: false } && pickup.DroppedBy == player
                && pickup.Item.AssetIndex == item.AssetIndex && pickup.Item.Amount + item.Amount <= maximum
                && Distance(pickup.Location, location) <= Pickups.MergeDistance) is { } target)
        {
            WorldPickup merged = target with { Item = target.Item with { Amount = target.Item.Amount + item.Amount }, Revision = target.Revision + 1 };
            _pickups[target.Index] = merged;
            _events.Add($"merge #{target.Index} {Describe(item)} into {merged.Item.Amount} by {player}");
            return merged;
        }

        WorldPickup spawned = new(_pickups.Count, _firstGuid + (GuidStride * (ulong)_pickups.Count), dataAsset, item, location, rotation, player, playerId, worldSeconds);
        _pickups.Add(spawned);
        _events.Add($"drop #{spawned.Index} {Describe(item)} by {player} at {Describe(location)} guid 0x{spawned.Guid:x}");
        return spawned;
    }

    /// <summary>
    /// A player takes what fits of a pickup into their inventory. A pickup already taken whole is left alone; one
    /// taken whole keeps its actor until <see cref="Expire"/> destroys it.
    /// </summary>
    /// <param name="index">Pickup index.</param>
    /// <param name="player">User id of the picking player.</param>
    /// <param name="inventory">The picking player's inventory.</param>
    /// <param name="pawn">The picking player's character.</param>
    /// <param name="now">Raid clock.</param>
    /// <returns>The amount taken.</returns>
    public int PickUp(int index, long player, RaidInventory inventory, ulong pawn, double now)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        WorldPickup pickup = _pickups[index];
        if (pickup.PickedUpBy is not null || pickup.Destroyed)
        {
            _events.Add($"pickup #{index} by {player} ignored: already taken");
            return 0;
        }

        int taken = inventory.PickUp(pickup.Item);
        if (taken <= 0)
        {
            _events.Add($"pickup #{index} by {player} ignored: no room for {Describe(pickup.Item)}");
            return 0;
        }

        int left = pickup.Item.Amount - taken;
        _pickups[index] = pickup with
        {
            Item = pickup.Item with { Amount = left },
            Revision = pickup.Revision + 1,
            PickedUpBy = left == 0 ? pawn : null,
            PickedUpAt = left == 0 ? now : null,
        };
        _events.Add(left == 0
            ? $"pickup #{index} {Describe(pickup.Item)} by {player}"
            : $"pickup #{index} {taken} of {Describe(pickup.Item)} by {player}, {left} left");
        return taken;
    }

    /// <summary>Destroys the pickups taken whole at least <see cref="DestroySeconds"/> ago.</summary>
    /// <param name="now">Raid clock.</param>
    public void Expire(double now)
    {
        for (int index = 0; index < _pickups.Count; index++)
        {
            if (_pickups[index] is not { Destroyed: false, PickedUpAt: { } at } pickup || now < at + DestroySeconds)
            {
                continue;
            }

            _pickups[index] = pickup with { Destroyed = true };
            _events.Add($"destroy #{index} guid 0x{pickup.Guid:x}");
        }
    }

    /// <summary>The pickups a player at a location gets: those on the ground within their net cull distance.</summary>
    /// <param name="location">The player's location.</param>
    /// <returns>The pickups, nearest first.</returns>
    public IReadOnlyList<WorldPickup> Relevant(Vector3D location) =>
        [.. _pickups.Where(pickup => pickup is { PickedUpBy: null, Destroyed: false } && Distance(pickup.Location, location) <= Pickups.NetCullDistance)
            .OrderBy(pickup => Distance(pickup.Location, location)).ThenBy(pickup => pickup.Index)];

    /// <summary>Exports pickup class defaults and data assets by name; the client loads them.</summary>
    /// <param name="roots">Ids to export: <see cref="Archetype"/> or a pickup's <see cref="WorldPickup.DataAsset"/>.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch ExportBunch(IReadOnlyList<ulong> roots) => Bunches.Exports(_exports, roots);

    /// <summary>
    /// Opens a pickup's channel: spawns the actor where it lies with all its properties, or, for a client that keeps
    /// it dormant, names the actor it already has and brings it up to date.
    /// </summary>
    /// <param name="pickup">The pickup.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch OpenBunch(WorldPickup pickup, int channel, int sequence)
    {
        ArgumentNullException.ThrowIfNull(pickup);
        PickupProperties properties = new()
        {
            ReplicateMovement = pickup.PickedUpBy is null ? null : false,
            Role = NetRole.Authority,
            RemoteRole = NetRole.SimulatedProxy,
            Movement = new RepMovement(pickup.Location, pickup.Rotation),
            DataAsset = pickup.DataAsset,
            Item = Item(pickup.Item),
            DropServerWorldTime = pickup.DroppedAt,
            DroppingPlayerId = pickup.DroppingPlayerId,
            PickedUpBy = pickup.PickedUpBy,
        };
        ActorContentBlock actor = _replication.Encode(Pickups.ActorClass, properties.ToValues(), Objects(pickup));
        ActorSpawn spawn = new(pickup.Guid, Archetype, 0, pickup.Location, pickup.Rotation);
        return new ActorInitialization(spawn, null, [actor]).ToBunch(channel, sequence, [pickup.DataAsset]);
    }

    /// <summary>Names the pickup's components.</summary>
    /// <param name="pickup">The pickup.</param>
    /// <returns>The bunch.</returns>
    public static ChannelBunch ComponentExportBunch(WorldPickup pickup)
    {
        ArgumentNullException.ThrowIfNull(pickup);
        ObjectExports exports = new([
            new DynamicActorExport(pickup.Guid),
            new StableSubobjectExport(pickup.Interaction, pickup.Guid, Pickups.InteractionComponent, 0),
            new StableSubobjectExport(pickup.RootCollider, pickup.Guid, Pickups.RootColliderComponent, 0),
        ]);
        return Bunches.Exports(exports, [pickup.Interaction, pickup.RootCollider]);
    }

    /// <summary>
    /// Sends the pickup's interaction: attached to the collider that also carries its prompt, idle while the pickup lies
    /// there, succeeded and disabled once taken whole.
    /// </summary>
    /// <param name="pickup">The pickup.</param>
    /// <param name="channel">Channel index.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch InteractionBunch(WorldPickup pickup, int channel)
    {
        ArgumentNullException.ThrowIfNull(pickup);
        ActorContentBlock interaction = _replication.Encode(Pickups.InteractionClass, new Dictionary<string, object?>
        {
            [PropertyNames.AttachParent] = pickup.RootCollider,
            [PropertyNames.CurrentInteractionState] = (ulong)InteractionState(pickup),
            [PropertyNames.ProxyPromptComponentOrStyle] = new ComponentOrStyle(pickup.RootCollider),
        }, new Dictionary<ulong, string> { [pickup.RootCollider] = Pickups.RootColliderClass });
        return Bunches.Actor(channel, writer => new StableSubobjectContent(pickup.Interaction, interaction).Write(writer));
    }

    /// <summary>
    /// Brings an open pickup channel up to date: the amount after a merge or a partial pickup; once taken whole, also
    /// movement off, the picker (clients float the item to them) and the interaction succeeded and disabled.
    /// </summary>
    /// <param name="pickup">The pickup.</param>
    /// <param name="channel">Channel index.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch UpdateBunch(WorldPickup pickup, int channel)
    {
        ArgumentNullException.ThrowIfNull(pickup);
        PickupProperties properties = new()
        {
            ReplicateMovement = pickup.PickedUpBy is null ? null : false,
            Item = new PickupItem { Amount = pickup.Item.Amount },
            PickedUpBy = pickup.PickedUpBy,
        };
        ActorContentBlock actor = _replication.Encode(Pickups.ActorClass, properties.ToValues(), Objects(pickup));
        ActorContentBlock? interaction = pickup.PickedUpBy is null ? null : _replication.Encode(Pickups.InteractionClass, new Dictionary<string, object?>
        {
            [PropertyNames.CurrentInteractionState] = (ulong)InteractionState(pickup),
        });
        return Bunches.Actor(channel, writer =>
        {
            actor.Write(writer);
            if (interaction is not null)
            {
                new StableSubobjectContent(pickup.Interaction, interaction).Write(writer);
            }
        });
    }

    /// <summary>Destroys a pickup whose channel is closed (dormant) on the client: opens and closes it naming only the actor.</summary>
    /// <param name="pickup">The pickup.</param>
    /// <param name="channel">A free channel index.</param>
    /// <returns>The bunch.</returns>
    public static ChannelBunch DestroyBunch(WorldPickup pickup, int channel)
    {
        ArgumentNullException.ThrowIfNull(pickup);
        BitWriter writer = new();
        NetGuid.Write(writer, pickup.Guid);
        return new ChannelBunch(channel, writer.ToArray(), writer.Length) { Name = ChannelName.Actor, Opened = true, CloseReason = Bunches.Destroyed };
    }

    // The stack as a pickup replicates it. Durability goes only when below full, as with items that have none.
    private static PickupItem Item(ContainerItem item)
    {
        int durability = (int)Math.Round(item.Durability * InventoryBlackboard.DurabilityScale);
        return new PickupItem
        {
            DataAssetIndex = item.AssetIndex,
            Quality = 0,
            Amount = item.Amount,
            QuantizedDurability = durability < InventoryBlackboard.DurabilityScale ? (sbyte)durability : null,
            AmmoInClip = checked((ushort)item.AmmoInClip),
            Guid = item.Id,
        };
    }

    private static CurrentInteractionState InteractionState(WorldPickup pickup) =>
        pickup.PickedUpBy is null ? CurrentInteractionState.Idle : CurrentInteractionState.SuccessDisabled;

    // The objects a pickup's properties name: its item's data asset and, once taken, the picker's character.
    private Dictionary<ulong, string> Objects(WorldPickup pickup)
    {
        Dictionary<ulong, string> objects = pickup.PickedUpBy is { } pawn ? _state.Objects([pawn]) : [];
        objects[pickup.DataAsset] = Pickups.DataAssetClass;
        return objects;
    }

    private static string Describe(ContainerItem item) => $"item {item.AssetIndex} x{item.Amount}";

    private static string Describe(Vector3D location) => string.Create(CultureInfo.InvariantCulture, $"({location.X:0},{location.Y:0},{location.Z:0})");

    private static double Distance(Vector3D a, Vector3D b) =>
        Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)) + ((a.Z - b.Z) * (a.Z - b.Z)));
}
