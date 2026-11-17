namespace Ares.Common.Game.Inventory;

/// <summary>
/// The catalogue items ARES does not serve yet. The game data lists every usable item the install cooks; the servers
/// hold, carry and roll as loot only the others (see <see cref="LoadoutDefinitions.PlainItems"/>).
/// </summary>
public static class ItemSupport
{
    /// <summary>Items ARES cannot serve yet, by technical name, with what they need.</summary>
    public static IReadOnlyDictionary<string, string> Unsupported { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["DA_Item_Zipline"] = "deployable: placing a zipline needs deployable gameplay the raid does not have",
    };

    /// <summary>Whether ARES serves the item named <paramref name="technicalName"/>.</summary>
    public static bool IsSupported(string technicalName) => !Unsupported.ContainsKey(technicalName);
}
