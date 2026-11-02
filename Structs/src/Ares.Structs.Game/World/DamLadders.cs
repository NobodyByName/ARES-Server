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
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_RoofEnd_01_C_12", 1190.9195298362247),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_RoofEnd_01_C_13", 1196.5276951863634),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_RoofEnd_01_C_6f4b63b7560697f3_1", 462.5),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_RoofEnd_01_C_6f4b63b7560697f3_2", 554.5343742994821),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_RoofEnd_01_C_a8cd2ad1af69bea6_1", 792.0054465728724),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_RoofEnd_01_C_e83740df1f1b488f_1", 462.5),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_RoofEnd_01_C_e83740df1f1b488f_2", 554.5343742994821),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_Ladder_WallEnd_01_C_1", 973.6699018106374),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_14", 958.29587),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_10", 1075.4937198335756),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_14", 1081.369450911472),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_3", 1056.6939376643568),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_4", 1061.0750252796352),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_5", 1061.435671320418),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_987e59662c40308c_1", 821.527694999997),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_UAID_107C614142F6DD6702_1275960258", 683.9183402450144),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_UAID_ECE7A7024099CAB702_ca2073eeb89b1243_1352503202", 712.5),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_UAID_ECE7A707B1811EAC02_ca2073eeb89b1243_1441331637", 1100.0),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_c7af8904b41bbf15_1", 462.5),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_Ladder_RoofEnd_01_C_c7af8904b41bbf15_2", 554.5343742994821),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_0", 925.0),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_1", 925.0),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_10", 925.0),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_2", 925.0),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_3", 925.0),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_4", 613.0176462085874),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_0", 1067.7046445157903),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_1", 1070.9771167895815),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_2", 1072.144293303696),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_430c3d9e3b496a7a_9", 1077.1903830561423),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_5", 1727.071211384653),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_6", 794.6306667532626),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_7", 677.1302731604083),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_8", 795.7025006903132),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_8dcfce4758e17c9e_0", 792.0708989828976),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_9", 925.0),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_UAID_ECE7A707B181479802_1184312675", 1477.5516445179492),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_a3fe8eae67942ad0_1", 462.5),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_a3fe8eae67942ad0_2", 554.5343742994821),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_c2dda45b4b69323a_1", 462.5),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_Ladder_RoofEnd_01_C_c2dda45b4b69323a_2", 554.5343742994821),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/D7SLI18I6UYP3QP4VHBRYZMGH", "BP_Ladder_RoofEnd_01_C_0867a5cfd45580e9_1", 462.5),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/D7SLI18I6UYP3QP4VHBRYZMGH", "BP_Ladder_RoofEnd_01_C_0867a5cfd45580e9_2", 554.5343742994821),
    ];
}
