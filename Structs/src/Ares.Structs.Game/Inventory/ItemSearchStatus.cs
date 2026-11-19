namespace Ares.Structs.Game.Inventory;

/// <summary>Search state of a container slot: hidden items are revealed one at a time while the player searches.</summary>
/// <remarks>The client blocks moving an item from or to a slot that is not <see cref="Shown"/>.</remarks>
public enum ItemSearchStatus
{
    /// <summary>The slot shows its item.</summary>
    Shown = 0,

    /// <summary>The slot is hidden and is the one being revealed (the client shows the reveal progress).</summary>
    HiddenSearching = 1,

    /// <summary>The slot is hidden and waits for its turn.</summary>
    Hidden = 2,
}
