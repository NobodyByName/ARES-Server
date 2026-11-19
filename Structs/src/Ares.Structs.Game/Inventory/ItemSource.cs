namespace Ares.Structs.Game.Inventory;

/// <summary>Where an item in a container came from (<c>EItemSources</c>).</summary>
public enum ItemSource : byte
{
    Unknown = 0,
    Pickup = 1,
    Assigned = 2,
    Debug = 3,
    SplitStack = 4,
    Crafted = 5,
    StartingEquipment = 6,
    CraftedField = 7,
    CraftedBench = 8,

    /// <summary>Found in a world loot container.</summary>
    PickupLootBox = 9,

    /// <summary>Found in the loot of a defeated enemy.</summary>
    PickupEnemy = 10,

    PickupEnvironment = 11,
    PickupOtherPlayer = 12,
    PickupSelf = 13,
    AssignedEnemy = 14,
    AssignedEnvironment = 15,
    BroughtToSession = 16,
    Unload = 17,
    Salvaged = 18,
    TemporaryStackDeposit = 19,
    PickupEnvironmentBuried = 20,
    PickupEnvironmentBreachGift = 21,

    /// <summary>Picked up from an item placed in the world when the raid started.</summary>
    PickupEnvironmentInitial = 22,
}
