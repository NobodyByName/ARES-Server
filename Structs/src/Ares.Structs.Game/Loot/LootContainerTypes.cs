using Ares.Structs.Game.World;

namespace Ares.Structs.Game.Loot;

/// <summary>Container classes for one colour variant of a prop, chosen by the prop's material.</summary>
/// <param name="Material">Object path of the material instance.</param>
/// <param name="Classes">Container classes to pick from.</param>
public sealed record LootContainerVariant(string Material, IReadOnlyList<ArchetypeClass> Classes);

/// <summary>The socket container types of one container socket tag.</summary>
/// <param name="Tag">Socket tag (<c>Socket.LootContainer.*</c>); a socket gets the type whose tag equals its own.</param>
/// <param name="Classes">Container classes to pick from by default, one at random.</param>
/// <param name="Variants">
/// Classes per material instance of the socketed prop. Empty when the asset ignores material overrides.
/// A prop whose material is not listed uses <paramref name="Classes"/>.
/// </param>
/// <param name="MaxPerGroup">Limit on the containers of this type per group.</param>
public sealed record LootContainerType(
    string Tag,
    IReadOnlyList<ArchetypeClass> Classes,
    IReadOnlyList<LootContainerVariant> Variants,
    int MaxPerGroup);

/// <summary>Container types by socket tag, for every type available to players.</summary>
public static class LootContainerTypes
{
    /// <summary>All container types, ordered by tag.</summary>
    public static IReadOnlyList<LootContainerType> All { get; } =
    [
        new(
            "Socket.LootCOntainer.Medical.RsrCartShelf.Drawer01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01.BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01_C", "/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01.Default__BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/MI_Rsr_CartShelf_M_01_Clean_A.MI_Rsr_CartShelf_M_01_Clean_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01_Clean.BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01_Clean_C", "/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01_Clean.Default__BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01_Clean_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/MI_Rsr_CartShelf_M_01_Clean_Red.MI_Rsr_CartShelf_M_01_Clean_Red", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01_Clean.BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01_Clean_C", "/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01_Clean.Default__BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01_Clean_C")]),
            ],
            1),
        new(
            "Socket.LootContainer.ARC.Parasitic.Lid",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Interactables/ParasiticContainer_Arc_01/BP_SocketContainer_WasabiPanel.BP_SocketContainer_WasabiPanel_C", "/Game/Pioneer/Environment/Props/Interactables/ParasiticContainer_Arc_01/BP_SocketContainer_WasabiPanel.Default__BP_SocketContainer_WasabiPanel_C")],
            [],
            2),
        new(
            "Socket.LootContainer.ARC.Payload01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Interactables/ArcPayloadDamaged_01/BP_SocketContainer_ArcPayload_01.BP_SocketContainer_ArcPayload_01_C", "/Game/Pioneer/Environment/Props/Interactables/ArcPayloadDamaged_01/BP_SocketContainer_ArcPayload_01.Default__BP_SocketContainer_ArcPayload_01_C")],
            [],
            3),
        new(
            "Socket.LootContainer.ARC.Wreck02",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/EnemyWreck_02/BP_SocketContainer_EnemyWreck_02_A.BP_SocketContainer_EnemyWreck_02_A_C", "/Game/Pioneer/Environment/Props/_Generic/EnemyWreck_02/BP_SocketContainer_EnemyWreck_02_A.Default__BP_SocketContainer_EnemyWreck_02_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Commercial.CashRegister",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/CashRegister_01/BP_SocketContainer_Cmr_CashRegister_01_Lid_01_A.BP_SocketContainer_Cmr_CashRegister_01_Lid_01_A_C", "/Game/Pioneer/Environment/Props/Commercial/CashRegister_01/BP_SocketContainer_Cmr_CashRegister_01_Lid_01_A.Default__BP_SocketContainer_Cmr_CashRegister_01_Lid_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Commercial.Centerbench.Drawers",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/CenterBench_M_01/BP_SocketContainer_Gvt_CenterBench_M_01_A.BP_SocketContainer_Gvt_CenterBench_M_01_A_C", "/Game/Pioneer/Environment/Props/Government/CenterBench_M_01/BP_SocketContainer_Gvt_CenterBench_M_01_A.Default__BP_SocketContainer_Gvt_CenterBench_M_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Commercial.CupboardCafe.DrawerA",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/CupboardCafe_01/BP_SocketContainer_Cmr_CupboardCafe_01_Drawer_01_A.BP_SocketContainer_Cmr_CupboardCafe_01_Drawer_01_A_C", "/Game/Pioneer/Environment/Props/Commercial/CupboardCafe_01/BP_SocketContainer_Cmr_CupboardCafe_01_Drawer_01_A.Default__BP_SocketContainer_Cmr_CupboardCafe_01_Drawer_01_A_C")],
            [],
            1),
        new(
            "Socket.LootContainer.Commercial.CupboardCafe.DrawerB",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/CupboardCafe_01/BP_SocketContainer_Cmr_CupboardCafe_01_Drawer_02_A.BP_SocketContainer_Cmr_CupboardCafe_01_Drawer_02_A_C", "/Game/Pioneer/Environment/Props/Commercial/CupboardCafe_01/BP_SocketContainer_Cmr_CupboardCafe_01_Drawer_02_A.Default__BP_SocketContainer_Cmr_CupboardCafe_01_Drawer_02_A_C")],
            [],
            1),
        new(
            "Socket.LootContainer.Commercial.Dumpster.Lid",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Dumpster_01/BP_SocketContainer_Res_Dumpster_Lid_01.BP_SocketContainer_Res_Dumpster_Lid_01_C", "/Game/Pioneer/Environment/Props/Residential/Dumpster_01/BP_SocketContainer_Res_Dumpster_Lid_01.Default__BP_SocketContainer_Res_Dumpster_Lid_01_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Residential/Dumpster_01/MI_Res_FZT_Dumpster_02.MI_Res_FZT_Dumpster_02", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Dumpster_01/BP_SocketContainer_Res_Dumpster_Lid_FZT.BP_SocketContainer_Res_Dumpster_Lid_FZT_C", "/Game/Pioneer/Environment/Props/Residential/Dumpster_01/BP_SocketContainer_Res_Dumpster_Lid_FZT.Default__BP_SocketContainer_Res_Dumpster_Lid_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Commercial.Freezer",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/Freezer_01/BP_SocketContainer_Cmr_Freezer_01_Lid_01_A.BP_SocketContainer_Cmr_Freezer_01_Lid_01_A_C", "/Game/Pioneer/Environment/Props/Commercial/Freezer_01/BP_SocketContainer_Cmr_Freezer_01_Lid_01_A.Default__BP_SocketContainer_Cmr_Freezer_01_Lid_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Commercial/Freezer_01/MI_FZT_Cmr_Freezer_01_A.MI_FZT_Cmr_Freezer_01_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/Freezer_01/BP_SocketContainer_Cmr_Freezer_01_Lid_01_A_FZT.BP_SocketContainer_Cmr_Freezer_01_Lid_01_A_FZT_C", "/Game/Pioneer/Environment/Props/Commercial/Freezer_01/BP_SocketContainer_Cmr_Freezer_01_Lid_01_A_FZT.Default__BP_SocketContainer_Cmr_Freezer_01_Lid_01_A_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Commercial.Fridge",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/Fridge_L_01/BP_SocketContainer_Cmr_Fridge_L_01_Lid_01_A.BP_SocketContainer_Cmr_Fridge_L_01_Lid_01_A_C", "/Game/Pioneer/Environment/Props/Commercial/Fridge_L_01/BP_SocketContainer_Cmr_Fridge_L_01_Lid_01_A.Default__BP_SocketContainer_Cmr_Fridge_L_01_Lid_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Commercial.Gvt.Trashbin",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/BinTrash_01/BP_SocketContainer_Gvt_BinTrash_01_A.BP_SocketContainer_Gvt_BinTrash_01_A_C", "/Game/Pioneer/Environment/Props/Government/BinTrash_01/BP_SocketContainer_Gvt_BinTrash_01_A.Default__BP_SocketContainer_Gvt_BinTrash_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/BinTrash_01/MI_FZT_Gvt_BinTrash_01_A.MI_FZT_Gvt_BinTrash_01_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/BinTrash_01/BP_SocketContainer_Gvt_BinTrash_01_FZT.BP_SocketContainer_Gvt_BinTrash_01_FZT_C", "/Game/Pioneer/Environment/Props/Government/BinTrash_01/BP_SocketContainer_Gvt_BinTrash_01_FZT.Default__BP_SocketContainer_Gvt_BinTrash_01_FZT_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/BinTrash_01/MI_Gvt_BinTrash_01_C.MI_Gvt_BinTrash_01_C", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/BinTrash_01/BP_SocketContainer_Gvt_BinTrash_01_C.BP_SocketContainer_Gvt_BinTrash_01_C_C", "/Game/Pioneer/Environment/Props/Government/BinTrash_01/BP_SocketContainer_Gvt_BinTrash_01_C.Default__BP_SocketContainer_Gvt_BinTrash_01_C_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Commercial.GvtDesk.Drawer",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01.BP_SocketContainer_Gvt_Desk_01_C", "/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01.Default__BP_SocketContainer_Gvt_Desk_01_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Desk_01/MI_Gvt_Desk_01_A_Blue.MI_Gvt_Desk_01_A_Blue", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01_Blue.BP_SocketContainer_Gvt_Desk_01_Blue_C", "/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01_Blue.Default__BP_SocketContainer_Gvt_Desk_01_Blue_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Desk_01/MI_Gvt_Desk_01_A_Green.MI_Gvt_Desk_01_A_Green", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01_Green.BP_SocketContainer_Gvt_Desk_01_Green_C", "/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01_Green.Default__BP_SocketContainer_Gvt_Desk_01_Green_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Desk_01/MI_Gvt_Desk_01_A_Red.MI_Gvt_Desk_01_A_Red", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01_Red.BP_SocketContainer_Gvt_Desk_01_Red_C", "/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01_Red.Default__BP_SocketContainer_Gvt_Desk_01_Red_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Desk_01/MI_Gvt_Desk_01_Corner_A_Blue.MI_Gvt_Desk_01_Corner_A_Blue", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01_Blue.BP_SocketContainer_Gvt_Desk_01_Blue_C", "/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01_Blue.Default__BP_SocketContainer_Gvt_Desk_01_Blue_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Desk_01/MI_Gvt_Desk_01_Corner_A_Red.MI_Gvt_Desk_01_Corner_A_Red", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01_Red.BP_SocketContainer_Gvt_Desk_01_Red_C", "/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01_Red.Default__BP_SocketContainer_Gvt_Desk_01_Red_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Desk_01/MI_Gvt_Desk_01_Corner_A_SpaceTravelCo.MI_Gvt_Desk_01_Corner_A_SpaceTravelCo", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01_SpaceTravelCo.BP_SocketContainer_Gvt_Desk_01_SpaceTravelCo_C", "/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01_SpaceTravelCo.Default__BP_SocketContainer_Gvt_Desk_01_SpaceTravelCo_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Desk_01/MI_Gvt_Desk_01_SpaceTravelCo.MI_Gvt_Desk_01_SpaceTravelCo", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01_SpaceTravelCo.BP_SocketContainer_Gvt_Desk_01_SpaceTravelCo_C", "/Game/Pioneer/Environment/Props/Government/Desk_01/BP_SocketContainer_Gvt_Desk_01_SpaceTravelCo.Default__BP_SocketContainer_Gvt_Desk_01_SpaceTravelCo_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Commercial.Shelf03",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/Shelf_03/BP_SocketContainer_Cmr_Shelf_03_Drawer_C.BP_SocketContainer_Cmr_Shelf_03_Drawer_C_C", "/Game/Pioneer/Environment/Props/Commercial/Shelf_03/BP_SocketContainer_Cmr_Shelf_03_Drawer_C.Default__BP_SocketContainer_Cmr_Shelf_03_Drawer_C_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Commercial.StackingBox",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/StackingBoxes/BP_SocketContainer_StackingBox.BP_SocketContainer_StackingBox_C", "/Game/Pioneer/Environment/Props/Commercial/StackingBoxes/BP_SocketContainer_StackingBox.Default__BP_SocketContainer_StackingBox_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Commercial.StackingBox.Lid",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/StackingBoxes/BP_SocketContainer_StackingBox_Lid_A.BP_SocketContainer_StackingBox_Lid_A_C", "/Game/Pioneer/Environment/Props/Commercial/StackingBoxes/BP_SocketContainer_StackingBox_Lid_A.Default__BP_SocketContainer_StackingBox_Lid_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Commercial.StackingBox.Lid2",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/StackingBoxes/BP_SocketContainer_StackingBox_Lid_B.BP_SocketContainer_StackingBox_Lid_B_C", "/Game/Pioneer/Environment/Props/Commercial/StackingBoxes/BP_SocketContainer_StackingBox_Lid_B.Default__BP_SocketContainer_StackingBox_Lid_B_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Commercial.StackingBox.Lid3",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/StackingBoxes/BP_SocketContainer_StackingBox_Lid_C.BP_SocketContainer_StackingBox_Lid_C_C", "/Game/Pioneer/Environment/Props/Commercial/StackingBoxes/BP_SocketContainer_StackingBox_Lid_C.Default__BP_SocketContainer_StackingBox_Lid_C_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Commercial.TrashBin.LidA",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/TrashBin_01/BP_SocketContainer_01_TrashBin_A_01.BP_SocketContainer_01_TrashBin_A_01_C", "/Game/Pioneer/Environment/Props/Commercial/TrashBin_01/BP_SocketContainer_01_TrashBin_A_01.Default__BP_SocketContainer_01_TrashBin_A_01_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Commercial.Wrh.FilingCabinet.Drawer",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/Drawers_01/BP_SocketContainer_Wrh_Drawers_01_A.BP_SocketContainer_Wrh_Drawers_01_A_C", "/Game/Pioneer/Environment/Props/Warehouse/Drawers_01/BP_SocketContainer_Wrh_Drawers_01_A.Default__BP_SocketContainer_Wrh_Drawers_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.Android.Lid1",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Interactables/Android_01/BP_SocketContainer_Android_01_Lid_A.BP_SocketContainer_Android_01_Lid_A_C", "/Game/Pioneer/Environment/Props/Interactables/Android_01/BP_SocketContainer_Android_01_Lid_A.Default__BP_SocketContainer_Android_01_Lid_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.Android.Lid2",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Interactables/Android_01/BP_SocketContainer_Android_01_Lid_B.BP_SocketContainer_Android_01_Lid_B_C", "/Game/Pioneer/Environment/Props/Interactables/Android_01/BP_SocketContainer_Android_01_Lid_B.Default__BP_SocketContainer_Android_01_Lid_B_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.AntennaSet.Lid1",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/AntennaSet_01/BP_SocketContainer_AntennaSet_01_Lid_01_A.BP_SocketContainer_AntennaSet_01_Lid_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/AntennaSet_01/BP_SocketContainer_AntennaSet_01_Lid_01_A.Default__BP_SocketContainer_AntennaSet_01_Lid_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.AntennaXL.Lid01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Antenna_XL_01/BP_SocketContainer_Antenaa_XL_01_Lid_01_A.BP_SocketContainer_Antenaa_XL_01_Lid_01_A_C", "/Game/Pioneer/Environment/Props/Government/Antenna_XL_01/BP_SocketContainer_Antenaa_XL_01_Lid_01_A.Default__BP_SocketContainer_Antenaa_XL_01_Lid_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.AntennaXL.Lid02",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Antenna_XL_01/BP_SocketContainer_Antenaa_XL_01_Lid_02_A.BP_SocketContainer_Antenaa_XL_01_Lid_02_A_C", "/Game/Pioneer/Environment/Props/Government/Antenna_XL_01/BP_SocketContainer_Antenaa_XL_01_Lid_02_A.Default__BP_SocketContainer_Antenaa_XL_01_Lid_02_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.ButtonPanel",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/WallAdd_M_02/BP_SocketContainer_Wrh_WallAdd_M_02_A.BP_SocketContainer_Wrh_WallAdd_M_02_A_C", "/Game/Pioneer/Environment/Props/Warehouse/WallAdd_M_02/BP_SocketContainer_Wrh_WallAdd_M_02_A.Default__BP_SocketContainer_Wrh_WallAdd_M_02_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/WallAdd_M_02/MI_FZT_Wrh_WallAdd_M_02_A.MI_FZT_Wrh_WallAdd_M_02_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/WallAdd_M_02/BP_SocketContainer_Wrh_WallAdd_M_02_A_FZT.BP_SocketContainer_Wrh_WallAdd_M_02_A_FZT_C", "/Game/Pioneer/Environment/Props/Warehouse/WallAdd_M_02/BP_SocketContainer_Wrh_WallAdd_M_02_A_FZT.Default__BP_SocketContainer_Wrh_WallAdd_M_02_A_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Electrical.CabinetDoor",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/ElectricalCabinet_01/BP_SocketContainer_Rsr_ElectricalCabinet_01_A.BP_SocketContainer_Rsr_ElectricalCabinet_01_A_C", "/Game/Pioneer/Environment/Props/Research/ElectricalCabinet_01/BP_SocketContainer_Rsr_ElectricalCabinet_01_A.Default__BP_SocketContainer_Rsr_ElectricalCabinet_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/ElectricalCabinet_01/MI_Rsr_ElectricalCabinet_01_Clean_A_01.MI_Rsr_ElectricalCabinet_01_Clean_A_01", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/ElectricalCabinet_01/BP_SocketContainer_Rsr_ElectricalCabinet_01_A_Clean.BP_SocketContainer_Rsr_ElectricalCabinet_01_A_Clean_C", "/Game/Pioneer/Environment/Props/Research/ElectricalCabinet_01/BP_SocketContainer_Rsr_ElectricalCabinet_01_A_Clean.Default__BP_SocketContainer_Rsr_ElectricalCabinet_01_A_Clean_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/ElectricalCabinet_01/MI_Rsr_ElectricalCabinet_01_Red_A.MI_Rsr_ElectricalCabinet_01_Red_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/ElectricalCabinet_01/BP_SocketContainer_Rsr_ElectricalCabinet_01_A_Red.BP_SocketContainer_Rsr_ElectricalCabinet_01_A_Red_C", "/Game/Pioneer/Environment/Props/Research/ElectricalCabinet_01/BP_SocketContainer_Rsr_ElectricalCabinet_01_A_Red.Default__BP_SocketContainer_Rsr_ElectricalCabinet_01_A_Red_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Electrical.Cmr.WallAdd_01.Lid_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/WallAdd_01/BP_SocketContainer_Cmr_WallAdd_01_A_Lid_01.BP_SocketContainer_Cmr_WallAdd_01_A_Lid_01_C", "/Game/Pioneer/Environment/Props/Commercial/WallAdd_01/BP_SocketContainer_Cmr_WallAdd_01_A_Lid_01.Default__BP_SocketContainer_Cmr_WallAdd_01_A_Lid_01_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.Cmr.WallAdd_01.Lid_02",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/WallAdd_01/BP_SocketContainer_Cmr_WallAdd_01_A_Lid_02.BP_SocketContainer_Cmr_WallAdd_01_A_Lid_02_C", "/Game/Pioneer/Environment/Props/Commercial/WallAdd_01/BP_SocketContainer_Cmr_WallAdd_01_A_Lid_02.Default__BP_SocketContainer_Cmr_WallAdd_01_A_Lid_02_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.Cmr.WallAdd_03.Lid_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/WallAdd_03/BP_SocketContainer_Cmr_WallAdd_03_A_Lid_01.BP_SocketContainer_Cmr_WallAdd_03_A_Lid_01_C", "/Game/Pioneer/Environment/Props/Commercial/WallAdd_03/BP_SocketContainer_Cmr_WallAdd_03_A_Lid_01.Default__BP_SocketContainer_Cmr_WallAdd_03_A_Lid_01_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.CoffeeMachine01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/CoffeeMachine_01/BP_SocketContainer_Cmr_CoffeeMachine_01_A.BP_SocketContainer_Cmr_CoffeeMachine_01_A_C", "/Game/Pioneer/Environment/Props/Commercial/CoffeeMachine_01/BP_SocketContainer_Cmr_CoffeeMachine_01_A.Default__BP_SocketContainer_Cmr_CoffeeMachine_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Commercial/CoffeeMachine_01/MI_FZT_Cmr_CoffeeMachine_01_A_01.MI_FZT_Cmr_CoffeeMachine_01_A_01", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/CoffeeMachine_01/BP_SocketContainer_Cmr_CoffeeMachine_01_A_FZT.BP_SocketContainer_Cmr_CoffeeMachine_01_A_FZT_C", "/Game/Pioneer/Environment/Props/Commercial/CoffeeMachine_01/BP_SocketContainer_Cmr_CoffeeMachine_01_A_FZT.Default__BP_SocketContainer_Cmr_CoffeeMachine_01_A_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Electrical.DisplayTimeBoard_02.Lid_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/DisplayTimeBoard_01/BP_SocketContainer_DisplayTimeBoard_02_A.BP_SocketContainer_DisplayTimeBoard_02_A_C", "/Game/Pioneer/Environment/Props/_Generic/DisplayTimeBoard_01/BP_SocketContainer_DisplayTimeBoard_02_A.Default__BP_SocketContainer_DisplayTimeBoard_02_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.DroidTorso",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Interactables/DestroyedRobot_01/BP_Destroyed_Robot_01_TorsoLid_A.BP_Destroyed_Robot_01_TorsoLid_A_C", "/Game/Pioneer/Environment/Props/Interactables/DestroyedRobot_01/BP_Destroyed_Robot_01_TorsoLid_A.Default__BP_Destroyed_Robot_01_TorsoLid_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.FloodLight.Lid01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/FloodlightTower_01/BP_SocketContainer_FloodlightTower_01_Lid_01_A.BP_SocketContainer_FloodlightTower_01_Lid_01_A_C", "/Game/Pioneer/Environment/Props/Government/FloodlightTower_01/BP_SocketContainer_FloodlightTower_01_Lid_01_A.Default__BP_SocketContainer_FloodlightTower_01_Lid_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.FloodLight.Lid02",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/FloodlightTower_01/BP_SocketContainer_FloodlightTower_01_Lid_02_A.BP_SocketContainer_FloodlightTower_01_Lid_02_A_C", "/Game/Pioneer/Environment/Props/Government/FloodlightTower_01/BP_SocketContainer_FloodlightTower_01_Lid_02_A.Default__BP_SocketContainer_FloodlightTower_01_Lid_02_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.Fusebox.PanelLarge",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/WallAdd_S_01/BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelLarge.BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelLarge_C", "/Game/Pioneer/Environment/Props/Warehouse/WallAdd_S_01/BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelLarge.Default__BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelLarge_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/WallAdd_S_01/MI_FZT_Wrh_WallAdd_S_01_A.MI_FZT_Wrh_WallAdd_S_01_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/WallAdd_S_01/BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelLarge_FZT.BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelLarge_FZT_C", "/Game/Pioneer/Environment/Props/Warehouse/WallAdd_S_01/BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelLarge_FZT.Default__BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelLarge_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Electrical.Fusebox.PanelSmall",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/WallAdd_S_01/BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelSmall.BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelSmall_C", "/Game/Pioneer/Environment/Props/Warehouse/WallAdd_S_01/BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelSmall.Default__BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelSmall_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/WallAdd_S_01/MI_FZT_Wrh_WallAdd_S_01_A.MI_FZT_Wrh_WallAdd_S_01_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/WallAdd_S_01/BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelSmall_FZT.BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelSmall_FZT_C", "/Game/Pioneer/Environment/Props/Warehouse/WallAdd_S_01/BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelSmall_FZT.Default__BP_SocketContainer_Wrh_WallAdd_S_01_A_PanelSmall_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Electrical.Fusebox2.Lid",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/FuseBox_01/BP_SocketContainer_FuseBox_01_A.BP_SocketContainer_FuseBox_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/FuseBox_01/BP_SocketContainer_FuseBox_01_A.Default__BP_SocketContainer_FuseBox_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/FuseBox_01/MI_FZT_FuseBox_01_A.MI_FZT_FuseBox_01_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/FuseBox_01/BP_SocketContainer_FuseBox_01_A_FZT.BP_SocketContainer_FuseBox_01_A_FZT_C", "/Game/Pioneer/Environment/Props/_Generic/FuseBox_01/BP_SocketContainer_FuseBox_01_A_FZT.Default__BP_SocketContainer_FuseBox_01_A_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Electrical.GeneratorM02.Lid01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Generator_M_02/BP_SocketContainer_Generator_M_02_Lid_01.BP_SocketContainer_Generator_M_02_Lid_01_C", "/Game/Pioneer/Environment/Props/Government/Generator_M_02/BP_SocketContainer_Generator_M_02_Lid_01.Default__BP_SocketContainer_Generator_M_02_Lid_01_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.GeneratorM02.Lid02",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Generator_M_02/BP_SocketContainer_Generator_M_02_Lid_02.BP_SocketContainer_Generator_M_02_Lid_02_C", "/Game/Pioneer/Environment/Props/Government/Generator_M_02/BP_SocketContainer_Generator_M_02_Lid_02.Default__BP_SocketContainer_Generator_M_02_Lid_02_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Generator_M_02/MI_FZT_Gvt_Generator_M.MI_FZT_Gvt_Generator_M", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Generator_M_02/BP_SocketContainer_Generator_M_02_Lid_02_FZT.BP_SocketContainer_Generator_M_02_Lid_02_FZT_C", "/Game/Pioneer/Environment/Props/Government/Generator_M_02/BP_SocketContainer_Generator_M_02_Lid_02_FZT.Default__BP_SocketContainer_Generator_M_02_Lid_02_FZT_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Generator_M_02/MI_Gvt_Generator_M_02_Green.MI_Gvt_Generator_M_02_Green", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Generator_M_02/BP_SocketContainer_Generator_M_02_Lid_02_Green.BP_SocketContainer_Generator_M_02_Lid_02_Green_C", "/Game/Pioneer/Environment/Props/Government/Generator_M_02/BP_SocketContainer_Generator_M_02_Lid_02_Green.Default__BP_SocketContainer_Generator_M_02_Lid_02_Green_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Generator_M_02/MI_Gvt_Generator_M_02_Orange.MI_Gvt_Generator_M_02_Orange", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Generator_M_02/BP_SocketContainer_Generator_M_02_Lid_02_Orange.BP_SocketContainer_Generator_M_02_Lid_02_Orange_C", "/Game/Pioneer/Environment/Props/Government/Generator_M_02/BP_SocketContainer_Generator_M_02_Lid_02_Orange.Default__BP_SocketContainer_Generator_M_02_Lid_02_Orange_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Electrical.HighwayAntenna",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Highway_01/BP_SocketContainer_HighwayAddon_Antenna_01.BP_SocketContainer_HighwayAddon_Antenna_01_C", "/Game/Pioneer/Environment/Props/_Generic/Highway_01/BP_SocketContainer_HighwayAddon_Antenna_01.Default__BP_SocketContainer_HighwayAddon_Antenna_01_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.LargeTicketBooth",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/LargeTicketBooth_01/BP_SocketContainer_LargeTicketBooth_Lid.BP_SocketContainer_LargeTicketBooth_Lid_C", "/Game/Pioneer/Environment/Props/Commercial/LargeTicketBooth_01/BP_SocketContainer_LargeTicketBooth_Lid.Default__BP_SocketContainer_LargeTicketBooth_Lid_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.PhoneBooth",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/PhoneBooth_01/BP_SocketContainer_PhoneBoothLid.BP_SocketContainer_PhoneBoothLid_C", "/Game/Pioneer/Environment/Props/Commercial/PhoneBooth_01/BP_SocketContainer_PhoneBoothLid.Default__BP_SocketContainer_PhoneBoothLid_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.PowerGenerator.Lid01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/PowerGenerator_01/BP_SocketContainer_PowerGenerator_01_A.BP_SocketContainer_PowerGenerator_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/PowerGenerator_01/BP_SocketContainer_PowerGenerator_01_A.Default__BP_SocketContainer_PowerGenerator_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.RoadBarrier03.Lid01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/RoadBarrier_03/BP_SocketContainer_RoadBarrier_03_Lid_01_A.BP_SocketContainer_RoadBarrier_03_Lid_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/RoadBarrier_03/BP_SocketContainer_RoadBarrier_03_Lid_01_A.Default__BP_SocketContainer_RoadBarrier_03_Lid_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.RoadBarrier03.Lid02",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/RoadBarrier_03/BP_SocketContainer_RoadBarrier_03_Lid_01_A.BP_SocketContainer_RoadBarrier_03_Lid_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/RoadBarrier_03/BP_SocketContainer_RoadBarrier_03_Lid_01_A.Default__BP_SocketContainer_RoadBarrier_03_Lid_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.RoadBarrier03.Lid03",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/RoadBarrier_03/BP_SocketContainer_RoadBarrier_03_Lid_01_A.BP_SocketContainer_RoadBarrier_03_Lid_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/RoadBarrier_03/BP_SocketContainer_RoadBarrier_03_Lid_01_A.Default__BP_SocketContainer_RoadBarrier_03_Lid_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.RoadBarrier03.Lid04",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/RoadBarrier_03/BP_SocketContainer_RoadBarrier_03_Lid_01_A.BP_SocketContainer_RoadBarrier_03_Lid_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/RoadBarrier_03/BP_SocketContainer_RoadBarrier_03_Lid_01_A.Default__BP_SocketContainer_RoadBarrier_03_Lid_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.Signals",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Trains/ExodusTrain_LineAccessories/ExodusTrain_Signals_01/BP_SocketContainer_Signals_Addon_Lid_02_A.BP_SocketContainer_Signals_Addon_Lid_02_A_C", "/Game/Pioneer/Environment/Props/Trains/ExodusTrain_LineAccessories/ExodusTrain_Signals_01/BP_SocketContainer_Signals_Addon_Lid_02_A.Default__BP_SocketContainer_Signals_Addon_Lid_02_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.SolarPanel03.HatchA",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/SolarPanel_03/BP_SocketContainer_SolarPanelHatch_03_A.BP_SocketContainer_SolarPanelHatch_03_A_C", "/Game/Pioneer/Environment/Props/_Generic/SolarPanel_03/BP_SocketContainer_SolarPanelHatch_03_A.Default__BP_SocketContainer_SolarPanelHatch_03_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.SolarPanel03.HatchB",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/SolarPanel_03/BP_SocketContainer_SolarPanelHatch_03_B.BP_SocketContainer_SolarPanelHatch_03_B_C", "/Game/Pioneer/Environment/Props/_Generic/SolarPanel_03/BP_SocketContainer_SolarPanelHatch_03_B.Default__BP_SocketContainer_SolarPanelHatch_03_B_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.SosBooth_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/SOS_Booth_01/BP_SocketContainer_Sosbooth_01_A.BP_SocketContainer_Sosbooth_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/SOS_Booth_01/BP_SocketContainer_Sosbooth_01_A.Default__BP_SocketContainer_Sosbooth_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.SupportStructure.Lid",
            [new ArchetypeClass("/Game/Pioneer/Environment/POI/TheBlueGate/Support_Structures/BP_SocketContainer_SupportStructure_Small_01.BP_SocketContainer_SupportStructure_Small_01_C", "/Game/Pioneer/Environment/POI/TheBlueGate/Support_Structures/BP_SocketContainer_SupportStructure_Small_01.Default__BP_SocketContainer_SupportStructure_Small_01_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.Transformer.PanelLarge",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/WallAdd_L_01/BP_SocketContainer_Wrh_WallAdd_L_01_A_LargePanel.BP_SocketContainer_Wrh_WallAdd_L_01_A_LargePanel_C", "/Game/Pioneer/Environment/Props/Research/WallAdd_L_01/BP_SocketContainer_Wrh_WallAdd_L_01_A_LargePanel.Default__BP_SocketContainer_Wrh_WallAdd_L_01_A_LargePanel_C")],
            [],
            1),
        new(
            "Socket.LootContainer.Electrical.Transformer.PanelMain",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/WallAdd_L_01/BP_SocketContainer_Wrh_WallAdd_L_01_A_MainPanel.BP_SocketContainer_Wrh_WallAdd_L_01_A_MainPanel_C", "/Game/Pioneer/Environment/Props/Research/WallAdd_L_01/BP_SocketContainer_Wrh_WallAdd_L_01_A_MainPanel.Default__BP_SocketContainer_Wrh_WallAdd_L_01_A_MainPanel_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.Transformer.PanelSmall",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/WallAdd_L_01/BP_SocketContainer_Wrh_WallAdd_L_01_A_SmallPanel.BP_SocketContainer_Wrh_WallAdd_L_01_A_SmallPanel_C", "/Game/Pioneer/Environment/Props/Research/WallAdd_L_01/BP_SocketContainer_Wrh_WallAdd_L_01_A_SmallPanel.Default__BP_SocketContainer_Wrh_WallAdd_L_01_A_SmallPanel_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.VendingMachine.Door",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/VendingMachine_01/BP_SocketContainer_VendingMachineDoor.BP_SocketContainer_VendingMachineDoor_C", "/Game/Pioneer/Environment/Props/Commercial/VendingMachine_01/BP_SocketContainer_VendingMachineDoor.Default__BP_SocketContainer_VendingMachineDoor_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Commercial/VendingMachine_01/MI_FZT_VendingMachine_01.MI_FZT_VendingMachine_01", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/VendingMachine_01/BP_SocketContainer_VendingMachineDoor.BP_SocketContainer_VendingMachineDoor_C", "/Game/Pioneer/Environment/Props/Commercial/VendingMachine_01/BP_SocketContainer_VendingMachineDoor.Default__BP_SocketContainer_VendingMachineDoor_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Electrical.VentMachineLarge.Lid",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/VentMachineLarge_02_A/BP_SocketContainer_VentMachineLarge_02_A.BP_SocketContainer_VentMachineLarge_02_A_C", "/Game/Pioneer/Environment/Props/_Generic/VentMachineLarge_02_A/BP_SocketContainer_VentMachineLarge_02_A.Default__BP_SocketContainer_VentMachineLarge_02_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.VentMachineLarge01.Lid",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/VentMachineLarge_01_A/BP_SocketContainer_VentMachineLarge_01_A.BP_SocketContainer_VentMachineLarge_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/VentMachineLarge_01_A/BP_SocketContainer_VentMachineLarge_01_A.Default__BP_SocketContainer_VentMachineLarge_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Electrical.vent03.Lid01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Vent_03/BP_SocketContainer_Vent_03_A.BP_SocketContainer_Vent_03_A_C", "/Game/Pioneer/Environment/Props/_Generic/Vent_03/BP_SocketContainer_Vent_03_A.Default__BP_SocketContainer_Vent_03_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/Vent_03/MI_FZT_Vent_03_Details_01_B.MI_FZT_Vent_03_Details_01_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Vent_03/BP_SocketContainer_Vent_03_A_fzt.BP_SocketContainer_Vent_03_A_fzt_C", "/Game/Pioneer/Environment/Props/_Generic/Vent_03/BP_SocketContainer_Vent_03_A_fzt.Default__BP_SocketContainer_Vent_03_A_fzt_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Exodus.Rocketparts_01.Lid_A",
            [new ArchetypeClass("/Game/Pioneer/Environment/POI/POI09/Props/RocketParts_01/BP_SocketContainer_Rocketparts_01_A_Lid.BP_SocketContainer_Rocketparts_01_A_Lid_C", "/Game/Pioneer/Environment/POI/POI09/Props/RocketParts_01/BP_SocketContainer_Rocketparts_01_A_Lid.Default__BP_SocketContainer_Rocketparts_01_A_Lid_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Exodus.Rocketparts_01.Lid_B",
            [new ArchetypeClass("/Game/Pioneer/Environment/POI/POI09/Props/RocketParts_01/BP_SocketContainer_Rocketparts_01_B_Lid.BP_SocketContainer_Rocketparts_01_B_Lid_C", "/Game/Pioneer/Environment/POI/POI09/Props/RocketParts_01/BP_SocketContainer_Rocketparts_01_B_Lid.Default__BP_SocketContainer_Rocketparts_01_B_Lid_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Exodus.Rocketparts_01.Lid_C",
            [new ArchetypeClass("/Game/Pioneer/Environment/POI/POI09/Props/RocketParts_01/BP_SocketContainer_Rocketparts_01_C_Lid.BP_SocketContainer_Rocketparts_01_C_Lid_C", "/Game/Pioneer/Environment/POI/POI09/Props/RocketParts_01/BP_SocketContainer_Rocketparts_01_C_Lid.Default__BP_SocketContainer_Rocketparts_01_C_Lid_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Exodus.Rocketparts_02.Lid_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/POI/POI09/Props/RocketParts_02/BP_SocketContainer_Rocketparts_02_Lid_01.BP_SocketContainer_Rocketparts_02_Lid_01_C", "/Game/Pioneer/Environment/POI/POI09/Props/RocketParts_02/BP_SocketContainer_Rocketparts_02_Lid_01.Default__BP_SocketContainer_Rocketparts_02_Lid_01_C")],
            [],
            1),
        new(
            "Socket.LootContainer.Exodus.Rocketparts_02.Lid_02",
            [new ArchetypeClass("/Game/Pioneer/Environment/POI/POI09/Props/RocketParts_02/BP_SocketContainer_Rocketparts_02_Lid_02.BP_SocketContainer_Rocketparts_02_Lid_02_C", "/Game/Pioneer/Environment/POI/POI09/Props/RocketParts_02/BP_SocketContainer_Rocketparts_02_Lid_02.Default__BP_SocketContainer_Rocketparts_02_Lid_02_C")],
            [],
            1),
        new(
            "Socket.LootContainer.Generic.CrateCart",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/CrateCart_01/BP_SocketContainer_CartCrate_01_A.BP_SocketContainer_CartCrate_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/CrateCart_01/BP_SocketContainer_CartCrate_01_A.Default__BP_SocketContainer_CartCrate_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Generic.TimeStampDrawer",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/TimeStampRecorder_01/BP_SocketContainer_Rsr_TimeStamp_03.BP_SocketContainer_Rsr_TimeStamp_03_C", "/Game/Pioneer/Environment/Props/Government/TimeStampRecorder_01/BP_SocketContainer_Rsr_TimeStamp_03.Default__BP_SocketContainer_Rsr_TimeStamp_03_C")],
            [],
            1),
        new(
            "Socket.LootContainer.Generic.TimeStampLocker",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/TimeStampRecorder_01/BP_SocketContainer_TimesStamp_01_A.BP_SocketContainer_TimesStamp_01_A_C", "/Game/Pioneer/Environment/Props/Government/TimeStampRecorder_01/BP_SocketContainer_TimesStamp_01_A.Default__BP_SocketContainer_TimesStamp_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Industrial.Crate.TopLid",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Crate_M_01/BP_SocketContainer_Crate_M_01_A.BP_SocketContainer_Crate_M_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/Crate_M_01/BP_SocketContainer_Crate_M_01_A.Default__BP_SocketContainer_Crate_M_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/Crate_M_01/MI_Crate_M_01_B.MI_Crate_M_01_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Crate_M_01/BP_SocketContainer_Crate_M_01_B.BP_SocketContainer_Crate_M_01_B_C", "/Game/Pioneer/Environment/Props/_Generic/Crate_M_01/BP_SocketContainer_Crate_M_01_B.Default__BP_SocketContainer_Crate_M_01_B_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Industrial.Lockers.Door",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Lockers_01/BP_SocketContainer_Rsr_Lockers_01_A.BP_SocketContainer_Rsr_Lockers_01_A_C", "/Game/Pioneer/Environment/Props/Research/Lockers_01/BP_SocketContainer_Rsr_Lockers_01_A.Default__BP_SocketContainer_Rsr_Lockers_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Lockers_01/MI_Rsr_Lockers_01_A_BC_ResearchCompany.MI_Rsr_Lockers_01_A_BC_ResearchCompany", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Lockers_01/BP_SocketContainer_Rsr_Lockers_01_A_BC_ResearchCompany.BP_SocketContainer_Rsr_Lockers_01_A_BC_ResearchCompany_C", "/Game/Pioneer/Environment/Props/Research/Lockers_01/BP_SocketContainer_Rsr_Lockers_01_A_BC_ResearchCompany.Default__BP_SocketContainer_Rsr_Lockers_01_A_BC_ResearchCompany_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Lockers_01/MI_Rsr_Lockers_01_A_BC_School_01.MI_Rsr_Lockers_01_A_BC_School_01", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Lockers_01/BP_SocketContainer_Rsr_Lockers_01_A_BC_School_01.BP_SocketContainer_Rsr_Lockers_01_A_BC_School_01_C", "/Game/Pioneer/Environment/Props/Research/Lockers_01/BP_SocketContainer_Rsr_Lockers_01_A_BC_School_01.Default__BP_SocketContainer_Rsr_Lockers_01_A_BC_School_01_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Lockers_01/MI_Rsr_Lockers_01_A_BC_SpaceTravelCo_01.MI_Rsr_Lockers_01_A_BC_SpaceTravelCo_01", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Lockers_01/BP_SocketContainer_Rsr_Lockers_01_A_BC_SpaceTravelCo_01.BP_SocketContainer_Rsr_Lockers_01_A_BC_SpaceTravelCo_01_C", "/Game/Pioneer/Environment/Props/Research/Lockers_01/BP_SocketContainer_Rsr_Lockers_01_A_BC_SpaceTravelCo_01.Default__BP_SocketContainer_Rsr_Lockers_01_A_BC_SpaceTravelCo_01_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Lockers_01/MI_Rsr_Lockers_01_A_Blue.MI_Rsr_Lockers_01_A_Blue", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Lockers_01/BP_SocketContainer_Rsr_Lockers_01_A_Blue.BP_SocketContainer_Rsr_Lockers_01_A_Blue_C", "/Game/Pioneer/Environment/Props/Research/Lockers_01/BP_SocketContainer_Rsr_Lockers_01_A_Blue.Default__BP_SocketContainer_Rsr_Lockers_01_A_Blue_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Lockers_01/MI_Rsr_Lockers_01_A_Green.MI_Rsr_Lockers_01_A_Green", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Lockers_01/BP_SocketContainer_Rsr_Lockers_01_A_Green.BP_SocketContainer_Rsr_Lockers_01_A_Green_C", "/Game/Pioneer/Environment/Props/Research/Lockers_01/BP_SocketContainer_Rsr_Lockers_01_A_Green.Default__BP_SocketContainer_Rsr_Lockers_01_A_Green_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Industrial.RsrMachineM01.Door",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Machine_M_01/BP_SocketContainer_Rsr_Machine_M_01_A.BP_SocketContainer_Rsr_Machine_M_01_A_C", "/Game/Pioneer/Environment/Props/Research/Machine_M_01/BP_SocketContainer_Rsr_Machine_M_01_A.Default__BP_SocketContainer_Rsr_Machine_M_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Industrial.Wrh.ShippingCon.Drawer",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/Container_L_02/BP_SocketContainer_Wrh_Container_L_02_B.BP_SocketContainer_Wrh_Container_L_02_B_C", "/Game/Pioneer/Environment/Props/Warehouse/Container_L_02/BP_SocketContainer_Wrh_Container_L_02_B.Default__BP_SocketContainer_Wrh_Container_L_02_B_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Industrial.Wrh.ToolTrolley.Tray",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/TrolleyTools_01/BP_SocketContainer_Wrh_TrolleyTools_01_A.BP_SocketContainer_Wrh_TrolleyTools_01_A_C", "/Game/Pioneer/Environment/Props/Warehouse/TrolleyTools_01/BP_SocketContainer_Wrh_TrolleyTools_01_A.Default__BP_SocketContainer_Wrh_TrolleyTools_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/TrolleyTools_01/MI_FZT_Wrh_TrolleyTools_01_A_Red.MI_FZT_Wrh_TrolleyTools_01_A_Red", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/TrolleyTools_01/BP_SocketContainer_Wrh_TrolleyTools_01_A.BP_SocketContainer_Wrh_TrolleyTools_01_A_C", "/Game/Pioneer/Environment/Props/Warehouse/TrolleyTools_01/BP_SocketContainer_Wrh_TrolleyTools_01_A.Default__BP_SocketContainer_Wrh_TrolleyTools_01_A_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/TrolleyTools_01/MI_Wrh_TrolleyTools_01_A_Blue.MI_Wrh_TrolleyTools_01_A_Blue", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/TrolleyTools_01/BP_SocketContainer_Wrh_TrolleyTools_01_A_Blue.BP_SocketContainer_Wrh_TrolleyTools_01_A_Blue_C", "/Game/Pioneer/Environment/Props/Warehouse/TrolleyTools_01/BP_SocketContainer_Wrh_TrolleyTools_01_A_Blue.Default__BP_SocketContainer_Wrh_TrolleyTools_01_A_Blue_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/TrolleyTools_01/MI_Wrh_TrolleyTools_01_A_MCP_Assembly_Yellow_01_A.MI_Wrh_TrolleyTools_01_A_MCP_Assembly_Yellow_01_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/TrolleyTools_01/BP_SocketContainer_Wrh_TrolleyTools_01_A_Yellow_MCP.BP_SocketContainer_Wrh_TrolleyTools_01_A_Yellow_MCP_C", "/Game/Pioneer/Environment/Props/Warehouse/TrolleyTools_01/BP_SocketContainer_Wrh_TrolleyTools_01_A_Yellow_MCP.Default__BP_SocketContainer_Wrh_TrolleyTools_01_A_Yellow_MCP_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/TrolleyTools_01/MI_Wrh_TrolleyTools_01_A_Yellow.MI_Wrh_TrolleyTools_01_A_Yellow", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/TrolleyTools_01/BP_SocketContainer_Wrh_TrolleyTools_01_A_Yellow.BP_SocketContainer_Wrh_TrolleyTools_01_A_Yellow_C", "/Game/Pioneer/Environment/Props/Warehouse/TrolleyTools_01/BP_SocketContainer_Wrh_TrolleyTools_01_A_Yellow.Default__BP_SocketContainer_Wrh_TrolleyTools_01_A_Yellow_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Mechanical.AirCompressor",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/AirCompressor_01/BP_SocketContainer_AirCompressor_01_A.BP_SocketContainer_AirCompressor_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/AirCompressor_01/BP_SocketContainer_AirCompressor_01_A.Default__BP_SocketContainer_AirCompressor_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Mechanical.Bus.Lid1",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Bus_01/BP_SocketContainer_Bus_Lid_01.BP_SocketContainer_Bus_Lid_01_C", "/Game/Pioneer/Environment/Props/_Generic/Bus_01/BP_SocketContainer_Bus_Lid_01.Default__BP_SocketContainer_Bus_Lid_01_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Mechanical.Bus.Lid2",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Bus_01/BP_SocketContainer_Bus_Lid_02.BP_SocketContainer_Bus_Lid_02_C", "/Game/Pioneer/Environment/Props/_Generic/Bus_01/BP_SocketContainer_Bus_Lid_02.Default__BP_SocketContainer_Bus_Lid_02_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Mechanical.Car01.Trunk",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_A.BP_SocketContainer_Car_01_Lid_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_A.Default__BP_SocketContainer_Car_01_Lid_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/Car_01/MI_Car_01_B.MI_Car_01_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_B.BP_SocketContainer_Car_01_Lid_01_B_C", "/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_B.Default__BP_SocketContainer_Car_01_Lid_01_B_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/Car_01/MI_Car_01_C.MI_Car_01_C", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_C.BP_SocketContainer_Car_01_Lid_01_C_C", "/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_C.Default__BP_SocketContainer_Car_01_Lid_01_C_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/Car_01/MI_Car_01_D.MI_Car_01_D", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_D.BP_SocketContainer_Car_01_Lid_01_D_C", "/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_D.Default__BP_SocketContainer_Car_01_Lid_01_D_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/Car_01/MI_FZT_Car_01_A.MI_FZT_Car_01_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_FZT_A.BP_SocketContainer_Car_01_Lid_01_FZT_A_C", "/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_FZT_A.Default__BP_SocketContainer_Car_01_Lid_01_FZT_A_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/Car_01/MI_FZT_Car_01_B.MI_FZT_Car_01_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_FZT_B_.BP_SocketContainer_Car_01_Lid_01_FZT_B__C", "/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_FZT_B_.Default__BP_SocketContainer_Car_01_Lid_01_FZT_B__C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/Car_01/MI_FZT_Car_01_C.MI_FZT_Car_01_C", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_FZT_C.BP_SocketContainer_Car_01_Lid_01_FZT_C_C", "/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_FZT_C.Default__BP_SocketContainer_Car_01_Lid_01_FZT_C_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/Car_01/MI_FZT_Car_01_D.MI_FZT_Car_01_D", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_FZT_D.BP_SocketContainer_Car_01_Lid_01_FZT_D_C", "/Game/Pioneer/Environment/Props/_Generic/Car_01/BP_SocketContainer_Car_01_Lid_01_FZT_D.Default__BP_SocketContainer_Car_01_Lid_01_FZT_D_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Mechanical.Car02.LidA",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_02/BP_SocketContainer_Car_02_Lid_01_A.BP_SocketContainer_Car_02_Lid_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/Car_02/BP_SocketContainer_Car_02_Lid_01_A.Default__BP_SocketContainer_Car_02_Lid_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/Car_02/MI_Car_02_B.MI_Car_02_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_02/BP_SocketContainer_Car_02_Lid_01_B.BP_SocketContainer_Car_02_Lid_01_B_C", "/Game/Pioneer/Environment/Props/_Generic/Car_02/BP_SocketContainer_Car_02_Lid_01_B.Default__BP_SocketContainer_Car_02_Lid_01_B_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/Car_02/MI_Car_02_C.MI_Car_02_C", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_02/BP_SocketContainer_Car_02_Lid_01_C.BP_SocketContainer_Car_02_Lid_01_C_C", "/Game/Pioneer/Environment/Props/_Generic/Car_02/BP_SocketContainer_Car_02_Lid_01_C.Default__BP_SocketContainer_Car_02_Lid_01_C_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/Car_02/MI_FZT_Car_02_A.MI_FZT_Car_02_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_02/BP_SocketContainer_Car_02_Lid_01_FZT_A.BP_SocketContainer_Car_02_Lid_01_FZT_A_C", "/Game/Pioneer/Environment/Props/_Generic/Car_02/BP_SocketContainer_Car_02_Lid_01_FZT_A.Default__BP_SocketContainer_Car_02_Lid_01_FZT_A_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/Car_02/MI_FZT_Car_02_B.MI_FZT_Car_02_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_02/BP_SocketContainer_Car_02_Lid_01_FZT_B.BP_SocketContainer_Car_02_Lid_01_FZT_B_C", "/Game/Pioneer/Environment/Props/_Generic/Car_02/BP_SocketContainer_Car_02_Lid_01_FZT_B.Default__BP_SocketContainer_Car_02_Lid_01_FZT_B_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/Car_02/MI_FZT_Car_02_C.MI_FZT_Car_02_C", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_02/BP_SocketContainer_Car_02_Lid_01_FZT_C.BP_SocketContainer_Car_02_Lid_01_FZT_C_C", "/Game/Pioneer/Environment/Props/_Generic/Car_02/BP_SocketContainer_Car_02_Lid_01_FZT_C.Default__BP_SocketContainer_Car_02_Lid_01_FZT_C_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Mechanical.Car03.Trunk",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_03/BP_SocketContainer_Car_03_A.BP_SocketContainer_Car_03_A_C", "/Game/Pioneer/Environment/Props/_Generic/Car_03/BP_SocketContainer_Car_03_A.Default__BP_SocketContainer_Car_03_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/_Generic/Car_03/MI_FZT_Car_03_A.MI_FZT_Car_03_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/Car_03/BP_SocketContainer_Car_03_FZT_A.BP_SocketContainer_Car_03_FZT_A_C", "/Game/Pioneer/Environment/Props/_Generic/Car_03/BP_SocketContainer_Car_03_FZT_A.Default__BP_SocketContainer_Car_03_FZT_A_C")]),
            ],
            3),
        new(
            "Socket.LootContainer.Mechanical.CompressorTrailer",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/TrailerCompressor_M_01/BP_SocketContainer_Wrh_TrailerCompressor_M_01_A.BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_C", "/Game/Pioneer/Environment/Props/Warehouse/TrailerCompressor_M_01/BP_SocketContainer_Wrh_TrailerCompressor_M_01_A.Default__BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/TrailerCompressor_M_01/MI_FZT_Wrh_TrailerCompressor_M_01_A_Red.MI_FZT_Wrh_TrailerCompressor_M_01_A_Red", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/TrailerCompressor_M_01/BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_Red.BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_Red_C", "/Game/Pioneer/Environment/Props/Warehouse/TrailerCompressor_M_01/BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_Red.Default__BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_Red_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/TrailerCompressor_M_01/MI_Wrh_TrailerCompressor_M_01_A_Black.MI_Wrh_TrailerCompressor_M_01_A_Black", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/TrailerCompressor_M_01/BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_Black.BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_Black_C", "/Game/Pioneer/Environment/Props/Warehouse/TrailerCompressor_M_01/BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_Black.Default__BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_Black_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/TrailerCompressor_M_01/MI_Wrh_TrailerCompressor_M_01_A_Red.MI_Wrh_TrailerCompressor_M_01_A_Red", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/TrailerCompressor_M_01/BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_Red.BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_Red_C", "/Game/Pioneer/Environment/Props/Warehouse/TrailerCompressor_M_01/BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_Red.Default__BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_Red_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/TrailerCompressor_M_01/MI_Wrh_TrailerCompressor_M_01_A_White.MI_Wrh_TrailerCompressor_M_01_A_White", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/TrailerCompressor_M_01/BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_White.BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_White_C", "/Game/Pioneer/Environment/Props/Warehouse/TrailerCompressor_M_01/BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_White.Default__BP_SocketContainer_Wrh_TrailerCompressor_M_01_A_White_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Mechanical.FuelSilo",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/FuelSilo_01/BP_SocketContainer_FuelSilo_01.BP_SocketContainer_FuelSilo_01_C", "/Game/Pioneer/Environment/Props/_Generic/FuelSilo_01/BP_SocketContainer_FuelSilo_01.Default__BP_SocketContainer_FuelSilo_01_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Mechanical.PressureMonitor.DoorLeft",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/BP_SocketContainer_Wrh_Machine_M_02_DoorLeft.BP_SocketContainer_Wrh_Machine_M_02_DoorLeft_C", "/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/BP_SocketContainer_Wrh_Machine_M_02_DoorLeft.Default__BP_SocketContainer_Wrh_Machine_M_02_DoorLeft_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/MI_Wrh_Machine_M_02_A_Green_B.MI_Wrh_Machine_M_02_A_Green_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/BP_SocketContainer_Wrh_Machine_M_02_DoorLeft_Green_B.BP_SocketContainer_Wrh_Machine_M_02_DoorLeft_Green_B_C", "/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/BP_SocketContainer_Wrh_Machine_M_02_DoorLeft_Green_B.Default__BP_SocketContainer_Wrh_Machine_M_02_DoorLeft_Green_B_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/MI_Wrh_Machine_M_02_Snow.MI_Wrh_Machine_M_02_Snow", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/BP_SocketContainer_Wrh_Machine_M_02_DoorLeft_Snow.BP_SocketContainer_Wrh_Machine_M_02_DoorLeft_Snow_C", "/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/BP_SocketContainer_Wrh_Machine_M_02_DoorLeft_Snow.Default__BP_SocketContainer_Wrh_Machine_M_02_DoorLeft_Snow_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Mechanical.PressureMonitor.DoorRight",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/BP_SocketContainer_Wrh_Machine_M_02_DoorRight.BP_SocketContainer_Wrh_Machine_M_02_DoorRight_C", "/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/BP_SocketContainer_Wrh_Machine_M_02_DoorRight.Default__BP_SocketContainer_Wrh_Machine_M_02_DoorRight_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/MI_Wrh_Machine_M_02_A_Green_B.MI_Wrh_Machine_M_02_A_Green_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/BP_SocketContainer_Wrh_Machine_M_02_DoorRight_Green_B.BP_SocketContainer_Wrh_Machine_M_02_DoorRight_Green_B_C", "/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/BP_SocketContainer_Wrh_Machine_M_02_DoorRight_Green_B.Default__BP_SocketContainer_Wrh_Machine_M_02_DoorRight_Green_B_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/MI_Wrh_Machine_M_02_Snow.MI_Wrh_Machine_M_02_Snow", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/BP_SocketContainer_Wrh_Machine_M_02_DoorRight_Snow.BP_SocketContainer_Wrh_Machine_M_02_DoorRight_Snow_C", "/Game/Pioneer/Environment/Props/Warehouse/Machine_M_02/BP_SocketContainer_Wrh_Machine_M_02_DoorRight_Snow.Default__BP_SocketContainer_Wrh_Machine_M_02_DoorRight_Snow_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Mechanical.Rover.Lid",
            [new ArchetypeClass("/Game/Pioneer/Environment/POI/MountainCompound/Props/Rover_01/BP_SocketContainer_Rover_01.BP_SocketContainer_Rover_01_C", "/Game/Pioneer/Environment/POI/MountainCompound/Props/Rover_01/BP_SocketContainer_Rover_01.Default__BP_SocketContainer_Rover_01_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Mechanical.StreetSweeper.Lid",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/StreetSweeper_01/BP_SocketContainer_StreetSweeper_Lid_01.BP_SocketContainer_StreetSweeper_Lid_01_C", "/Game/Pioneer/Environment/Props/_Generic/StreetSweeper_01/BP_SocketContainer_StreetSweeper_Lid_01.Default__BP_SocketContainer_StreetSweeper_Lid_01_C")],
            [],
            1),
        new(
            "Socket.LootContainer.Mechanical.UtilityTruck.Hatch",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/TruckUtility_01/BP_SocketContainer_TruckUtility_01.BP_SocketContainer_TruckUtility_01_C", "/Game/Pioneer/Environment/Props/_Generic/TruckUtility_01/BP_SocketContainer_TruckUtility_01.Default__BP_SocketContainer_TruckUtility_01_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Medical.Console_03.Drawer",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Console_03/BP_SocketContainer_Console_03_A.BP_SocketContainer_Console_03_A_C", "/Game/Pioneer/Environment/Props/Research/Console_03/BP_SocketContainer_Console_03_A.Default__BP_SocketContainer_Console_03_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Medical.Lockers_02.Door_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Lockers_02/BP_SocketContainer_Lockers_02_A.BP_SocketContainer_Lockers_02_A_C", "/Game/Pioneer/Environment/Props/Research/Lockers_02/BP_SocketContainer_Lockers_02_A.Default__BP_SocketContainer_Lockers_02_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Medical.Lockers_02.Door_02",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Lockers_02/BP_SocketContainer_Lockers_02_B.BP_SocketContainer_Lockers_02_B_C", "/Game/Pioneer/Environment/Props/Research/Lockers_02/BP_SocketContainer_Lockers_02_B.Default__BP_SocketContainer_Lockers_02_B_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Medical.Lockers_03.Door_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Lockers_03/BP_SocketContainer_Lockers_03_A.BP_SocketContainer_Lockers_03_A_C", "/Game/Pioneer/Environment/Props/Research/Lockers_03/BP_SocketContainer_Lockers_03_A.Default__BP_SocketContainer_Lockers_03_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Medical.Rsr.Cabinet.Drawer",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Cabinet_S_02/BP_SocketContainer_Rsr_Cabinet_S_02_A_Blue.BP_SocketContainer_Rsr_Cabinet_S_02_A_Blue_C", "/Game/Pioneer/Environment/Props/Research/Cabinet_S_02/BP_SocketContainer_Rsr_Cabinet_S_02_A_Blue.Default__BP_SocketContainer_Rsr_Cabinet_S_02_A_Blue_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Cabinet_S_02/MI_Rsr_Cabinet_S_02_A_Green.MI_Rsr_Cabinet_S_02_A_Green", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Cabinet_S_02/BP_SocketContainer_Rsr_Cabinet_S_02_A_Green.BP_SocketContainer_Rsr_Cabinet_S_02_A_Green_C", "/Game/Pioneer/Environment/Props/Research/Cabinet_S_02/BP_SocketContainer_Rsr_Cabinet_S_02_A_Green.Default__BP_SocketContainer_Rsr_Cabinet_S_02_A_Green_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Cabinet_S_02/MI_Rsr_Cabinet_S_02_A_Red.MI_Rsr_Cabinet_S_02_A_Red", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Cabinet_S_02/BP_SocketContainer_Rsr_Cabinet_S_02_A_Red.BP_SocketContainer_Rsr_Cabinet_S_02_A_Red_C", "/Game/Pioneer/Environment/Props/Research/Cabinet_S_02/BP_SocketContainer_Rsr_Cabinet_S_02_A_Red.Default__BP_SocketContainer_Rsr_Cabinet_S_02_A_Red_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Cabinet_S_02/MI_Rsr_Cabinet_S_02_A_ResearchCompany_BC.MI_Rsr_Cabinet_S_02_A_ResearchCompany_BC", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Cabinet_S_02/BP_SocketContainer_Rsr_Cabinet_S_02_A_ResearchCompany_BC.BP_SocketContainer_Rsr_Cabinet_S_02_A_ResearchCompany_BC_C", "/Game/Pioneer/Environment/Props/Research/Cabinet_S_02/BP_SocketContainer_Rsr_Cabinet_S_02_A_ResearchCompany_BC.Default__BP_SocketContainer_Rsr_Cabinet_S_02_A_ResearchCompany_BC_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Cabinet_S_02/MI_Rsr_Cabinet_S_02_A_Yellow.MI_Rsr_Cabinet_S_02_A_Yellow", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Cabinet_S_02/BP_SocketContainer_Rsr_Cabinet_S_02_A_Yellow.BP_SocketContainer_Rsr_Cabinet_S_02_A_Yellow_C", "/Game/Pioneer/Environment/Props/Research/Cabinet_S_02/BP_SocketContainer_Rsr_Cabinet_S_02_A_Yellow.Default__BP_SocketContainer_Rsr_Cabinet_S_02_A_Yellow_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Medical.Rsr.CabinetM01.Drawer",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Cabinet_M_01/BP_SocketContainer_Rsr_Cabinet_M_01.BP_SocketContainer_Rsr_Cabinet_M_01_C", "/Game/Pioneer/Environment/Props/Research/Cabinet_M_01/BP_SocketContainer_Rsr_Cabinet_M_01.Default__BP_SocketContainer_Rsr_Cabinet_M_01_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Cabinet_M_01/MI_Rsr_Cabinet_M_01_A_Clean_01.MI_Rsr_Cabinet_M_01_A_Clean_01", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Cabinet_M_01/BP_SocketContainer_Rsr_Cabinet_M_01_Clean_01.BP_SocketContainer_Rsr_Cabinet_M_01_Clean_01_C", "/Game/Pioneer/Environment/Props/Research/Cabinet_M_01/BP_SocketContainer_Rsr_Cabinet_M_01_Clean_01.Default__BP_SocketContainer_Rsr_Cabinet_M_01_Clean_01_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Cabinet_M_01/MI_Rsr_Cabinet_M_01_A_Green.MI_Rsr_Cabinet_M_01_A_Green", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Cabinet_M_01/BP_SocketContainer_Rsr_Cabinet_M_01_Green.BP_SocketContainer_Rsr_Cabinet_M_01_Green_C", "/Game/Pioneer/Environment/Props/Research/Cabinet_M_01/BP_SocketContainer_Rsr_Cabinet_M_01_Green.Default__BP_SocketContainer_Rsr_Cabinet_M_01_Green_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Cabinet_M_01/MI_Rsr_Cabinet_M_01_A_Red.MI_Rsr_Cabinet_M_01_A_Red", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Cabinet_M_01/BP_SocketContainer_Rsr_Cabinet_M_01_Red.BP_SocketContainer_Rsr_Cabinet_M_01_Red_C", "/Game/Pioneer/Environment/Props/Research/Cabinet_M_01/BP_SocketContainer_Rsr_Cabinet_M_01_Red.Default__BP_SocketContainer_Rsr_Cabinet_M_01_Red_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Cabinet_M_01/MI_Rsr_Cabinet_M_01_A_Yellow.MI_Rsr_Cabinet_M_01_A_Yellow", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Cabinet_M_01/BP_SocketContainer_Rsr_Cabinet_M_01_Yellow.BP_SocketContainer_Rsr_Cabinet_M_01_Yellow_C", "/Game/Pioneer/Environment/Props/Research/Cabinet_M_01/BP_SocketContainer_Rsr_Cabinet_M_01_Yellow.Default__BP_SocketContainer_Rsr_Cabinet_M_01_Yellow_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Medical.RsrCabinetS.Door",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Cabinet_S_01/BP_SocketContainer_Rsr_Cabinet_S_01_A.BP_SocketContainer_Rsr_Cabinet_S_01_A_C", "/Game/Pioneer/Environment/Props/Research/Cabinet_S_01/BP_SocketContainer_Rsr_Cabinet_S_01_A.Default__BP_SocketContainer_Rsr_Cabinet_S_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Cabinet_S_01/MI_Rsr_Cabinet_S_01_A_Clean_01.MI_Rsr_Cabinet_S_01_A_Clean_01", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Cabinet_S_01/BP_SocketContainer_Rsr_Cabinet_S_01_A_Clean.BP_SocketContainer_Rsr_Cabinet_S_01_A_Clean_C", "/Game/Pioneer/Environment/Props/Research/Cabinet_S_01/BP_SocketContainer_Rsr_Cabinet_S_01_A_Clean.Default__BP_SocketContainer_Rsr_Cabinet_S_01_A_Clean_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Cabinet_S_01/MI_Rsr_Cabinet_S_01_A_Green.MI_Rsr_Cabinet_S_01_A_Green", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Cabinet_S_01/BP_SocketContainer_Rsr_Cabinet_S_01_A_Green.BP_SocketContainer_Rsr_Cabinet_S_01_A_Green_C", "/Game/Pioneer/Environment/Props/Research/Cabinet_S_01/BP_SocketContainer_Rsr_Cabinet_S_01_A_Green.Default__BP_SocketContainer_Rsr_Cabinet_S_01_A_Green_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Cabinet_S_01/MI_Rsr_Cabinet_S_01_A_Red.MI_Rsr_Cabinet_S_01_A_Red", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Cabinet_S_01/BP_SocketContainer_Rsr_Cabinet_S_01_A_Red.BP_SocketContainer_Rsr_Cabinet_S_01_A_Red_C", "/Game/Pioneer/Environment/Props/Research/Cabinet_S_01/BP_SocketContainer_Rsr_Cabinet_S_01_A_Red.Default__BP_SocketContainer_Rsr_Cabinet_S_01_A_Red_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/Cabinet_S_01/MI_Rsr_Cabinet_S_01_A_Yellow.MI_Rsr_Cabinet_S_01_A_Yellow", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Cabinet_S_01/BP_SocketContainer_Rsr_Cabinet_S_01_A_Yellow.BP_SocketContainer_Rsr_Cabinet_S_01_A_Yellow_C", "/Game/Pioneer/Environment/Props/Research/Cabinet_S_01/BP_SocketContainer_Rsr_Cabinet_S_01_A_Yellow.Default__BP_SocketContainer_Rsr_Cabinet_S_01_A_Yellow_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Medical.RsrCartShelf.Drawer02",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02.BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02_C", "/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02.Default__BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/MI_Rsr_CartShelf_M_01_Clean_A.MI_Rsr_CartShelf_M_01_Clean_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02_Clean.BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02_Clean_C", "/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02_Clean.Default__BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02_Clean_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/MI_Rsr_CartShelf_M_01_Clean_Red.MI_Rsr_CartShelf_M_01_Clean_Red", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02_Clean.BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02_Clean_C", "/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02_Clean.Default__BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02_Clean_C")]),
            ],
            1),
        new(
            "Socket.LootContainer.Medical.RsrCartShelf.Drawer05",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01.BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01_C", "/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01.Default__BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_01_C")],
            [],
            1),
        new(
            "Socket.LootContainer.Medical.RsrCartShelf.Drawer06",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02.BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02_C", "/Game/Pioneer/Environment/Props/Research/CartShelf_M_01/BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02.Default__BP_SocketContainer_Rsr_CartShelf_M_01_Drawer_02_C")],
            [],
            1),
        new(
            "Socket.LootContainer.Medical.TrashCan_01.Lid_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/TrashCan_01/BP_SocketContainer_Trashcan_01_A.BP_SocketContainer_Trashcan_01_A_C", "/Game/Pioneer/Environment/Props/Research/TrashCan_01/BP_SocketContainer_Trashcan_01_A.Default__BP_SocketContainer_Trashcan_01_A_C")],
            [],
            1),
        new(
            "Socket.LootContainer.Medical.Workbench_01.Drawer",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Workbench_01/BP_SocketContainer_Workbench_01_A.BP_SocketContainer_Workbench_01_A_C", "/Game/Pioneer/Environment/Props/Research/Workbench_01/BP_SocketContainer_Workbench_01_A.Default__BP_SocketContainer_Workbench_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Medical.Workbench_02.Drawer",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Workbench_02/BP_SocketContainer_Workbench_02_A.BP_SocketContainer_Workbench_02_A_C", "/Game/Pioneer/Environment/Props/Research/Workbench_02/BP_SocketContainer_Workbench_02_A.Default__BP_SocketContainer_Workbench_02_A_C")],
            [],
            5),
        new(
            "Socket.LootContainer.Military.WeaponLocker.DoorLeft",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/WeaponsRack_01/BP_SocketContainer_Rsr_WeaponsRack_01_DoorLeft_A.BP_SocketContainer_Rsr_WeaponsRack_01_DoorLeft_A_C", "/Game/Pioneer/Environment/Props/Research/WeaponsRack_01/BP_SocketContainer_Rsr_WeaponsRack_01_DoorLeft_A.Default__BP_SocketContainer_Rsr_WeaponsRack_01_DoorLeft_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Military.WeaponLocker.DoorRight",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/WeaponsRack_01/BP_SocketContainer_Rsr_WeaponsRack_01_DoorRight_A.BP_SocketContainer_Rsr_WeaponsRack_01_DoorRight_A_C", "/Game/Pioneer/Environment/Props/Research/WeaponsRack_01/BP_SocketContainer_Rsr_WeaponsRack_01_DoorRight_A.Default__BP_SocketContainer_Rsr_WeaponsRack_01_DoorRight_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Nature.Agave",
            [new ArchetypeClass("/Game/Pioneer/Environment/South/Base/Vegetation/Agave_01/BP_SalvageContainer_Agave.BP_SalvageContainer_Agave_C", "/Game/Pioneer/Environment/South/Base/Vegetation/Agave_01/BP_SalvageContainer_Agave.Default__BP_SalvageContainer_Agave_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Nature.Apricot",
            [new ArchetypeClass("/Game/Pioneer/Environment/Nature/Locations/Generic/Trees/ApricotTree_01/BP_SalvageContainer_Apricot.BP_SalvageContainer_Apricot_C", "/Game/Pioneer/Environment/Nature/Locations/Generic/Trees/ApricotTree_01/BP_SalvageContainer_Apricot.Default__BP_SalvageContainer_Apricot_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Nature.BirdNest.A",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/BirdNest_01/BP_SocketContainer_BirdNest_A.BP_SocketContainer_BirdNest_A_C", "/Game/Pioneer/Environment/Props/_Generic/BirdNest_01/BP_SocketContainer_BirdNest_A.Default__BP_SocketContainer_BirdNest_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Nature.BirdNest.B",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/BirdNest_01/BP_SocketContainer_BirdNest_B.BP_SocketContainer_BirdNest_B_C", "/Game/Pioneer/Environment/Props/_Generic/BirdNest_01/BP_SocketContainer_BirdNest_B.Default__BP_SocketContainer_BirdNest_B_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Nature.BirdNest.C",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/BirdNest_01/BP_SocketContainer_BirdNest_C.BP_SocketContainer_BirdNest_C_C", "/Game/Pioneer/Environment/Props/_Generic/BirdNest_01/BP_SocketContainer_BirdNest_C.Default__BP_SocketContainer_BirdNest_C_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Nature.Candleberries",
            [new ArchetypeClass("/Game/Pioneer/Environment/Nature/Locations/TheBlueGate/Bushes/CandleBerries_01/BP_SocketContainer_CandleBerries.BP_SocketContainer_CandleBerries_C", "/Game/Pioneer/Environment/Nature/Locations/TheBlueGate/Bushes/CandleBerries_01/BP_SocketContainer_CandleBerries.Default__BP_SocketContainer_CandleBerries_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Nature.Fertilizer",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Dam/Planter_01/BP_SalvageContainer_Fertilizer.BP_SalvageContainer_Fertilizer_C", "/Game/Pioneer/Environment/Props/Dam/Planter_01/BP_SalvageContainer_Fertilizer.Default__BP_SalvageContainer_Fertilizer_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Nature.FruitBasket",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_FruitBasket_01.BP_SocketContainer_FruitBasket_01_C", "/Game/Pioneer/Environment/Props/_Generic/FruitBasket_01/BP_SocketContainer_FruitBasket_01.Default__BP_SocketContainer_FruitBasket_01_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Nature.GreatMullein",
            [new ArchetypeClass("/Game/Pioneer/Environment/Nature/Locations/Generic/Bushes/Mullein_01/BP_SalvageContainer_GreatMullein.BP_SalvageContainer_GreatMullein_C", "/Game/Pioneer/Environment/Nature/Locations/Generic/Bushes/Mullein_01/BP_SalvageContainer_GreatMullein.Default__BP_SalvageContainer_GreatMullein_C")],
            [],
            5),
        new(
            "Socket.LootContainer.Nature.Lemon",
            [new ArchetypeClass("/Game/Pioneer/Environment/Nature/Locations/Generic/Trees/LemonTree_01/BP_SalvageContainer_Lemon.BP_SalvageContainer_Lemon_C", "/Game/Pioneer/Environment/Nature/Locations/Generic/Trees/LemonTree_01/BP_SalvageContainer_Lemon.Default__BP_SalvageContainer_Lemon_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Nature.Moss",
            [new ArchetypeClass("/Game/Pioneer/Environment/South/Base/Meshes/Rock_XL_07/BP_SalvageContainer_Moss_Rock.BP_SalvageContainer_Moss_Rock_C", "/Game/Pioneer/Environment/South/Base/Meshes/Rock_XL_07/BP_SalvageContainer_Moss_Rock.Default__BP_SalvageContainer_Moss_Rock_C")],
            [],
            3),
        new(
            "Socket.LootContainer.Nature.Mushroom",
            [new ArchetypeClass("/Game/Pioneer/Environment/South/Swamp/Lootables/BP_SalvageContainer_MushroomCluster_01_A.BP_SalvageContainer_MushroomCluster_01_A_C", "/Game/Pioneer/Environment/South/Swamp/Lootables/BP_SalvageContainer_MushroomCluster_01_A.Default__BP_SalvageContainer_MushroomCluster_01_A_C")],
            [],
            3),
        new(
            "Socket.LootContainer.Nature.Olive",
            [new ArchetypeClass("/Game/Pioneer/Environment/Nature/Locations/Generic/Trees/Olive_01/BP_SalvageContainer_Olive.BP_SalvageContainer_Olive_C", "/Game/Pioneer/Environment/Nature/Locations/Generic/Trees/Olive_01/BP_SalvageContainer_Olive.Default__BP_SalvageContainer_Olive_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Nature.Planter.Cmr",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/Planters/BP_SalvageContainer_Shrub_03_H.BP_SalvageContainer_Shrub_03_H_C", "/Game/Pioneer/Environment/Props/Commercial/Planters/BP_SalvageContainer_Shrub_03_H.Default__BP_SalvageContainer_Shrub_03_H_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Nature.Resin",
            [new ArchetypeClass("/Game/Pioneer/Environment/South/Forest/Vegetation/AleppoPine_01/BP_SalvageContainer_Resin.BP_SalvageContainer_Resin_C", "/Game/Pioneer/Environment/South/Forest/Vegetation/AleppoPine_01/BP_SalvageContainer_Resin.Default__BP_SalvageContainer_Resin_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Nature.Seedbox_01.Drawer_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/SeedVault/SeedBox_01/BP_SocketContainer_VAU_SeedBox_01_A.BP_SocketContainer_VAU_SeedBox_01_A_C", "/Game/Pioneer/Environment/Props/SeedVault/SeedBox_01/BP_SocketContainer_VAU_SeedBox_01_A.Default__BP_SocketContainer_VAU_SeedBox_01_A_C")],
            [],
            1),
        new(
            "Socket.LootContainer.Nature.Seedvault.Console_01.Lid_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/SeedVault/SeedConsole_01/BP_SocketContainer_VAU_SeedConsole_01_A.BP_SocketContainer_VAU_SeedConsole_01_A_C", "/Game/Pioneer/Environment/Props/SeedVault/SeedConsole_01/BP_SocketContainer_VAU_SeedConsole_01_A.Default__BP_SocketContainer_VAU_SeedConsole_01_A_C")],
            [],
            3),
        new(
            "Socket.LootContainer.Nature.VolcanicRock",
            [new ArchetypeClass("/Game/Pioneer/Environment/South/VolcanicRed/Meshes/Cliff_XL_01/BP_SalvageContainer_VolcanicRock_Temp.BP_SalvageContainer_VolcanicRock_Temp_C", "/Game/Pioneer/Environment/South/VolcanicRed/Meshes/Cliff_XL_01/BP_SalvageContainer_VolcanicRock_Temp.Default__BP_SalvageContainer_VolcanicRock_Temp_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Nature.WaterTank",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Dam/WaterTank_01/BP_SalvageContainer_Moss.BP_SalvageContainer_Moss_C", "/Game/Pioneer/Environment/Props/Dam/WaterTank_01/BP_SalvageContainer_Moss.Default__BP_SalvageContainer_Moss_C")],
            [],
            2),
        new(
            "Socket.LootContainer.OldWorld.MuseumCrate_01.Lid_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/CulturalArchives/MuseumCrate_01/BP_SocketContainer_CUL_MuseumCrate_01_A.BP_SocketContainer_CUL_MuseumCrate_01_A_C", "/Game/Pioneer/Environment/Props/CulturalArchives/MuseumCrate_01/BP_SocketContainer_CUL_MuseumCrate_01_A.Default__BP_SocketContainer_CUL_MuseumCrate_01_A_C")],
            [],
            1),
        new(
            "Socket.LootContainer.OldWorld.MuseumCrate_02.Lid_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/CulturalArchives/MuseumCrate_02/BP_SocketContainer_CUL_MuseumCrate_02_A.BP_SocketContainer_CUL_MuseumCrate_02_A_C", "/Game/Pioneer/Environment/Props/CulturalArchives/MuseumCrate_02/BP_SocketContainer_CUL_MuseumCrate_02_A.Default__BP_SocketContainer_CUL_MuseumCrate_02_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Raider.WeaponCase",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A.BP_SocketContainer_Raider_WeaponCase_01_Lid_A_C", "/Game/Pioneer/Environment/Props/Raider/WeaponCase_01/BP_SocketContainer_Raider_WeaponCase_01_Lid_A.Default__BP_SocketContainer_Raider_WeaponCase_01_Lid_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Residential.AlpineWardrobe",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/AlpineWardrobe_01/BP_SocketContainer_AlpineWardrobeDoor_01_A.BP_SocketContainer_AlpineWardrobeDoor_01_A_C", "/Game/Pioneer/Environment/Props/Residential/AlpineWardrobe_01/BP_SocketContainer_AlpineWardrobeDoor_01_A.Default__BP_SocketContainer_AlpineWardrobeDoor_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Residential.Closet",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Closet_01/BP_SocketContainer_Res_Closet_01_A_Lid_01_A.BP_SocketContainer_Res_Closet_01_A_Lid_01_A_C", "/Game/Pioneer/Environment/Props/Residential/Closet_01/BP_SocketContainer_Res_Closet_01_A_Lid_01_A.Default__BP_SocketContainer_Res_Closet_01_A_Lid_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Residential.Closet02.Lid01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_01_A.BP_SocketContainer_Res_Closet_02_Lid_01_A_C", "/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_01_A.Default__BP_SocketContainer_Res_Closet_02_Lid_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/POI/FrozenTrail/Village/MI_FZT_Village_TrimWood_01_A.MI_FZT_Village_TrimWood_01_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT_01.BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT_01_C", "/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT_01.Default__BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT_01_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/POI/FrozenTrail/Village/MI_FZT_Village_TrimWood_01_B.MI_FZT_Village_TrimWood_01_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT.BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT_C", "/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT.Default__BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/POI/FrozenTrail/Village/MI_FZT_Village_TrimWood_01_C.MI_FZT_Village_TrimWood_01_C", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT.BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT_C", "/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT.Default__BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Village/Stair_01/MI_Vil_Stair_01_A.MI_Vil_Stair_01_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT_02.BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT_02_C", "/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT_02.Default__BP_SocketContainer_Res_Closet_02_Lid_01_A_FZT_02_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Residential.Closet02.Lid02",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_02_A.BP_SocketContainer_Res_Closet_02_Lid_02_A_C", "/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_02_A.Default__BP_SocketContainer_Res_Closet_02_Lid_02_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/POI/FrozenTrail/Village/MI_FZT_Village_TrimWood_01_A.MI_FZT_Village_TrimWood_01_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT_01.BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT_01_C", "/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT_01.Default__BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT_01_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/POI/FrozenTrail/Village/MI_FZT_Village_TrimWood_01_B.MI_FZT_Village_TrimWood_01_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT.BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT_C", "/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT.Default__BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/POI/FrozenTrail/Village/MI_FZT_Village_TrimWood_01_C.MI_FZT_Village_TrimWood_01_C", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT.BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT_C", "/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT.Default__BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Village/Stair_01/MI_Vil_Stair_01_A.MI_Vil_Stair_01_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT_02.BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT_02_C", "/Game/Pioneer/Environment/Props/Residential/Closet_02/BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT_02.Default__BP_SocketContainer_Res_Closet_02_Lid_02_A_FZT_02_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Residential.ConsoleChest",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ConsoleChest_01/BP_SocketContainer_ConsoleChestDrawer_01_A.BP_SocketContainer_ConsoleChestDrawer_01_A_C", "/Game/Pioneer/Environment/Props/Residential/ConsoleChest_01/BP_SocketContainer_ConsoleChestDrawer_01_A.Default__BP_SocketContainer_ConsoleChestDrawer_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Residential.CupboardKitchen.lidA",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_A.BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_A_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_A.Default__BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/POI/FrozenTrail/Village/MI_FZT_Village_TrimWood_01_B.MI_FZT_Village_TrimWood_01_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_A_FZT.BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_A_FZT_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_A_FZT.Default__BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_A_FZT_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Residential/TrimInteriorCeiling_02/MI_TrimInteriorCeiling_02_A.MI_TrimInteriorCeiling_02_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_A_FZT.BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_A_FZT_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_A_FZT.Default__BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_A_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Residential.CupboardKitchen.lidB",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_B.BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_B_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_B.Default__BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_B_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/POI/FrozenTrail/Village/MI_FZT_Village_TrimWood_01_B.MI_FZT_Village_TrimWood_01_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_B_FZT.BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_B_FZT_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_B_FZT.Default__BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_175x100_B_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Residential.CupboardKitchen.lidC",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_A.BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_A_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_A.Default__BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/POI/FrozenTrail/Village/MI_FZT_Village_TrimWood_01_B.MI_FZT_Village_TrimWood_01_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_A_FZT.BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_A_FZT_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_A_FZT.Default__BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_A_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Residential.CupboardKitchen.lidD",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_B.BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_B_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_B.Default__BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_B_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/POI/FrozenTrail/Village/MI_FZT_Village_TrimWood_01_B.MI_FZT_Village_TrimWood_01_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_B_FZT.BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_B_FZT_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_B_FZT.Default__BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_B_FZT_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Residential/TrimInteriorCeiling_02/MI_TrimInteriorCeiling_02_A.MI_TrimInteriorCeiling_02_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_B_FZT.BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_B_FZT_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_B_FZT.Default__BP_SocketContainer_SM_Res_CupboardKitchen_Lid_01_300x100_B_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Residential.Drawer.Drawer",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Drawer_01/BP_SocketContainer_Res_Drawer_01_Shelf_A.BP_SocketContainer_Res_Drawer_01_Shelf_A_C", "/Game/Pioneer/Environment/Props/Residential/Drawer_01/BP_SocketContainer_Res_Drawer_01_Shelf_A.Default__BP_SocketContainer_Res_Drawer_01_Shelf_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Residential.DrawerMedical.lid1",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/DrawerMedical_01/BP_SocketContainer_Res_DrawerMedical_01_A.BP_SocketContainer_Res_DrawerMedical_01_A_C", "/Game/Pioneer/Environment/Props/Residential/DrawerMedical_01/BP_SocketContainer_Res_DrawerMedical_01_A.Default__BP_SocketContainer_Res_DrawerMedical_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Village/Stair_01/MI_Vil_Stair_01_A.MI_Vil_Stair_01_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/DrawerMedical_01/BP_SocketContainer_Res_DrawerMedical_01_A_FZT.BP_SocketContainer_Res_DrawerMedical_01_A_FZT_C", "/Game/Pioneer/Environment/Props/Residential/DrawerMedical_01/BP_SocketContainer_Res_DrawerMedical_01_A_FZT.Default__BP_SocketContainer_Res_DrawerMedical_01_A_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Residential.KitchenBench.LidA",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_A.BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_A_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_A.Default__BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/POI/FrozenTrail/Village/MI_FZT_Village_TrimWood_01_B.MI_FZT_Village_TrimWood_01_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_A_FZT.BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_A_FZT_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_A_FZT.Default__BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_A_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Residential.KitchenBench.LidB",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_B.BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_B_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_B.Default__BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_B_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/POI/FrozenTrail/Village/MI_FZT_Village_TrimWood_01_B.MI_FZT_Village_TrimWood_01_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_B_FZT.BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_B_FZT_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_B_FZT.Default__BP_SocketContainer_Res_BenchKitchen_175x125_Lid_01_B_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Residential.KitchenBench.LidC",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_01_175x125_Lid_01_C.BP_SocketContainer_Res_BenchKitchen_01_175x125_Lid_01_C_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_01_175x125_Lid_01_C.Default__BP_SocketContainer_Res_BenchKitchen_01_175x125_Lid_01_C_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/POI/FrozenTrail/Village/MI_FZT_Village_TrimWood_01_B.MI_FZT_Village_TrimWood_01_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_01_175x125_Lid_01_C_FZT.BP_SocketContainer_Res_BenchKitchen_01_175x125_Lid_01_C_FZT_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_01_175x125_Lid_01_C_FZT.Default__BP_SocketContainer_Res_BenchKitchen_01_175x125_Lid_01_C_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Residential.KitchenBench.LidD",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_01_Lid_01_175x125_C.BP_SocketContainer_Res_BenchKitchen_01_Lid_01_175x125_C_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_01_Lid_01_175x125_C.Default__BP_SocketContainer_Res_BenchKitchen_01_Lid_01_175x125_C_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/POI/FrozenTrail/Village/MI_FZT_Village_TrimWood_01_B.MI_FZT_Village_TrimWood_01_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_01_Lid_01_175x125_C_FZT.BP_SocketContainer_Res_BenchKitchen_01_Lid_01_175x125_C_FZT_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_01_Lid_01_175x125_C_FZT.Default__BP_SocketContainer_Res_BenchKitchen_01_Lid_01_175x125_C_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Residential.KitchenBench.LidE",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_01_175x125_D.BP_SocketContainer_Res_BenchKitchen_01_175x125_D_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_01_175x125_D.Default__BP_SocketContainer_Res_BenchKitchen_01_175x125_D_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/POI/FrozenTrail/Village/MI_FZT_Village_TrimWood_01_B.MI_FZT_Village_TrimWood_01_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_01_175x125_D_FZT.BP_SocketContainer_Res_BenchKitchen_01_175x125_D_FZT_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_BenchKitchen_01_175x125_D_FZT.Default__BP_SocketContainer_Res_BenchKitchen_01_175x125_D_FZT_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Residential.Refrigerator_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_Refrigerator_01_Lid_01_A.BP_SocketContainer_Res_Refrigerator_01_Lid_01_A_C", "/Game/Pioneer/Environment/Props/Residential/ModulesKitchen/BP_SocketContainer_Res_Refrigerator_01_Lid_01_A.Default__BP_SocketContainer_Res_Refrigerator_01_Lid_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Residential.Suitcase.Lid",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Suitcase_01/BP_SocketContainer_Res_Suitcase_01_Lid_A.BP_SocketContainer_Res_Suitcase_01_Lid_A_C", "/Game/Pioneer/Environment/Props/Residential/Suitcase_01/BP_SocketContainer_Res_Suitcase_01_Lid_A.Default__BP_SocketContainer_Res_Suitcase_01_Lid_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Residential/Suitcase_01/MI_Res_Suitcase_02.MI_Res_Suitcase_02", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Suitcase_01/BP_SocketContainer_Res_Suitcase_01_Lid_A_Black.BP_SocketContainer_Res_Suitcase_01_Lid_A_Black_C", "/Game/Pioneer/Environment/Props/Residential/Suitcase_01/BP_SocketContainer_Res_Suitcase_01_Lid_A_Black.Default__BP_SocketContainer_Res_Suitcase_01_Lid_A_Black_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Residential/Suitcase_01/MI_Res_Suitcase_03.MI_Res_Suitcase_03", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Suitcase_01/BP_SocketContainer_Res_Suitcase_01_Lid_A_Red.BP_SocketContainer_Res_Suitcase_01_Lid_A_Red_C", "/Game/Pioneer/Environment/Props/Residential/Suitcase_01/BP_SocketContainer_Res_Suitcase_01_Lid_A_Red.Default__BP_SocketContainer_Res_Suitcase_01_Lid_A_Red_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Residential.TVUnit.Lid",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/TvUnit_01/BP_SocketContainer_Res_TvUnit_01_A_Lid_01.BP_SocketContainer_Res_TvUnit_01_A_Lid_01_C", "/Game/Pioneer/Environment/Props/Residential/TvUnit_01/BP_SocketContainer_Res_TvUnit_01_A_Lid_01.Default__BP_SocketContainer_Res_TvUnit_01_A_Lid_01_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Residential.Trashbin",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Trashbin_01/BP_SocketContainer_Res_TrashBin_01_Lid.BP_SocketContainer_Res_TrashBin_01_Lid_C", "/Game/Pioneer/Environment/Props/Residential/Trashbin_01/BP_SocketContainer_Res_TrashBin_01_Lid.Default__BP_SocketContainer_Res_TrashBin_01_Lid_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Residential/Trashbin_01/MI_Res_Trashbin_01_Green.MI_Res_Trashbin_01_Green", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Trashbin_01/BP_SocketContainer_Res_TrashBin_01_Lid_Green.BP_SocketContainer_Res_TrashBin_01_Lid_Green_C", "/Game/Pioneer/Environment/Props/Residential/Trashbin_01/BP_SocketContainer_Res_TrashBin_01_Lid_Green.Default__BP_SocketContainer_Res_TrashBin_01_Lid_Green_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Residential/Trashbin_01/MI_Res_Trashbin_01_Yellow.MI_Res_Trashbin_01_Yellow", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/Trashbin_01/BP_SocketContainer_Res_TrashBin_01_Lid_Yellow.BP_SocketContainer_Res_TrashBin_01_Lid_Yellow_C", "/Game/Pioneer/Environment/Props/Residential/Trashbin_01/BP_SocketContainer_Res_TrashBin_01_Lid_Yellow.Default__BP_SocketContainer_Res_TrashBin_01_Lid_Yellow_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Residential.VenetianDrawer",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Residential/VenetianDrawer_01/BP_SocketContainer_VenetianDrawer_01_A.BP_SocketContainer_VenetianDrawer_01_A_C", "/Game/Pioneer/Environment/Props/Residential/VenetianDrawer_01/BP_SocketContainer_VenetianDrawer_01_A.Default__BP_SocketContainer_VenetianDrawer_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Residential.Wrh.Desk.DrawerLarge",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/Desk_01/BP_SocketContainer_Wrh_Desk_DrawerLarge.BP_SocketContainer_Wrh_Desk_DrawerLarge_C", "/Game/Pioneer/Environment/Props/Warehouse/Desk_01/BP_SocketContainer_Wrh_Desk_DrawerLarge.Default__BP_SocketContainer_Wrh_Desk_DrawerLarge_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/Desk_01/MI_Wrh_Desk_01_A_White.MI_Wrh_Desk_01_A_White", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/Desk_01/BP_SocketContainer_Wrh_Desk_DrawerLarge_White.BP_SocketContainer_Wrh_Desk_DrawerLarge_White_C", "/Game/Pioneer/Environment/Props/Warehouse/Desk_01/BP_SocketContainer_Wrh_Desk_DrawerLarge_White.Default__BP_SocketContainer_Wrh_Desk_DrawerLarge_White_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Residential.Wrh.Desk.DrawerSmall",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/Desk_01/BP_SocketContainer_Wrh_Desk_DrawerSmall.BP_SocketContainer_Wrh_Desk_DrawerSmall_C", "/Game/Pioneer/Environment/Props/Warehouse/Desk_01/BP_SocketContainer_Wrh_Desk_DrawerSmall.Default__BP_SocketContainer_Wrh_Desk_DrawerSmall_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/Desk_01/MI_Wrh_Desk_01_A_White.MI_Wrh_Desk_01_A_White", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/Desk_01/BP_SocketContainer_Wrh_Desk_DrawerSmall_White.BP_SocketContainer_Wrh_Desk_DrawerSmall_White_C", "/Game/Pioneer/Environment/Props/Warehouse/Desk_01/BP_SocketContainer_Wrh_Desk_DrawerSmall_White.Default__BP_SocketContainer_Wrh_Desk_DrawerSmall_White_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Security.Crate01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Security/Crate_01/BP_SocketContainer_Sct_Crate_01_A.BP_SocketContainer_Sct_Crate_01_A_C", "/Game/Pioneer/Environment/Props/Security/Crate_01/BP_SocketContainer_Sct_Crate_01_A.Default__BP_SocketContainer_Sct_Crate_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Security.Locker01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Security/Locker_01/BP_SocketContainer_Sct_Locker_01_A.BP_SocketContainer_Sct_Locker_01_A_C", "/Game/Pioneer/Environment/Props/Security/Locker_01/BP_SocketContainer_Sct_Locker_01_A.Default__BP_SocketContainer_Sct_Locker_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Security.LockerMedical01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Security/LockerMedical_01/BP_SocketContainer_Sct_LockerMedical_01_A.BP_SocketContainer_Sct_LockerMedical_01_A_C", "/Game/Pioneer/Environment/Props/Security/LockerMedical_01/BP_SocketContainer_Sct_LockerMedical_01_A.Default__BP_SocketContainer_Sct_LockerMedical_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Security.SecurityCamera01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/SecurityCamera_01/BP_SocketContainer_Sct_SecurityCamera_01_A.BP_SocketContainer_Sct_SecurityCamera_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/SecurityCamera_01/BP_SocketContainer_Sct_SecurityCamera_01_A.Default__BP_SocketContainer_Sct_SecurityCamera_01_A_C")],
            [],
            4),
        new(
            "Socket.LootContainer.Security.TruckScannerLeft01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Security/TruckScanner_01/BP_SocketContainer_TruckScanner_01_Lid_A.BP_SocketContainer_TruckScanner_01_Lid_A_C", "/Game/Pioneer/Environment/Props/Security/TruckScanner_01/BP_SocketContainer_TruckScanner_01_Lid_A.Default__BP_SocketContainer_TruckScanner_01_Lid_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Security.TruckScannerRight01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Security/TruckScanner_01/BP_SocketContainer_TruckScanner_01_Lid_B.BP_SocketContainer_TruckScanner_01_Lid_B_C", "/Game/Pioneer/Environment/Props/Security/TruckScanner_01/BP_SocketContainer_TruckScanner_01_Lid_B.Default__BP_SocketContainer_TruckScanner_01_Lid_B_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Tech.AnalogDisplay",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/AnalogDisplay_01/BP_SocketContainer_AnalogDisplay_01_A.BP_SocketContainer_AnalogDisplay_01_A_C", "/Game/Pioneer/Environment/Props/_Generic/AnalogDisplay_01/BP_SocketContainer_AnalogDisplay_01_A.Default__BP_SocketContainer_AnalogDisplay_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Tech.Computer01.Screen",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/Computer_01/BP_SocketContainer_Wrh_Computer_01_A.BP_SocketContainer_Wrh_Computer_01_A_C", "/Game/Pioneer/Environment/Props/Warehouse/Computer_01/BP_SocketContainer_Wrh_Computer_01_A.Default__BP_SocketContainer_Wrh_Computer_01_A_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Warehouse/Computer_01/MI_Wrh_Computer_01_A_Black.MI_Wrh_Computer_01_A_Black", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Warehouse/Computer_01/BP_SocketContainer_Wrh_Computer_01_A_Black.BP_SocketContainer_Wrh_Computer_01_A_Black_C", "/Game/Pioneer/Environment/Props/Warehouse/Computer_01/BP_SocketContainer_Wrh_Computer_01_A_Black.Default__BP_SocketContainer_Wrh_Computer_01_A_Black_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Tech.Computer02.Screen",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Computer_01/BP_SocketContainer_Gvt_Computer_01_A.BP_SocketContainer_Gvt_Computer_01_A_C", "/Game/Pioneer/Environment/Props/Government/Computer_01/BP_SocketContainer_Gvt_Computer_01_A.Default__BP_SocketContainer_Gvt_Computer_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Tech.Computer03.Panel",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Computer_03/BP_SocketContainer_Gvt_Computer_03.BP_SocketContainer_Gvt_Computer_03_C", "/Game/Pioneer/Environment/Props/Government/Computer_03/BP_SocketContainer_Gvt_Computer_03.Default__BP_SocketContainer_Gvt_Computer_03_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Computer_03/MI_FZT_Gvt_Computer_03_A.MI_FZT_Gvt_Computer_03_A", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Computer_03/BP_SocketContainer_Gvt_Computer_03_FZT.BP_SocketContainer_Gvt_Computer_03_FZT_C", "/Game/Pioneer/Environment/Props/Government/Computer_03/BP_SocketContainer_Gvt_Computer_03_FZT.Default__BP_SocketContainer_Gvt_Computer_03_FZT_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Computer_03/MI_Gvt_Computer_03_A_Clean.MI_Gvt_Computer_03_A_Clean", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Computer_03/BP_SocketContainer_Gvt_Computer_03_Clean.BP_SocketContainer_Gvt_Computer_03_Clean_C", "/Game/Pioneer/Environment/Props/Government/Computer_03/BP_SocketContainer_Gvt_Computer_03_Clean.Default__BP_SocketContainer_Gvt_Computer_03_Clean_C")]),
            ],
            1),
        new(
            "Socket.LootContainer.Tech.Coms.Console_02.Lid_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Communications/Console_02/BP_SocketContainer_Coms_Console_02_A.BP_SocketContainer_Coms_Console_02_A_C", "/Game/Pioneer/Environment/Props/Communications/Console_02/BP_SocketContainer_Coms_Console_02_A.Default__BP_SocketContainer_Coms_Console_02_A_C")],
            [],
            1),
        new(
            "Socket.LootContainer.Tech.Coms.Console_03.Lid_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Communications/Console_03/BP_SocketContainer_Coms_Console_03_A.BP_SocketContainer_Coms_Console_03_A_C", "/Game/Pioneer/Environment/Props/Communications/Console_03/BP_SocketContainer_Coms_Console_03_A.Default__BP_SocketContainer_Coms_Console_03_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Tech.Coms.Server_01.Rack_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Communications/Servers_01/BP_SocketContainer_Coms_Servers_01_A.BP_SocketContainer_Coms_Servers_01_A_C", "/Game/Pioneer/Environment/Props/Communications/Servers_01/BP_SocketContainer_Coms_Servers_01_A.Default__BP_SocketContainer_Coms_Servers_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Tech.Coms.Servers_02.Rack_01",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Communications/Servers_02/BP_SocketContainer_Coms_Servers_02_A.BP_SocketContainer_Coms_Servers_02_A_C", "/Game/Pioneer/Environment/Props/Communications/Servers_02/BP_SocketContainer_Coms_Servers_02_A.Default__BP_SocketContainer_Coms_Servers_02_A_C")],
            [],
            1),
        new(
            "Socket.LootContainer.Tech.DataStorageContainer",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/_Generic/DataStorageContainer_01/BP_SocketContainer_DataStorageContainer_01.BP_SocketContainer_DataStorageContainer_01_C", "/Game/Pioneer/Environment/Props/_Generic/DataStorageContainer_01/BP_SocketContainer_DataStorageContainer_01.Default__BP_SocketContainer_DataStorageContainer_01_C")],
            [],
            1),
        new(
            "Socket.LootContainer.Tech.DeskMonitor",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/Monitor_01/BP_SocketContainer_DeskMonitor_01_A.BP_SocketContainer_DeskMonitor_01_A_C", "/Game/Pioneer/Environment/Props/Research/Monitor_01/BP_SocketContainer_DeskMonitor_01_A.Default__BP_SocketContainer_DeskMonitor_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Tech.FaxMachine",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/FaxingMachine_01/BP_SocketContainer_FaxMachine_01_A.BP_SocketContainer_FaxMachine_01_A_C", "/Game/Pioneer/Environment/Props/Research/FaxingMachine_01/BP_SocketContainer_FaxMachine_01_A.Default__BP_SocketContainer_FaxMachine_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Tech.InformationTerminal",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Commercial/InformationTerminal_01/BP_SocketContainer_InformationTerminal_01_Lid_01_A.BP_SocketContainer_InformationTerminal_01_Lid_01_A_C", "/Game/Pioneer/Environment/Props/Commercial/InformationTerminal_01/BP_SocketContainer_InformationTerminal_01_Lid_01_A.Default__BP_SocketContainer_InformationTerminal_01_Lid_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Tech.Server.Door",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door.BP_SocketContainer_Gvt_Server_01_x400z250_Door_C", "/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door.Default__BP_SocketContainer_Gvt_Server_01_x400z250_Door_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Server_01/MI_Gvt_Server_01_x400z250_A_Black.MI_Gvt_Server_01_x400z250_A_Black", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door_Black.BP_SocketContainer_Gvt_Server_01_x400z250_Door_Black_C", "/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door_Black.Default__BP_SocketContainer_Gvt_Server_01_x400z250_Door_Black_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Server_01/MI_Gvt_Server_01_x400z250_A_Clean.MI_Gvt_Server_01_x400z250_A_Clean", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door_Clean.BP_SocketContainer_Gvt_Server_01_x400z250_Door_Clean_C", "/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door_Clean.Default__BP_SocketContainer_Gvt_Server_01_x400z250_Door_Clean_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Server_01/MI_Gvt_Server_01_x400z250_A_Green.MI_Gvt_Server_01_x400z250_A_Green", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door_Green.BP_SocketContainer_Gvt_Server_01_x400z250_Door_Green_C", "/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door_Green.Default__BP_SocketContainer_Gvt_Server_01_x400z250_Door_Green_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Server_01/MI_Gvt_Server_01_x400z250_A_MCP_01.MI_Gvt_Server_01_x400z250_A_MCP_01", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door_MCP.BP_SocketContainer_Gvt_Server_01_x400z250_Door_MCP_C", "/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door_MCP.Default__BP_SocketContainer_Gvt_Server_01_x400z250_Door_MCP_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Server_01/MI_Gvt_Server_01_x400z250_A_Red.MI_Gvt_Server_01_x400z250_A_Red", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door_Red.BP_SocketContainer_Gvt_Server_01_x400z250_Door_Red_C", "/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door_Red.Default__BP_SocketContainer_Gvt_Server_01_x400z250_Door_Red_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Server_01/MI_Gvt_Server_01_x400z250_A_Red2.MI_Gvt_Server_01_x400z250_A_Red2", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door_Red2.BP_SocketContainer_Gvt_Server_01_x400z250_Door_Red2_C", "/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door_Red2.Default__BP_SocketContainer_Gvt_Server_01_x400z250_Door_Red2_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Server_01/MI_Gvt_Server_01_x400z250_B.MI_Gvt_Server_01_x400z250_B", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door_B.BP_SocketContainer_Gvt_Server_01_x400z250_Door_B_C", "/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door_B.Default__BP_SocketContainer_Gvt_Server_01_x400z250_Door_B_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Tech.SwitchGearPanel.BreachPanel",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/SwitchGearPanel_01/BP_SocketContainer_SwitchGearPanel_01_DoorBreach_A.BP_SocketContainer_SwitchGearPanel_01_DoorBreach_A_C", "/Game/Pioneer/Environment/Props/Research/SwitchGearPanel_01/BP_SocketContainer_SwitchGearPanel_01_DoorBreach_A.Default__BP_SocketContainer_SwitchGearPanel_01_DoorBreach_A_C")],
            [],
            3),
        new(
            "Socket.LootContainer.Tech.SwitchGearPanel.ExtendLid",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/SwitchGearPanel_01/BP_SocketContainer_SwitchGearPanel_01_ExtendLid_A.BP_SocketContainer_SwitchGearPanel_01_ExtendLid_A_C", "/Game/Pioneer/Environment/Props/Research/SwitchGearPanel_01/BP_SocketContainer_SwitchGearPanel_01_ExtendLid_A.Default__BP_SocketContainer_SwitchGearPanel_01_ExtendLid_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Tech.SwitchGearPanel.UpperLid",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Research/SwitchGearPanel_01/BP_SocketContainer_SwitchGearPanel_01_UpperLid_A.BP_SocketContainer_SwitchGearPanel_01_UpperLid_A_C", "/Game/Pioneer/Environment/Props/Research/SwitchGearPanel_01/BP_SocketContainer_SwitchGearPanel_01_UpperLid_A.Default__BP_SocketContainer_SwitchGearPanel_01_UpperLid_A_C")],
            [],
            1),
        new(
            "Socket.LootContainer.Tech.TapeDrive01.Lid",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Machine_M_01/BP_SocketContainer_Gvt_Machine_M_01_A.BP_SocketContainer_Gvt_Machine_M_01_A_C", "/Game/Pioneer/Environment/Props/Government/Machine_M_01/BP_SocketContainer_Gvt_Machine_M_01_A.Default__BP_SocketContainer_Gvt_Machine_M_01_A_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Tech.TapeDrive02.PanelLarge",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Machine_M_02/BP_SocketContainer_Gvt_Machine_M_02_A_LargePanel.BP_SocketContainer_Gvt_Machine_M_02_A_LargePanel_C", "/Game/Pioneer/Environment/Props/Government/Machine_M_02/BP_SocketContainer_Gvt_Machine_M_02_A_LargePanel.Default__BP_SocketContainer_Gvt_Machine_M_02_A_LargePanel_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Tech.TapeDrive02.PanelSmall",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Machine_M_02/BP_SocketContainer_Gvt_Machine_M_02_A_SmallPanel.BP_SocketContainer_Gvt_Machine_M_02_A_SmallPanel_C", "/Game/Pioneer/Environment/Props/Government/Machine_M_02/BP_SocketContainer_Gvt_Machine_M_02_A_SmallPanel.Default__BP_SocketContainer_Gvt_Machine_M_02_A_SmallPanel_C")],
            [],
            2),
        new(
            "Socket.LootContainer.Tech.TapeDrive04.PanelLarge",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Machine_M_04/BP_SocketContainer_Gvt_Machine_M_04_A_PanelLarge.BP_SocketContainer_Gvt_Machine_M_04_A_PanelLarge_C", "/Game/Pioneer/Environment/Props/Government/Machine_M_04/BP_SocketContainer_Gvt_Machine_M_04_A_PanelLarge.Default__BP_SocketContainer_Gvt_Machine_M_04_A_PanelLarge_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Machine_M_04/MI_Gvt_Machine_M_04_A_Green.MI_Gvt_Machine_M_04_A_Green", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Machine_M_04/BP_SocketContainer_Gvt_Machine_M_04_A_PanelLarge_Green.BP_SocketContainer_Gvt_Machine_M_04_A_PanelLarge_Green_C", "/Game/Pioneer/Environment/Props/Government/Machine_M_04/BP_SocketContainer_Gvt_Machine_M_04_A_PanelLarge_Green.Default__BP_SocketContainer_Gvt_Machine_M_04_A_PanelLarge_Green_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Machine_M_04/MI_Gvt_Machine_M_04_A_SpaceTravelCo_01.MI_Gvt_Machine_M_04_A_SpaceTravelCo_01", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Machine_M_04/BP_SocketContainer_Gvt_Machine_M_04_A_PanelLarge_SpaceTravelCo_01.BP_SocketContainer_Gvt_Machine_M_04_A_PanelLarge_SpaceTravelCo_01_C", "/Game/Pioneer/Environment/Props/Government/Machine_M_04/BP_SocketContainer_Gvt_Machine_M_04_A_PanelLarge_SpaceTravelCo_01.Default__BP_SocketContainer_Gvt_Machine_M_04_A_PanelLarge_SpaceTravelCo_01_C")]),
            ],
            2),
        new(
            "Socket.LootContainer.Tech.TapeDrive04.PanelSmall",
            [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Machine_M_04/BP_SocketContainer_Gvt_Machine_M_04_A_PanelSmall.BP_SocketContainer_Gvt_Machine_M_04_A_PanelSmall_C", "/Game/Pioneer/Environment/Props/Government/Machine_M_04/BP_SocketContainer_Gvt_Machine_M_04_A_PanelSmall.Default__BP_SocketContainer_Gvt_Machine_M_04_A_PanelSmall_C")],
            [
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Machine_M_04/MI_Gvt_Machine_M_04_A_Green.MI_Gvt_Machine_M_04_A_Green", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Machine_M_04/BP_SocketContainer_Gvt_Machine_M_04_A_PanelSmall_Green.BP_SocketContainer_Gvt_Machine_M_04_A_PanelSmall_Green_C", "/Game/Pioneer/Environment/Props/Government/Machine_M_04/BP_SocketContainer_Gvt_Machine_M_04_A_PanelSmall_Green.Default__BP_SocketContainer_Gvt_Machine_M_04_A_PanelSmall_Green_C")]),
                new LootContainerVariant("/Game/Pioneer/Environment/Props/Government/Machine_M_04/MI_Gvt_Machine_M_04_A_SpaceTravelCo_01.MI_Gvt_Machine_M_04_A_SpaceTravelCo_01", [new ArchetypeClass("/Game/Pioneer/Environment/Props/Government/Machine_M_04/BP_SocketContainer_Gvt_Machine_M_04_A_PanelSmall_SpaceTravelCo_01.BP_SocketContainer_Gvt_Machine_M_04_A_PanelSmall_SpaceTravelCo_01_C", "/Game/Pioneer/Environment/Props/Government/Machine_M_04/BP_SocketContainer_Gvt_Machine_M_04_A_PanelSmall_SpaceTravelCo_01.Default__BP_SocketContainer_Gvt_Machine_M_04_A_PanelSmall_SpaceTravelCo_01_C")]),
            ],
            2),
    ];

    private static readonly Dictionary<string, LootContainerType> s_byTag = All.ToDictionary(type => type.Tag, StringComparer.OrdinalIgnoreCase);

    /// <summary>The container type of a socket tag (compared like the game compares tag names: ignoring case), or null.</summary>
    public static LootContainerType? Find(string tag)
    {
        return s_byTag.GetValueOrDefault(tag);
    }
}
