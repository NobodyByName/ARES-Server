using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>
/// Ziplines of the Dam map: rideable lines placed in cells, and launchers whose activation makes the
/// server deploy an anchor and spawn a line.
/// </summary>
public static class DamZiplines
{
    /// <summary>Class of the placed rideable lines.</summary>
    public const string LineBlueprint = "/Game/Pioneer/Core/Gameplay/BP_PlaceableZipline.BP_PlaceableZipline_C";

    /// <summary>Class of the placed launchers.</summary>
    public const string LauncherBlueprint = "/Game/Pioneer/Core/Gameplay/ActivatableZiplines/BP_ActivatableZipline.BP_ActivatableZipline_C";

    /// <summary>Class whose replication layout the line Blueprints use.</summary>
    public const string LineClass = "/Script/Angelscript.ZippableLine";

    /// <summary>Class whose replication layout the launcher Blueprint uses.</summary>
    public const string LauncherClass = "/Script/Angelscript.ActivatableStaticZipline";

    /// <summary>Name of a line's interaction component that attaches a character.</summary>
    public const string AttachInteraction = "AttachInteraction";

    /// <summary>Name of a launcher's interaction component that deploys its line.</summary>
    public const string ActivationInteraction = "ActivationInteraction";

    /// <summary>
    /// Distance from the line end within which a character attaching on the end half rides back towards the start
    /// (<c>ForcedTravelDirectionThresholdHome</c> of the placed line class).
    /// </summary>
    public const double ForcedTravelDirectionThresholdHome = 500.0;

    /// <summary>The line attachment pose (<c>OffsetToUse</c> of the lines' attachment information).</summary>
    public static Transform AttachmentOffset { get; } = new(new Quaternion(0.0, -0.0, 0.7071067690849304, 0.7071067690849304), new Vector3D(0.0, -4.9960036108132044e-14, 225.0), Vector3D.One);

    /// <summary>Relative location of the player character's mesh.</summary>
    public static Vector3D CharacterMeshLocation { get; } = new(0.0, 0.0008559224661439657, -94.8175048828125);

    /// <summary>Relative yaw of the player character's mesh, in degrees.</summary>
    public const double CharacterMeshYaw = 270.0;

