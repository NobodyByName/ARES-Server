namespace Ares.Structs.Game.World;

/// <summary>A regular door placed in a World Partition cell's persistent level.</summary>
/// <param name="Cell">Generated cell package.</param>
/// <param name="Actor">Door actor name in the cell.</param>
public readonly record struct DoorPlacement(string Cell, string Actor);

/// <summary>Replicated open state of a door (<c>DoorState</c>, bits 0..2).</summary>
public enum DoorOpenState : byte
{
    /// <summary>Closed, animation finished.</summary>
    Closed = 0,

    /// <summary>Open, animation finished.</summary>
    Open = 1,

    /// <summary>Plays the opening animation.</summary>
    Opening = 2,

    /// <summary>Plays the closing animation.</summary>
    Closing = 3,
}
