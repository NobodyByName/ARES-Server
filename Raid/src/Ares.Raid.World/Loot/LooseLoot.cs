using System.Numerics;
using Ares.Common.Game.Loot;
using Ares.Raid.World.Actors;
using Ares.Raid.World.Collision;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.Inventory;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Loot;

/// <summary>How a raid's loose item sockets turned into pickups.</summary>
/// <param name="Sockets">Loose item sockets of the map.</param>
/// <param name="Spawned">Pickups placed on sockets.</param>
/// <param name="InactiveLayer">Sockets skipped because their prop streams with a data layer the round leaves inactive.</param>
/// <param name="NothingToHold">Sockets skipped because no item may lie on their tag.</param>
/// <param name="SkippedByRoll">Sockets whose spawn roll failed.</param>
/// <param name="SkippedByLimiter">Sockets under a placement limiter cap that was reached.</param>
/// <param name="Grounded">Pickups of physics sockets placed on the ground below their socket.</param>
/// <param name="DecidedByRule">
/// Sockets of the active data layers whose spawn chance, stack or both an operator rule decides (a loot list counts only
/// when it lists an item that can lie in the world).
/// </param>
/// <param name="SpawnedByLootValue">Pickups placed, by the loot value of the area around their socket.</param>
public readonly record struct LooseLootCounts(int Sockets, int Spawned, int InactiveLayer, int NothingToHold, int SkippedByRoll, int SkippedByLimiter, int Grounded,
    int DecidedByRule, IReadOnlyDictionary<PointOfInterestLootValue, int> SpawnedByLootValue);

/// <summary>A loose item a raid puts on a socket.</summary>
/// <param name="Socket">The socket.</param>
/// <param name="Stack">The stack lying there.</param>
/// <param name="Location">Where the pickup stands: the socket, or the ground below a physics socket.</param>
/// <param name="Rotation">The pickup's rotation: the socket's, level (yaw only) on a physics socket.</param>
public sealed record LooseItemPlacement(LooseItemSocket Socket, LootStack Stack, Vector3D Location, Rotator Rotation);

/// <summary>
/// The loose items of a raid: among the loose item sockets of the active data layers that can hold an item, those the
/// raid's rolls pick (<see cref="LootSpawnSelection.SelectLoose"/>), each with one stack (<see cref="LooseItemRolls"/>).
/// The operator's loot rules cover loose item sockets by tag or id: a rule's spawn chance replaces the socket's roll
/// (loose item chance, area value, limiters), and a rule's loot list gives its one stack when the list has an item that
/// can lie in the world (else the socket keeps its default roll). Group fills and reveal delays do not apply.
/// An item of a physics socket (<see cref="LooseItemOptions.EnablePhysics"/>) lies on the first surface straight below
/// the socket when the raid has collision, else at the socket.
/// </summary>
/// <param name="Placements">The items to place, in socket order.</param>
/// <param name="Counts">How the sockets turned into items.</param>
public sealed record LooseLoot(IReadOnlyList<LooseItemPlacement> Placements, LooseLootCounts Counts)
{
    // Turn the raid's seed into the seeds of the loose item rolls and contents, apart from the containers' streams.
    private const int SelectionSeedSalt = 0x4C4F4F53;
    private const int ContentSeedSalt = 0x4954454D;

    // The ground probe starts a little above the socket, so a socket lying on a surface finds that surface.
    private const double GroundProbeLift = 5;
    private const double GroundProbeDepth = 5000;

