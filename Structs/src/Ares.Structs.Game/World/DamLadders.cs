using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>
/// Climbable ladders of the Dam map: ladder splines placed in cells, and the actor the server spawns
/// for each one (the class the ladders' logic data names) to make it climbable.
/// </summary>
public static class DamLadders
{
    /// <summary>Class the server spawns for each ladder, with its class default.</summary>
    public static ArchetypeClass Logic { get; } = new("/Game/Pioneer/Interactables/Ladder/BP_Ladder_Logic.BP_Ladder_Logic_C", "/Game/Pioneer/Interactables/Ladder/BP_Ladder_Logic.Default__BP_Ladder_Logic_C");

    /// <summary>Class whose replication layout the spawned Blueprint uses.</summary>
    public const string ActorClass = "/Script/Angelscript.ClimbableLadder";

    /// <summary>Class of the climb interactions.</summary>
    public const string InteractionClass = "/Script/Angelscript.StandardInteractionComponent";

    /// <summary>Names of the climb interaction components (bottom and top entry).</summary>
    public static IReadOnlyList<string> InteractionComponents { get; } = ["AttachInteraction", "TopEntryInteraction"];

    /// <summary>Height of one climb cycle (<c>ULadderLogicData.CycleHeight</c>), in centimetres.</summary>
    public const double CycleHeight = 132.0;

    /// <summary>
    /// Distance below the climbing height where the climb ends (<c>AClimbableLadder.EndLadderInteractionDistance</c>),
    /// in centimetres.
    /// </summary>
    public const double EndInteractionDistance = 100.0;

