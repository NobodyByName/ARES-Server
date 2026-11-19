namespace Ares.GameDataExtract.Loot;

/// <summary>
/// The wait after a first search's hold before a loot container shows its contents, as the client sets it when it
/// builds the container: when <see cref="Ares.Structs.Game.Loot.LootContainerBlueprintMembers.UseLidAnimationForOpenDelay"/>
/// is set and the lid animation has a track, half the end time of the first track's curve, at least
/// <see cref="Ares.Structs.Game.Loot.LootContainerBlueprintMembers.MinOpenDelay"/>; otherwise the search interaction's
/// own <see cref="Ares.Structs.Game.Loot.LootContainerBlueprintMembers.WaitDelayOnFirstOpen"/>.
/// </summary>
internal static class LootOpenDelay
{
    /// <summary>The open delay in seconds.</summary>
    /// <param name="useLidAnimation">Whether the lid animation sets the delay.</param>
    /// <param name="minSeconds">The shortest delay the lid animation sets.</param>
    /// <param name="lidEnd">End time of the first lid track's curve, or <see langword="null"/> when the lid animation has no track.</param>
    /// <param name="waitSeconds">The search interaction's own delay.</param>
    public static double Seconds(bool useLidAnimation, double minSeconds, float? lidEnd, double waitSeconds)
    {
        return useLidAnimation && lidEnd is { } end ? Math.Max(minSeconds, LootNumbers.Single(end / 2.0f)) : waitSeconds;
    }
}