    /// <summary>Rolls the loose items of a raid.</summary>
    /// <param name="rolls">What a socket can hold and the stack it gets.</param>
    /// <param name="sockets">The map's loose item sockets.</param>
    /// <param name="rules">Chances and limiters; without a loose item chance no socket gets an item, whatever the loot rules say.</param>
    /// <param name="lootRules">The operator's loot rules.</param>
    /// <param name="activeDataLayers">Names of the raid's active data layers.</param>
    /// <param name="seed">The raid's seed.</param>
    /// <param name="collision">The raid's static collision, or null.</param>
    /// <returns>The loose items.</returns>
    public static LooseLoot Roll(LooseItemRolls rolls, IReadOnlyList<LooseItemSocket> sockets, LootSpawnRules rules, LootRules lootRules, IReadOnlyCollection<string> activeDataLayers,
        int seed, CollisionWorld? collision)
    {
        ArgumentNullException.ThrowIfNull(rolls);
        ArgumentNullException.ThrowIfNull(sockets);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(lootRules);
        ArgumentNullException.ThrowIfNull(activeDataLayers);
        HashSet<string> active = new(activeDataLayers, StringComparer.Ordinal);
        Dictionary<string, bool> holds = new(StringComparer.OrdinalIgnoreCase);
        List<LooseItemSocket> servable = [];
        List<double?> setChances = [];
        List<LootList?> lists = [];
        (int inactive, int empty, int ruled) = (0, 0, 0);
        foreach (LooseItemSocket socket in sockets)
        {
            if (socket.DataLayer is { } layer && !active.Contains(layer))
            {
                inactive++;
                continue;
            }

            string id = LootContainerIds.Of(socket);
            LootList? list = lootRules.LootOf(id, socket.Tag)?.Loot is { } listed && rolls.CanHold(listed) ? listed : null;
            if (list is null && !CanHold(socket.Tag))
            {
                empty++;
                continue;
            }

            double? chance = lootRules.SpawnChance(id, [socket.Tag]);
            ruled += chance is not null || list is not null ? 1 : 0;
            servable.Add(socket);
            setChances.Add(chance);
            lists.Add(list);
        }

        Dictionary<PointOfInterestLootValue, int> byValue = Enum.GetValues<PointOfInterestLootValue>().ToDictionary(value => value, _ => 0);
        if (rules.LooseChance is null)
        {
            return new LooseLoot([], new LooseLootCounts(sockets.Count, 0, inactive, empty, 0, 0, 0, 0, byValue));
        }

        LootSpawnSelection selection = LootSpawnSelection.SelectLoose(servable, rules, activeDataLayers, new Random(seed ^ SelectionSeedSalt), setChances);
        Random contents = new(seed ^ ContentSeedSalt);
        List<LooseItemPlacement> placements = [];
        int grounded = 0;
        for (int index = 0; index < servable.Count; index++)
        {
            if (!selection.Spawns[index]
                || (lists[index] is { } list ? rolls.Roll(list, contents) : rolls.Roll(servable[index].Tag, selection.Values[index], contents)) is not { } stack)
            {
                continue;
            }

            LooseItemSocket socket = servable[index];
            (Vector3D location, Rotator rotation) = (socket.Location, socket.Rotation);
            if (socket.Options.HasFlag(LooseItemOptions.EnablePhysics))
            {
                rotation = rotation with { Pitch = 0, Roll = 0 };
                if (collision?.Raycast(location with { Z = location.Z + GroundProbeLift }, -Vector3.UnitZ, GroundProbeDepth, CollisionChannel.PhysicsBody,
                        CollisionComplexity.Simple) is { } ground)
                {
                    location = ground.Point;
                    grounded++;
                }
            }

            placements.Add(new LooseItemPlacement(socket, stack, location, rotation));
            byValue[selection.Values[index]]++;
        }

        return new LooseLoot(placements, new LooseLootCounts(sockets.Count, placements.Count, inactive, empty, selection.SkippedByRoll, selection.SkippedByLimiter, grounded,
            ruled, byValue));

        // Whether a socket tag can hold an item, worked out once per tag.
        bool CanHold(string tag)
        {
            if (holds.TryGetValue(tag, out bool can))
            {
                return can;
            }

            can = rolls.CanHold(tag);
            holds[tag] = can;
            return can;
        }
    }

    /// <summary>Places the items in the raid, each a new stack with its own identity, at full durability.</summary>
    /// <param name="pickups">The raid's pickups.</param>
    public void Place(WorldPickups pickups)
    {
        ArgumentNullException.ThrowIfNull(pickups);
        foreach (LooseItemPlacement placement in Placements)
        {
            ContainerItem item = new(checked((ushort)placement.Stack.Item.UsableIndex), Guid.NewGuid(), placement.Stack.Amount, placement.Stack.Durability);
            _ = pickups.Place(item, placement.Location, placement.Rotation);
        }
    }
}
