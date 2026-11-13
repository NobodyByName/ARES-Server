using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>
/// Free-standing loot containers of the Dam map: the spawners the map places and the groups that pick which of
/// them run.
/// </summary>
public static class DamDynamicLoot
{
    /// <summary>All spawners, ordered by cell and actor.</summary>
    public static IReadOnlyList<DynamicLootSpawner> Spawners { get; } =
    [
        new(
            "",
            "ActorSpawner_100",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(501949.63, 534144.21, -43550.83), new Rotator(0.0, 136.022, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_101",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481960.81, 571339.47, -43212.16), new Rotator(0.0, 52.221, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_102",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(480591.01, 565907.89, -28435.4), new Rotator(0.0, -151.23, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(479602.49, 565207.2, -28492.08), new Rotator(0.0, -56.698, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(481033.39, 566230.37, -28358.0), new Rotator(-4.165, 118.77, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_103",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(445225.49, 601222.8, -33191.5), new Rotator(0.0, 41.242, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_104",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(478363.68, 571160.81, -35412.57), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_105",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(465523.8, 616005.39, -31759.68), new Rotator(0.0, -10.447, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_106",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(418209.42, 559907.33, -39962.56), new Rotator(0.0, -81.547, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_107",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(417264.25, 563021.7, -40595.57), new Rotator(0.0, -90.597, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_108",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(469566.84, 529111.56, -40770.25), new Rotator(0.0, 28.558, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_109",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(471841.54, 570263.99, -35412.57), new Rotator(0.0, -172.632, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_110",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(471278.95, 571188.4, -35412.57), new Rotator(0.0, 52.974, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_111",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(443692.87, 560984.65, -38998.77), new Rotator(0.0, 172.748, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_112",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(473154.44, 564385.42, -37799.05), new Rotator(0.0, 167.059, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_113",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(480831.97, 564100.24, -37488.65), new Rotator(0.0, -130.018, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_114",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(440165.65, 581529.07, -38488.26), new Rotator(0.0, -24.518, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_115",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(478829.31, 562456.59, -34400.0), new Rotator(0.0, 50.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_116",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(496444.94, 592457.41, -41122.11), new Rotator(-6.083, -66.221, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_117",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(498619.23, 590254.15, -41198.74), new Rotator(0.0, -86.847, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_118",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(471818.21, 571974.97, -35412.57), new Rotator(0.0, -75.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_119",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(471575.02, 570142.08, -35412.57), new Rotator(0.0, -180.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_120",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(444445.94, 562478.92, -38998.77), new Rotator(0.0, -90.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(444286.18, 562021.4, -38998.77), new Rotator(0.0, 75.091, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(444594.29, 561961.16, -38998.36), new Rotator(0.0, 63.762, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_121",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(444523.62, 561946.18, -38998.77), new Rotator(0.0, 170.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(444529.39, 562502.26, -38998.77), new Rotator(0.0, 62.933, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_122",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(505742.92, 547301.18, -44581.16), new Rotator(0.0, 105.0, 5.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_123",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(501672.42, 534277.31, -43596.5), new Rotator(0.0, 105.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_124",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(475555.79, 500005.92, -39050.48), new Rotator(0.0, 80.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_125",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(476420.43, 499926.52, -39000.31), new Rotator(3.405, -110.28, 0.592), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_126",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(499171.54, 590069.71, -40949.29), new Rotator(0.0, -58.242, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(498752.97, 589020.31, -41096.37), new Rotator(0.0, -149.641, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_127",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(495514.69, 508338.75, -36719.73), new Rotator(3.073, 30.638, 0.8), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_128",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(476751.95, 572992.45, -35412.57), new Rotator(0.0, -45.303, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_129",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(445546.07, 556258.72, -38875.39), new Rotator(10.598, 101.276, 2.861), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_130",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481205.74, 563986.76, -34400.0), new Rotator(0.0, 120.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_131",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(456502.46, 545020.74, -40347.43), new Rotator(0.0, -157.162, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_132",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481282.7, 549706.36, -38864.05), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_133",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(478969.99, 555142.57, -38239.15), new Rotator(0.0, 32.971, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_134",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(479852.52, 564622.0, -37598.76), new Rotator(0.0, 107.13, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_135",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481349.93, 580551.48, -39847.31), new Rotator(0.0, -120.333, 9.75), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_136",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(478703.69, 597997.92, -39083.34), new Rotator(0.0, 23.117, -11.49), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_137",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(478596.09, 597739.21, -39143.19), new Rotator(0.0, 80.725, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_138",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(452746.39, 610988.95, -36004.79), new Rotator(0.0, -110.976, 15.273), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_139",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(479484.72, 563965.82, -28599.95), new Rotator(0.0, 31.169, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(479846.18, 565976.18, -28430.88), new Rotator(0.0, 31.169, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_140",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(495160.96, 591417.69, -41200.0), new Rotator(0.0, 73.176, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(495957.57, 591394.29, -41200.0), new Rotator(0.0, -147.66, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_141",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(444200.99, 562167.35, -38925.0), new Rotator(0.0, -5.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_142",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(522937.28, 522514.88, -38095.12), new Rotator(-0.577, 108.386, -6.727), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_143",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(467309.51, 615552.48, -31557.79), new Rotator(0.0, -55.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(467478.16, 615651.69, -30888.77), new Rotator(0.0, -117.804, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(467397.49, 614358.98, -30972.03), new Rotator(15.0, -105.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(468563.69, 614854.99, -30918.3), new Rotator(0.0, 80.0, 35.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_144",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(445292.63, 601111.87, -33191.5), new Rotator(0.0, -1.935, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_145",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(480710.49, 564057.62, -28598.76), new Rotator(0.0, 117.753, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(481168.24, 566311.04, -28431.4), new Rotator(0.0, -160.19, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(479297.24, 563672.35, -28500.15), new Rotator(0.0, -120.786, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(479002.6, 563351.74, -28500.14), new Rotator(0.0, -165.046, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_146",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481347.84, 572385.25, -42680.07), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(481890.5, 573136.38, -43260.72), new Rotator(0.0, 30.0, -9.462), 1.0),
                new DynamicLootPoint(new Vector3D(481676.41, 572952.31, -43243.05), new Rotator(-2.119, -14.853, -8.362), 1.0),
                new DynamicLootPoint(new Vector3D(482568.31, 572584.38, -42450.8), new Rotator(0.0, -155.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(482397.87, 572886.57, -42700.8), new Rotator(0.0, -155.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(481629.81, 571906.12, -43164.82), new Rotator(-4.356, 54.693, 0.382), 1.0),
                new DynamicLootPoint(new Vector3D(482669.25, 572255.44, -43220.0), new Rotator(-1.086, 30.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(482051.62, 571715.13, -43272.91), new Rotator(-24.807, -3.199, 7.589), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_147",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(500678.13, 547797.35, -44215.64), new Rotator(-4.296, 154.423, -5.696), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_148",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(469749.58, 529771.0, -40712.31), new Rotator(0.0, -51.995, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_149",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(419280.12, 561385.69, -40919.26), new Rotator(-8.914, 87.936, -0.786), 1.0),
                new DynamicLootPoint(new Vector3D(418962.44, 560869.0, -40038.07), new Rotator(4.652, 137.567, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(418378.25, 561655.38, -40027.37), new Rotator(1.161, 97.325, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(418294.78, 561130.69, -40030.36), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(418079.0, 560671.56, -40007.45), new Rotator(1.399, 73.833, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(418084.16, 562193.88, -40896.71), new Rotator(-7.947, -2.996, -0.761), 1.0),
                new DynamicLootPoint(new Vector3D(417621.53, 562070.06, -39917.2), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(417764.03, 563165.56, -39992.65), new Rotator(12.229, 53.191, -0.394), 1.0),
                new DynamicLootPoint(new Vector3D(417406.75, 561790.25, -40695.57), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(417261.94, 561220.75, -40829.34), new Rotator(-0.969, 149.995, -0.56), 1.0),
                new DynamicLootPoint(new Vector3D(417685.59, 563758.81, -40976.69), new Rotator(5.687, -101.31, 1.313), 1.0),
                new DynamicLootPoint(new Vector3D(417037.38, 562427.81, -39920.96), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(416755.84, 563265.75, -40879.24), new Rotator(-4.436, 160.448, -4.499), 1.0),
                new DynamicLootPoint(new Vector3D(417104.19, 563132.88, -39132.41), new Rotator(0.59, -53.527, -91.677), 1.0),
                new DynamicLootPoint(new Vector3D(416438.84, 562510.38, -40734.64), new Rotator(-2.998, 171.938, 19.011), 1.0),
                new DynamicLootPoint(new Vector3D(416535.81, 562851.31, -39982.83), new Rotator(14.502, 140.737, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(417869.78, 562205.88, -40639.95), new Rotator(0.0, 156.64, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_150",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(479015.97, 569686.39, -35412.57), new Rotator(0.0, 62.759, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_151",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(444109.71, 560467.62, -38898.77), new Rotator(0.0, 154.866, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_152",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(510533.31, 521651.41, -36792.45), new Rotator(0.0, -50.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(511256.31, 520153.5, -36193.25), new Rotator(0.0, -20.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(511479.66, 519584.31, -36510.88), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(509983.09, 521204.84, -36792.45), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(510382.84, 521456.5, -36207.23), new Rotator(0.0, -55.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(511175.62, 519720.75, -36771.12), new Rotator(0.0, 119.97, 0.021), 1.0),
                new DynamicLootPoint(new Vector3D(510073.84, 520599.03, -36581.63), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(510569.28, 519638.63, -36799.88), new Rotator(0.0, -150.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(509366.0, 521227.16, -36792.45), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(510039.28, 519861.0, -36193.25), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(509514.22, 520259.91, -36207.24), new Rotator(0.0, 120.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(509206.66, 520625.12, -36152.23), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(509554.41, 519500.94, -36792.45), new Rotator(0.0, 55.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(509358.37, 519052.94, -36207.24), new Rotator(0.0, -145.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(508491.35, 520064.32, -36107.09), new Rotator(0.0, 125.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(508675.16, 519131.16, -36207.24), new Rotator(0.0, -65.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(509069.47, 518208.28, -36522.29), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_153",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(487288.8, 531963.07, -35992.93), new Rotator(0.0, -144.099, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_154",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(458603.73, 551355.15, -40878.05), new Rotator(-2.537, -93.188, 1.068), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_155",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(457679.44, 546634.81, -40900.08), new Rotator(-2.738, 75.038, -0.214), 1.0),
                new DynamicLootPoint(new Vector3D(457661.66, 546278.19, -39706.26), new Rotator(86.395, -60.0, -119.258), 1.0),
                new DynamicLootPoint(new Vector3D(458300.0, 545806.81, -40873.45), new Rotator(-1.118, 29.981, 2.904), 1.0),
                new DynamicLootPoint(new Vector3D(458400.75, 544590.38, -40700.91), new Rotator(2.904, -30.043, -1.826), 1.0),
                new DynamicLootPoint(new Vector3D(456019.94, 546053.69, -40797.7), new Rotator(-2.307, 21.951, 5.78), 1.0),
                new DynamicLootPoint(new Vector3D(456378.03, 545087.38, -39968.76), new Rotator(15.248, 40.953, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(456922.06, 544838.81, -40792.31), new Rotator(-3.341, 5.009, -0.293), 1.0),
                new DynamicLootPoint(new Vector3D(456246.28, 544554.94, -40286.72), new Rotator(69.966, -100.598, 12.436), 1.0),
                new DynamicLootPoint(new Vector3D(456539.84, 544054.0, -40864.86), new Rotator(-7.79, -145.835, 7.873), 1.0),
                new DynamicLootPoint(new Vector3D(456365.41, 544881.63, -40762.13), new Rotator(-12.128, -106.334, 2.044), 1.0),
                new DynamicLootPoint(new Vector3D(455792.94, 545100.25, -40768.4), new Rotator(10.888, 23.962, -0.339), 1.0),
                new DynamicLootPoint(new Vector3D(456574.78, 545152.25, -40353.73), new Rotator(0.0, -144.812, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(456664.25, 545212.25, -40353.73), new Rotator(0.0, 55.188, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_156",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(479661.13, 551064.31, -38954.71), new Rotator(3.534, -18.435, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(480578.56, 551295.56, -38908.71), new Rotator(7.377, 123.874, 9.205), 1.0),
                new DynamicLootPoint(new Vector3D(480991.81, 551358.06, -38858.21), new Rotator(-3.526, 18.544, 4.602), 1.0),
                new DynamicLootPoint(new Vector3D(479329.41, 550212.31, -38858.0), new Rotator(-7.101, 123.621, -0.939), 1.0),
                new DynamicLootPoint(new Vector3D(481883.91, 551205.25, -38808.77), new Rotator(-7.128, 128.66, -6.013), 1.0),
                new DynamicLootPoint(new Vector3D(480051.69, 550151.69, -38829.6), new Rotator(-4.83, 117.566, -10.924), 1.0),
                new DynamicLootPoint(new Vector3D(479065.0, 549564.62, -38841.32), new Rotator(-0.434, 126.556, 4.024), 1.0),
                new DynamicLootPoint(new Vector3D(480100.25, 549292.62, -38845.34), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(479695.0, 548902.19, -38891.24), new Rotator(0.0, -60.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(480700.0, 549396.62, -38843.89), new Rotator(3.009, -49.198, 2.787), 1.0),
                new DynamicLootPoint(new Vector3D(480177.22, 548584.25, -39049.67), new Rotator(0.0, -60.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(481191.84, 548888.06, -38907.77), new Rotator(-10.572, 177.632, 1.471), 1.0),
                new DynamicLootPoint(new Vector3D(482621.69, 549533.62, -38404.86), new Rotator(4.467, 90.0, -10.014), 1.0),
                new DynamicLootPoint(new Vector3D(481846.87, 548701.56, -38746.98), new Rotator(-17.638, -169.38, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(480527.94, 547890.0, -38950.91), new Rotator(-16.229, 121.377, 3.445), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_157",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(483668.51, 545018.2, -39310.17), new Rotator(-0.747, 121.364, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_158",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(479341.77, 564487.15, -37598.76), new Rotator(0.0, 108.663, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_159",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(484697.75, 580959.74, -39422.66), new Rotator(0.0, 178.745, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_160",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(468516.16, 593488.35, -39083.85), new Rotator(0.0, 19.105, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_161",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(453917.23, 609517.01, -36633.18), new Rotator(0.715, -99.698, -10.257), 1.0),
                new DynamicLootPoint(new Vector3D(453284.56, 609304.5, -36715.44), new Rotator(8.949, 60.255, 0.002), 1.0),
                new DynamicLootPoint(new Vector3D(452754.12, 609210.5, -36771.07), new Rotator(7.806, 12.792, -9.757), 1.0),
                new DynamicLootPoint(new Vector3D(454395.81, 610472.06, -36719.36), new Rotator(9.909, -153.435, -1.48), 1.0),
                new DynamicLootPoint(new Vector3D(453398.22, 610452.44, -36586.43), new Rotator(-8.347, -76.032, 6.176), 1.0),
                new DynamicLootPoint(new Vector3D(452288.84, 609424.88, -36752.16), new Rotator(-16.401, -92.382, -2.661), 1.0),
                new DynamicLootPoint(new Vector3D(454271.16, 611496.25, -36756.2), new Rotator(18.108, -166.107, 1.282), 1.0),
                new DynamicLootPoint(new Vector3D(452888.97, 611002.5, -36053.59), new Rotator(24.211, 177.51, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(452411.72, 610881.25, -36051.54), new Rotator(50.494, 75.069, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(451503.28, 610805.62, -36060.8), new Rotator(17.551, 171.049, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(451091.06, 610335.5, -36529.39), new Rotator(10.629, -17.34, -24.534), 1.0),
                new DynamicLootPoint(new Vector3D(453333.44, 612090.62, -36047.16), new Rotator(45.779, 171.254, -0.001), 1.0),
                new DynamicLootPoint(new Vector3D(452732.84, 611467.69, -35964.12), new Rotator(4.028, 123.69, -0.001), 1.0),
                new DynamicLootPoint(new Vector3D(452173.63, 611510.75, -35937.6), new Rotator(2.501, 63.435, -0.001), 1.0),
                new DynamicLootPoint(new Vector3D(451482.09, 611367.28, -35876.58), new Rotator(16.191, 132.274, -0.002), 1.0),
                new DynamicLootPoint(new Vector3D(452548.44, 612239.75, -35940.34), new Rotator(-3.353, 0.0, -0.002), 1.0),
                new DynamicLootPoint(new Vector3D(451089.31, 612018.12, -35919.16), new Rotator(2.501, 153.435, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(452686.51, 610817.96, -36017.56), new Rotator(0.0, 118.133, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_162",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(480265.97, 565716.16, -28437.87), new Rotator(0.0, 28.86, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_163",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(505789.51, 547402.55, -44627.86), new Rotator(4.698, 69.93, -1.714), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_164",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(502406.27, 532988.35, -43500.88), new Rotator(0.0, -170.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_165",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(468111.97, 615619.87, -30890.63), new Rotator(0.0, -110.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_166",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(466613.94, 615516.68, -31668.42), new Rotator(2.405, 71.544, 2.77), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_167",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(446698.99, 599710.64, -32992.88), new Rotator(0.0, -165.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_168",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(446652.12, 599826.57, -32992.88), new Rotator(0.0, -140.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_169",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(418134.0, 559698.23, -39976.3), new Rotator(0.0, 4.143, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_170",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(424021.29, 536576.35, -40375.64), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_171",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(424447.93, 536857.2, -40356.1), new Rotator(0.0, -150.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_172",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(478761.41, 499714.13, -38687.34), new Rotator(4.924, -90.87, -10.037), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_173",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481361.81, 505804.49, -36443.75), new Rotator(0.0, -140.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_174",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(510618.71, 594732.88, -45021.52), new Rotator(0.0, -30.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_175",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(480565.96, 565797.04, -28600.15), new Rotator(0.0, -60.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(481917.64, 564942.68, -28400.38), new Rotator(0.0, -170.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_176",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(522808.91, 522492.47, -38080.03), new Rotator(5.741, 170.89, -6.369), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_177",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(500567.95, 547697.63, -44198.56), new Rotator(-8.482, 95.966, -4.226), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_178",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(491972.02, 500199.14, -35497.16), new Rotator(1.886, 51.822, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_179",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(487669.72, 503876.49, -36781.0), new Rotator(-1.284, -70.293, 0.263), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_180",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(479093.92, 570188.29, -35412.57), new Rotator(0.0, -81.228, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_181",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(472125.52, 572061.91, -35412.57), new Rotator(0.0, 48.133, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_182",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(474971.93, 613470.74, -33368.5), new Rotator(0.0, -15.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_183",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(457879.19, 561342.0, -38300.78), new Rotator(0.0, -36.673, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_184",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(443223.36, 557103.57, -38904.97), new Rotator(0.0, -120.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_185",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(444273.2, 560449.66, -38898.77), new Rotator(0.0, 73.957, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_186",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(463500.12, 561969.25, -37517.19), new Rotator(0.0, 31.591, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_187",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(505825.64, 523543.51, -38960.98), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_188",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(476702.03, 561393.66, -34401.03), new Rotator(0.0, -55.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_189",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(445075.46, 601257.3, -33191.5), new Rotator(0.0, 145.999, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_190",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(444599.73, 601417.92, -33190.81), new Rotator(0.0, -149.87, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_191",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(496410.17, 591505.6, -41099.3), new Rotator(0.0, -118.172, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_192",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(499273.96, 589102.97, -41198.74), new Rotator(0.0, 141.148, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(498862.04, 589858.26, -41198.74), new Rotator(0.0, -64.549, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_193",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(497376.2, 530347.69, -34261.37), new Rotator(0.0, 118.77, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(498843.11, 532879.82, -34796.96), new Rotator(0.0, 118.77, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_194",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(496224.28, 531073.71, -34260.92), new Rotator(0.0, 108.52, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(499669.51, 530623.4, -34798.91), new Rotator(0.0, 118.08, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_195",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(496019.42, 530832.6, -34271.01), new Rotator(0.0, 29.171, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(495893.59, 529053.62, -33961.4), new Rotator(0.0, 45.256, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_196",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(497283.6, 530276.05, -34261.41), new Rotator(0.0, 124.027, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(494843.82, 529902.21, -34261.41), new Rotator(0.0, -148.824, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_197",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(478719.77, 558802.6, -37700.52), new Rotator(0.0, 107.767, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_198",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(484437.52, 548699.93, -37698.94), new Rotator(0.0, 64.834, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_199",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(484643.58, 549295.66, -37697.62), new Rotator(0.0, 111.381, 0.5), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_200",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(482022.29, 553999.13, -37699.69), new Rotator(-0.246, -98.091, -0.435), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_201",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481256.9, 553788.01, -37702.36), new Rotator(0.0, 36.722, 0.576), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_202",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(479006.04, 559353.57, -37700.9), new Rotator(0.0, 26.672, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_203",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(478857.68, 558864.38, -37700.52), new Rotator(0.0, 116.628, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_204",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481872.55, 554377.86, -37700.88), new Rotator(0.572, -59.951, -0.067), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_205",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(453968.08, 531625.23, -40603.48), new Rotator(0.0, -32.911, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_206",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(457023.07, 532811.73, -40605.48), new Rotator(0.0, 170.198, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_207",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(456257.78, 533309.2, -40609.61), new Rotator(0.0, -10.217, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_208",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(461205.98, 521614.08, -40722.09), new Rotator(0.0, 103.87, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_209",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(467209.97, 521467.05, -40677.32), new Rotator(0.0, 121.192, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_210",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(466640.82, 530914.91, -40806.41), new Rotator(0.0, -24.305, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_211",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(438513.89, 534687.99, -40914.06), new Rotator(0.0, -20.837, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_212",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(443039.46, 508484.3, -38708.08), new Rotator(0.0, 74.205, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_213",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(427257.64, 515411.36, -37434.08), new Rotator(0.0, 74.205, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_214",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(496911.39, 556671.24, -44613.08), new Rotator(-1.653, -20.062, -1.228), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_215",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(458702.95, 568791.3, -36221.96), new Rotator(0.0, -171.316, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_216",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(496990.85, 556328.28, -44664.32), new Rotator(-3.173, -103.167, -9.972), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_217",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(497841.35, 564309.94, -44376.29), new Rotator(0.0, 152.497, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_218",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(448856.21, 563748.55, -36273.46), new Rotator(0.0, 34.314, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_219",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(515221.38, 587488.02, -44002.97), new Rotator(0.0, 51.628, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_220",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(513069.34, 599660.62, -44524.4), new Rotator(0.0, 125.927, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_221",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(467476.73, 615659.53, -30640.2), new Rotator(0.0, -127.822, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_222",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(468301.92, 615056.95, -30906.38), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_223",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(468222.3, 614895.34, -30905.76), new Rotator(0.0, 136.022, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_224",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(449371.0, 589171.76, -36660.96), new Rotator(0.0, -69.446, -9.22), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_225",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(427944.91, 587503.37, -36993.43), new Rotator(0.0, 144.501, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(434261.72, 590427.83, -36851.24), new Rotator(0.0, 176.423, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(427922.64, 592533.1, -36967.82), new Rotator(0.0, -6.737, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_226",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(501949.63, 534144.21, -43550.83), new Rotator(0.0, 136.022, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_227",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(428189.44, 587680.38, -36995.13), new Rotator(0.0, -161.104, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(434332.38, 590321.83, -36856.11), new Rotator(0.0, -97.528, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_228",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(427974.44, 587387.76, -36993.43), new Rotator(0.0, -137.562, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(428114.46, 588131.6, -38316.7), new Rotator(9.223, 18.419, -0.267), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_229",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(430265.83, 587053.12, -38054.09), new Rotator(-3.712, 152.858, 3.352), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_230",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(446151.57, 561754.71, -38200.01), new Rotator(0.0, -74.208, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_231",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(443218.1, 560994.4, -38200.17), new Rotator(0.011, 102.938, 0.297), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_232",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(487789.0, 504177.66, -36676.26), new Rotator(9.677, -82.932, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_233",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(489648.21, 506481.04, -36685.43), new Rotator(0.0, -73.789, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_235",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(461201.05, 522328.11, -36852.46), new Rotator(6.38, -177.63, 0.132), 1.0),
                new DynamicLootPoint(new Vector3D(467106.15, 521551.06, -36802.49), new Rotator(-14.116, 27.2, 1.313), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_236",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(467040.63, 521523.61, -36820.92), new Rotator(0.0, 47.337, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(456735.57, 534612.22, -36747.77), new Rotator(0.0, 118.872, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_68",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(459335.65, 560284.09, -38400.46), new Rotator(78.437, 94.291, -89.002), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_69",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(443267.92, 557219.35, -39000.69), new Rotator(32.925, -102.596, 3.744), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_70",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(444804.55, 561888.97, -39008.8), new Rotator(17.423, 79.599, 2.328), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_71",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(443712.38, 560647.13, -39004.72), new Rotator(20.497, 1.091, 0.884), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_72",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(468050.73, 564539.19, -37805.48), new Rotator(86.438, -31.926, 38.698), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_73",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(473066.88, 564298.05, -37804.74), new Rotator(14.867, 79.599, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_74",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(478183.17, 566394.72, -36801.67), new Rotator(88.063, 9.325, 155.002), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_75",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(476161.57, 570484.06, -35415.58), new Rotator(86.733, -73.509, -95.819), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_76",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(472393.02, 570533.69, -35418.78), new Rotator(11.793, 142.227, -2.417), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_77",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(475520.66, 572792.41, -35416.52), new Rotator(10.812, 163.411, -3.199), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_78",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(484381.98, 581223.89, -39212.07), new Rotator(86.017, 54.706, 76.171), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_79",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(497904.39, 518224.61, -37613.79), new Rotator(9.385, -100.422, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_80",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(444147.16, 507807.8, -37861.88), new Rotator(87.215, 138.923, 68.398), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_81",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481464.55, 554160.39, -37602.97), new Rotator(85.014, -4.709, -79.788), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_82",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(505772.54, 523488.25, -38967.51), new Rotator(31.719, -35.556, -2.297), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_83",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(503760.97, 568311.87, -43105.13), new Rotator(10.431, 169.353, -6.402), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_84",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(483603.55, 549128.15, -38550.4), new Rotator(61.532, 48.088, 40.212), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_85",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(474174.54, 549664.31, -36799.98), new Rotator(50.762, -156.916, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_86",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(480681.59, 553832.45, -38067.96), new Rotator(83.665, 65.756, 148.575), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_87",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(473579.07, 561371.61, -37601.07), new Rotator(89.137, 36.925, -58.435), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_88",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(479764.81, 564801.65, -37201.11), new Rotator(86.976, 3.136, -136.036), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_89",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(474422.38, 577020.56, -38631.61), new Rotator(68.617, 36.123, 164.513), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_90",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481770.13, 579985.21, -39831.13), new Rotator(83.108, 1.345, 178.926), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_91",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(479277.91, 589540.37, -40566.87), new Rotator(70.234, -69.48, -37.55), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_92",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(475503.31, 597130.75, -39132.05), new Rotator(64.23, 142.665, 21.151), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_93",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(468448.84, 593490.61, -39079.37), new Rotator(82.615, -143.611, -156.573), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_94",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(458077.1, 592563.49, -36456.67), new Rotator(68.783, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_95",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(448921.06, 595459.74, -35381.68), new Rotator(63.173, 151.559, -51.427), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_96",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(446132.45, 589700.03, -37220.26), new Rotator(62.467, -131.111, -134.543), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_97",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(441690.31, 582684.46, -38005.19), new Rotator(87.023, -138.286, -71.77), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_98",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(465099.21, 506591.81, -39592.09), new Rotator(90.0, 0.0, 135.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_99",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(522647.76, 522663.87, -38077.07), new Rotator(-2.981, -10.811, 5.037), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_UAID_BCFCE753915F527802_1934609195",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(459755.15, 530157.91, -40510.49), new Rotator(2.576, -55.218, -4.666), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_UAID_BCFCE753915F527802_2054040196",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(458670.31, 527254.51, -40392.01), new Rotator(0.0, -150.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_UAID_BCFCE753915F537802_1091495373",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(461949.99, 531493.51, -40913.9), new Rotator(9.981, -169.558, 10.058), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_UAID_BCFCE753915F537802_1194668374",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(457893.8, 530622.46, -40638.79), new Rotator(0.0, 80.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_UAID_BCFCE753915F6FBD02_1933606173",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Research/WeaponsRack_01/BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic.BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(488314.35, 526739.51, -38524.99), new Rotator(0.0, -151.607, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_UAID_ECE7A707B18101BE02_1873168230",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(492481.55, 526981.17, -39056.14), new Rotator(0.0, 25.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(492972.07, 526693.33, -39056.02), new Rotator(0.0, -150.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_UAID_ECE7A707B181349A02_1864879350",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481162.89, 566619.07, -28550.8), new Rotator(0.0, -145.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(481394.28, 567309.99, -28550.75), new Rotator(0.0, 30.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(478395.84, 562715.64, -28550.85), new Rotator(0.0, -60.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(479813.17, 567231.6, -28550.59), new Rotator(0.0, 120.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_UAID_ECE7A707B181369A02_1478220528",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481431.88, 567011.37, -28557.05), new Rotator(24.595, 68.99, -4.629), 1.0),
                new DynamicLootPoint(new Vector3D(482051.35, 565155.53, -28557.52), new Rotator(25.0, 70.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(481328.51, 566460.62, -28518.26), new Rotator(45.0, -140.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_UAID_ECE7A707B18176B702_1840841391",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(491187.31, 524070.56, -39118.19), new Rotator(20.0, -5.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(490562.58, 525203.34, -39117.62), new Rotator(20.0, 125.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(489504.3, 525913.63, -38180.94), new Rotator(10.0, 120.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_UAID_ECE7A707B181B4B702_1829690278",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/HikingBag/BP_SocketContainer_Raider_Backpack_Dynamic.BP_SocketContainer_Raider_Backpack_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(491135.13, 527065.86, -38932.45), new Rotator(15.0, -150.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(492788.52, 526417.81, -39062.76), new Rotator(15.0, -60.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_UAID_ECE7A707B181DCB602_1528797971",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(492368.52, 527537.13, -38749.18), new Rotator(0.0, -150.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(492876.88, 526275.81, -38549.43), new Rotator(0.0, -150.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_UAID_ECE7A707B181DCB602_1660367999",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(487671.85, 525430.04, -37650.12), new Rotator(0.0, -60.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(487533.18, 525693.19, -37650.12), new Rotator(0.0, -150.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(487271.62, 524817.29, -37650.5), new Rotator(0.0, -60.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "",
            "ActorSpawner_UAID_ECE7A707B181DCB602_1736430019",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(492605.55, 526804.17, -38549.94), new Rotator(0.0, 30.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(491312.58, 526691.62, -38926.28), new Rotator(0.0, -150.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(490977.46, 526526.57, -38926.28), new Rotator(0.0, -60.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "418E58TX3DAEY1FH9WC805A5X",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1729934289",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(517694.16, 559663.36, -44706.11), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "418E58TX3DAEY1FH9WC805A5X",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1741426290",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(512646.25, 573553.74, -44102.33), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "418E58TX3DAEY1FH9WC805A5X",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1851130298",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(515160.35, 587975.84, -43546.73), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "418E58TX3DAEY1FH9WC805A5X",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1869799299",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(511171.4, 598359.47, -43803.62), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "418E58TX3DAEY1FH9WC805A5X",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1886530300",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(513267.24, 598941.08, -44682.4), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "418E58TX3DAEY1FH9WC805A5X",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461990928",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(511171.4, 598359.47, -43803.62), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "418E58TX3DAEY1FH9WC805A5X",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461994933",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(517694.16, 559663.36, -44706.11), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "418E58TX3DAEY1FH9WC805A5X",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461994934",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(515160.35, 587975.84, -43546.73), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "418E58TX3DAEY1FH9WC805A5X",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461995935",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(512646.25, 573553.74, -44102.33), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "418E58TX3DAEY1FH9WC805A5X",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461996936",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(513267.24, 598941.08, -44682.4), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "6DM0DSA13D7FSPDKAZWH0EJB1",
            "ActorSpawner_UAID_BCFCE753915F11CA02_1577516788",
            "DataLayer_3E5D143841DB59A6407488B4ED6D9ACD",
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 3.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C", 2.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 3.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C", 0.3),
            ],
            [
                new DynamicLootPoint(new Vector3D(507959.56, 537468.25, -45152.97), new Rotator(1.119, 90.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(508407.34, 537475.31, -45142.32), new Rotator(4.992, -63.435, -0.001), 1.0),
                new DynamicLootPoint(new Vector3D(507998.91, 537808.88, -45137.94), new Rotator(4.992, 116.565, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(507678.5, 537340.31, -45153.09), new Rotator(1.582, 135.0, -0.002), 1.0),
                new DynamicLootPoint(new Vector3D(508471.91, 537279.94, -45122.9), new Rotator(8.017, -56.31, 0.001), 1.0),
                new DynamicLootPoint(new Vector3D(508182.72, 537440.44, -45147.95), new Rotator(2.501, -26.565, -0.001), 1.0),
                new DynamicLootPoint(new Vector3D(507889.16, 538047.44, -45119.85), new Rotator(4.028, 123.69, -0.001), 1.0),
                new DynamicLootPoint(new Vector3D(508605.66, 537486.62, -45133.13), new Rotator(6.497, -59.036, -0.001), 1.0),
                new DynamicLootPoint(new Vector3D(508106.03, 537877.75, -45138.14), new Rotator(3.534, 108.435, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(508152.09, 538408.25, -45095.68), new Rotator(6.775, 80.538, -0.003), 1.0),
            ],
            5,
            true),
        new(
            "6DM0DSA13D7FSPDKAZWH0EJB1",
            "ActorSpawner_UAID_BCFCE753915F11CA02_1743636822",
            "DataLayer_3E5D143841DB59A6407488B4ED6D9ACD",
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 3.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C", 2.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 3.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C", 0.3),
            ],
            [
                new DynamicLootPoint(new Vector3D(481985.06, 578463.73, -39933.54), new Rotator(20.993, 165.256, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(482204.8, 577744.35, -39983.75), new Rotator(28.811, -173.884, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(482319.68, 578166.35, -40070.51), new Rotator(20.463, -173.991, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(482244.42, 578687.6, -39989.31), new Rotator(17.916, 154.983, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(481691.59, 578559.62, -39801.23), new Rotator(21.578, 159.775, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(482235.0, 577978.98, -40018.13), new Rotator(26.888, -164.358, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(482130.5, 578292.46, -40004.99), new Rotator(22.392, 174.56, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(482325.09, 578937.6, -39960.46), new Rotator(18.368, 151.928, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(482473.32, 578040.52, -40119.51), new Rotator(25.134, 177.614, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(482369.88, 578665.97, -40037.62), new Rotator(21.337, 143.13, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(482762.54, 579025.34, -40077.39), new Rotator(14.407, 171.254, 0.0), 1.0),
            ],
            5,
            true),
        new(
            "6DM0DSA13D7FSPDKAZWH0EJB1",
            "ActorSpawner_UAID_BCFCE753915F11CA02_1825743834",
            "DataLayer_3E5D143841DB59A6407488B4ED6D9ACD",
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 3.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C", 2.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 3.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C", 0.3),
            ],
            [
                new DynamicLootPoint(new Vector3D(476703.37, 595877.48, -39381.58), new Rotator(11.525, 106.699, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(477037.99, 595580.09, -39432.27), new Rotator(13.41, 55.008, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(476962.73, 596101.35, -39349.4), new Rotator(7.785, 90.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(476409.91, 595973.37, -39344.69), new Rotator(13.582, 104.036, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(476953.31, 595392.73, -39478.87), new Rotator(11.105, 84.289, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(476848.81, 595706.21, -39418.82), new Rotator(9.97, 90.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(477043.41, 596351.35, -39317.87), new Rotator(9.149, 104.036, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(477191.63, 595454.27, -39447.34), new Rotator(18.632, 79.992, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(477088.2, 596079.72, -39348.75), new Rotator(7.464, 63.435, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(477480.85, 596439.08, -39316.44), new Rotator(2.501, 116.565, 0.0), 1.0),
            ],
            5,
            true),
        new(
            "6DM0DSA13D7FSPDKAZWH0EJB1",
            "ActorSpawner_UAID_BCFCE753915F11CA02_1882063844",
            "DataLayer_3E5D143841DB59A6407488B4ED6D9ACD",
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 3.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C", 2.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 3.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C", 0.3),
            ],
            [
                new DynamicLootPoint(new Vector3D(444795.72, 599824.94, -32992.87), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(445310.07, 600246.94, -32992.87), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(444679.52, 600888.77, -33191.48), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(444282.53, 600640.19, -33191.48), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(444825.94, 600059.56, -32992.87), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(444407.81, 600373.0, -33191.48), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(444554.03, 601018.19, -33191.48), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(445064.25, 600121.06, -32992.87), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(444960.81, 600746.56, -33191.48), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(445353.47, 601105.88, -33191.48), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            5,
            true),
        new(
            "6DM0DSA13D7FSPDKAZWH0EJB1",
            "ActorSpawner_UAID_BCFCE753915F13CA02_1412018373",
            "DataLayer_3E5D143841DB59A6407488B4ED6D9ACD",
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 3.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C", 2.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 3.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C", 0.3),
            ],
            [
                new DynamicLootPoint(new Vector3D(460584.5, 524056.91, -40805.36), new Rotator(5.687, -11.31, -0.002), 1.0),
                new DynamicLootPoint(new Vector3D(460804.09, 523337.19, -40757.3), new Rotator(6.497, -59.036, -0.001), 1.0),
                new DynamicLootPoint(new Vector3D(461366.47, 523759.56, -40781.92), new Rotator(1.119, -90.0, -4.468), 1.0),
                new DynamicLootPoint(new Vector3D(460843.72, 524280.75, -40799.8), new Rotator(3.534, -18.435, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(460290.88, 524152.84, -40830.62), new Rotator(3.353, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(460544.75, 523571.97, -40782.85), new Rotator(5.578, -90.0, -3.35), 1.0),
                new DynamicLootPoint(new Vector3D(460729.41, 523885.84, -40798.58), new Rotator(3.162, 135.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(460924.53, 524531.44, -40787.62), new Rotator(10.44, 57.995, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(461071.94, 523633.53, -40785.79), new Rotator(7.464, -153.435, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(461550.53, 524259.34, -40762.3), new Rotator(2.237, 0.0, -2.234), 1.0),
                new DynamicLootPoint(new Vector3D(461361.69, 524618.88, -40741.46), new Rotator(4.604, 75.964, 0.002), 1.0),
            ],
            5,
            true),
        new(
            "6DM0DSA13D7FSPDKAZWH0EJB1",
            "ActorSpawner_UAID_BCFCE753915F13CA02_1490847418",
            "DataLayer_3E5D143841DB59A6407488B4ED6D9ACD",
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 3.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C", 2.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 3.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C", 0.3),
            ],
            [
                new DynamicLootPoint(new Vector3D(440433.28, 533773.38, -40927.24), new Rotator(3.865, -125.807, -0.871), 1.0),
                new DynamicLootPoint(new Vector3D(440653.34, 533054.62, -40961.39), new Rotator(9.474, 110.556, 0.001), 1.0),
                new DynamicLootPoint(new Vector3D(440768.22, 533476.0, -40941.5), new Rotator(3.353, -180.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(440476.28, 533997.31, -40930.25), new Rotator(3.353, -180.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(440140.38, 533869.38, -40926.38), new Rotator(1.119, 90.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(440683.53, 533288.81, -40938.93), new Rotator(4.028, 146.31, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(440579.12, 533602.12, -40932.79), new Rotator(2.237, -180.0, 0.002), 1.0),
                new DynamicLootPoint(new Vector3D(440773.69, 534247.31, -40939.46), new Rotator(2.501, 153.435, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(440921.62, 533350.25, -40952.73), new Rotator(6.775, 170.538, -0.002), 1.0),
                new DynamicLootPoint(new Vector3D(441442.97, 533655.25, -40928.86), new Rotator(7.785, -180.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(441211.31, 534334.94, -40959.67), new Rotator(1.119, -90.0, 0.0), 1.0),
            ],
            5,
            true),
        new(
            "6DM0DSA13D7FSPDKAZWH0EJB1",
            "ActorSpawner_UAID_BCFCE753915F13CA02_1556556457",
            "DataLayer_3E5D143841DB59A6407488B4ED6D9ACD",
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_Dynamic_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_Dynamic_C", 3.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/AmmoBox_01/BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier.BP_SocketContainer_Raider_AmmoBox_01_Lid_A_DynamicHighTier_C", 2.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/MedicalBag_01/BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic.BP_SocketContainer_Raider_MedicalBag_01_Lid_A__Dynamic_C", 3.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/GrenadeContainer_01/BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic.BP_SocketContainer_Raider_GrenadeContainer_01_Lid_A_Dynamic_C", 1.0),
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_01_DynamicHigh_C", 0.3),
            ],
            [
                new DynamicLootPoint(new Vector3D(429426.0, 544247.0, -40407.5), new Rotator(24.59, 50.194, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(429759.94, 543948.75, -40411.27), new Rotator(7.863, 45.0, 0.003), 1.0),
                new DynamicLootPoint(new Vector3D(429684.22, 544470.0, -40326.84), new Rotator(5.687, 101.31, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(429132.88, 544342.62, -40474.39), new Rotator(24.98, 33.024, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(429675.62, 543761.38, -40436.27), new Rotator(12.318, 26.565, 0.001), 1.0),
                new DynamicLootPoint(new Vector3D(429571.06, 544075.12, -40419.14), new Rotator(13.235, 41.634, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(429764.97, 544719.62, -40314.46), new Rotator(0.0, 0.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(429913.5, 543822.81, -40405.82), new Rotator(5.578, 36.87, 0.003), 1.0),
                new DynamicLootPoint(new Vector3D(430098.0, 544195.81, -40392.62), new Rotator(11.729, 138.814, 0.107), 1.0),
                new DynamicLootPoint(new Vector3D(430202.09, 544807.69, -40345.27), new Rotator(6.305, 135.0, 0.002), 1.0),
            ],
            5,
            true),
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1183844493",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(447523.68, 595390.95, -35900.59), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1207055495",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(430945.89, 590152.52, -38787.12), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1209650496",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(430994.27, 590085.7, -38787.12), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1221202497",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(417847.8, 561716.46, -40045.1), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1228372498",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(422494.2, 560176.53, -40899.81), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1380497713",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(445086.14, 554482.56, -40142.75), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461950879",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(422494.2, 560176.53, -40899.81), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461951880",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(417847.8, 561716.46, -40045.1), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461959890",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(445086.14, 554482.56, -40142.75), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461960891",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(430994.27, 590085.7, -38787.12), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461961892",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(430945.89, 590152.52, -38787.12), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "6VLN5OPYR367ZIKOMVAOOKL4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461964896",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(447523.68, 595390.95, -35900.59), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1336428505",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(456669.19, 533997.07, -40508.48), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1353580506",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(459396.66, 531971.81, -40624.52), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1370909507",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(463751.87, 528700.31, -40817.74), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1943635508",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(461028.88, 522210.86, -40720.12), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1955515509",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(463019.61, 523822.93, -40647.2), new Rotator(0.0, 0.0, -10.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1966338510",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(466937.58, 521419.87, -40675.37), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_2014793513",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(465280.28, 506164.71, -39884.02), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_2028817514",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(466353.22, 507238.84, -40071.35), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_2044909515",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(467364.99, 504234.71, -39603.39), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_2057942516",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(467644.72, 506894.65, -40094.55), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_2070283517",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(466612.44, 508413.46, -40142.91), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_2078985518",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(464920.95, 510809.81, -40645.73), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_2095714519",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(475260.91, 507125.29, -40300.8), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_2112590520",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(482162.89, 504065.18, -37274.83), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_2120797521",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(482394.59, 505272.77, -37251.1), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_2128875522",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481087.51, 505666.23, -36450.06), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_2140396523",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(487719.79, 504136.48, -36686.78), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1189737703",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(493023.93, 524430.3, -39530.83), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1197214704",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(494409.12, 524824.62, -39554.23), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1224435705",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481027.73, 549902.97, -38847.04), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1265441706",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(482385.35, 551500.76, -38845.35), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1329126709",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(465825.27, 546914.05, -40867.14), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461938865",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(465280.28, 506164.71, -39884.02), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461939866",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(482162.89, 504065.18, -37274.83), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461940867",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(466612.44, 508413.46, -40142.91), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461941868",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481087.51, 505666.23, -36450.06), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461942869",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(467644.72, 506894.65, -40094.55), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461943870",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(475260.91, 507125.29, -40300.8), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461943871",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(482394.59, 505272.77, -37251.1), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461944872",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(467364.99, 504234.71, -39603.39), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461945873",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(466353.22, 507238.84, -40071.35), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461946874",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(464920.95, 510809.81, -40645.73), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461947875",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(487719.79, 504136.48, -36686.78), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461954884",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(456669.19, 533997.07, -40508.48), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461955885",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(459396.66, 531971.81, -40624.52), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461965897",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(461028.88, 522210.86, -40720.12), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461966898",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(463019.61, 523822.93, -40647.2), new Rotator(0.0, 0.0, -10.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461966899",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(466937.58, 521419.87, -40675.37), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461967900",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(463751.87, 528700.31, -40817.74), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461968901",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(482385.35, 551500.76, -38845.35), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461969902",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(465825.27, 546914.05, -40867.14), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461970904",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481027.73, 549902.97, -38847.04), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461972907",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(493023.93, 524430.3, -39530.83), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461974909",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(494409.12, 524824.62, -39554.23), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic_C_UAID_BCFCE753915FD99F02_2056661365",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Research/WeaponsRack_01/BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic.BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(502022.43, 520974.86, -37600.0), new Rotator(0.0, 120.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(500608.0, 520160.19, -37600.0), new Rotator(0.0, 120.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(502586.99, 521171.82, -37600.0), new Rotator(0.0, -60.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "78VSCAY5CCYC9B657XV2IPJ4Z",
            "StaticMeshActor_6718",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Research/WeaponsRack_01/BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic.BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(466452.97, 530889.51, -40800.0), new Rotator(0.0, 165.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(466352.34, 529602.83, -40800.0), new Rotator(0.0, 75.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(465957.32, 529708.67, -40800.0), new Rotator(0.0, 75.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(468475.42, 531730.02, -40800.0), new Rotator(0.0, -65.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(468520.52, 529905.46, -40800.0), new Rotator(0.0, 85.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1752645291",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(503893.49, 565985.01, -43039.66), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1759049292",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(503507.79, 567108.14, -43071.24), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1768076293",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(497574.76, 564635.87, -44376.49), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1778523294",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(482139.19, 571133.47, -43220.0), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1797867295",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481132.43, 572700.26, -42978.65), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1818143296",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(491628.01, 576684.75, -42592.11), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1901589301",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(500302.82, 599334.75, -42430.45), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_2018697309",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(464888.8, 599934.57, -38939.32), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_2044125310",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(469317.39, 592393.1, -39662.19), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_2065174311",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(477594.52, 597994.77, -38999.77), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_2079971312",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(478564.45, 597757.09, -39129.09), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_2093320313",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(487310.65, 585493.61, -41462.71), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_2105854314",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(482281.31, 579752.42, -39993.31), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1132439491",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(484245.83, 581519.78, -39217.32), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1143812492",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(483942.03, 579783.09, -40199.03), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1291451707",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(476615.54, 554031.8, -38802.51), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1303609708",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(478197.86, 558296.38, -38845.35), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1338946710",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(463613.95, 555339.96, -40327.11), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1345893711",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(458702.02, 555242.14, -40740.27), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1428594715",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(461186.68, 571403.54, -35619.82), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1433263716",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(460694.28, 571539.07, -35647.78), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461957887",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(458702.02, 555242.14, -40740.27), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461961893",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(460694.28, 571539.07, -35647.78), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461969903",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(476615.54, 554031.8, -38802.51), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461971905",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(463613.95, 555339.96, -40327.11), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461972906",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(478197.86, 558296.38, -38845.35), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461975910",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(482139.19, 571133.47, -43220.0), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461976911",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(484245.83, 581519.78, -39217.32), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461977912",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(483942.03, 579783.09, -40199.03), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461978913",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(481132.43, 572700.26, -42978.65), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461979914",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(482281.31, 579752.42, -39993.31), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461980915",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(461186.68, 571403.54, -35619.82), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461983919",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(464888.8, 599934.57, -38939.32), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461984920",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(477594.52, 597994.77, -38999.77), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461984921",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(469317.39, 592393.1, -39662.19), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461985922",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(478564.45, 597757.09, -39129.09), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461986923",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(487310.65, 585493.61, -41462.71), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461987924",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(497574.76, 564635.87, -44376.49), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461987925",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(503893.49, 565985.01, -43039.66), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461988926",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(503507.79, 567108.14, -43071.24), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461989927",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(491628.01, 576684.75, -42592.11), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461992931",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(500302.82, 599334.75, -42430.45), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic_C_UAID_BCFCE753915FD49F02_2038122093",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Research/WeaponsRack_01/BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic.BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(477235.72, 564962.04, -36798.76), new Rotator(0.0, 30.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic_C_UAID_BCFCE753915FD49F02_2038132094",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Research/WeaponsRack_01/BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic.BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(480280.92, 565637.6, -28598.76), new Rotator(0.0, -60.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(481052.59, 566904.2, -28544.43), new Rotator(0.0, -60.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(478466.04, 563026.9, -28500.18), new Rotator(0.0, 30.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(479781.58, 565756.06, -28428.62), new Rotator(0.0, 30.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic_C_UAID_BCFCE753915FDA9F02_1411336749",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Research/WeaponsRack_01/BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic.BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(499268.07, 585938.9, -40400.0), new Rotator(0.0, -60.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(498670.82, 585594.07, -40400.0), new Rotator(0.0, -60.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(497321.88, 585570.52, -40400.0), new Rotator(0.0, -150.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(496663.94, 589211.77, -40596.58), new Rotator(0.0, -60.0, 0.0), 1.0),
                new DynamicLootPoint(new Vector3D(496345.02, 589535.2, -41368.81), new Rotator(0.0, 120.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "StaticMeshActor_UAID_BCFCE753915FD89F02_1890613185",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Research/WeaponsRack_01/BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic.BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(478597.28, 559206.36, -37700.0), new Rotator(0.0, -60.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "799Z1TX1MWV1JVES1KJXD650D",
            "StaticMeshActor_UAID_BCFCE753915FD89F02_1890624186",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/Research/WeaponsRack_01/BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic.BP_SocketContainer_Rsr_WeaponsRack_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(478722.67, 559279.0, -37700.0), new Rotator(0.0, -60.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "8635EV0VJ3V10LNUJ9RO28RQ9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_2005828512",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(458830.66, 500190.97, -38771.55), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "8635EV0VJ3V10LNUJ9RO28RQ9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461927864",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(458830.66, 500190.97, -38771.55), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1243513499",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(418315.63, 551273.33, -40937.83), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1258826500",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(421108.62, 544039.95, -40792.57), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1268873501",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(413408.51, 540278.83, -40923.85), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1291484502",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(423491.58, 536694.85, -40752.11), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1297274503",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(424625.2, 536866.5, -40846.22), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1316078504",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(437138.92, 532551.42, -40937.54), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1986734511",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(432187.58, 520400.3, -39450.97), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1366247712",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(445889.68, 550719.76, -40849.13), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1403222714",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(441172.52, 540486.01, -40876.16), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461947876",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(423491.58, 536694.85, -40752.11), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461948877",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(432187.58, 520400.3, -39450.97), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461949878",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(424625.2, 536866.5, -40846.22), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461952881",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(421108.62, 544039.95, -40792.57), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461952882",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(418315.63, 551273.33, -40937.83), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461953883",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(413408.51, 540278.83, -40923.85), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461955886",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(437138.92, 532551.42, -40937.54), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461957888",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(445889.68, 550719.76, -40849.13), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "9S6VM9T6Q1DWC3A03BDTNBDPO",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461958889",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(441172.52, 540486.01, -40876.16), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CAMP3HBSEA6OHYXJ752UWG3X9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1928160302",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(489058.8, 600632.02, -37205.11), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CAMP3HBSEA6OHYXJ752UWG3X9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1936097303",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(488430.81, 602385.58, -37232.08), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CAMP3HBSEA6OHYXJ752UWG3X9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1948198304",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(475492.52, 613898.24, -33369.52), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CAMP3HBSEA6OHYXJ752UWG3X9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1966519305",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(465690.94, 616688.61, -30985.0), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CAMP3HBSEA6OHYXJ752UWG3X9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1976393306",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(461524.42, 610310.68, -36200.01), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CAMP3HBSEA6OHYXJ752UWG3X9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_1991455307",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(463491.26, 602273.48, -38820.33), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CAMP3HBSEA6OHYXJ752UWG3X9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5E5C02_2004426308",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(459750.13, 601448.75, -38657.26), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CAMP3HBSEA6OHYXJ752UWG3X9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461963895",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(459750.13, 601448.75, -38657.26), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CAMP3HBSEA6OHYXJ752UWG3X9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461980916",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(475492.52, 613898.24, -33369.52), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CAMP3HBSEA6OHYXJ752UWG3X9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461981917",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(461524.42, 610310.68, -36200.01), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CAMP3HBSEA6OHYXJ752UWG3X9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461982918",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(463491.26, 602273.48, -38820.33), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CAMP3HBSEA6OHYXJ752UWG3X9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461990929",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(488430.81, 602385.58, -37232.08), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CAMP3HBSEA6OHYXJ752UWG3X9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461991930",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(489058.8, 600632.02, -37205.11), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CAMP3HBSEA6OHYXJ752UWG3X9",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461997937",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(465690.94, 616688.61, -30985.0), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CQ20PUX5W75YZK1PSKT0OT1OH",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E5F5C02_1197837494",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(445238.74, 601245.75, -33191.5), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "CQ20PUX5W75YZK1PSKT0OT1OH",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461962894",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(445238.74, 601245.75, -33191.5), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "D7SLI18I6UYP3QP4VHBRYZMGH",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1085044700",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(517232.14, 515657.25, -34711.88), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "D7SLI18I6UYP3QP4VHBRYZMGH",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_7486E223302E605C02_1143553702",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(506195.3, 523753.65, -38959.53), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "D7SLI18I6UYP3QP4VHBRYZMGH",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461974908",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(506195.3, 523753.65, -38959.53), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
        new(
            "D7SLI18I6UYP3QP4VHBRYZMGH",
            "BP_SocketContainer_Raider_FruitBasket_01_Dynamic_C_UAID_BCFCE753915F98A502_1461993932",
            null,
            [
                new DynamicLootClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent.BP_SocketContainer_Raider_FruitBasket_01_Dynamic_WinterEvent_C", 1.0),
            ],
            [
                new DynamicLootPoint(new Vector3D(517232.14, 515657.25, -34711.88), new Rotator(0.0, 0.0, 0.0), 1.0),
            ],
            1,
            false),
    ];

    /// <summary>All groups, ordered by activity node.</summary>
    public static IReadOnlyList<DynamicLootGroup> Groups { get; } =
    [
        new(
            "ActivityNode_109/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "FruitBasketsWinterEvent"],
            ["Coldsnap"],
            12,
            18,
            [Spawners[186], Spawners[187], Spawners[188], Spawners[189], Spawners[190], Spawners[204], Spawners[205], Spawners[206], Spawners[207], Spawners[208], Spawners[209], Spawners[232], Spawners[233], Spawners[234], Spawners[235], Spawners[236], Spawners[237], Spawners[238], Spawners[239], Spawners[240], Spawners[241], Spawners[242], Spawners[243], Spawners[244], Spawners[245], Spawners[246], Spawners[247], Spawners[248], Spawners[249], Spawners[250], Spawners[251], Spawners[252], Spawners[253], Spawners[277], Spawners[278], Spawners[279], Spawners[280], Spawners[281], Spawners[282], Spawners[283], Spawners[284], Spawners[285], Spawners[286], Spawners[287], Spawners[288], Spawners[289], Spawners[290], Spawners[291], Spawners[292], Spawners[293], Spawners[294], Spawners[295], Spawners[296], Spawners[297], Spawners[304], Spawners[314], Spawners[315], Spawners[316], Spawners[317], Spawners[318], Spawners[319], Spawners[320], Spawners[321], Spawners[322], Spawners[330], Spawners[331], Spawners[332], Spawners[333], Spawners[334], Spawners[335], Spawners[336], Spawners[338], Spawners[341], Spawners[342]],
            []),
        new(
            "ActivityNode_121/1",
            ["TheDam", "BaseGameplay", "MapConditionHarvestSeason", "Containers"],
            ["HarvestSeason"],
            22,
            26,
            [Spawners[136], Spawners[137], Spawners[138], Spawners[139], Spawners[140], Spawners[141], Spawners[142], Spawners[143], Spawners[144], Spawners[145], Spawners[146], Spawners[147], Spawners[148], Spawners[149], Spawners[150], Spawners[151], Spawners[152], Spawners[153], Spawners[154], Spawners[155], Spawners[156], Spawners[157], Spawners[158], Spawners[159], Spawners[160], Spawners[161], Spawners[162], Spawners[163], Spawners[164], Spawners[165], Spawners[166]],
            []),
        new(
            "ActivityNode_121/0",
            ["TheDam", "BaseGameplay", "MapConditionHarvestSeason", "Containers"],
            ["HarvestSeason"],
            40,
            45,
            [Spawners[181], Spawners[182], Spawners[183], Spawners[184], Spawners[185], Spawners[198], Spawners[199], Spawners[200], Spawners[201], Spawners[202], Spawners[203], Spawners[210], Spawners[211], Spawners[212], Spawners[213], Spawners[214], Spawners[215], Spawners[216], Spawners[217], Spawners[218], Spawners[219], Spawners[220], Spawners[221], Spawners[222], Spawners[223], Spawners[224], Spawners[225], Spawners[226], Spawners[227], Spawners[228], Spawners[229], Spawners[230], Spawners[231], Spawners[256], Spawners[257], Spawners[258], Spawners[259], Spawners[260], Spawners[261], Spawners[262], Spawners[263], Spawners[264], Spawners[265], Spawners[266], Spawners[267], Spawners[268], Spawners[269], Spawners[270], Spawners[271], Spawners[272], Spawners[273], Spawners[274], Spawners[275], Spawners[276], Spawners[303], Spawners[305], Spawners[306], Spawners[307], Spawners[308], Spawners[309], Spawners[310], Spawners[311], Spawners[312], Spawners[313], Spawners[323], Spawners[324], Spawners[325], Spawners[326], Spawners[327], Spawners[328], Spawners[329], Spawners[337], Spawners[339], Spawners[340]],
            []),
        new(
            "ActivityNode_123/2",
            ["TheDam", "BaseGameplay", "MapConditionToxicSwamp", "HydrodomeActivities"],
            ["ToxicSwamp"],
            1,
            2,
            [Spawners[31], Spawners[109], Spawners[110], Spawners[134]],
            []),
        new(
            "ActivityNode_123/0",
            ["TheDam", "BaseGameplay", "MapConditionToxicSwamp", "HydrodomeActivities"],
            ["ToxicSwamp"],
            1,
            2,
            [Spawners[48], Spawners[55], Spawners[105], Spawners[106]],
            []),
        new(
            "ActivityNode_123/1",
            ["TheDam", "BaseGameplay", "MapConditionToxicSwamp", "HydrodomeActivities"],
            ["ToxicSwamp"],
            1,
            2,
            [Spawners[168], Spawners[169], Spawners[170], Spawners[171]],
            []),
        new(
            "ActivityNode_123/3",
            ["TheDam", "BaseGameplay", "MapConditionToxicSwamp", "HydrodomeActivities"],
            ["ToxicSwamp"],
            1,
            2,
            [Spawners[8], Spawners[107], Spawners[108], Spawners[111], Spawners[135]],
            []),
        new(
            "ActivityNode_139/0",
            ["TheDam", "BaseGameplay", "MapConditionExtractor", "clusters", "ActivityNode_139"],
            ["Extractor"],
            1,
            1,
            [Spawners[191]],
            [
                new DynamicLootSelection("ActivityNode_137", 7, 8, 13, false),
            ]),
        new(
            "ActivityNode_140/0",
            ["TheDam", "BaseGameplay", "MapConditionExtractor", "clusters", "ActivityNode_140"],
            ["Extractor"],
            1,
            1,
            [Spawners[192]],
            [
                new DynamicLootSelection("ActivityNode_137", 7, 8, 13, false),
            ]),
        new(
            "ActivityNode_141/0",
            ["TheDam", "BaseGameplay", "MapConditionExtractor", "clusters", "ActivityNode_141"],
            ["Extractor"],
            1,
            1,
            [Spawners[193]],
            [
                new DynamicLootSelection("ActivityNode_137", 7, 8, 13, false),
            ]),
        new(
            "ActivityNode_142/0",
            ["TheDam", "BaseGameplay", "MapConditionExtractor", "clusters", "ActivityNode_142"],
            ["Extractor"],
            1,
            1,
            [Spawners[194]],
            [
                new DynamicLootSelection("ActivityNode_137", 7, 8, 13, false),
            ]),
        new(
            "ActivityNode_151/0",
            ["TheDam", "BaseGameplay", "MapConditionExtractor", "clusters", "ActivityNode_151"],
            ["Extractor"],
            1,
            1,
            [Spawners[195]],
            [
                new DynamicLootSelection("ActivityNode_137", 7, 8, 13, false),
            ]),
        new(
            "ActivityNode_152/0",
            ["TheDam", "BaseGameplay", "MapConditionExtractor", "clusters", "ActivityNode_152"],
            ["Extractor"],
            1,
            1,
            [Spawners[196]],
            [
                new DynamicLootSelection("ActivityNode_137", 7, 8, 13, false),
            ]),
        new(
            "ActivityNode_153/0",
            ["TheDam", "BaseGameplay", "MapConditionExtractor", "clusters", "ActivityNode_153"],
            ["Extractor"],
            1,
            1,
            [Spawners[197]],
            [
                new DynamicLootSelection("ActivityNode_137", 7, 8, 13, false),
            ]),
        new(
            "ActivityNode_154/1",
            ["TheDam", "BaseGameplay", "MapConditionExtractor", "AI", "Raider containers"],
            ["Extractor"],
            5,
            6,
            [Spawners[0], Spawners[1], Spawners[3], Spawners[4], Spawners[5], Spawners[6], Spawners[7], Spawners[8], Spawners[9], Spawners[10], Spawners[11], Spawners[12], Spawners[13], Spawners[14], Spawners[18], Spawners[19], Spawners[101], Spawners[104], Spawners[107], Spawners[108], Spawners[111], Spawners[117], Spawners[123], Spawners[124], Spawners[126], Spawners[132]],
            []),
        new(
            "ActivityNode_154/2",
            ["TheDam", "BaseGameplay", "MapConditionExtractor", "AI", "Raider containers"],
            ["Extractor"],
            2,
            2,
            [Spawners[43], Spawners[44], Spawners[46], Spawners[47], Spawners[48], Spawners[49], Spawners[50], Spawners[51], Spawners[52], Spawners[53], Spawners[54], Spawners[55], Spawners[56], Spawners[57], Spawners[58], Spawners[59], Spawners[60], Spawners[61], Spawners[102], Spawners[105], Spawners[106], Spawners[118], Spawners[120], Spawners[121]],
            []),
        new(
            "ActivityNode_154/3",
            ["TheDam", "BaseGameplay", "MapConditionExtractor", "AI", "Raider containers"],
            ["Extractor"],
            5,
            6,
            [Spawners[22], Spawners[23], Spawners[24], Spawners[25], Spawners[27], Spawners[28], Spawners[29], Spawners[31], Spawners[32], Spawners[33], Spawners[34], Spawners[35], Spawners[36], Spawners[37], Spawners[38], Spawners[90], Spawners[99], Spawners[100], Spawners[109], Spawners[110], Spawners[112], Spawners[113], Spawners[114], Spawners[122], Spawners[129], Spawners[131]],
            []),
        new(
            "ActivityNode_154/0",
            ["TheDam", "BaseGameplay", "MapConditionExtractor", "AI", "Raider containers"],
            ["Extractor"],
            5,
            10,
            [Spawners[62], Spawners[63], Spawners[64], Spawners[65], Spawners[66], Spawners[67], Spawners[68], Spawners[69], Spawners[70], Spawners[71], Spawners[72], Spawners[73], Spawners[74], Spawners[77], Spawners[78], Spawners[79], Spawners[80], Spawners[81], Spawners[82], Spawners[83], Spawners[84], Spawners[85], Spawners[86], Spawners[87], Spawners[89], Spawners[97], Spawners[98], Spawners[103], Spawners[115], Spawners[116], Spawners[119], Spawners[130], Spawners[133], Spawners[168], Spawners[169], Spawners[170], Spawners[171]],
            []),
        new(
            "ActivityNode_37/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "Clusters", "Cluster2", "Cluster3", "Cluster4", "Cluster5"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            2,
            2,
            [Spawners[42], Spawners[76], Spawners[167]],
            []),
        new(
            "ActivityNode_38/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "Clusters", "Cluster2", "Cluster3", "Cluster4", "Cluster5", "Cluster6"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            3,
            3,
            [Spawners[16], Spawners[40], Spawners[91]],
            []),
        new(
            "ActivityNode_39/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "WeaponCases"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            7,
            8,
            [Spawners[43], Spawners[44], Spawners[46], Spawners[47], Spawners[48], Spawners[49], Spawners[50], Spawners[51], Spawners[52], Spawners[53], Spawners[54], Spawners[55], Spawners[56], Spawners[57], Spawners[58], Spawners[59], Spawners[60], Spawners[61], Spawners[102], Spawners[105], Spawners[106], Spawners[118], Spawners[120], Spawners[121]],
            []),
        new(
            "ActivityNode_40/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "MedicalBags"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            7,
            10,
            [Spawners[0], Spawners[1], Spawners[3], Spawners[4], Spawners[5], Spawners[6], Spawners[7], Spawners[8], Spawners[9], Spawners[10], Spawners[11], Spawners[12], Spawners[13], Spawners[14], Spawners[18], Spawners[19], Spawners[101], Spawners[104], Spawners[107], Spawners[108], Spawners[111], Spawners[117], Spawners[123], Spawners[124], Spawners[126], Spawners[132]],
            []),
        new(
            "ActivityNode_41/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "GrenadeContainers"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            8,
            11,
            [Spawners[22], Spawners[23], Spawners[24], Spawners[25], Spawners[27], Spawners[28], Spawners[29], Spawners[31], Spawners[32], Spawners[33], Spawners[34], Spawners[35], Spawners[36], Spawners[37], Spawners[38], Spawners[90], Spawners[99], Spawners[100], Spawners[109], Spawners[110], Spawners[112], Spawners[113], Spawners[114], Spawners[122], Spawners[129], Spawners[131]],
            []),
        new(
            "ActivityNode_42/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "AmmoBoxes"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            10,
            12,
            [Spawners[62], Spawners[63], Spawners[64], Spawners[65], Spawners[66], Spawners[67], Spawners[68], Spawners[69], Spawners[70], Spawners[71], Spawners[72], Spawners[73], Spawners[74], Spawners[77], Spawners[78], Spawners[79], Spawners[80], Spawners[81], Spawners[82], Spawners[83], Spawners[84], Spawners[85], Spawners[86], Spawners[87], Spawners[89], Spawners[97], Spawners[98], Spawners[103], Spawners[115], Spawners[116], Spawners[119], Spawners[130], Spawners[133]],
            []),
        new(
            "ActivityNode_43/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "Clusters", "Cluster2", "Cluster3", "Cluster4", "Cluster5", "Cluster6", "Cluster7_PowerControlRoof"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            2,
            3,
            [Spawners[93], Spawners[94], Spawners[95], Spawners[96]],
            []),
        new(
            "ActivityNode_44/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "Backpacks"],
            ["CacheThemAll", "Coldsnap", "FTUE", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            14,
            18,
            [Spawners[136], Spawners[137], Spawners[138], Spawners[139], Spawners[140], Spawners[141], Spawners[142], Spawners[143], Spawners[144], Spawners[145], Spawners[146], Spawners[147], Spawners[148], Spawners[149], Spawners[150], Spawners[151], Spawners[152], Spawners[153], Spawners[154], Spawners[155], Spawners[156], Spawners[157], Spawners[158], Spawners[159], Spawners[160], Spawners[161], Spawners[162], Spawners[163], Spawners[164], Spawners[165], Spawners[166]],
            []),
        new(
            "ActivityNode_45/1",
            ["TheDam", "BaseGameplay", "RaiderContainers", "Clusters"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            3,
            3,
            [Spawners[15], Spawners[30], Spawners[88]],
            []),
        new(
            "ActivityNode_45/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "Clusters"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            6,
            6,
            [Spawners[173], Spawners[176], Spawners[177], Spawners[178], Spawners[179], Spawners[180]],
            []),
        new(
            "ActivityNode_46/1",
            ["TheDam", "BaseGameplay", "RaiderContainers", "Clusters", "Cluster2"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            4,
            4,
            [Spawners[2], Spawners[39], Spawners[45], Spawners[75]],
            []),
        new(
            "ActivityNode_46/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "Clusters", "Cluster2"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            2,
            2,
            [Spawners[174], Spawners[175]],
            []),
        new(
            "ActivityNode_47/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "Clusters", "Cluster2", "Cluster3"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            3,
            3,
            [Spawners[17], Spawners[26], Spawners[92]],
            []),
        new(
            "ActivityNode_48/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "Clusters", "Cluster2", "Cluster3", "Cluster4"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            3,
            3,
            [Spawners[20], Spawners[21], Spawners[41]],
            []),
        new(
            "ActivityNode_49/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "Clusters", "Cluster2", "Cluster3", "Cluster4", "Cluster5", "Cluster6", "Cluster7_PowerControlRoof", "Cluster8_WaterTower"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            2,
            3,
            [Spawners[125], Spawners[127], Spawners[128]],
            []),
        new(
            "ActivityNode_62/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "Clusters", "Cluster2", "Cluster3", "Cluster4", "Cluster5", "Cluster6", "Cluster7_PowerControlRoof", "Cluster8_WaterTower", "Cluster9_HydrodomeTowers"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            2,
            2,
            [Spawners[134], Spawners[135]],
            []),
        new(
            "ActivityNode_63/0",
            ["TheDam", "BaseGameplay", "RaiderContainers", "WeaponRacks"],
            ["CacheThemAll", "Coldsnap", "FTUE", "HarvestSeason", "Harvester", "HuskusPocus", "NightRaid", "ProbeHeist", "Spearmint", "Standard", "ToxicSwamp", "Turbulence"],
            5,
            6,
            [Spawners[172], Spawners[254], Spawners[255], Spawners[298], Spawners[299], Spawners[300], Spawners[301], Spawners[302]],
            []),
    ];
}
