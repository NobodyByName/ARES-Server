namespace Ares.Structs.Game.Movement;

/// <summary>The snap hook item class (<c>BP_MobilityGadget_SnapHook</c>): the values it sets over its script class.</summary>
public static class SnapHookItem
{
    /// <summary>Item data asset of the snap hook.</summary>
    public const string DataAsset = "/Game/Pioneer/Items/MobilityGadgets/SnapHook/DA_Item_SnapHook";

    /// <summary>Length of the throw's aim trace and of the hook's flight (<c>SnaphookRange</c>).</summary>
    public const double Range = 1800.0;

    /// <summary>Gadget power a throw costs (<c>ActivationCost</c>); a hook that attaches to nothing gives it back.</summary>
    public const double ActivationCost = 60.0;
}
