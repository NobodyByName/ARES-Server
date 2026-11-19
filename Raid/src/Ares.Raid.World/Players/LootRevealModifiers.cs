namespace Ares.Raid.World.Players;

/// <summary>How a player's character searches containers: the speed of its reveals and its chance to reveal two items at once.</summary>
/// <param name="DurationMultiplier">Multiplies the wait before the first reveal and the time each reveal takes.</param>
/// <param name="DoubleRevealChance">Chance from 0 to 1 that a reveal shows two items instead of one.</param>
public sealed record LootRevealModifiers(double DurationMultiplier = 1, double DoubleRevealChance = 0);
