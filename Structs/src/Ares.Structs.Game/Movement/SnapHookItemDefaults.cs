namespace Ares.Structs.Game.Movement;

/// <summary>The values the snap hook item class (<c>BP_MobilityGadget_SnapHook</c>) sets over its script class.</summary>
/// <param name="DataAsset">Item data asset of the snap hook (package path).</param>
/// <param name="Range">Length of the throw's aim trace and of the hook's flight (<c>SnaphookRange</c>).</param>
/// <param name="ActivationCost">Gadget power a throw costs (<c>ActivationCost</c>); a hook that attaches to nothing gives it back.</param>
public sealed record SnapHookItemDefaults(string DataAsset, double Range, double ActivationCost);
