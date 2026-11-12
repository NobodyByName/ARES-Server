namespace Ares.Structs.Game.Movement;

/// <summary>The variables a client sends in its Pendulum input frames.</summary>
public static class PendulumInput
{
    /// <summary>Direct-replication stream kind carrying the input history.</summary>
    public const int DirectStreamKind = 1;

    /// <summary>Descriptor id of each input frame slot, by slot.</summary>
    public static IReadOnlyList<int> SlotDescriptors { get; } = [75, 66, 98, 101, 39, 97, 99, 85];

    /// <summary>Slot holding character button claims and inventory commands.</summary>
    public const int CharacterClaimsSlot = 5;

    /// <summary>Packed character button flags in the claims slot.</summary>
    public const int CharacterClaimsFlagsField = 4;

    /// <summary>Bit requesting slow walking.</summary>
    public const int WalkBit = 0;

    /// <summary>Bit indicating that the flashlight button is down.</summary>
    public const int FlashlightDownBit = 9;

    /// <summary>Bit indicating that the flashlight button was down on the previous input.</summary>
    public const int FlashlightPreviousDownBit = 10;

    /// <summary>Slot holding movement axes, buttons and control rotation.</summary>
    public const int MovementSlot = 4;

    /// <summary>Forward movement axis, stored with a bias of 128.</summary>
    public const int ForwardField = 6;

    /// <summary>Right movement axis, stored with a bias of 128.</summary>
    public const int RightField = 7;

    /// <summary>Packed movement buttons.</summary>
    public const int MovementFlagsField = 8;

    /// <summary>Bias added to a signed movement axis.</summary>
    public const int MovementAxisBias = 128;

    /// <summary>Magnitude of a full signed movement axis.</summary>
    public const int MovementAxisMaximum = 126;

    /// <summary>Slot of the interaction claim: what the player asks to interact with.</summary>
    public const int InteractionSlot = 6;

    /// <summary>Field of the interaction claim holding the request (0 for none).</summary>
    public const int InteractionRequestField = 1;

    /// <summary>Field of the interaction claim holding the target object id (0 for none).</summary>
    public const int InteractionTargetField = 2;

    /// <summary>Field of the interaction claim holding the focused object, whose prompt shows (0 for none).</summary>
    public const int InteractionFocusField = 3;

    /// <summary>Slot of the input variable holding the character position the client predicted.</summary>
    public const int PositionSlot = 3;

    /// <summary>
    /// Field of the position slot holding the quantized world X position (Y and Z follow):
    /// World endpoints map to 0 and Maximum - 1; the default integer is not the world origin.
    /// </summary>
    public const int PositionXField = 7;

    /// <summary>Field of the position slot holding the quantized world Y position.</summary>
    public const int PositionYField = 8;

    /// <summary>Field of the position slot holding the quantized world Z position.</summary>
    public const int PositionZField = 9;

    /// <summary>Slot of the input variable holding the control (camera) rotation.</summary>
    public const int ControlRotationSlot = 4;

    /// <summary>Field of the control-rotation slot holding the rotation; the character faces its heading.</summary>
    public const int ControlRotationField = 0;

    /// <summary>Flag set while the player's last interaction ended successfully. It asks for nothing.</summary>
    public const int LastInteractionSuccessfulFlag = 1;

    /// <summary>Request of a button press: the player asks to start the focused interaction.</summary>
    public const int PressRequest = 2;

    /// <summary>Request sent when the player's looting menu closed: the open container closes.</summary>
    public const int LootingMenuClosedRequest = 8;

    /// <summary>Flag added to a press request when the interaction first takes out the raider tool.</summary>
    public const int EquipToolRequestFlag = 32;
}