    /// <summary>All ladders, in cell order.</summary>
    public static IReadOnlyList<LadderPlacement> All { get; } =
    [
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_RoofEnd_01_C_12", 1190.9195298362247, new Vector3D(443459.54029403994, 555132.7177624163, -40191.1871094968), 74.99999941902247),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_RoofEnd_01_C_13", 1196.5276951863634, new Vector3D(445462.7035637103, 554595.9717444604, -40188.11796458838), 74.999999),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_RoofEnd_01_C_6f4b63b7560697f3_1", 462.5, new Vector3D(436648.0496873816, 585605.925902412, -38503.354509532925), 114.19816899999998),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_RoofEnd_01_C_6f4b63b7560697f3_2", 554.5343742994821, new Vector3D(436172.0393719939, 586146.7605607274, -38577.49431488508), -65.80182960565493),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_RoofEnd_01_C_a8cd2ad1af69bea6_1", 792.0054465728724, new Vector3D(449616.46872052277, 558135.8722608977, -38975.00000000019), 74.99999895424048),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_RoofEnd_01_C_e83740df1f1b488f_1", 462.5, new Vector3D(428901.23147605057, 554727.9182552816, -40783.150350532924), -170.80183200000002),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_RoofEnd_01_C_e83740df1f1b488f_2", 554.5343742994821, new Vector3D(428255.62477096217, 554408.1059191603, -40857.29015588508), 9.198169394345083),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_WallEnd_01_C_1", 973.6699018106374, new Vector3D(444917.28661222925, 555711.0625148074, -38975.93220884799), 74.99999941902247),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_14", 958.29587, new Vector3D(484199.431771, 512820.032215, -38575.863704), -419.999997),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_10", 1075.4937198335756, new Vector3D(494525.55245817814, 534310.885205272, -38787.5), 119.99999907043625),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_14", 1081.369450911472, new Vector3D(495676.8744125597, 532320.7990709129, -38787.500000040745), -59.99999953521801),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_3", 1056.6939376643568, new Vector3D(485523.20486590633, 549898.7204301669, -38767.87662323515), -60.000002000000094),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_4", 1061.0750252796352, new Vector3D(494233.37417705, 534824.1415053282, -38787.5), -59.99999953521695),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_5", 1061.435671320418, new Vector3D(484373.20503466675, 551890.578965646, -38762.499999999505), 119.99999900000002),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_987e59662c40308c_1", 821.527694999997, new Vector3D(488090.4006421114, 512146.5304013251, -38212.5), -150.0000020457595),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_UAID_107C614142F6DD6702_1275960258", 683.9183402450144, new Vector3D(485018.75000139326, 522035.82531743497, -37629.00212446665), 119.99999907043619),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_UAID_ECE7A7024099CAB702_ca2073eeb89b1243_1352503202", 712.5, new Vector3D(492306.24999981106, 526064.1746823436, -39812.5), 119.999999),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_UAID_ECE7A707B1811EAC02_ca2073eeb89b1243_1441331637", 1100.0, new Vector3D(489634.2299783608, 526434.7811793387, -37904.84837956415), 119.99999907043647),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_c7af8904b41bbf15_1", 462.5, new Vector3D(459008.91369166755, 527469.2715365883, -40854.512671532924), -150.80183200000002),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_c7af8904b41bbf15_2", 554.5343742994821, new Vector3D(458511.6240960036, 526947.9357464924, -40928.65247688508), 29.19816939434509),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_0", 925.0, new Vector3D(490344.61887, 574018.259902, -43533.111073), -149.999999),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_1", 925.0, new Vector3D(494859.587682, 576624.978244, -43533.111073), 30.0),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_10", 925.0, new Vector3D(492812.791321, 575443.259815, -43533.111072), -149.999999),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_2", 925.0, new Vector3D(495345.915627, 576905.759815, -43533.111073), -149.999999),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_3", 925.0, new Vector3D(489858.290925, 573737.478331, -43533.111073), 30.0),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_4", 613.0176462085874, new Vector3D(500742.00050554884, 589279.028768581, -42115.48807983886), -149.99999883804514),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_0", 1067.7046445157903, new Vector3D(481473.20503850176, 556913.5263009771, -38762.5), 119.99999900000002),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_1", 1070.9771167895815, new Vector3D(481173.20504724747, 557433.1415439539, -38762.5), -60.00000000000005),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_2", 1072.144293303696, new Vector3D(484073.2049535521, 552410.1941519878, -38762.5), -60.00000239434624),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_9", 1077.1903830561423, new Vector3D(478573.20505745005, 561936.4736658831, -38762.5), 119.99999900000002),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_5", 1727.071211384653, new Vector3D(498593.75, 591089.174683, -41437.5), -60.0),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_6", 794.6306667532626, new Vector3D(497376.75938156625, 589710.1664145803, -41400.0), 30.0),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_7", 677.1302731604083, new Vector3D(466091.76071650715, 554649.4700529961, -40060.31025444137), 119.999999),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_8", 795.7025006903132, new Vector3D(495905.657429, 588860.825354, -41412.5), 209.999999),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_8dcfce4758e17c9e_0", 792.0708989828976, new Vector3D(463063.78406783193, 564005.1522969676, -37550.0), 119.9999986514136),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_9", 925.0, new Vector3D(492326.463376, 575162.478244, -43533.111073), 30.0),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_UAID_ECE7A707B181479802_1184312675", 1477.5516445179492, new Vector3D(476906.40604227665, 564458.3490856293, -28426.26849718314), -329.99999744369853),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_a3fe8eae67942ad0_1", 462.5, new Vector3D(462391.6832623377, 565593.6415898629, -36656.365795532925), 119.19816900000001),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_a3fe8eae67942ad0_2", 554.5343742994821, new Vector3D(461870.3474635624, 566090.9311764278, -36730.50560088508), -60.801829605654916),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_c2dda45b4b69323a_1", 462.5, new Vector3D(490682.99267139984, 576103.7025189439, -42465.36064853292), 129.19817099999997),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_c2dda45b4b69323a_2", 554.5343742994821, new Vector3D(490083.2236904284, 576502.9081269396, -42539.50045388508), -50.801827605654935),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/D7SLI18I6UYP3QP4VHBRYZMGH", "BP_Ladder_RoofEnd_01_C_0867a5cfd45580e9_1", 462.5, new Vector3D(513607.35808669194, 522698.73711383966, -37941.06966353292), 69.19816899999996),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/D7SLI18I6UYP3QP4VHBRYZMGH", "BP_Ladder_RoofEnd_01_C_0867a5cfd45580e9_2", 554.5343742994821, new Vector3D(513653.1958191621, 523417.7550901605, -38015.20946888508), -110.80182960565496),
    ];
}
