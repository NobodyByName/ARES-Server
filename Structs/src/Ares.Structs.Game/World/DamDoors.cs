using Ares.Structs.Net.Replication;

namespace Ares.Structs.Game.World;

/// <summary>State of an interaction component (<c>ECurrentInteractionState</c>).</summary>
public enum CurrentInteractionState : byte
{
    Uninitialized = 0,
    Disabled = 1,
    Idle = 2,
    Ongoing = 3,
    Succeeding = 4,
    Cancelling = 5,
    SuccessIdle = 6,
    SuccessDisabled = 7,
}

/// <summary>
/// Regular doors of the Dam map: placed directly in a cell's persistent level, with two
/// open/close buttons, and how each one starts. Puzzle and Harvester doors are not included.
/// </summary>
public static class DamDoors
{
    /// <summary>Class whose replication layout the door Blueprints use.</summary>
    public const string ActorClass = "/Script/Angelscript.BreachableDoorActor";

    /// <summary>Class of the door buttons.</summary>
    public const string InteractionClass = "/Script/Angelscript.StandardInteractionComponent";

    /// <summary>Names of the door button components.</summary>
    public static IReadOnlyList<string> InteractionComponents { get; } = ["OpenCloseInteraction_Exterior", "OpenCloseInteraction_Interior"];

    /// <summary>Name of the door breach button component (a hold interaction).</summary>
    public const string BreachInteractionComponent = "BreachInteraction";

    /// <summary>Seconds the breach button is held, after its alignment.</summary>
    public const double BreachHoldSeconds = 10.0;

    /// <summary>Seconds the character aligns with the breach button before the hold starts.</summary>
    public const double BreachAlignmentSeconds = 1.0;

    /// <summary>The interaction-state enum as replication needs it.</summary>
    public static EnumDefinition InteractionState { get; } = new("/Script/Angelscript.ECurrentInteractionState", 8, ["Uninitialized", "Disabled", "Idle", "Ongoing", "Succeeding", "Cancelling", "SuccessIdle", "SuccessDisabled"]);

