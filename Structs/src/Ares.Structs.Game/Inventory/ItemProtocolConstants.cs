namespace Ares.Structs.Game.Inventory;

/// <summary>
/// Character-state fields of the hand items that the installed game does not hold: positions in the Pendulum field map
/// of the inventory state, read from a running client's memory during development. Written by hand; the authored hand
/// item values (classes, usable indices, equip times) are in the game data's usable-item table.
/// </summary>
public static class HandItemFields
{
    /// <summary>Unarmed item node field linking the held item object.</summary>
    public const int UnarmedItemInstanceField = 11;

    /// <summary>Handle of the Raider Tool in the inventory state.</summary>
    public const int RaiderToolHandle = 5;

    /// <summary>Raider Tool item node field linking the held item object.</summary>
    public const int RaiderToolItemInstanceField = 18;
}

/// <summary>
/// Ferro weapon-state values that the installed game does not hold, written by hand; its authored values (magazine,
/// fire rate, reload, zoom, mod slots, equip times) are in the game data's firearm and usable-item tables.
/// </summary>
public static class FerroProtocol
{
    /// <summary>Firing mode index sent in the weapon state (the Ferro has one firing mode).</summary>
    public const int FiringMode = 0;
}

/// <summary>
/// Shield facts that the installed game's item assets do not hold, written by hand; the authored shield values are in
/// the game data's shield table.
/// </summary>
public static class ShieldProtocol
{
    /// <summary>The character component that replicates shield health (from the replication schema).</summary>
    public const string ComponentName = "HealthComponent";
}