    /// <summary>All placed rideable lines, in cell order.</summary>
    public static IReadOnlyList<ZiplinePlacement> Lines { get; } =
    [
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_PlaceableZipline", new Vector3D(496536.53125, 530524.25, -43600.0), new Rotator(0.0, -58.57389450073242, 0.0), new Vector3D(-2.494685173034668, 0.3524211645126343, 8681.91015625), [new ZiplineExitPoint(new Vector3D(313.1500244140625, -0.0670623779296875, 7001.7109375), new Vector3D(-0.7170024514198303, 0.14513325691223145, 7152.57421875), 900.0, 600.0, false, true, false, ZiplineTravelDirection.Away, 0.8238479435511283, 0.06910921259694912, true)]),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_PlaceableZipline_4", new Vector3D(480212.1719216687, 551295.7177982475, -39068.55665749147), new Rotator(0.0, 29.845826, 0.0), new Vector3D(-1.3645960054023467, -0.11768799696005175, 2116.986927796046), [new ZiplineExitPoint(new Vector3D(286.2339556196275, 2.3612485073235767, 1487.0802059999987), new Vector3D(-1.238817, 1.877867, 1699.311404), 900.0, 600.0, false, true, false, ZiplineTravelDirection.Away, 0.8027028247126681, 0.28342162464239046, true)]),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_PlaceableZipline_C_3", new Vector3D(485507.3943025312, 521920.77538297675, -40480.48855761899), new Rotator(0.0, 30.0, 0.0), new Vector3D(-8e-06, 2e-06, 3083.529562), [new ZiplineExitPoint(new Vector3D(1.2493756740519082, 258.5022444003857, 2860.9885660654472), new Vector3D(-7.47084111955217e-09, -4.100281825738021e-06, 2999.227399509131), 400.0, 100.0, true, true, false, ZiplineTravelDirection.Away, 0.9726604980442628, 0.032430368507684826, true)]),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_PlaceableZipline_C_UAID_ECE7A707B18116AC02_ca2073eeb89b1243_2037221872", new Vector3D(493509.5993632302, 525536.9632498993, -39825.0), new Rotator(0.0, 29.999999767609015, 0.0), new Vector3D(-8.700839240959184e-07, 7.70143772765802e-12, 2580.0726336664848), [new ZiplineExitPoint(new Vector3D(4.547473508864641e-12, 275.0000000000156, 2237.5), new Vector3D(0.0, 0.0, 2275.0), 400.0, 100.0, true, true, false, ZiplineTravelDirection.Away, 0.8817581219669194, 0.038758598767776625, true)]),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_PlaceableZipline_C_UAID_ECE7A707B1817CAC02_ca2073eeb89b1243_1744586953", new Vector3D(488957.34528870234, 526370.3005375395, -39825.0), new Rotator(0.0, 119.99999911619557, 0.0), new Vector3D(-1e-06, 0.0, 2580.072634), [new ZiplineExitPoint(new Vector3D(-255.41366300510666, -4.302745864848191, 2078.4899810821425), new Vector3D(-6.70518795609141e-08, -1.4117329252384994e-06, 2162.45128717881), 400.0, 100.0, true, true, false, ZiplineTravelDirection.Away, 0.8381358178379148, 0.03875859876276646, true)]),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_PlaceableZipline_0", new Vector3D(481464.875, 565213.0625, -41198.86328125), new Rotator(0.0, 119.8458251953125, 0.0), new Vector3D(0.0, 0.0, 4695.9921875), [new ZiplineExitPoint(new Vector3D(222.96835327146775, 0.041351318397175874, 4425.6484375), new Vector3D(0.015108287316742464, -0.22483991082498045, 4643.35546875), 900.0, 600.0, false, true, false, ZiplineTravelDirection.Away, 0.9887911400512738, 0.1277685260203598, true)]),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_PlaceableZipline_1", new Vector3D(481060.71875, 565069.8125, -36802.55859375), new Rotator(0.0, 119.8458251953125, 0.0), new Vector3D(-1.3645915985107422, -0.11775505542755127, 8034.158203125), [new ZiplineExitPoint(new Vector3D(223.2868194580078, -149.22750854492188, 7604.697265625), new Vector3D(0.0928688496351242, -0.08931370079517365, 7848.21484375), 900.0, 600.0, false, true, false, ZiplineTravelDirection.Away, 0.9768558699947942, 0.07468112727075718, true)]),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_PlaceableZipline_2", new Vector3D(481637.762193, 572050.912943, -43165.224249), new Rotator(0.0, 119.845825, 0.0), new Vector3D(-1.364595, -0.117688, 1492.627444), [new ZiplineExitPoint(new Vector3D(195.957943, -5.890875, 1178.93528), new Vector3D(-1.238818, 1.877867, 1404.723966), 900.0, 600.0, false, true, false, ZiplineTravelDirection.Away, 0.9411080932038344, 0.4019755564629641, true)]),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_PlaceableZipline_3", new Vector3D(475726.73333521525, 559120.2757119138, -39068.55667667925), new Rotator(0.0, 29.845826, 0.0), new Vector3D(-1.3645959884120928, -0.11768799986345904, 2118.7664701211834), [new ZiplineExitPoint(new Vector3D(254.285942, 2.396299, 1487.080206), new Vector3D(-1.238817, 1.877867, 1699.311404), 900.0, 600.0, false, true, false, ZiplineTravelDirection.Away, 0.8020286385482822, 0.2831835802079685, true)]),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_PlaceableZipline_C_2", new Vector3D(477223.18053225556, 566451.5881906139, -36799.99999414933), new Rotator(0.0, 0.0, 0.0), new Vector3D(-12.5, 12.5, 1912.5), [new ZiplineExitPoint(new Vector3D(-272.0235901262495, 474.7991446510423, 1458.0568600367114), new Vector3D(-12.499996817612555, 12.500001344829798, 1547.1008635425023), 900.0, 600.0, true, true, false, ZiplineTravelDirection.Away, 0.8089579500924019, 0.3137120891371374, true)]),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/8635EV0VJ3V10LNUJ9RO28RQ9", "BP_PlaceableZipline_C_1", new Vector3D(489806.60424086015, 497342.46710577357, -35566.84053343565), new Rotator(0.0, -29.370073538851127, 0.0), new Vector3D(-1.1292910568882488e-06, -2.4576714079055533e-05, 1972.9594437399137), [new ZiplineExitPoint(new Vector3D(268.53096913923724, 488.79404122037727, 1749.6708063222104), new Vector3D(-3.5271860752196914e-06, -2.7141247859124976e-07, 1885.9251425689872), 900.0, 800.0, true, true, false, ZiplineTravelDirection.Away, 0.9558864215647811, 0.40548223256101573, true)]),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/D7SLI18I6UYP3QP4VHBRYZMGH", "BP_PlaceableZipline_C_0", new Vector3D(504764.09376794787, 524136.0469634217, -38994.27749413738), new Rotator(0.0, -42.207623, 0.0), new Vector3D(-3.831377641974793e-05, 1.0000003807201125e-06, 1714.9257107488884), [new ZiplineExitPoint(new Vector3D(236.274955502097, 135.66580944532978, 1564.5742850987444), new Vector3D(5.136965340962751e-06, -0.3629109214720793, 1669.5358655475502), 400.0, 100.0, true, true, false, ZiplineTravelDirection.Away, 0.9735324714551457, 0.05831156380315222, true)]),
    ];

    /// <summary>All placed launchers, in cell order.</summary>
    public static IReadOnlyList<ActivatableZiplinePlacement> Launchers { get; } =
    [
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/418E58TX3DAEY1FH9WC805A5X", "BP_ActivatableZipline_2", new Vector3D(514475.9366539341, 587148.9172804693, -43523.754937961196), new Rotator(0.0, -139.51756485561674, 0.0), new Vector3D(6673.179600978092, -3655.7221530027364, 915.598942160781), false, true, "/Game/Pioneer/Core/Gameplay/ActivatableZiplines/BP_ActivatableZiplineObject_Child"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/6VLN5OPYR367ZIKOMVAOOKL4Z", "BP_ActivatableZipline_C_2", new Vector3D(445896.1991274342, 598940.5813011288, -32992.79389381462), new Rotator(0.0, -64.99999949648587, 0.0), new Vector3D(3726.99372644358, -383.63568874124076, -3095.2571579320793), false, false, "/Game/Pioneer/Core/Gameplay/ActivatableZiplines/BP_ActivatableZiplineObject_Child"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/78VSCAY5CCYC9B657XV2IPJ4Z", "BP_ActivatableZipline_8", new Vector3D(469863.7389948905, 547571.7244025471, -40715.77867780558), new Rotator(0.0, -331.1171098891716, 0.0), new Vector3D(4017.1308745949877, -224.40885991385196, 3911.1393875862996), true, true, null),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_ActivatableZipline_4", new Vector3D(473686.1875, 559447.125, -35699.16796875), new Rotator(0.0, 38.20283126831055, 0.0), new Vector3D(3103.4082031249663, -90.734802245938, 1588.171875), false, true, "/Game/Pioneer/Core/Gameplay/ActivatableZiplines/BP_ActivatableZiplineObject_Child"),
        new("/Game/Pioneer/Maps/TheDam_02/TheDam_02_P/_Generated_/799Z1TX1MWV1JVES1KJXD650D", "BP_ActivatableZipline_C_0", new Vector3D(495257.8034, 576176.708507, -42595.768088), new Rotator(0.0, 30.0, 0.0), new Vector3D(-327.9873742216969, -3987.2060519999122, -2495.273878757005), true, false, null),
    ];
}
