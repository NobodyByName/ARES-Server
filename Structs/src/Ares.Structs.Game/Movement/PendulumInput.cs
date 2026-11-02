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

    /// <summary>Request of a button press. Other request values are not decoded yet.</summary>
    public const int PressRequest = 2;
}
