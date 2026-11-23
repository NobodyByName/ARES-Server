using Ares.Structs.Net.Replication;

namespace Ares.Structs.Game.World;

/// <summary>
/// The regular doors the map tables list (placed directly in a cell's persistent level, with two open/close buttons;
/// Harvester doors are another class, puzzle doors are placed under another name) and the parts of a door the server
/// drives.
/// </summary>
public static class DoorClasses
{
    /// <summary>Classes of the regular doors; a regular door actor is named after its class.</summary>
    public static IReadOnlyList<string> Blueprints { get; } =
    [
        "/Game/Pioneer/Interactables/Doors/BP_InteractiveDoor_Rsr_x350z300_01.BP_InteractiveDoor_Rsr_x350z300_01_C",
        "/Game/Pioneer/Interactables/Doors/BP_InteractiveDoor_Wrh_x300z325_01.BP_InteractiveDoor_Wrh_x300z325_01_C",
    ];

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

    /// <summary>The interaction-state enum (<c>ECurrentInteractionState</c>) as replication needs it.</summary>
    public static EnumDefinition InteractionState { get; } = new("/Script/Angelscript.ECurrentInteractionState", 8,
        ["Uninitialized", "Disabled", "Idle", "Ongoing", "Succeeding", "Cancelling", "SuccessIdle", "SuccessDisabled"]);
}

/// <summary>
/// The start flags of a door (members of the door script class <c>ADoorBase</c>), by script member name. Each is a bool
/// member of the door's property chain: a placed door or a Blueprint default that lists it sets it; otherwise the class
/// default (<see cref="Defaults"/>) applies.
/// </summary>
public static class DoorStartFlagMembers
{
    /// <summary>Starts open (<see cref="DoorPlacement.StartAsOpened"/>).</summary>
    public const string StartAsOpened = "bStartAsOpened";

    /// <summary>Starts locked (<see cref="DoorPlacement.StartAsLocked"/>).</summary>
    public const string StartAsLocked = "bStartAsLocked";

    /// <summary>Starts barricaded (<see cref="DoorPlacement.StartAsSealed"/>).</summary>
    public const string StartAsSealed = "bStartAsSealed";

    /// <summary>Starts powered (<see cref="DoorPlacement.StartAsPowered"/>).</summary>
    public const string StartAsPowered = "bStartAsPowered";

    /// <summary>Opens once breached (<see cref="DoorPlacement.BreachOpensDoor"/>).</summary>
    public const string BreachOpensDoor = "bBreachOpensDoor";

    /// <summary>The start flags with the value a door whose header and Blueprint defaults list none of them takes.</summary>
    public static IReadOnlyList<(string Member, bool Default)> Defaults { get; } =
    [
        (StartAsOpened, false),
        (StartAsLocked, false),
        (StartAsSealed, false),
        (StartAsPowered, true),
        (BreachOpensDoor, false),
    ];
}

/// <summary>The members a locked door's unlock items are read from (<c>ADoorBase.UnlockRequiredItems</c>, <c>FRequiredItemInfo</c>).</summary>
public static class DoorUnlockMembers
{
    /// <summary>The door's unlock items (an array of <c>FRequiredItemInfo</c>).</summary>
    public const string UnlockRequiredItems = "UnlockRequiredItems";

    /// <summary>The item class.</summary>
    public const string RequiredItem = "RequiredItem";

    /// <summary>How many of the item are needed.</summary>
    public const string RequiredAmount = "RequiredAmount";

    /// <summary>Whether unlocking takes the items.</summary>
    public const string ConsumeItemOnInteract = "bConsumeItemOnInteract";

    /// <summary>The locked room's exploit prevention area (a soft reference to an actor of the door's level).</summary>
    public const string LockedRoomExploitPreventionArea = "LockedRoomExploitPreventionArea";
}
