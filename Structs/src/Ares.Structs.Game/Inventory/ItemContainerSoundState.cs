namespace Ares.Structs.Game.Inventory;

/// <summary>
/// What a world item container sounds like to every client near it (<c>EItemContainerSoundState</c>, replicated in the
/// item container's <c>SoundData</c>): its opening while a search is held, then its lid open, then its search.
/// </summary>
public enum ItemContainerSoundState : byte
{
    /// <summary>Silent: nobody opens or searches the container.</summary>
    None = 0,

    /// <summary>A search hold runs.</summary>
    Opening = 1,

    /// <summary>The container is open and nothing is being revealed.</summary>
    Opened = 2,

    /// <summary>The container is open and its hidden items are being revealed.</summary>
    OpenedSearching = 3,
}
