namespace Ares.Structs.Game.Movement;

/// <summary>
/// The server pawn's blackboard (sent to the owning client). The client runs its character simulation
/// from these variables and rolls it back to the graph-state tick, so every frame carries its own tick;
/// the first frame also carries the spawn position. The other values are the initial state.
/// </summary>
public static class PawnBlackboard
{
    /// <summary>Slot of the graph-state variable.</summary>
    public const int GraphStateSlot = 0;

    /// <summary>Slot of the movement-state variable.</summary>
    public const int MovementStateSlot = 7;

    /// <summary>Field of the graph-state variable holding the simulation tick, in client input frames.</summary>
    public const int TickField = 2;

    /// <summary>Field of the graph-state variable holding the simulation time (tick times <see cref="TimePerTick"/>).</summary>
    public const int TimeField = 3;

    /// <summary>Field of the movement-state variable holding the quantized world X position.</summary>
    public const int PositionXField = 28;

    /// <summary>Field of the movement-state variable holding the quantized world Y position.</summary>
    public const int PositionYField = 29;

    /// <summary>Field of the movement-state variable holding the quantized world Z position.</summary>
    public const int PositionZField = 30;

    /// <summary>Field of the movement-state variable holding quantized X velocity divided by three (Y and Z follow).</summary>
    public const int VelocityXField = 1;

    /// <summary>Field of the movement-state variable holding quantized Y velocity divided by three.</summary>
    public const int VelocityYField = 2;

    /// <summary>Field of the movement-state variable holding quantized Z velocity divided by three.</summary>
    public const int VelocityZField = 3;

    /// <summary>
    /// World units per second mapped onto each velocity field's range:
    /// quantized = floor((velocity + VelocityRange) * (maximum - 1) / (2 * VelocityRange)).
    /// </summary>
    public const double VelocityRange = 6000.0;

    /// <summary>Duration of one simulation tick in seconds.</summary>
    public const double TickSeconds = 1.0 / 30;

    /// <summary>Field of the movement-state variable holding the character's facing (a rotation about the vertical axis).</summary>
    public const int FacingField = 31;

    /// <summary>Slot of the interactor-state variable (focused and current interaction, interaction phase).</summary>
    public const int InteractorSlot = 9;

    /// <summary>Field of the interactor-state variable holding its flags.</summary>
    public const int InteractorFlagsField = 9;

    /// <summary>
    /// Interactor flag: the character may use its focused interaction. Only the server's simulation sets it;
    /// the client's interaction does not start (and plays no animation) without it.
    /// </summary>
    public const ulong CanAccessFocusedInteractionFlag = 1;

    /// <summary>Field of the interactor-state variable holding the focused interaction.</summary>
    public const int FocusedInteractionField = 12;

    /// <summary>Ground normal represented as a quantized rotation.</summary>
    public const int GroundNormalField = 0;

    /// <summary>Ground X velocity divided by three; Y and Z follow.</summary>
    public const int GroundVelocityDiv3XField = 7;

    /// <summary>Packed movement-state flags.</summary>
    public const int MovementFlagsField = 13;

    /// <summary>Broad movement mode.</summary>
    public const int RoughMovementModeField = 15;

    /// <summary>Character posture and traversal state.</summary>
    public const int CharacterStanceField = 17;

    /// <summary>Character locomotion intensity.</summary>
    public const int LocoModeField = 18;

    /// <summary>Physical surface type.</summary>
    public const int SurfaceTypeField = 19;

    /// <summary>Slot of persistent character actions.</summary>
    public const int ActionStateSlot = 2;

    /// <summary>Action posture, including slide, air and ladder states.</summary>
    public const int ActionStanceField = 44;

    /// <summary>Action locomotion intensity.</summary>
    public const int ActionLocoModeField = 45;

    /// <summary>Graph-state flags containing the flashlight state.</summary>
    public const int FlashlightFlagsField = 7;

    /// <summary>Graph-state flag indicating an active flashlight.</summary>
    public const ulong FlashlightFlag = 1UL << 55;

    /// <summary>Simulation time units per tick.</summary>
    public const int TimePerTick = 20;

