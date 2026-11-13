namespace Ares.Structs.Game.Loot;

/// <summary>Rules of loot container searches that hold for every container class.</summary>
public static class LootInteractions
{
    /// <summary>Distance the server adds to a prompt distance before an interaction under way ends.</summary>
    public const double ServerPromptLeeway = 75;

    /// <summary>Hold start time of a search interaction nobody holds.</summary>
    public const double NoHoldStartedTime = -1;

    /// <summary>Hold time of a container that has been opened: it opens again as soon as the character is lined up.</summary>
    public const float OpenedHoldSeconds = 0;
}
