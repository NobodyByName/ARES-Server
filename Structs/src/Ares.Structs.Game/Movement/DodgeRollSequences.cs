namespace Ares.Structs.Game.Movement;

/// <summary>Authored metrics and root motion of the dodge-roll variants.</summary>
public static class DodgeRollSequences
{
    /// <summary>Roll from walking or standing.</summary>
    public static DodgeRollSequence Standard { get; } = new(
        400.0, 1.0, 20.0, 0.4, 30.0,
        [
            -0.852768838f, 10.5041933f, 21.9203758f, 33.3918304f, 44.9146118f, 56.4847641f, 68.0983505f, 79.7514191f,
            91.4400177f, 103.160202f, 114.908012f, 126.67952f, 138.470764f, 150.277817f, 162.096695f, 173.923477f,
            185.754196f, 197.584915f, 209.411713f, 221.230576f, 233.037628f, 244.828873f, 256.600372f, 268.348175f,
            280.06839f, 291.756989f, 303.410034f, 315.023621f, 326.593781f, 338.116577f, 349.588013f, 361.004211f,
            372.361145f,
        ],
        [
            new DodgeRollWindow(DodgeRollTransientState.CollisionGrace, 0.044403661f, 0.212747663f),
            new DodgeRollWindow(DodgeRollTransientState.CrouchExit, 0.766569495f, 1.06666684f),
        ]);

    /// <summary>Roll while sprinting.</summary>
    public static DodgeRollSequence Sprint { get; } = new(
        545.0, 1.0, 40.0, 0.57, 30.0,
        [
            -6.18278599f, 18.827589f, 45.5526657f, 78.6084595f, 116.036263f, 144.355438f, 168.782364f, 193.11911f,
            217.375671f, 241.562042f, 265.688202f, 289.76416f, 313.799896f, 337.805389f, 361.79068f, 385.765747f,
            409.74054f, 432.091614f, 452.142731f, 465.053955f, 474.29718f, 481.230927f, 487.213745f, 493.604218f,
            501.760895f, 511.934998f, 523.146057f, 534.904846f, 546.72229f, 557.751526f, 568.054321f, 578.504517f,
            589.97583f,
        ],
        [
            new DodgeRollWindow(DodgeRollTransientState.CollisionGrace, 0.0348796099f, 0.213337243f),
            new DodgeRollWindow(DodgeRollTransientState.CrouchExit, 0.785015881f, 1.06666672f),
        ]);

    /// <summary>Roll while exhausted or carrying a heavy load (not sprinting).</summary>
    public static DodgeRollSequence Exhausted { get; } = new(
        350.0, 1.0, 20.0, 0.58, 30.0,
        [
            0.0f, 7.29999971f, 14.9382973f, 22.8893833f, 31.169178f, 39.1380386f, 47.3419724f, 56.0215836f,
            65.5236511f, 76.1626129f, 88.0326538f, 100.743233f, 113.765839f, 126.542526f, 139.185776f, 151.583099f,
            163.92131f, 175.874786f, 187.026001f, 197.827026f, 208.320053f, 218.101364f, 225.950943f, 232.097229f,
            237.451935f, 242.536423f, 247.951401f, 252.696808f, 257.058044f, 261.315308f, 265.470642f, 269.831635f,
            274.390991f, 279.161713f, 283.593079f, 287.787445f, 292.136261f, 296.775909f, 301.723724f, 306.819641f,
            312.012115f, 317.324707f, 322.818024f, 328.604004f, 334.593658f, 340.732025f, 347.078522f,
        ],
        [
            new DodgeRollWindow(DodgeRollTransientState.CollisionGrace, 0.0725981742f, 0.254970819f),
            new DodgeRollWindow(DodgeRollTransientState.CrouchExit, 1.01427233f, 1.5333333f),
        ]);

    /// <summary>Roll with the roll-improving character skill.</summary>
    public static DodgeRollSequence Ninja { get; } = new(
        300.0, 1.25, 10.0, 0.38, 30.0,
        [
            -0.852768838f, 10.5041933f, 21.9203758f, 33.3918304f, 44.9146118f, 56.4847641f, 68.0983505f, 79.7514191f,
            91.4400177f, 103.160202f, 114.908012f, 126.67952f, 138.470764f, 150.277817f, 162.096695f, 173.923477f,
            185.754196f, 197.584915f, 209.411713f, 221.230576f, 233.037628f, 244.828873f, 256.600372f, 268.348175f,
            280.06839f, 291.756989f, 303.410034f, 315.023621f, 326.593781f, 338.116577f, 349.588013f, 361.004211f,
            372.361145f,
        ],
        [
            new DodgeRollWindow(DodgeRollTransientState.CollisionGrace, 0.044403661f, 0.212747663f),
            new DodgeRollWindow(DodgeRollTransientState.CrouchExit, 0.766569495f, 1.06666684f),
        ]);
}
