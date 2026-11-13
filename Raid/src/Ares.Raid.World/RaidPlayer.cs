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
    private bool _connected;
    private PlayerMotion? _motion;
    private PlayerRaidEnd? _end;

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

    /// <summary>The player's channels have started at least once: they connected to the raid.</summary>
    public bool Connected => Volatile.Read(ref _connected);

    /// <summary>
    /// How the player's raid ended and what they carry home, frozen when they departed with an elevator or left the raid;
    /// null before that, for a player who left without ever connecting, and when an item sits where Host cannot keep it.
    /// </summary>
    public PlayerRaidEnd? End => Volatile.Read(ref _end);

    /// <summary>The character's latest simulated movement, which other players see; null before its simulation starts.</summary>
    public PlayerMotion? Motion => Volatile.Read(ref _motion);

    // A player who connected and leaves without having departed has lost the raid: only the Safe Pocket items come home.
    // The result is frozen before the player is marked as left, so whoever sees Left also sees the result.
    internal void Leave()
    {
        if (Connected)
        {
            Freeze(RaidOutcomes.Lost);
        }

        Volatile.Write(ref _left, true);
    }

    // Called by the channel that owns the inventory at the departure tick; the player cannot change it afterwards.
    internal void Depart()
    {
        Freeze(RaidOutcomes.Extracted);
        Volatile.Write(ref _departed, true);
    }

    internal void Connect() => Volatile.Write(ref _connected, true);

    // The first outcome stays: a departed player who later leaves keeps their extraction. An inventory Host cannot
    // keep leaves no result, so Host returns the original loadout unchanged.
    private void Freeze(string outcome)
    {
        if (End is not null)
        {
            return;
        }

        try
        {
            Interlocked.CompareExchange(ref _end, new PlayerRaidEnd(outcome, Inventory.CarriedHome(outcome)), null);
        }
        catch (InvalidOperationException)
        {
        }
    }

    internal void Publish(PlayerMotion motion) => Volatile.Write(ref _motion, motion);
}

/// <summary>A character's movement and held weapons at one of its simulation ticks.</summary>
/// <param name="Tick">The owning client's input frame the movement belongs to.</param>
/// <param name="Motion">Position, velocity, facing, stance and locomotion.</param>
/// <param name="Weapons">Weapon simulation variables (equipped guns by item type, without item links).</param>
/// <param name="Pitch">Aim pitch in radians, positive up.</param>
/// <param name="TimeOffset">Time units from the tick's simulation time to the raid's server time.</param>
public sealed record PlayerMotion(uint Tick, PawnMotion Motion, IReadOnlyList<PendulumVariable> Weapons, double Pitch, long TimeOffset);

/// <summary>How a player's raid ended and what they carry home.</summary>
/// <param name="Outcome">See <see cref="RaidOutcomes"/>.</param>
/// <param name="Items">The carried items: the whole loadout after an extraction, only the Safe Pocket items after a lost raid.</param>
public sealed record PlayerRaidEnd(string Outcome, IReadOnlyList<CarriedItem> Items);
