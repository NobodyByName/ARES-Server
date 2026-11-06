namespace Ares.Structs.Game.Movement;

/// <summary>The variables a client sends in its Pendulum input frames.</summary>
public static class PendulumInput
{
    /// <summary>Direct-replication stream kind carrying the input history.</summary>
    public const int DirectStreamKind = 1;

    /// <summary>Descriptor id of each input frame slot, by slot.</summary>
    public static IReadOnlyList<int> SlotDescriptors { get; } = [75, 66, 98, 101, 39, 97, 99, 85];

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

    /// <summary>Request of a button press. Other request values are not decoded yet.</summary>
    public const int PressRequest = 2;
}
