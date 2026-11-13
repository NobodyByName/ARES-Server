using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>
/// Loot placement limiters of the Dam map: areas that cap how many placements the sockets inside them get, per
/// gameplay tag.
/// </summary>
public static class DamLootLimiters
{
    /// <summary>All limiters, ordered by cell and actor.</summary>
    public static IReadOnlyList<LootLimiter> All { get; } =
    [
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "LootPlacementLimiter_11",
            "LootPlacementLimiter_ElectricalSubstation",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(-26.9, 1134.03, -2744.25),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_81",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.500000007, 0.8660254, 0.0), 290273.1967),
                            new LootVolumePlane(new Vector3D(0.8660254, 0.500000007, 0.0), 676328.6978),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 39800.0),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -35350.0),
                            new LootVolumePlane(new Vector3D(-0.8660254, -0.500000007, 0.0), -667928.6978),
                            new LootVolumePlane(new Vector3D(0.500000007, -0.8660254, 0.0), -274710.6983),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 10),
                new LootLimit("Socket.LootContainer.Electrical.GeneratorM02", true, 0),
                new LootLimit("Socket.LootContainer.Mechanical.CompressorTrailer", false, 4),
                new LootLimit("Socket.LootContainer.Industrial.Lockers.Door", false, 2),
                new LootLimit("Socket.LootContainer.Electrical.Transformer.PanelSmall", false, 6),
                new LootLimit("Socket.LootContainer.Electrical.Transformer.PanelMain", true, 0),
                new LootLimit("Socket.LootContainer.Electrical.Transformer.PanelLarge", false, 2),
            ]),
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "LootPlacementLimiter_20",
            "LootPlacementLimiter_RaiderComplex",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(418140.49, 561895.08, -38783.75),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "EnemyHusk14_76",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(0.990484408, -0.13762499, 0.0), 347535.5317),
                            new LootVolumePlane(new Vector3D(-0.137625061, -0.990484398, 0.0), -605851.9008),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 42265.1433),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -36583.9504),
                            new LootVolumePlane(new Vector3D(0.137624994, 0.990484407, 0.0), 621485.7796),
                            new LootVolumePlane(new Vector3D(-0.99048442, 0.137624902, 0.0), -332553.1177),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 10),
                new LootLimit("Socket.LootContainer.Electrical.GeneratorM02", true, 0),
            ]),
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "LootPlacementLimiter_22",
            "LootPlacementLimiter_Pumphouse",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(422894.4, 578206.49, -40230.42),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_84",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.965925826, 0.258819048, 0.0), -254361.3093),
                            new LootVolumePlane(new Vector3D(0.258800245, 0.965930863, -4.4e-08), 670972.252),
                            new LootVolumePlane(new Vector3D(0.258800261, 0.965930859, -4.4e-08), 670972.2561),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 41025.0),
                            new LootVolumePlane(new Vector3D(0.258800245, 0.965930863, 0.0), 670972.2502),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -36225.0),
                            new LootVolumePlane(new Vector3D(0.258800261, 0.965930859, 0.0), 670972.2545),
                            new LootVolumePlane(new Vector3D(-0.258819048, -0.965925826, 0.0), -664857.4597),
                            new LootVolumePlane(new Vector3D(0.965925179, -0.258821461, 0.0), 267422.149),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 8),
            ]),
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "LootPlacementLimiter_5",
            "LootPlacementLimiter_WaterControl",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(1021.01, 2585.78, -3164.85),
            new Rotator(0.0, -30.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_138",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.965925827, 0.258819043, 0.0), -281226.8758),
                            new LootVolumePlane(new Vector3D(0.258819043, 0.965925827, 0.0), 659268.2252),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 40950.0),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -38462.5),
                            new LootVolumePlane(new Vector3D(-0.258819043, -0.965925827, 0.0), -651405.7702),
                            new LootVolumePlane(new Vector3D(0.965925827, -0.258819043, 0.0), 287326.8758),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 10),
                new LootLimit("Socket.LootContainer.Residential", false, 3),
                new LootLimit("Socket.LootContainer.Mechanical.PressureMonitor", false, 2),
                new LootLimit("Socket.LootContainer.Mechanical.CompressorTrailer", true, 0),
                new LootLimit("Socket.LootContainer.Industrial.Lockers.Door", false, 8),
                new LootLimit("Socket.LootContainer.Electrical.Transformer.PanelSmall", true, 0),
                new LootLimit("Socket.LootContainer.Electrical.Transformer.PanelMain", true, 0),
                new LootLimit("Socket.LootContainer.Electrical.Transformer.PanelLarge", true, 0),
            ]),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "LootPlacementLimiter_1",
            "LootPlacementLimiter_PowerControl",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(6609.63, 10826.9, 378.41),
            new Rotator(0.0, -30.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "ResearchFacility",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -33822.8696),
                            new LootVolumePlane(new Vector3D(-0.294566272, 0.955631054, 0.0), 353204.2636),
                            new LootVolumePlane(new Vector3D(-0.499472369, 0.866329817, 0.0), 203083.07),
                            new LootVolumePlane(new Vector3D(0.776777178, 0.629775528, 0.0), 734108.667),
                            new LootVolumePlane(new Vector3D(0.494264881, -0.869311352, 0.0), -199390.7595),
                            new LootVolumePlane(new Vector3D(-0.866017515, -0.500013663, 0.0), -676113.4645),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 46084.1304),
                        ]),
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -33822.8696),
                            new LootVolumePlane(new Vector3D(0.500006966, -0.866021382, 1.364e-06), -194763.5932),
                            new LootVolumePlane(new Vector3D(-0.494265227, 0.869311156, 1.854e-06), 199390.4283),
                            new LootVolumePlane(new Vector3D(-0.866211453, -0.499677614, 0.0), -676037.7873),
                            new LootVolumePlane(new Vector3D(0.500006356, -0.866021734, 0.0), -194764.0233),
                            new LootVolumePlane(new Vector3D(-0.494266059, 0.869310683, 0.0), 199389.8451),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 46084.1304),
                            new LootVolumePlane(new Vector3D(-0.052777026, 0.092823963, -0.994282806), 67111.3269),
                            new LootVolumePlane(new Vector3D(0.072203641, -0.125058051, -0.989518629), 17476.223),
                            new LootVolumePlane(new Vector3D(8.9e-08, 5.1e-08, -1.0), 46084.1999),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 20),
                new LootLimit("Socket.LootContainer.Mechanical.CompressorTrailer", false, 4),
                new LootLimit("Socket.LootContainer.Electrical.Transformer.PanelSmall", false, 10),
                new LootLimit("Socket.LootContainer.Electrical.Transformer.PanelMain", false, 4),
                new LootLimit("Socket.LootContainer.Electrical.Transformer.PanelLarge", false, 8),
                new LootLimit("Socket.LootContainer.Tech.Server.Door", false, 4),
                new LootLimit("Socket.LootContainer.Electrical.Fusebox.PanelLarge", false, 8),
                new LootLimit("Socket.LootContainer.Electrical.Fusebox.PanelSmall", false, 10),
                new LootLimit("Socket.LootContainer.Industrial.RsrMachineM01.Door", true, 0),
            ]),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "LootPlacementLimiter_10",
            "LootPlacementLimiter_SupportBuilding2",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(488898.06, 514418.8, -35964.61),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_208",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.866025404, -0.5, 0.0), -679487.3536),
                            new LootVolumePlane(new Vector3D(-0.5, 0.866025404, 0.0), 201254.1981),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 36910.683),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -36144.9316),
                            new LootVolumePlane(new Vector3D(0.5, -0.866025404, 0.0), -200187.9891),
                            new LootVolumePlane(new Vector3D(0.866025404, 0.5, 0.0), 681017.9094),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 2),
                new LootLimit("Socket.LootContainer.Electrical", false, 2),
                new LootLimit("Socket.LootContainer.Residential", true, 0),
                new LootLimit("Socket.LootContainer.Commercial", true, 0),
                new LootLimit("Socket.LootContainer.Industrial", false, 1),
            ]),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "LootPlacementLimiter_12",
            "LootPlacementLimiter_SupportBuilding3",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(486290.97, 523600.88, -35964.61),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_114",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.500000011, 0.866025398, 0.0), 211065.932),
                            new LootVolumePlane(new Vector3D(0.866025398, 0.500000011, 0.0), 683396.2595),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 36858.5387),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -36041.4613),
                            new LootVolumePlane(new Vector3D(-0.866025398, -0.500000011, 0.0), -682211.9503),
                            new LootVolumePlane(new Vector3D(0.500000011, -0.866025398, 0.0), -209107.9696),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", true, 0),
                new LootLimit("Socket.LootContainer.Electrical", false, 2),
                new LootLimit("Socket.LootContainer.Residential", true, 0),
                new LootLimit("Socket.LootContainer.Commercial", true, 0),
                new LootLimit("Socket.LootContainer.Industrial", false, 2),
            ]),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "LootPlacementLimiter_13",
            "LootPlacementLimiter_SupportBuilding4",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(490693.81, 523600.88, -35964.61),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_115",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.866025401, -0.500000005, 0.0), -685712.6223),
                            new LootVolumePlane(new Vector3D(-0.500000005, 0.866025401, 0.0), 208866.6102),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 36858.5387),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -36041.4613),
                            new LootVolumePlane(new Vector3D(0.500000005, -0.866025401, 0.0), -207682.301),
                            new LootVolumePlane(new Vector3D(0.866025401, 0.500000005, 0.0), 687670.5847),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 2),
                new LootLimit("Socket.LootContainer.Electrical", false, 2),
                new LootLimit("Socket.LootContainer.Residential", true, 0),
                new LootLimit("Socket.LootContainer.Commercial", true, 0),
                new LootLimit("Socket.LootContainer.Industrial", false, 2),
            ]),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "LootPlacementLimiter_14",
            "LootPlacementLimiter_SupportBuilding5",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(486728.45, 526865.44, -35964.61),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_113",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.866025406, -0.499999996, 0.0), -683936.2707),
                            new LootVolumePlane(new Vector3D(-0.499999996, 0.866025406, 0.0), 212574.9336),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 36858.5387),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -36041.4613),
                            new LootVolumePlane(new Vector3D(0.499999996, -0.866025406, 0.0), -211390.6243),
                            new LootVolumePlane(new Vector3D(0.866025406, 0.499999996, 0.0), 685894.2331),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 2),
                new LootLimit("Socket.LootContainer.Electrical", false, 2),
                new LootLimit("Socket.LootContainer.Residential", true, 0),
                new LootLimit("Socket.LootContainer.Commercial", true, 0),
                new LootLimit("Socket.LootContainer.Industrial", false, 2),
                new LootLimit("Socket.LootContainer.Tech", false, 2),
            ]),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "LootPlacementLimiter_21",
            "LootPlacementLimiter_GateHouse",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(496950.92, 531568.64, -33632.73),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "GateControlHouse",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.500002221, 0.866024121, 0.0), 213366.4185),
                            new LootVolumePlane(new Vector3D(-0.500002208, 0.866024129, 0.0), 213366.4293),
                            new LootVolumePlane(new Vector3D(-0.9701425, 0.242535625, 0.0), -349592.957),
                            new LootVolumePlane(new Vector3D(-0.241763093, 0.41874348, -0.875331083), 142972.5774),
                            new LootVolumePlane(new Vector3D(-0.143883615, 0.249209145, -0.957701575), 104948.4377),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -33136.4766),
                            new LootVolumePlane(new Vector3D(-0.866022549, -0.500004944, 7.259e-06), -692598.2792),
                            new LootVolumePlane(new Vector3D(0.500007834, -0.866020881, 6.1e-08), -208687.3957),
                            new LootVolumePlane(new Vector3D(0.500007692, -0.866020963, 6.1e-08), -208687.5095),
                            new LootVolumePlane(new Vector3D(-0.866012985, -0.500021509, 1.3e-08), -692602.0306),
                            new LootVolumePlane(new Vector3D(0.500007692, -0.866020963, -4.3e-08), -208687.5061),
                            new LootVolumePlane(new Vector3D(0.500007834, -0.866020881, -4.3e-08), -208687.391),
                            new LootVolumePlane(new Vector3D(6.006e-06, -3.407e-06, -1.0), 45475.184),
                            new LootVolumePlane(new Vector3D(5.2e-08, -9e-08, -1.0), 45473.9821),
                            new LootVolumePlane(new Vector3D(0.866024654, 0.500001299, -1.7e-08), 700414.2767),
                            new LootVolumePlane(new Vector3D(0.866024709, 0.500001204, -1.7e-08), 700414.2531),
                            new LootVolumePlane(new Vector3D(0.866024654, 0.500001299, 2.5e-08), 700414.2748),
                            new LootVolumePlane(new Vector3D(0.276372469, 0.159564272, -0.947711718), 266617.8281),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.LootContainer", false, 10),
                new LootLimit("Socket.LootContainer.Electrical.Transformer.PanelMain", true, 0),
                new LootLimit("Socket.LootContainer.Electrical.Transformer.PanelLarge", false, 1),
                new LootLimit("Socket.LootContainer.Electrical.Transformer.PanelSmall", false, 1),
            ]),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "LootPlacementLimiter_23",
            "LootPlacementLimiter_SpillwayTowers",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(-301.72, -175.83, -128.72),
            new Rotator(0.0, -30.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "SpillwayLanes_0",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.86602186, -0.500006138, 0.0), -693092.1189),
                            new LootVolumePlane(new Vector3D(-0.499994482, 0.86602859, 0.0), 247331.4255),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 39976.9609),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -34976.9609),
                            new LootVolumePlane(new Vector3D(-0.688368945, -0.725360734, 0.0), -730435.8521),
                            new LootVolumePlane(new Vector3D(0.49999121, -0.866030478, -2.9109e-05), -231991.6951),
                            new LootVolumePlane(new Vector3D(0.866019623, 0.500010012, 0.0), 696267.7534),
                            new LootVolumePlane(new Vector3D(0.866022615, 0.50000483, 1.8359e-05), 696265.6313),
                            new LootVolumePlane(new Vector3D(0.500043221, -0.866000449, 9.022e-06), -231951.4033),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.LootContainer.Military.WeaponLocker", false, 2),
            ]),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "LootPlacementLimiter_4",
            "LootPlacementLimiter_SupportBuilding",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(489724.69, 510386.87, -36126.58),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_181",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(0.499999989, -0.86602541, 0.0), -196304.8871),
                            new LootVolumePlane(new Vector3D(-0.86602541, -0.499999989, 0.0), -678628.1097),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 36848.0381),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -35948.0381),
                            new LootVolumePlane(new Vector3D(0.86602541, 0.499999989, 0.0), 679778.1097),
                            new LootVolumePlane(new Vector3D(-0.499999989, 0.86602541, 0.0), 198079.8871),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 2),
                new LootLimit("Socket.LootContainer.Commercial", false, 1),
                new LootLimit("Socket.LootContainer.Electrical.Transformer.PanelSmall", false, 1),
                new LootLimit("Socket.LootContainer.Industrial.Wrh.ToolTrolley.Tray", false, 1),
                new LootLimit("Socket.LootContainer.Commercial.Wrh.FilingCabinet.Drawer", false, 1),
            ]),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "LootPlacementLimiter_8",
            "LootPlacementLimiter_SupportBuilding",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(483236.7, 511190.57, -36902.42),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_123",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.866025406, -0.499999996, 0.0), -673551.5136),
                            new LootVolumePlane(new Vector3D(-0.499999996, 0.866025406, 0.0), 201571.5518),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 37620.3757),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -36854.6243),
                            new LootVolumePlane(new Vector3D(0.499999996, -0.866025406, 0.0), -200505.3429),
                            new LootVolumePlane(new Vector3D(0.866025406, 0.499999996, 0.0), 675082.0693),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 2),
                new LootLimit("Socket.LootContainer.Electrical", false, 2),
                new LootLimit("Socket.LootContainer.Residential", true, 0),
                new LootLimit("Socket.LootContainer.Commercial", true, 0),
                new LootLimit("Socket.LootContainer.Industrial", false, 2),
            ]),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "LootPlacementLimiter_9",
            "LootPlacementLimiter_Hydrodomes",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(3161.41, 587.6, -7720.29),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "Campsite5",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.281176064, 0.959656199, 0.0), 386265.2921),
                            new LootVolumePlane(new Vector3D(0.959206321, 0.282706976, 0.0), 600845.9515),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 41031.9551),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -34935.4824),
                            new LootVolumePlane(new Vector3D(-0.959206271, -0.282707143, 0.0), -585375.6701),
                            new LootVolumePlane(new Vector3D(0.281175943, -0.959656235, 0.0), -368389.4854),
                        ]),
                    ]),
                new LootLimiterVolume(
                    "Campsite5_0",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.280581893, 0.95983009, 0.0), 391256.8998),
                            new LootVolumePlane(new Vector3D(0.958936406, 0.283621171, 0.0), 574368.3531),
                            new LootVolumePlane(new Vector3D(-2.851e-06, 1.605e-06, -1.0), 41340.3403),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 41340.7345),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -38292.4845),
                            new LootVolumePlane(new Vector3D(-0.958936735, -0.283620059, 0.0), -571377.2766),
                            new LootVolumePlane(new Vector3D(0.300720169, -0.95371242, 0.0), -376197.4175),
                            new LootVolumePlane(new Vector3D(0.280582659, -0.959829866, -1.96e-07), -388345.1939),
                            new LootVolumePlane(new Vector3D(0.279515225, -0.956178338, 0.087144857), -390204.7942),
                            new LootVolumePlane(new Vector3D(0.280582467, -0.959829922, 0.0), -388345.316),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 30),
                new LootLimit("Socket.LootContainer.Nature.Fertilizer", false, 20),
                new LootLimit("Socket.LootContainer.Industrial.Crate.TopLid", false, 10),
            ]),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "LootPlacementLimiter_UAID_BCFCE753915FADBD02_2068511553",
            "LootPlacementLimiter",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(232.87, -235.25, 41.77),
            new Rotator(0.0, -30.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_UAID_ECE7A707B181FFB902_1469203103",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.866025406, -0.499999996, 0.0), -685247.0264),
                            new LootVolumePlane(new Vector3D(-0.499999996, 0.866025406, 0.0), 212259.4232),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 38579.3702),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -38075.2435),
                            new LootVolumePlane(new Vector3D(0.499999996, -0.866025406, 0.0), -211145.1735),
                            new LootVolumePlane(new Vector3D(0.866025406, 0.499999996, 0.0), 686344.4608),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.LootContainer.Commercial", false, 6),
            ]),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "LootPlacementLimiter_15",
            "LootPlacementLimiter_SupportBuilding6",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(467921.25, 556490.7, -35797.42),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_128",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.866025406, -0.499999996, 0.0), -682907.1815),
                            new LootVolumePlane(new Vector3D(-0.499999996, 0.866025406, 0.0), 248474.6145),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 36775.0),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -36062.5),
                            new LootVolumePlane(new Vector3D(0.499999996, -0.866025406, 0.0), -247146.2326),
                            new LootVolumePlane(new Vector3D(0.866025406, 0.499999996, 0.0), 685010.4792),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 2),
                new LootLimit("Socket.LootContainer.Electrical", false, 2),
                new LootLimit("Socket.LootContainer.Residential", true, 0),
                new LootLimit("Socket.LootContainer.Commercial", true, 0),
                new LootLimit("Socket.LootContainer.Industrial", false, 2),
                new LootLimit("Socket.LootContainer.Tech", false, 2),
            ]),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "LootPlacementLimiter_16",
            "LootPlacementLimiter_SupportBuilding7",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(466265.44, 578356.73, -35375.73),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_133",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(0.499999989, -0.86602541, 0.0), -266682.5833),
                            new LootVolumePlane(new Vector3D(-0.86602541, -0.499999989, 0.0), -692216.0),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 36889.1496),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -35808.6462),
                            new LootVolumePlane(new Vector3D(0.86602541, 0.499999989, 0.0), 693991.0),
                            new LootVolumePlane(new Vector3D(-0.499999989, 0.86602541, 0.0), 268257.5833),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 2),
                new LootLimit("Socket.LootContainer.Electrical", false, 2),
                new LootLimit("Socket.LootContainer.Residential", true, 0),
                new LootLimit("Socket.LootContainer.Commercial", true, 0),
                new LootLimit("Socket.LootContainer.Industrial", false, 2),
                new LootLimit("Socket.LootContainer.Tech", false, 2),
            ]),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "LootPlacementLimiter_2",
            "LootPlacementLimiter_LoadingBay",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(472365.68, 563584.29, -34536.47),
            new Rotator(0.0, 0.0, 2.88),
            1933.609024,
            [
                new LootLimiterVolume(
                    "LoadingBay",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.866025406, -0.499999996, 0.0), -687514.6552),
                            new LootVolumePlane(new Vector3D(-0.499999996, 0.866025406, 0.0), 264661.2734),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 38096.9626),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -36928.0374),
                            new LootVolumePlane(new Vector3D(0.499999996, -0.866025406, 0.0), -246969.6172),
                            new LootVolumePlane(new Vector3D(0.866025406, 0.499999996, 0.0), 692514.6552),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.LootContainer.Tech.Computer01.Screen", true, 0),
                new LootLimit("Socket.LootContainer.Commercial.Wrh.FilingCabinet.Drawer", false, 5),
            ]),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "LootPlacementLimiter_24",
            "LootPlacementLimiter_SupportBuilding",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(484606.57, 567232.35, -39899.16),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_122",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.866025406, -0.499999996, 0.0), -700082.9662),
                            new LootVolumePlane(new Vector3D(-0.499999996, 0.866025406, 0.0), 253914.6835),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 41300.0),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -39825.0),
                            new LootVolumePlane(new Vector3D(0.499999996, -0.866025406, 0.0), -247521.7036),
                            new LootVolumePlane(new Vector3D(0.866025406, 0.499999996, 0.0), 704493.7417),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 2),
                new LootLimit("Socket.LootContainer.Electrical", false, 2),
                new LootLimit("Socket.LootContainer.Tech.Server.Door", false, 2),
            ]),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "LootPlacementLimiter_26",
            "LootPlacementLimiter_ScrapYard",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(466487.97, 598588.42, -35091.68),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_87",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.766044447, 0.642787606, 0.0), 35924.6479),
                            new LootVolumePlane(new Vector3D(0.642787606, 0.766044447, 0.0), 762826.379),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 40950.0),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -36700.0),
                            new LootVolumePlane(new Vector3D(-0.642787606, -0.766044447, 0.0), -753576.379),
                            new LootVolumePlane(new Vector3D(0.766044447, -0.642787606, 0.0), -21140.9183),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 10),
                new LootLimit("Socket.LootContainer.Tech.Server.Door", false, 2),
                new LootLimit("Socket.LootContainer.Mechanical.UtilityTruck.Hatch", false, 3),
            ]),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "LootPlacementLimiter_28",
            "LootPlacementLimiter_MainControl",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(-446.75, 1.52, -21.86),
            new Rotator(0.0, -30.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "GeneratorControlTower",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.857967589, -0.495356926, 0.136062975), -691802.2712),
                            new LootVolumePlane(new Vector3D(-0.857967553, -0.495356987, 0.136062975), -691802.2891),
                            new LootVolumePlane(new Vector3D(-0.50000696, 0.866021386, 2.9e-08), 251794.0381),
                            new LootVolumePlane(new Vector3D(-0.500006996, 0.866021365, 3.5e-08), 251794.0091),
                            new LootVolumePlane(new Vector3D(-0.857967557, -0.49535699, 0.13606294), -691802.2912),
                            new LootVolumePlane(new Vector3D(0.005224975, 0.003016697, -0.999981799), 38338.212),
                            new LootVolumePlane(new Vector3D(-0.857967593, -0.495356928, 0.13606294), -691802.2736),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -25605.6133),
                            new LootVolumePlane(new Vector3D(0.500006943, -0.866021395, -3e-09), -247478.3635),
                            new LootVolumePlane(new Vector3D(0.500006948, -0.866021393, -4e-09), -247478.3602),
                            new LootVolumePlane(new Vector3D(0.866021429, 0.500006885, 0.0), 700695.3834),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 6),
                new LootLimit("Socket.LootContainer.Industrial.Lockers.Door", false, 6),
            ]),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "LootPlacementLimiter_3",
            "LootPlacementLimiter_Administration",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(3044.68, 501.84, -2745.25),
            new Rotator(0.0, -30.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "GeneratorAdministrationBuilding",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.866021427, -0.500006889, 5.14e-07), -692747.1332),
                            new LootVolumePlane(new Vector3D(-0.866021443, -0.500006859, 5.14e-07), -692747.1244),
                            new LootVolumePlane(new Vector3D(-0.500007206, 0.866021243, 8.351e-06), 259817.9004),
                            new LootVolumePlane(new Vector3D(-0.500007213, 0.866021239, 8.351e-06), 259817.8946),
                            new LootVolumePlane(new Vector3D(8.7e-07, 5.03e-07, -1.0), 45348.5),
                            new LootVolumePlane(new Vector3D(-7.75e-07, -4.48e-07, 1.0), -34148.0431),
                            new LootVolumePlane(new Vector3D(0.500007766, -0.86602092, -8.373e-06), -247303.3448),
                            new LootVolumePlane(new Vector3D(0.500007777, -0.866020914, -8.373e-06), -247303.3361),
                            new LootVolumePlane(new Vector3D(0.866021382, 0.500006966, -4.17e-07), 700520.3355),
                            new LootVolumePlane(new Vector3D(0.866021387, 0.500006958, -4.17e-07), 700520.3329),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 10),
                new LootLimit("Socket.LootContainer.Electrical", false, 15),
                new LootLimit("Socket.LootContainer.Medical", false, 20),
                new LootLimit("Socket.LootContainer.Tech.Computer02", false, 15),
                new LootLimit("Socket.LootContainer.Residential.Wrh.Desk", false, 8),
                new LootLimit("Socket.LootContainer.Industrial.Lockers.Door", false, 18),
                new LootLimit("Socket.LootContainer.Commercial.GvtDesk.Drawer", false, 15),
            ]),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "LootPlacementLimiter_31",
            "LootPlacementLimiter_SecureRoomA",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(-15.03, -57.54, -49.17),
            new Rotator(0.0, -30.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_13",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(0.885647716, 0.464357753, 0.0), 715052.7693),
                            new LootVolumePlane(new Vector3D(1.1805e-05, 6.189e-06, 1.0), -40762.3851),
                            new LootVolumePlane(new Vector3D(-0.560149977, 0.828391214, 0.0), 212771.9316),
                            new LootVolumePlane(new Vector3D(-0.075482789, 0.111629532, 0.990879002), -11728.0376),
                            new LootVolumePlane(new Vector3D(0.463447085, -0.886124596, 0.0), -293713.7706),
                            new LootVolumePlane(new Vector3D(8.718e-06, -1.6669e-05, 1.0), -40777.4411),
                            new LootVolumePlane(new Vector3D(-0.289333031, -0.957228498, 0.0), -709775.7125),
                            new LootVolumePlane(new Vector3D(-0.121962428, -0.403500118, 0.906814656), -336164.0226),
                            new LootVolumePlane(new Vector3D(-0.560232606, 0.828335271, -0.000325742), 212711.1789),
                            new LootVolumePlane(new Vector3D(-0.289388554, -0.957211695, -0.000190484), -709785.5036),
                            new LootVolumePlane(new Vector3D(4e-09, -5e-09, -1.0), 41259.9129),
                            new LootVolumePlane(new Vector3D(3e-09, -6e-09, -1.0), 41259.9124),
                            new LootVolumePlane(new Vector3D(4e-09, -7e-09, -1.0), 41259.9121),
                        ]),
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.502949237, 0.864315952, 0.0), 262443.8611),
                            new LootVolumePlane(new Vector3D(-0.499806451, 0.858915088, 0.111617129), 256253.0801),
                            new LootVolumePlane(new Vector3D(-0.502947236, 0.864317116, -5.97e-06), 262445.7872),
                            new LootVolumePlane(new Vector3D(-0.50177249, 0.864999635, 5.11e-07), 263432.8977),
                            new LootVolumePlane(new Vector3D(-0.289971345, -0.957035328, 0.0), -709977.5652),
                            new LootVolumePlane(new Vector3D(-0.121882593, -0.402267153, 0.907373005), -335417.6048),
                            new LootVolumePlane(new Vector3D(-0.289159802, -0.957280822, -0.000190347), -709713.1131),
                            new LootVolumePlane(new Vector3D(-0.10400904, -0.994576349, -7.8899e-05), -640080.4673),
                            new LootVolumePlane(new Vector3D(-0.000138716, 0.000205096, 0.999999969), -40719.2309),
                            new LootVolumePlane(new Vector3D(-0.000138716, 0.000205098, 0.999999969), -40719.2293),
                            new LootVolumePlane(new Vector3D(-0.001899703, 0.00323136, 0.999992975), -39800.0356),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 41259.9143),
                            new LootVolumePlane(new Vector3D(0.560208538, -0.828351613, 7.789e-06), -212719.6526),
                            new LootVolumePlane(new Vector3D(0.560233903, -0.828334454, 8.2386e-05), -212699.9759),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -40771.8949),
                        ]),
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.504788541, 0.86324303, 3.1e-08), 260896.4603),
                            new LootVolumePlane(new Vector3D(-0.009760009, 0.016690672, 0.999813064), -35719.9011),
                            new LootVolumePlane(new Vector3D(-0.504788541, 0.86324303, 0.0), 260896.4616),
                            new LootVolumePlane(new Vector3D(-0.504784324, 0.863245496, -5.34e-06), 260900.2336),
                            new LootVolumePlane(new Vector3D(-0.504787207, 0.86324381, -1.668e-06), 260897.654),
                            new LootVolumePlane(new Vector3D(-0.503435432, 0.86403285, 0.0), 262035.9532),
                            new LootVolumePlane(new Vector3D(-0.50478427, 0.863245527, -5.34e-06), 260900.2783),
                            new LootVolumePlane(new Vector3D(-0.009529219, 0.016354745, 0.999820842), -35804.6945),
                            new LootVolumePlane(new Vector3D(0.502947212, -0.864317129, 4.247e-05), -262447.2922),
                            new LootVolumePlane(new Vector3D(-0.009782621, 0.01672953, 0.999812194), -35708.0664),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 41259.9143),
                            new LootVolumePlane(new Vector3D(0.502972315, -0.864302522, 1.1432e-05), -262424.9142),
                            new LootVolumePlane(new Vector3D(0.562613985, -0.826719725, 0.0), -210558.7107),
                            new LootVolumePlane(new Vector3D(0.560093471, -0.828429414, 9.9512e-05), -212826.6373),
                            new LootVolumePlane(new Vector3D(0.509733619, -0.860332283, 1.1518e-05), -256717.158),
                        ]),
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -40771.916),
                            new LootVolumePlane(new Vector3D(0.511266525, -0.859422213, 0.0), -254618.2386),
                            new LootVolumePlane(new Vector3D(0.513545189, -0.85806255, -5e-09), -252685.4505),
                            new LootVolumePlane(new Vector3D(0.513545189, -0.85806255, 0.0), -252685.4507),
                            new LootVolumePlane(new Vector3D(0.513545189, -0.85806255, 5e-09), -252685.4509),
                            new LootVolumePlane(new Vector3D(0.513545186, -0.858062551, -5e-09), -252685.4527),
                            new LootVolumePlane(new Vector3D(0.513544837, -0.85806276, -1.5e-08), -252685.7485),
                            new LootVolumePlane(new Vector3D(0.513545192, -0.858062548, 5e-09), -252685.4487),
                            new LootVolumePlane(new Vector3D(0.511301784, -0.859401236, -3.4069e-05), -254586.9791),
                            new LootVolumePlane(new Vector3D(0.513544837, -0.85806276, 0.0), -252685.7491),
                            new LootVolumePlane(new Vector3D(-0.865060501, -0.501667548, 0.0), -724773.3675),
                            new LootVolumePlane(new Vector3D(-0.974671325, 0.223642143, 0.0), -350247.778),
                            new LootVolumePlane(new Vector3D(-0.502947028, 0.864317237, 0.0), 262445.7157),
                            new LootVolumePlane(new Vector3D(0.289373886, 0.957216148, 0.0), 709788.7086),
                            new LootVolumePlane(new Vector3D(0.513545186, -0.858062551, 0.0), -252685.4529),
                            new LootVolumePlane(new Vector3D(0.513545192, -0.858062548, -1e-08), -252685.448),
                            new LootVolumePlane(new Vector3D(0.513545192, -0.858062548, 0.0), -252685.4485),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 41259.9143),
                            new LootVolumePlane(new Vector3D(0.513545186, -0.858062551, -1.1e-08), -252685.4525),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 6),
                new LootLimit("Socket.LootContainer.Medical", false, 2),
            ]),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "LootPlacementLimiter_32",
            "LootPlacementLimiter_SecureRoomB",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(-303.9, -495.49, -49.17),
            new Rotator(0.0, -30.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_15",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.868744976, -0.495259696, 0.0), -725040.1205),
                            new LootVolumePlane(new Vector3D(-0.495259696, 0.868744976, 0.0), 265873.1117),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 41350.1918),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -40849.8079),
                            new LootVolumePlane(new Vector3D(0.495259696, -0.868744976, 0.0), -264477.655),
                            new LootVolumePlane(new Vector3D(0.868744976, 0.495259696, 0.0), 726020.1045),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.LootContainer.Tech.Server.Door", false, 4),
            ]),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "LootPlacementLimiter_6",
            "LootPlacementLimiter_SupportBuilding",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(487866.35, 588498.22, -41066.2),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_134",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(0.984807749, -0.173648202, 0.0), 378988.603),
                            new LootVolumePlane(new Vector3D(-0.173648202, -0.984807749, 0.0), -663293.5512),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 41642.6894),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -40562.186),
                            new LootVolumePlane(new Vector3D(0.173648202, 0.984807749, 0.0), 665068.5512),
                            new LootVolumePlane(new Vector3D(-0.984807749, 0.173648202, 0.0), -377413.603),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 2),
                new LootLimit("Socket.LootContainer.Tech", true, 0),
                new LootLimit("Socket.LootContainer.Electrical", true, 0),
                new LootLimit("Socket.LootContainer.Residential", false, 2),
                new LootLimit("Socket.LootContainer.Commercial", true, 0),
                new LootLimit("Socket.LootContainer.Industrial", false, 2),
                new LootLimit("Socket.LootContainer.Industrial.Lockers.Door", false, 1),
            ]),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "LootPlacementLimiter_7",
            "LootPlacementLimiter_ResearchCenter",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(60.62, -1744.78, -2227.09),
            new Rotator(0.0, -30.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "OutfallObservation_8",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.866022011, -0.500005873, 5.7897e-05), -721846.7575),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -38394.0228),
                            new LootVolumePlane(new Vector3D(-0.866023798, -0.500002781, 3.307e-06), -721843.7059),
                            new LootVolumePlane(new Vector3D(-0.866024225, -0.500002042, 0.0), -721843.3503),
                            new LootVolumePlane(new Vector3D(-0.500005926, 0.866021982, 0.0), 267949.5761),
                            new LootVolumePlane(new Vector3D(0.224320606, -0.9745154, -3.5632e-05), -456232.2645),
                            new LootVolumePlane(new Vector3D(0.500005933, -0.866021978, 0.0), -255969.0241),
                            new LootVolumePlane(new Vector3D(0.500005933, -0.866021978, 1.138e-06), -255969.0678),
                            new LootVolumePlane(new Vector3D(-0.015774473, -0.744985169, -0.666894493), -415744.1952),
                            new LootVolumePlane(new Vector3D(0.071402545, -0.720222264, -0.690059104), -357069.9486),
                            new LootVolumePlane(new Vector3D(-0.270510629, -0.156179949, -0.949964117), -186530.2572),
                            new LootVolumePlane(new Vector3D(-0.270502733, -0.156177275, -0.949966805), -186524.667),
                            new LootVolumePlane(new Vector3D(0.043682555, -0.559799901, -0.827475622), -271464.5768),
                            new LootVolumePlane(new Vector3D(-0.251151884, -0.089699289, -0.963782532), -137034.1002),
                            new LootVolumePlane(new Vector3D(-0.328130678, 0.568330025, -0.754540417), 206775.4126),
                            new LootVolumePlane(new Vector3D(-0.227464723, -0.131328879, -0.964889903), -149957.6788),
                            new LootVolumePlane(new Vector3D(0.17158352, -0.683467005, -0.709529386), -285088.1218),
                            new LootVolumePlane(new Vector3D(0.866021996, 0.500005902, 0.0), 728521.3789),
                            new LootVolumePlane(new Vector3D(0.609470398, 0.351883436, -0.710439217), 541827.767),
                            new LootVolumePlane(new Vector3D(0.609470359, 0.351883414, -0.710439262), 541827.7361),
                            new LootVolumePlane(new Vector3D(0.609470414, 0.351883427, -0.710439208), 541827.7691),
                            new LootVolumePlane(new Vector3D(0.500006049, -0.866021911, 0.0), -255968.9267),
                            new LootVolumePlane(new Vector3D(0.609470358, 0.351883394, -0.710439272), 541827.7245),
                            new LootVolumePlane(new Vector3D(0.171719723, -0.683602242, -0.709366133), -285106.2432),
                            new LootVolumePlane(new Vector3D(0.171720311, -0.683604584, -0.709363734), -285107.4179),
                        ]),
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.866010289, -0.500026178, -2.4965e-05), -721849.5134),
                            new LootVolumePlane(new Vector3D(0.500005933, -0.866021978, 2.3721e-05), -255969.9965),
                            new LootVolumePlane(new Vector3D(-0.171715146, 0.683602366, 0.709367122), 285108.5768),
                            new LootVolumePlane(new Vector3D(-0.270498018, -0.15618452, -0.949966956), -186526.5536),
                            new LootVolumePlane(new Vector3D(0.120732655, -0.209111784, -0.970410165), -22024.7358),
                            new LootVolumePlane(new Vector3D(-0.171716141, 0.683606327, 0.709363064), 285110.5638),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 10),
                new LootLimit("Socket.Loot.Medical", false, 3),
                new LootLimit("Socket.LootContainer.Tech", false, 8),
                new LootLimit("Socket.LootContainer.Electrical", false, 5),
                new LootLimit("Socket.LootCOntainer.Medical.RsrCartShelf", false, 5),
                new LootLimit("Socket.LootContainer.Medical.Rsr", false, 10),
                new LootLimit("Socket.LootContainer.Commercial.GvtDesk.Drawer", false, 3),
                new LootLimit("Socket.LootContainer.Industrial.Lockers.Door", false, 7),
            ]),
        new(
            "8635EV0VJ3V10LNUJ9RO28RQ9",
            "LootPlacementLimiter_UAID_7486E223302E0A6102_1842779785",
            "LootPlacementLimiter_ApartmentComplex3",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(489431.88, 490262.4, -33075.0),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_86",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.866025404, -0.499999999, 0.0), -671304.053),
                            new LootVolumePlane(new Vector3D(-0.499999999, 0.866025404, 0.0), 189941.9032),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 37247.4382),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -31727.5618),
                            new LootVolumePlane(new Vector3D(0.499999999, -0.866025404, 0.0), -184404.6123),
                            new LootVolumePlane(new Vector3D(0.866025404, 0.499999999, 0.0), 676497.3792),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 10),
                new LootLimit("Socket.LootContainer.Electrical", false, 5),
                new LootLimit("Socket.LootContainer.Residential.CupboardKitchen", false, 3),
                new LootLimit("Socket.LootContainer.Residential.Closet", false, 2),
                new LootLimit("Socket.LootContainer.Residential.KitchenBench", false, 3),
                new LootLimit("Socket.LootContainer.Residential.DrawerMedical.lid1", false, 4),
                new LootLimit("Socket.LootContainer.Residential.DrawerMedical.lid2", false, 4),
                new LootLimit("Socket.LootContainer.Residential.Wrh.Desk.DrawerSmall", false, 3),
            ]),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "LootPlacementLimiter_29",
            "LootPlacementLimiter_ApartmentComplex2",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(440350.0, 504725.0, -33075.0),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_75",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.906307787, 0.422618262, 0.0), -180580.0985),
                            new LootVolumePlane(new Vector3D(0.422618262, 0.906307787, 0.0), 650515.659),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 39387.5),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -35287.5),
                            new LootVolumePlane(new Vector3D(-0.422618262, -0.906307787, 0.0), -639065.659),
                            new LootVolumePlane(new Vector3D(0.906307787, -0.422618262, 0.0), 189630.0985),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 15),
                new LootLimit("Socket.LootContainer.Electrical", false, 12),
                new LootLimit("Socket.LootContainer.Residential.CupboardKitchen", false, 5),
                new LootLimit("Socket.LootContainer.Residential.Closet", false, 5),
                new LootLimit("Socket.LootContainer.Residential.KitchenBench", false, 8),
                new LootLimit("Socket.LootContainer.Residential.DrawerMedical.lid1", false, 4),
                new LootLimit("Socket.LootContainer.Residential.DrawerMedical.lid2", false, 4),
                new LootLimit("Socket.LootContainer.Residential.Wrh.Desk.DrawerSmall", false, 3),
            ]),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "LootPlacementLimiter_30",
            "LootPlacementLimiter_ApartmentComplex1",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(418125.0, 515212.5, -33075.0),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_74",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.866025406, 0.499999996, 0.0), -103918.9235),
                            new LootVolumePlane(new Vector3D(0.499999996, 0.866025406, 0.0), 663176.5194),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 40075.0),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -35975.0),
                            new LootVolumePlane(new Vector3D(-0.499999996, -0.866025406, 0.0), -655026.5194),
                            new LootVolumePlane(new Vector3D(0.866025406, -0.499999996, 0.0), 113175.7213),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 15),
                new LootLimit("Socket.LootContainer.Electrical", false, 12),
                new LootLimit("Socket.LootContainer.Residential.Refrigerator_01", true, 0),
                new LootLimit("Socket.LootContainer.Residential.Closet", false, 4),
                new LootLimit("Socket.LootContainer.Residential.CupboardKitchen", false, 5),
                new LootLimit("Socket.LootContainer.Residential.KitchenBench", false, 4),
                new LootLimit("Socket.LootContainer.Residential.DrawerMedical.lid1", false, 4),
                new LootLimit("Socket.LootContainer.Residential.Wrh.Desk.DrawerSmall", false, 2),
            ]),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "LootPlacementLimiter_33",
            "LootPlacementLimiter_ApartmentBasement",
            null,
            LootLimiterShape.Sphere,
            new Vector3D(427500.94, 515418.93, -38257.97),
            new Rotator(0.0, 0.0, 0.0),
            500.0,
            [],
            [
                new LootLimit("Socket.Loot", false, 8),
            ]),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "LootPlacementLimiter_34",
            "LootPlacementLimiter_ApartmentBasement2",
            null,
            LootLimiterShape.Sphere,
            new Vector3D(421719.58, 520104.04, -38908.33),
            new Rotator(0.0, 0.0, 0.0),
            500.0,
            [],
            [
                new LootLimit("Socket.Loot", false, 8),
            ]),
        new(
            "CAMP3HBSEA6OHYXJ752UWG3X9",
            "LootPlacementLimiter_17",
            "LootPlacementLimiter_CliffsideOutpost",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(3434.95, -8081.92, -137.88),
            new Rotator(0.0, -30.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_92",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.866025406, 0.499999996, 0.0), -93544.8902),
                            new LootVolumePlane(new Vector3D(0.499999996, 0.866025406, 0.0), 768825.3446),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 32551.3954),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -28648.6046),
                            new LootVolumePlane(new Vector3D(-0.499999996, -0.866025406, 0.0), -764362.3269),
                            new LootVolumePlane(new Vector3D(0.866025406, -0.499999996, 0.0), 99324.8314),
                        ]),
                    ]),
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_93",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(0.258819035, 0.965925829, 0.0), 717497.8907),
                            new LootVolumePlane(new Vector3D(0.965925829, -0.258819035, 0.0), 300815.677),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 33675.0),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -32375.0),
                            new LootVolumePlane(new Vector3D(-0.965925829, 0.258819035, 0.0), -299515.677),
                            new LootVolumePlane(new Vector3D(-0.258819035, -0.965925829, 0.0), -714997.8907),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.LootContainer", false, 6),
                new LootLimit("Socket.Loot", false, 5),
                new LootLimit("Socket.LootContainer.Tech.Server.Door", false, 2),
            ]),
        new(
            "D7SLI18I6UYP3QP4VHBRYZMGH",
            "LootPlacementLimiter_18",
            "LootPlacementLimiter_RaiderOutpost",
            null,
            LootLimiterShape.Volumes,
            new Vector3D(517214.57, 515900.86, -33164.28),
            new Rotator(0.0, 0.0, 0.0),
            1000.0,
            [
                new LootLimiterVolume(
                    "PioneerPointOfInterestVolume_78",
                    [
                        new LootVolumeHull(
                        [
                            new LootVolumePlane(new Vector3D(-0.819152047, 0.573576432, 0.0), -125002.7854),
                            new LootVolumePlane(new Vector3D(0.573576432, 0.819152047, 0.0), 721226.3154),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, -1.0), 37750.0),
                            new LootVolumePlane(new Vector3D(0.0, 0.0, 1.0), -32300.0),
                            new LootVolumePlane(new Vector3D(-0.573576432, -0.819152047, 0.0), -717376.3154),
                            new LootVolumePlane(new Vector3D(0.819152047, -0.573576432, 0.0), 131102.7854),
                        ]),
                    ]),
            ],
            [
                new LootLimit("Socket.Loot", false, 5),
            ]),
        new(
            "D7SLI18I6UYP3QP4VHBRYZMGH",
            "LootPlacementLimiter_27",
            "LootPlacementLimiter_SupportBuilding",
            null,
            LootLimiterShape.Box,
            new Vector3D(516058.42, 524451.68, -37179.47),
            new Rotator(0.0, 0.0, 0.0),
            2000.0,
            [],
            [
                new LootLimit("Socket.Loot", false, 2),
                new LootLimit("Socket.LootContainer.Electrical", false, 2),
                new LootLimit("Socket.LootContainer.Industrial.Lockers.Door", false, 2),
            ]),
    ];
}
