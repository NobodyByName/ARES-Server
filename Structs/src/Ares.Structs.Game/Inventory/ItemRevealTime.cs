namespace Ares.Structs.Game.Inventory;

/// <summary>
/// Time one hidden container slot takes to reveal, as the player container state sends it to the client (which uses it
/// as the length of the reveal progress). Only the default of one second has a known encoding.
/// </summary>
public sealed record ItemRevealTime
{
    private ItemRevealTime(double seconds) => Seconds = seconds;

    /// <summary>The default reveal time, one second.</summary>
    public static ItemRevealTime Default { get; } = new(1.0);

    /// <summary>Reveal time in seconds.</summary>
    public double Seconds { get; }

    /// <summary>Gets the reveal time for a duration.</summary>
    /// <param name="seconds">Duration in seconds.</param>
    /// <returns>The reveal time.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The duration has no known encoding (anything but one second).</exception>
    public static ItemRevealTime FromSeconds(double seconds) => seconds.Equals(Default.Seconds)
        ? Default
        : throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "Only the default reveal time can be encoded");
}
