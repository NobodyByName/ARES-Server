namespace Ares.Structs.Backend.Progression;

/// <summary>Supported shield item definitions.</summary>
public static class ShieldItems
{
    /// <summary>Asset id of LightShield.</summary>
    public const long LightShieldAssetId = 1505999062L;

    /// <summary>Item definitions and their allowed child slots.</summary>
    public static IReadOnlyList<ItemDescription> Definitions { get; } =
    [
        new(1505999062L, "GameItem", 1L, false, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = 9197506L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
    ];

    /// <summary>Unit weight of a Light Shield.</summary>
    public const double LightShieldWeight = 5.0;

    /// <summary>Light Shield index in the usable-item catalogue.</summary>
    public const int LightShieldUsableIndex = 3;
}
