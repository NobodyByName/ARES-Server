using Ares.Common.Game.Inventory;
using Ares.Raid.World.Inventory;
using Ares.Structs.Net.Replication;

namespace Ares.Raid.World;

/// <summary>A reserved player of a raid: their frozen loadout and party profiles, inventory, weapons and spawn.</summary>
public sealed class RaidPlayer
{
    private bool _left;

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

    internal void Leave() => Volatile.Write(ref _left, true);
}

