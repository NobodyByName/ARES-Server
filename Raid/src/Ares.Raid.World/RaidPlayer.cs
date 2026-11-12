using Ares.Common.Game.Inventory;
using Ares.Raid.World.Inventory;
using Ares.Raid.World.Movement;
using Ares.Structs.Game.Movement;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.World;

/// <summary>A reserved player of a raid: their frozen loadout and party profiles, inventory, weapons and spawn.</summary>
public sealed class RaidPlayer
{
    private bool _left;
    private bool _departed;
    private PlayerMotion? _motion;

    internal RaidPlayer(long id, PlayerLoadout loadout, IReadOnlyList<CompactPlayerProfile> profiles, SpawnPlacement spawn)
    {
        Id = id;
        Loadout = loadout;
        Profiles = profiles;
        Spawn = spawn;
        Inventory = new RaidInventory(loadout);
        Weapons = new RaidWeapons(Inventory);
    }

    /// <summary>The player's user id.</summary>
    public long Id { get; }

    /// <summary>The frozen loadout.</summary>
    public PlayerLoadout Loadout { get; }

    /// <summary>The frozen public squad profiles of the player's party (empty when omitted).</summary>
    public IReadOnlyList<CompactPlayerProfile> Profiles { get; }

    /// <summary>Where the player spawns.</summary>
    public SpawnPlacement Spawn { get; }

    /// <summary>The player's inventory containers.</summary>
    public RaidInventory Inventory { get; }

    /// <summary>The player's weapon simulation.</summary>
    public RaidWeapons Weapons { get; }

    /// <summary>The player has left the raid (surrendered, or their reservation ended); they cannot rejoin it.</summary>
    public bool Left => Volatile.Read(ref _left);

    /// <summary>
    /// The player left with an extraction elevator: their character is gone from the raid while their client finishes
    /// the round; they leave the raid (<see cref="Left"/>) when their connection closes.
    /// </summary>
    public bool Departed => Volatile.Read(ref _departed);

    /// <summary>The character's latest simulated movement, which other players see; null before its simulation starts.</summary>
    public PlayerMotion? Motion => Volatile.Read(ref _motion);

    internal void Leave() => Volatile.Write(ref _left, true);

    internal void Depart() => Volatile.Write(ref _departed, true);

    internal void Publish(PlayerMotion motion) => Volatile.Write(ref _motion, motion);
}

/// <summary>A character's movement and held weapons at one of its simulation ticks.</summary>
/// <param name="Tick">The owning client's input frame the movement belongs to.</param>
/// <param name="Motion">Position, velocity, facing, stance and locomotion.</param>
/// <param name="Weapons">Weapon simulation variables (equipped guns by item type, without item links).</param>
/// <param name="Pitch">Aim pitch in radians, positive up.</param>
/// <param name="TimeOffset">Time units from the tick's simulation time to the raid's server time.</param>
public sealed record PlayerMotion(uint Tick, PawnMotion Motion, IReadOnlyList<PendulumVariable> Weapons, double Pitch, long TimeOffset);
