using Ares.Structs.Core.Geometry;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Game.World;

/// <summary>Type of a standard interaction button (<c>EStandardInteractionType</c>).</summary>
public enum StandardInteractionType : byte
{
    Press = 0,
    Hold = 1,
    Continuous = 2,
}

/// <summary>State of an extraction point (<c>EExtractionState</c>).</summary>
public enum ExtractionState : byte
{
    Dormant = 0,
    Disabled = 1,
    Broken = 2,
    Closed = 3,
    Closing = 4,
    Requested = 5,
    Arriving = 6,
    Ready = 7,
}

/// <summary>
/// Elevator extraction points of the Dam map: Blueprint actors placed in the map's persistent level,
/// the buttons each one has, and the timings of the settings asset they share.
/// </summary>
public static class DamExtraction
{
    /// <summary>Elevator Blueprint, with its class default.</summary>
    public static ArchetypeClass Elevator { get; } = new("/Game/Pioneer/Core/GameModes/Scavenge/BP_SalvageExtractionPoint_Elevator.BP_SalvageExtractionPoint_Elevator_C", "/Game/Pioneer/Core/GameModes/Scavenge/BP_SalvageExtractionPoint_Elevator.Default__BP_SalvageExtractionPoint_Elevator_C");

    /// <summary>Class whose replication layout the elevator Blueprint uses.</summary>
    public const string ActorClass = "/Script/Angelscript.SalvageExtractionPoint_Elevator";

    /// <summary>Gameplay tag naming the extraction type of the elevators.</summary>
    public const string ExtractionType = "GameMode.Salvage.ExtractionPoint.Elevator";

    /// <summary>The extraction-state enum as replication needs it.</summary>
    public static EnumDefinition StateEnum { get; } = new("/Script/Angelscript.EExtractionState", 8, ["Dormant", "Disabled", "Broken", "Closed", "Closing", "Requested", "Arriving", "Ready"]);

    /// <summary>Settings asset the elevator Blueprint points at.</summary>
    public const string SettingsAsset = "/Game/Pioneer/Core/Gameplay/ExtractionPoints/DA_ExtractionPointSettings_Elevator";

    /// <summary>Timings of the settings asset.</summary>
    public static ExtractionSettings Settings { get; } = new(
        120.0,
        30.0,
        45.0,
        0.0,
        5.0
    );

    /// <summary>Seconds the elevator doors take to open or to close (the length of their animation).</summary>
    public const double DoorsSeconds = 5.266667;

    /// <summary>Hold start time of a button nobody holds (<c>InteractionServerHoldStartedTime</c> default).</summary>
    public const double NoHoldStartedTime = -1.0;

    /// <summary>Start time of an extraction timer that does not run (<c>ExtractionStartedTS</c> default).</summary>
    public const double NoExtractionStartedTime = -1.0;

    /// <summary>Half-extents of the extraction zone trigger (a box), in centimetres.</summary>
    public static Vector3D ZoneHalfExtent { get; } = new(530.0, 415.0, 160.0);

    /// <summary>Centre of the extraction zone trigger relative to the elevator, in centimetres.</summary>
    public static Vector3D ZoneLocation { get; } = new(0.0, 0.0, 60.0);

    /// <summary>Rotation of the extraction zone trigger relative to the elevator, in degrees.</summary>
    public static Rotator ZoneRotation { get; } = new(0.0, 0.0, 0.0);

    /// <summary>The buttons of each elevator (the same components on every elevator).</summary>
    public static IReadOnlyList<ExtractionButton> Buttons { get; } =
    [
        new("ElevatorSummonInteraction", ExtractionButtonRole.Summon, "/Script/Angelscript.StandardInteractionComponent", true, "CallElevator", StandardInteractionType.Press, 0.0, 0.2, null),
        new("DescendInteraction_A", ExtractionButtonRole.Descend, "/Script/Angelscript.StandardInteractionComponent", false, "Extract", StandardInteractionType.Hold, 5.0, 1.0, "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_ExtractionElevatorBox"),
        new("DescendInteraction_B", ExtractionButtonRole.Descend, "/Script/Angelscript.StandardInteractionComponent", false, "Extract", StandardInteractionType.Hold, 5.0, 1.0, "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_ExtractionElevatorBox"),
        new("RepairInteraction_A", ExtractionButtonRole.Repair, "/Script/Angelscript.RepairInteractionComponent", false, null, null, 0.0, 0.0, null),
        new("RepairInteraction_B", ExtractionButtonRole.Repair, "/Script/Angelscript.RepairInteractionComponent", false, null, null, 0.0, 0.0, null),
        new("RepairInteraction_C", ExtractionButtonRole.Repair, "/Script/Angelscript.RepairInteractionComponent", false, null, null, 0.0, 0.0, null),
    ];

    /// <summary>All elevators, in map order.</summary>
    public static IReadOnlyList<ExtractionPointPlacement> All { get; } =
    [
        new("GameModeLocationMarker_3", "/Game/Pioneer/Core/Gameplay/ExtractionPoints/DA_ExtractionPoint_SwampLift", "DAM_SWAMPLIFT_TITLE", "DAM_SWAMPLIFT_DESC", "DialogueBucketParameter.POI.Extraction.Elevator.CentralSwampLift", new Vector3D(439081.764614, 542099.490913, -40662.5), new Rotator(0.0, -64.999999, 0.0)),
        new("GameModeLocationMarker_ExctractionPoint10_1", "/Game/Pioneer/Core/Gameplay/ExtractionPoints/DA_ExtractionPoint_RedLakeBalcony", "DAM_REDLAKEBALCONY_TITLE", "DAM_REDLAKEBALCONY_DESC", "DialogueBucketParameter.POI.Extraction.Elevator.RedLakeBalcony", new Vector3D(500202.284013, 582946.941918, -42535.203333), new Rotator(0.0, -150.491788, 0.0)),
        new("GameModeLocationMarker_ExctractionPoint10_2", "/Game/Pioneer/Core/Gameplay/ExtractionPoints/DA_ExtractionPoint_Swampside", "ID_EXTRACTIONPOINTS_WATER_TREATMENT_ELEVATOR", "DAM_SWAMPSIDE_DESC", "DialogueBucketParameter.POI.Extraction.Elevator.Swampside", new Vector3D(452085.591684, 565102.586736, -37415.695201), new Rotator(0.0, 30.000001, 0.0)),
        new("GameModeLocationMarker_ExctractionPoint3_0", "/Game/Pioneer/Core/Gameplay/ExtractionPoints/DA_ExtractionPoint_NorthComplex", "DAM_NORTHCOMPLEX_TITLE", "DAM_NORTHCOMPLEX_DESC", "DialogueBucketParameter.POI.Extraction.Elevator.NorthComplex", new Vector3D(493170.048061, 510677.75341, -36725.502447), new Rotator(0.0, 119.943712, 0.0)),
    ];
}