    /// <summary>All doors, in cell order.</summary>
    public static IReadOnlyList<DoorPlacement> All { get; } =
    [
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/2LCLHICIKARFJOYKQ31JKCTKT", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_A0369F2048201BB702_1584051613") { StartAsOpened = true, StartAsPowered = false },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/2LCLHICIKARFJOYKQ31JKCTKT", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_A0369F2048201BB702_1584055614") { StartAsOpened = true, StartAsPowered = false },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/2LCLHICIKARFJOYKQ31JKCTKT", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_A0369F2048201BB702_1584060618") { StartAsOpened = true, StartAsPowered = false },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/2LCLHICIKARFJOYKQ31JKCTKT", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_A0369F2048201BB702_1584069622") { StartAsOpened = true, StartAsPowered = false },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/3DYL1WFYK09576PDBOFTH79Y6", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_A0369F2048201BB702_1584022593") { StartAsLocked = true },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/3DYL1WFYK09576PDBOFTH79Y6", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_A0369F2048201BB702_1584028596") { StartAsLocked = true },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/3DYL1WFYK09576PDBOFTH79Y6", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_A0369F2048201BB702_1584042605") { StartAsLocked = true },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/3DYL1WFYK09576PDBOFTH79Y6", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_A0369F2048201BB702_1584079626") { StartAsLocked = true },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/3XAKI4LXH57PLHVVIGI733WFK", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_A0369F2048201BB702_1584035600") { StartAsOpened = true, StartAsPowered = false },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6QA1PNENREWQJEC0AJ4PSDVK7", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_A0369F2048201BB702_1584076625") { StartAsLocked = true },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_47a5049820c0950a_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_47a5049820c0950a_1"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_59f2d924aecddadf_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_59f2d924aecddadf_1"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_a8cd2ad1af69bea6_1"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_a8cd2ad1af69bea6_2"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_a8cd2ad1af69bea6_4"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_a8cd2ad1af69bea6_5"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_a8cd2ad1af69bea6_6"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_InteractiveDoor_Wrh_x300z325_01_C_b6a3f9a1077f48a9_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_InteractiveDoor_Wrh_x300z325_01_C_b6a3f9a1077f48a9_1"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_774acc0159628c8f_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_774acc0159628c8f_1"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_1670"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_1671"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_1674"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_1675"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_1676"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_1677"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_1678"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_1679"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_1681"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_1683"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_2"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_3"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_5"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_6"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_7"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_8"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_7486E223302EE05302_987e59662c40308c_1136268570"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_7486E223302EE05302_987e59662c40308c_1197905571"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_7486E223302EE05302_987e59662c40308c_1222325572"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_ECE7A707B18125B702_ca2073eeb89b1243_1489160922") { StartAsLocked = true },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_ECE7A707B181B8AC02_ca2073eeb89b1243_1081050354"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_ECE7A707B181C7AB02_ca2073eeb89b1243_1615055787") { StartAsLocked = true, StartAsPowered = false },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Wrh_x300z325_01_C_4277dc7733e58af9_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Wrh_x300z325_01_C_4277dc7733e58af9_1"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Wrh_x300z325_01_C_430c3d9e3b496a7a_10"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Wrh_x300z325_01_C_430c3d9e3b496a7a_7"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Wrh_x300z325_01_C_430c3d9e3b496a7a_8"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Wrh_x300z325_01_C_430c3d9e3b496a7a_9"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Wrh_x300z325_01_C_987e59662c40308c_1"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Wrh_x300z325_01_C_987e59662c40308c_2"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_InteractiveDoor_Wrh_x300z325_01_C_UAID_ECE7A7024099D6B602_ca2073eeb89b1243_1327962690"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_091785a43ffd220e_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_091785a43ffd220e_2") { StartAsSealed = true },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_091785a43ffd220e_4"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_091785a43ffd220e_5") { StartAsSealed = true },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_091785a43ffd220e_7"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_4896d35478120a39_10"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_4896d35478120a39_11"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_4896d35478120a39_12"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_4896d35478120a39_8"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_831a2f8ea84ec66e_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_831a2f8ea84ec66e_1"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_8dcfce4758e17c9e_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_8dcfce4758e17c9e_10"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_8dcfce4758e17c9e_11"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_8dcfce4758e17c9e_12"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_8dcfce4758e17c9e_8"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_8dcfce4758e17c9e_9"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_7486E223302EDE5302_8dcfce4758e17c9e_1757308215") { StartAsSealed = true },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_7486E223302EDE5302_8dcfce4758e17c9e_1838240216") { StartAsSealed = true },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_7486E223302EDF5302_8dcfce4758e17c9e_2080883393"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_a13088f6642146c5_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_a13088f6642146c5_1"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_a49fd4f7f8af6403_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_a49fd4f7f8af6403_1"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_a8cd2ad1af69bea6_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_a8cd2ad1af69bea6_10"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_f40b503706721af3_1"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_f40b503706721af3_11"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_f40b503706721af3_13"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_f40b503706721af3_16"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_f40b503706721af3_17"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_f40b503706721af3_3"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_f40b503706721af3_4"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_f40b503706721af3_5"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_f40b503706721af3_7"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Rsr_x350z300_01_C_f40b503706721af3_8"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_091785a43ffd220e_0") { StartAsSealed = true },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_430c3d9e3b496a7a_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_430c3d9e3b496a7a_1") { StartAsSealed = true },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_430c3d9e3b496a7a_2"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_430c3d9e3b496a7a_3") { StartAsSealed = true },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_430c3d9e3b496a7a_4"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_430c3d9e3b496a7a_5"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_4896d35478120a39_10"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_4896d35478120a39_12"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_4896d35478120a39_13"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_4896d35478120a39_4"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_4896d35478120a39_5"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_4896d35478120a39_6"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_4896d35478120a39_8"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_83934d46e6ea3580_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_83934d46e6ea3580_1"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_cd5bac4b5cfa349a_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_InteractiveDoor_Wrh_x300z325_01_C_cd5bac4b5cfa349a_1"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/9S6VM9T6Q1DWC3A03BDTNBDPO", "BP_InteractiveDoor_Wrh_x300z325_01_C_bf7185668dd55644_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/9S6VM9T6Q1DWC3A03BDTNBDPO", "BP_InteractiveDoor_Wrh_x300z325_01_C_bf7185668dd55644_1"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/9S6VM9T6Q1DWC3A03BDTNBDPO", "BP_InteractiveDoor_Wrh_x300z325_01_C_bf7185668dd55644_2"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/AP1HZR4WT45VSHZQYFGOFX0ES", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_A0369F2048201BB702_1584072623") { StartAsOpened = true, StartAsPowered = false },
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/D7SLI18I6UYP3QP4VHBRYZMGH", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_1672"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/D7SLI18I6UYP3QP4VHBRYZMGH", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_1673"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/D7SLI18I6UYP3QP4VHBRYZMGH", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_1682"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/D7SLI18I6UYP3QP4VHBRYZMGH", "BP_InteractiveDoor_Rsr_x350z300_01_C_987e59662c40308c_9"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/D7SLI18I6UYP3QP4VHBRYZMGH", "BP_InteractiveDoor_Wrh_x300z325_01_C_3f127d0469c44935_0"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/D7SLI18I6UYP3QP4VHBRYZMGH", "BP_InteractiveDoor_Wrh_x300z325_01_C_3f127d0469c44935_1"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/E1TWCM779FDK4P5XXBFUDZ3QF", "BP_InteractiveDoor_Rsr_x350z300_01_C_UAID_A0369F2048201BB702_1584063619") { StartAsLocked = true },
    ];
}