    /// <summary>
    /// World units mapped onto the 24-bit position range around the origin:
    /// quantized = round((world + PositionRange) * (2^24 - 1) / (2 * PositionRange)).
    /// </summary>
    public const double PositionRange = 800000.0;

    /// <summary>Initial variables, by slot. Per-frame fields are left at their defaults.</summary>
    public static IReadOnlyList<PendulumVariable> InitialState { get; } =
    [
        new(0, 0, 1, 75, [
            new PendulumIntegerValue(0, 2147483648UL),
            new PendulumIntegerValue(1, 2147483649UL),
            new PendulumIntegerValue(4, 2147773508UL),
            new PendulumIntegerValue(5, 6845471433603153919UL),
            new PendulumIntegerValue(6, 263882790666367UL),
            new PendulumIntegerValue(7, 536870912UL),
            new PendulumIntegerValue(9, 1UL),
        ]),
        new(1, 1, 1, 66, [
            new PendulumIntegerValue(0, 2147483649UL),
            new PendulumArrayValue(1,
            [
                new PendulumArrayElement(0, [
                    new PendulumIntegerValue(0, 2UL),
                ]),
                new PendulumArrayElement(1, [
                    new PendulumIntegerValue(0, 3UL),
                ]),
                new PendulumArrayElement(2, [
                    new PendulumIntegerValue(0, 4UL),
                ]),
                new PendulumArrayElement(3, [
                    new PendulumIntegerValue(0, 7UL),
                ]),
                new PendulumArrayElement(4, [
                    new PendulumIntegerValue(0, 9UL),
                ]),
            ]),
        ]),
        new(2, 2, 2, 71, [
            new PendulumQuaternionValue(0, new PendulumQuaternion(8U, 9U, 61U, 0U, 1, 0, 1, 2)),
            new PendulumIntegerValue(21, 2147483651UL),
            new PendulumIntegerValue(22, 19261UL),
            new PendulumIntegerValue(45, 3UL),
            new PendulumIntegerValue(46, 6UL),
        ]),
        new(3, 4, 1, 77, [
            new PendulumIntegerValue(0, 2147483671UL),
        ]),
        new(4, 5, 3, 61, [
            new PendulumIntegerValue(4, 4339UL),
            new PendulumIntegerValue(7, 8958UL),
            new PendulumIntegerValue(11, 3277UL),
            new PendulumIntegerValue(12, 8738UL),
            new PendulumIntegerValue(13, 4245UL),
            new PendulumIntegerValue(17, 6554UL),
            new PendulumIntegerValue(18, 6554UL),
            new PendulumIntegerValue(19, 6554UL),
            new PendulumIntegerValue(20, 2621UL),
            new PendulumIntegerValue(21, 1966UL),
            new PendulumIntegerValue(22, 2505UL),
            new PendulumIntegerValue(23, 3277UL),
            new PendulumIntegerValue(32, 2147483672UL),
            new PendulumIntegerValue(33, 2147483708UL),
            new PendulumIntegerValue(37, 3402UL),
            new PendulumIntegerValue(39, 6UL),
            new PendulumIntegerValue(44, 1536UL),
            new PendulumIntegerValue(45, 3UL),
            new PendulumIntegerValue(46, 11UL),
            new PendulumIntegerValue(48, 5UL),
            new PendulumIntegerValue(49, 6UL),
            new PendulumIntegerValue(50, 6UL),
            new PendulumIntegerValue(52, 11UL),
            new PendulumIntegerValue(54, 2UL),
            new PendulumIntegerValue(55, 11UL),
            new PendulumIntegerValue(58, 7UL),
            new PendulumIntegerValue(59, 4UL),
            new PendulumIntegerValue(60, 5UL),
            new PendulumIntegerValue(61, 1UL),
            new PendulumIntegerValue(62, 1UL),
            new PendulumIntegerValue(63, 16UL),
            new PendulumIntegerValue(64, 1UL),
            new PendulumArrayValue(66,
            [
                new PendulumArrayElement(0, [
                    new PendulumIntegerValue(0, 2147483684UL),
                ]),
            ]),
        ]),
        new(5, 8, 1, 67, [
            new PendulumIntegerValue(1, 2147483675UL),
            new PendulumIntegerValue(3, 2147483649UL),
            new PendulumIntegerValue(13, 45UL),
            new PendulumIntegerValue(14, 65535UL),
        ]),
        new(6, 9, 1, 89, [
            new PendulumIntegerValue(1, 2147483674UL),
            new PendulumIntegerValue(3, 2147483649UL),
            new PendulumIntegerValue(7, 852UL),
            new PendulumIntegerValue(8, 65535UL),
        ]),
        new(7, 10, 2, 68, [
            new PendulumQuaternionValue(0, new PendulumQuaternion(10U, 3U, 26U, 0U, 15, 1, 0, 2)),
            new PendulumIntegerValue(1, 1947UL),
            new PendulumIntegerValue(2, 2190UL),
            new PendulumIntegerValue(3, 2078UL),
            new PendulumIntegerValue(4, 1915UL),
            new PendulumIntegerValue(5, 2096UL),
            new PendulumIntegerValue(6, 2078UL),
            new PendulumIntegerValue(11, 179UL),
            new PendulumIntegerValue(12, 2147483650UL),
            new PendulumIntegerValue(13, 76UL),
            new PendulumIntegerValue(14, 8UL),
            new PendulumIntegerValue(18, 3UL),
            new PendulumIntegerValue(19, 26UL),
            new PendulumQuaternionValue(31, new PendulumQuaternion(0U, 0U, 3991U, 0U, 15, 0, 1, 2)),
        ]),
        new(8, 12, 1, 69, [
            new PendulumIntegerValue(0, 8388616UL),
            new PendulumIntegerValue(1, 2046UL),
            new PendulumIntegerValue(2, 1331UL),
            new PendulumIntegerValue(3, 8389446UL),
            new PendulumIntegerValue(4, 2147483652UL),
            new PendulumIntegerValue(5, 2147483648UL),
            new PendulumIntegerValue(8, 2147498328UL),
            new PendulumIntegerValue(9, 2147500408UL),
            new PendulumIntegerValue(13, 6UL),
            new PendulumIntegerValue(14, 6UL),
            new PendulumIntegerValue(15, 3UL),
        ]),
        new(9, 13, 1, 60, [
            new PendulumIntegerValue(6, 2147483670UL),
        ]),
        new(10, 14, 19, 35, [
            new PendulumIntegerValue(1, 7UL),
            new PendulumIntegerValue(2, 30UL),
            new PendulumIntegerValue(4, 8UL),
            new PendulumArrayValue(6,
            [
                new PendulumArrayElement(0, [
                    new PendulumIntegerValue(0, 2147484867UL),
                    new PendulumIntegerValue(1, 5UL),
                    new PendulumIntegerValue(2, 13UL),
                    new PendulumIntegerValue(3, 0UL),
                    new PendulumIntegerValue(4, 5UL),
                ]),
                new PendulumArrayElement(1, [
                    new PendulumIntegerValue(0, 2147484868UL),
                    new PendulumIntegerValue(1, 5UL),
                    new PendulumIntegerValue(2, 6UL),
                    new PendulumIntegerValue(3, 0UL),
                    new PendulumIntegerValue(4, 16UL),
                ]),
                new PendulumArrayElement(2, [
                    new PendulumIntegerValue(0, 2147484869UL),
                    new PendulumIntegerValue(1, 5UL),
                    new PendulumIntegerValue(2, 2UL),
                    new PendulumIntegerValue(3, 0UL),
                    new PendulumIntegerValue(4, 4UL),
                ]),
                new PendulumArrayElement(3, [
                    new PendulumIntegerValue(0, 2147484870UL),
                    new PendulumIntegerValue(1, 5UL),
                    new PendulumIntegerValue(2, 2UL),
                    new PendulumIntegerValue(3, 0UL),
                    new PendulumIntegerValue(4, 1UL),
                ]),
                new PendulumArrayElement(4, [
                    new PendulumIntegerValue(0, 2147484871UL),
                    new PendulumIntegerValue(1, 5UL),
                    new PendulumIntegerValue(2, 2UL),
                    new PendulumIntegerValue(3, 0UL),
                ]),
                new PendulumArrayElement(5, [
                    new PendulumIntegerValue(0, 2147484872UL),
                    new PendulumIntegerValue(1, 1UL),
                    new PendulumIntegerValue(2, 2UL),
                    new PendulumIntegerValue(3, 0UL),
                    new PendulumIntegerValue(4, 4UL),
                ]),
                new PendulumArrayElement(6, [
                    new PendulumIntegerValue(0, 2147484873UL),
                    new PendulumIntegerValue(1, 1UL),
                    new PendulumIntegerValue(2, 2UL),
                    new PendulumIntegerValue(3, 0UL),
                ]),
            ]),
            new PendulumArrayValue(7,
            [
                new PendulumArrayElement(0, [
                    new PendulumIntegerValue(0, 1UL),
                    new PendulumIntegerValue(1, 0UL),
                    new PendulumIntegerValue(2, 16384UL),
                    new PendulumIntegerValue(4, 13568UL),
                    new PendulumIntegerValue(5, 128UL),
                    new PendulumIntegerValue(6, 243UL),
                    new PendulumIntegerValue(8, 21UL),
                    new PendulumIntegerValue(9, 20UL),
                    new PendulumIntegerValue(10, 16UL),
                    new PendulumIntegerValue(11, 5UL),
                ]),
                new PendulumArrayElement(2, [
                    new PendulumIntegerValue(0, 1UL),
                    new PendulumIntegerValue(1, 0UL),
                    new PendulumIntegerValue(2, 16384UL),
                    new PendulumIntegerValue(4, 13570UL),
                    new PendulumIntegerValue(5, 128UL),
                    new PendulumIntegerValue(8, 3UL),
                    new PendulumIntegerValue(9, 0UL),
                    new PendulumIntegerValue(10, 16UL),
                    new PendulumIntegerValue(11, 6UL),
                ]),
                new PendulumArrayElement(3, [
                    new PendulumIntegerValue(0, 1UL),
                    new PendulumIntegerValue(1, 0UL),
                    new PendulumIntegerValue(2, 16384UL),
                    new PendulumIntegerValue(4, 13571UL),
                    new PendulumIntegerValue(5, 128UL),
                    new PendulumIntegerValue(8, 45UL),
                ]),
                new PendulumArrayElement(4, [
                    new PendulumIntegerValue(0, 1UL),
                    new PendulumIntegerValue(1, 0UL),
                    new PendulumIntegerValue(2, 16384UL),
                    new PendulumIntegerValue(4, 13572UL),
                    new PendulumIntegerValue(5, 128UL),
                    new PendulumIntegerValue(8, 852UL),
                ]),
                new PendulumArrayElement(5, [
                    new PendulumIntegerValue(0, 36UL),
                    new PendulumIntegerValue(1, 0UL),
                    new PendulumIntegerValue(2, 16384UL),
                    new PendulumIntegerValue(4, 13573UL),
                    new PendulumIntegerValue(5, 128UL),
                    new PendulumIntegerValue(8, 176UL),
                    new PendulumIntegerValue(9, 0UL),
                    new PendulumIntegerValue(10, 16UL),
                ]),
            ]),
        ]),
        new(11, 33, 1, 96, [
            new PendulumIntegerValue(2, 46UL),
            new PendulumIntegerValue(4, 2147483673UL),
            new PendulumIntegerValue(6, 2147483649UL),
            new PendulumIntegerValue(13, 4UL),
            new PendulumIntegerValue(17, 128UL),
            new PendulumIntegerValue(18, 448UL),
            new PendulumIntegerValue(19, 21UL),
            new PendulumIntegerValue(20, 62535UL),
            new PendulumIntegerValue(21, 600UL),
            new PendulumIntegerValue(22, 240UL),
            new PendulumIntegerValue(23, 120UL),
            new PendulumIntegerValue(24, 150UL),
            new PendulumIntegerValue(25, 3000UL),
            new PendulumIntegerValue(26, 360UL),
            new PendulumIntegerValue(27, 20UL),
            new PendulumIntegerValue(29, 20UL),
        ]),
    ];
}
