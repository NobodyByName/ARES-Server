using Ares.Structs.Core.Geometry;
using Ares.Structs.Net.Replication;

namespace Ares.Structs.Game.World;

/// <summary>
/// What the elevator extraction points of a map share: the settings asset the elevator Blueprint names and its hold
/// times, the extraction zone trigger, and the "not set" times of the replicated timers.
/// </summary>
/// <param name="ExtractionType">Gameplay tag naming the extraction type of the elevators.</param>
/// <param name="SettingsAsset">Settings asset the elevator Blueprint points at.</param>
/// <param name="Settings">Hold times of the settings asset.</param>
/// <param name="ZoneHalfExtent">Half-extents of the extraction zone trigger (a box), in centimetres.</param>
/// <param name="ZoneLocation">Centre of the extraction zone trigger relative to the elevator, in centimetres.</param>
/// <param name="ZoneRotation">Rotation of the extraction zone trigger relative to the elevator, in degrees.</param>
/// <param name="NoHoldStartedTime">Hold start time of a button nobody holds (<c>InteractionServerHoldStartedTime</c> default).</param>
/// <param name="NoExtractionStartedTime">Start time of an extraction timer that does not run (<c>ExtractionStartedTS</c> default).</param>
/// <param name="DoorsSeconds">
/// Seconds the doors take to open or to close: the latest key of the curves the door animation's tracks use, at the
/// animation's default play speed.
/// </param>
public sealed record ExtractionElevator(
    string ExtractionType,
    string SettingsAsset,
    ExtractionSettings Settings,
    Vector3D ZoneHalfExtent,
    Vector3D ZoneLocation,
    Rotator ZoneRotation,
    double NoHoldStartedTime,
    double NoExtractionStartedTime,
    double DoorsSeconds);

/// <summary>The elevator extraction point classes, the buttons the server drives and the script members the tables read.</summary>
public static class ExtractionClasses
{
    /// <summary>Package of the elevator Blueprint.</summary>
    public const string ElevatorPackage = "/Game/Pioneer/Core/GameModes/Scavenge/BP_SalvageExtractionPoint_Elevator";

    /// <summary>Elevator Blueprint, with its class default.</summary>
    public static ArchetypeClass Elevator { get; } = new(
        ElevatorPackage + ".BP_SalvageExtractionPoint_Elevator_C",
        ElevatorPackage + ".Default__BP_SalvageExtractionPoint_Elevator_C");

    /// <summary>Class whose replication layout the elevator Blueprint uses.</summary>
    public const string ActorClass = "/Script/Angelscript.SalvageExtractionPoint_Elevator";

    /// <summary>Script class of the elevators.</summary>
    public const string ScriptClass = "ASalvageExtractionPoint_Elevator";

    /// <summary>The buttons of each elevator, in table order, and what each one does.</summary>
    public static IReadOnlyList<(string Component, ExtractionButtonRole Role)> Buttons { get; } =
    [
        ("ElevatorSummonInteraction", ExtractionButtonRole.Summon),
        ("DescendInteraction_A", ExtractionButtonRole.Descend),
        ("DescendInteraction_B", ExtractionButtonRole.Descend),
        ("RepairInteraction_A", ExtractionButtonRole.Repair),
        ("RepairInteraction_B", ExtractionButtonRole.Repair),
        ("RepairInteraction_C", ExtractionButtonRole.Repair),
    ];

    /// <summary>The extraction-state enum (<c>EExtractionState</c>) as replication needs it.</summary>
    public static EnumDefinition StateEnum { get; } = new("/Script/Angelscript.EExtractionState", 8,
        ["Dormant", "Disabled", "Broken", "Closed", "Closing", "Requested", "Arriving", "Ready"]);

    /// <summary>Script name of the interaction type enum (<see cref="StandardInteractionType"/>).</summary>
    public const string InteractionTypeEnum = "EStandardInteractionType";
}

/// <summary>Script members of the extraction point classes the tables read, by name.</summary>
public static class ExtractionMembers
{
    /// <summary><c>ASalvageExtractionPointBase.ExtractionDataAsset</c>: the placed elevator's data asset.</summary>
    public const string ExtractionDataAsset = "ExtractionDataAsset";

    /// <summary>The elevator's <c>SettingsDataAsset</c>: the settings asset its Blueprint default names.</summary>
    public const string SettingsDataAsset = "SettingsDataAsset";

    /// <summary>The elevator's extraction zone trigger component (a box).</summary>
    public const string ExtractionZoneTrigger = "ExtractionZoneTrigger";

    /// <summary><c>UExtractionPointDataAsset.TitleText</c>: string-table title.</summary>
    public const string TitleText = "TitleText";

    /// <summary><c>UExtractionPointDataAsset.DescriptiveText</c>: string-table description.</summary>
    public const string DescriptiveText = "DescriptiveText";

    /// <summary><c>UExtractionPointDataAsset.DialogueTag</c>: dialogue tag of the location.</summary>
    public const string DialogueTag = "DialogueTag";

    /// <summary><c>UStandardInteractionComponent.InteractionType</c>.</summary>
    public const string InteractionType = "InteractionType";

    /// <summary><c>UStandardInteractionComponent.HoldDuration</c>.</summary>
    public const string HoldDuration = "HoldDuration";

    /// <summary><c>UStandardInteractionComponent.AlignmentTime</c>.</summary>
    public const string AlignmentTime = "AlignmentTime";

    /// <summary><c>UStandardInteractionComponent.AnimationContainer</c>, the last standard member the tables read.</summary>
    public const string AnimationContainer = "AnimationContainer";

    /// <summary>The elevator Blueprint's door animation component (a <see cref="AnimationComponentClass"/>).</summary>
    public const string DoorAnimation = "ElevatorDoorAnimationComponent";

    /// <summary>Script class of the door animation component.</summary>
    public const string AnimationComponentClass = "USimpleAnimationSequenceComponent";

    /// <summary><c>USimpleAnimationSequenceComponent.AnimationTracks</c>: its tracks, the first reflected member.</summary>
    public const string AnimationTracks = "AnimationTracks";

    /// <summary><c>USimpleAnimationSequenceComponent.StandardPlaySpeed</c>: the play speed, the second reflected member.</summary>
    public const string StandardPlaySpeed = "StandardPlaySpeed";
}

/// <summary>Class defaults of the elevator extraction points that no asset of the game stores.</summary>
public static class ExtractionDefaults
{
    /// <summary>Script class of the elevator settings asset, whose class defaults are its hold times.</summary>
    public const string SettingsClass = "USalvageExtractionPointSettingsDataAsset_Major";

    /// <summary><c>SummonHoldDuration</c> of <see cref="SettingsClass"/>: the call button's hold time, in seconds.</summary>
    public const double SummonHoldSeconds = 0;

    /// <summary><c>ExtractionHoldDuration</c> of <see cref="SettingsClass"/>: a descend switch's hold time, in seconds.</summary>
    public const double ExtractionHoldSeconds = 5;

    /// <summary><c>FExtractionInfo.ExtractionStartedTS</c>: the start time of an extraction timer that does not run.</summary>
    public const double NoExtractionStartedTime = -1;

    /// <summary><c>USimpleAnimationSequenceComponent.StandardPlaySpeed</c>: the door animation's play speed.</summary>
    public const double AnimationPlaySpeed = 1;
}
