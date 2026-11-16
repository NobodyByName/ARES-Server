namespace Ares.Structs.Backend.Progression;

/// <summary>Supported gadget item definitions.</summary>
public static class GadgetItems
{
    /// <summary>Asset id of SnapHook.</summary>
    public const long SnapHookAssetId = -1963108876L;

    /// <summary>Item definitions and their allowed child slots.</summary>
    public static IReadOnlyList<ItemDescription> Definitions { get; } =
    [
        new(-1963108876L, "GameItem", 1L, false, false,
            ["Online.Release.Base"], [])
        {
            BaseGameAssetId = -1963108876L,
            QualityLevel = 0L,
            SlotConfig = new NoItemSlots(),
        },
    ];

    /// <summary>Unit weight of a Snap Hook.</summary>
    public const double SnapHookWeight = 5.0;

    /// <summary>Snap Hook index in the usable-item catalogue.</summary>
    public const int SnapHookUsableIndex = 808;

    /// <summary>Time to draw the Snap Hook, in seconds.</summary>
    public const double SnapHookEquipSeconds = 0.35;

    /// <summary>Time to put the Snap Hook away, in seconds.</summary>
    public const double SnapHookUnequipSeconds = 0.2;

    /// <summary>Blueprint class path of the Snap Hook item a character holds.</summary>
    public const string SnapHookItemClass = "/Game/Pioneer/Items/MobilityGadgets/SnapHook/BP_MobilityGadget_SnapHook.BP_MobilityGadget_SnapHook_C";
}
