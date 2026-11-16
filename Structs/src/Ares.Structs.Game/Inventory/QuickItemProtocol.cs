namespace Ares.Structs.Game.Inventory;

/// <summary>Quick-use items in hand (the Snap Hook) and gadget power in the inventory state.</summary>
public static class QuickItemProtocol
{
    /// <summary>Descriptor of the node a quick-use item in hand (or being drawn or put away) has in the character state; its slot is the item's handle in the inventory state.</summary>
    public const int ItemNodeDescriptor = 80;

    /// <summary>Pages the item node takes in the character state.</summary>
    public const int ItemNodePages = 2;

    /// <summary>Item node field naming its node data.</summary>
    public const int ItemNodeDataField = 4;

    /// <summary>Node data of a Snap Hook item node (biased like the weapon node data).</summary>
    public const int ItemNodeDataId = 26;

    /// <summary>Item node field holding the usable-item index.</summary>
    public const int ItemNodeAssetField = 11;

    /// <summary>Item node field holding the durability over 0..DurabilityMaximum.</summary>
    public const int ItemNodeDurabilityField = 12;

    /// <summary>Item node field linking the held item object.</summary>
    public const int ItemNodeItemInstanceField = 17;

    /// <summary>Inventory-state field holding the character's gadget power.</summary>
    public const int GadgetPowerField = 3;

    /// <summary>Value of the gadget power field at full power; the field holds power (0..100) scaled to this and rounded.</summary>
    public const int GadgetPowerMaximum = 255;

    /// <summary>Time after a throw from which a hook that comes back without holding on stops gadget power recharging until it is cleared.</summary>
    public const double PowerRechargePauseSeconds = 0.35;

    /// <summary>Item node field holding the X of the hook's target (Y and Z follow), quantized like a position with 27 bits.</summary>
    public const int HookLocationXField = 0;

    /// <summary>Item node field holding the simulation time of the latest throw (biased like the other times).</summary>
    public const int LastThrowTimeField = 8;

    /// <summary>Item node field holding the simulation time of the throw while the hook is out (biased; the never time otherwise).</summary>
    public const int ThrowTimeField = 9;

    /// <summary>Item node field holding the hook's state bits.</summary>
    public const int HookStateField = 10;

    /// <summary>Item node field naming what the item was last used for (ItemUseThrown, ItemUseAiming; 0 before any use).</summary>
    public const int ItemUseField = 13;

    /// <summary>Item node field set on a new node and cleared once the primary use is released after its first throw.</summary>
    public const int UnknownFreshField = 14;

    /// <summary>Item node field holding the surface type the hook hit while it is out.</summary>
    public const int SurfaceField = 15;

    /// <summary>Hook state bit set while the hook flies out to its target or back.</summary>
    public const int HookFlyingBit = 0;

    /// <summary>Hook state bit set while the character aims the item.</summary>
    public const int AimingBit = 1;

    /// <summary>Hook state bit set while the hook is out.</summary>
    public const int HookOutBit = 5;

    /// <summary>Hook state bit set once the hook holds on.</summary>
    public const int HookAttachedBit = 6;

    /// <summary>Hook state bit set while the throw has a target it hit.</summary>
    public const int HookTargetBit = 7;

    /// <summary>Hook state bit set after a hook that held on was called back, until the next throw.</summary>
    public const int HookCancelledBit = 8;

    /// <summary>Hook state bit set while the hook that holds on pulls its owner (a move target exists).</summary>
    public const int HookPullsBit = 9;

    /// <summary>Item use value after a throw.</summary>
    public const int ItemUseThrown = 1;

    /// <summary>Item use value while aiming.</summary>
    public const int ItemUseAiming = 2;

    /// <summary>Descriptor of the move target a holding hook gives its owner, kept in the character state after the pull.</summary>
    public const int MoveTargetDescriptor = 81;

    /// <summary>Move target field holding the simulation time it pulls from (biased; the never time once it is removed).</summary>
    public const int MoveTargetCreatedField = 6;

    /// <summary>Move target field holding the simulation time it was removed (biased; the never time while it pulls).</summary>
    public const int MoveTargetRemovedField = 7;

    /// <summary>Descriptor of the movement node that pulls the character to its move target.</summary>
    public const int PullNodeDescriptor = 79;

    /// <summary>Pages the move target takes in the character state.</summary>
    public const int MoveTargetPages = 1;

    /// <summary>Move target field holding the X of the point it pulls to (Y and Z follow), quantized like a position with 21 bits.</summary>
    public const int MoveTargetPositionXField = 2;

    /// <summary>Pages the pull node takes in the character state.</summary>
    public const int PullNodePages = 1;

    /// <summary>Pull node field holding the rotation towards the move target when the pull started.</summary>
    public const int PullNodeDirectionField = 0;

    /// <summary>Pull node field naming its node data.</summary>
    public const int PullNodeDataField = 1;

    /// <summary>Node data of the pull node (biased like the times).</summary>
    public const int PullNodeDataId = 18;

    /// <summary>Pull node field holding the simulation time the pull started at (biased; the end of its first tick).</summary>
    public const int PullNodeStartTimeField = 2;

    /// <summary>Pull node field holding the slot of the move target it pulls to.</summary>
    public const int PullNodeMoveTargetField = 4;

}
