namespace Ares.Raid.World.Replication;

/// <summary>
/// Which actor channel carries what in a raid: fixed channels for each player's own actors and the map's static
/// actors, then one run of channels per family of world actors, sized by the raid's map and player count. Every
/// connection of the raid uses the same layout.
/// </summary>
public sealed class ChannelLayout
{
    /// <summary>Channel of the player's controller.</summary>
    public const int Controller = 2;

    /// <summary>Channel of the player's player state.</summary>
    public const int PlayerState = 3;

    /// <summary>Channel of the level script actor.</summary>
    public const int LevelScript = 4;

    /// <summary>Channel of the game state.</summary>
    public const int GameState = 5;

    /// <summary>Channel of the player's character.</summary>
    public const int Pawn = 6;

    /// <summary>Channel of the world data layers actor.</summary>
    public const int DataLayers = 7;

    /// <summary>Channel of the player's squad.</summary>
    public const int Squad = 48;

    /// <summary>Channels the loot containers share; a container holds one only while it is delivered or changes.</summary>
    public const int LootChannels = 32;

    private const int FirstGlobal = 8;
    private const int FirstDoor = 64;

    // Channels the pickups share; a pickup holds one only while it is delivered or changes.
    private const int PickupChannels = 32;

    // Channels the ARCs share; an ARC holds one while the client has it.
    private const int ArcChannels = 16;

    /// <summary>Lays out the channels of a raid.</summary>
    /// <param name="globals">Number of global actors the map replicates.</param>
    /// <param name="doors">Number of map doors.</param>
    /// <param name="ladders">Number of map ladders.</param>
    /// <param name="elevators">Number of extraction elevators.</param>
    /// <param name="maxPlayers">Most players the raid can hold.</param>
    /// <param name="exploitAreas">Number of locked rooms' exploit prevention areas.</param>
    public ChannelLayout(int globals, int doors, int ladders, int elevators, int maxPlayers, int exploitAreas)
    {
        Globals = new ChannelRange(FirstGlobal, globals);
        Doors = new ChannelRange(FirstDoor, doors);
        Ladders = new ChannelRange(Doors.End, ladders);
        Loot = new ChannelRange(Ladders.End, LootChannels);
        Elevators = new ChannelRange(Loot.End, elevators);
        ObservedPlayers = new ChannelRange(Elevators.End, 2 * maxPlayers);
        Pickups = new ChannelRange(ObservedPlayers.End, PickupChannels);
        Arcs = new ChannelRange(Pickups.End, ArcChannels);
        ExploitAreas = new ChannelRange(Arcs.End, exploitAreas);
    }

    /// <summary>The map's global actors, one channel each.</summary>
    public ChannelRange Globals { get; }

    /// <summary>The map doors, one channel each.</summary>
    public ChannelRange Doors { get; }

    /// <summary>The map ladders, one channel each.</summary>
    public ChannelRange Ladders { get; }

    /// <summary>The channels the loot containers share.</summary>
    public ChannelRange Loot { get; }

    /// <summary>The extraction elevators, one channel each.</summary>
    public ChannelRange Elevators { get; }

    /// <summary>
    /// The other players, two channels for each place in the raid's player list: its player state, then its character.
    /// </summary>
    private ChannelRange ObservedPlayers { get; }

    /// <summary>The channels the pickups share.</summary>
    public ChannelRange Pickups { get; }

    /// <summary>The channels the ARCs share.</summary>
    public ChannelRange Arcs { get; }

    /// <summary>The locked rooms' exploit prevention areas, one channel each.</summary>
    public ChannelRange ExploitAreas { get; }

    /// <summary>The place in the raid's player list of the player an observed-player channel belongs to.</summary>
    /// <param name="channel">Channel index.</param>
    /// <returns>The index, or null when the channel is not an observed-player channel.</returns>
    public int? ObservedPlayer(int channel) => ObservedPlayers.IndexOf(channel) / 2;

    /// <summary>The player-state channel of a player seen by the others.</summary>
    /// <param name="player">Index in the raid's player list.</param>
    /// <returns>The channel; the character's channel follows it.</returns>
    public int ObservedState(int player) => ObservedPlayers.Channel(2 * player);
}

/// <summary>A run of consecutive actor channels.</summary>
/// <param name="First">The first channel.</param>
/// <param name="Count">Number of channels.</param>
public readonly record struct ChannelRange(int First, int Count)
{
    /// <summary>The channel after the run.</summary>
    public int End => First + Count;

    /// <summary>The channel at a place in the run.</summary>
    /// <param name="index">Place in the run, from 0.</param>
    /// <returns>The channel.</returns>
    public int Channel(int index) => First + index;

    /// <summary>The place of a channel in the run.</summary>
    /// <param name="channel">Channel index.</param>
    /// <returns>The place from 0, or null when the channel is outside the run.</returns>
    public int? IndexOf(int channel) => channel >= First && channel < End ? channel - First : null;

    /// <summary>Whether a channel is in the run.</summary>
    /// <param name="channel">Channel index.</param>
    /// <returns>True when the channel is in the run.</returns>
    public bool Contains(int channel) => channel >= First && channel < End;
}
