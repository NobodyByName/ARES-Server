namespace Ares.Structs.Game.World;

/// <summary>The classes and component names of the placed zipline actors.</summary>
public static class ZiplineClasses
{
    /// <summary>Package of the placed rideable line Blueprint.</summary>
    public const string LineBlueprintPackage = "/Game/Pioneer/Core/Gameplay/BP_PlaceableZipline";

    /// <summary>Package of the placed launcher Blueprint.</summary>
    public const string LauncherBlueprintPackage = "/Game/Pioneer/Core/Gameplay/ActivatableZiplines/BP_ActivatableZipline";

    /// <summary>Class of the placed rideable lines.</summary>
    public const string LineBlueprint = LineBlueprintPackage + ".BP_PlaceableZipline_C";

    /// <summary>Class of the placed launchers.</summary>
    public const string LauncherBlueprint = LauncherBlueprintPackage + ".BP_ActivatableZipline_C";

    /// <summary>Class whose replication layout the line Blueprints use.</summary>
    public const string LineClass = "/Script/Angelscript.ZippableLine";

    /// <summary>Class whose replication layout the launcher Blueprint uses.</summary>
    public const string LauncherClass = "/Script/Angelscript.ActivatableStaticZipline";

    /// <summary>Name of a line's interaction component that attaches a character.</summary>
    public const string AttachInteraction = "AttachInteraction";

    /// <summary>Name of a launcher's interaction component that deploys its line.</summary>
    public const string ActivationInteraction = "ActivationInteraction";

    /// <summary>Name of a zipline actor's root component.</summary>
    public const string DefaultSceneRoot = "DefaultSceneRoot";

    /// <summary>Name prefix of placed line actors.</summary>
    public const string LineActorPrefix = "BP_PlaceableZipline";

    /// <summary>Name prefix of placed launcher actors.</summary>
    public const string LauncherActorPrefix = "BP_ActivatableZipline";

    /// <summary>Components every placed line actor has.</summary>
    public static IReadOnlyList<string> LineComponents { get; } = [AttachInteraction, "AttachmentInformationContainerComponent", "CableMesh", DefaultSceneRoot];

    /// <summary>Components every placed launcher actor has.</summary>
    public static IReadOnlyList<string> LauncherComponents { get; } = [ActivationInteraction, "DeployAnimation", DefaultSceneRoot];
}

/// <summary>
/// The members a placed <c>BP_PlaceableZipline</c> (an <c>AZippableLine</c>) may store, by chain position (the
/// Blueprint's own members first, then each script parent's and <c>AActor</c>'s).
/// </summary>
public static class PlaceableZiplineMembers
{
    /// <summary><c>RoofAttachement</c>: object reference.</summary>
    public const int RoofAttachement = 0;

    /// <summary><c>BottomCablePile</c>: object reference.</summary>
    public const int BottomCablePile = 1;

    /// <summary><c>bAngledRoofAttachment</c>: one byte.</summary>
    public const int AngledRoofAttachment = 2;

    /// <summary><c>LocationOffset</c>: vector of three doubles.</summary>
    public const int LocationOffset = 3;

    /// <summary><c>RotationOffset</c>: three doubles.</summary>
    public const int RotationOffset = 4;

    /// <summary><c>CableMesh</c>: object reference.</summary>
    public const int CableMesh = 5;

    /// <summary><c>bDoExtraBlockBarricadeCheckAroundExits</c>: one byte.</summary>
    public const int DoExtraBlockBarricadeCheckAroundExits = 11;

    /// <summary><c>PioneerPingInfoComponent</c>: object reference.</summary>
    public const int PioneerPingInfoComponent = 12;

    /// <summary><c>AttachSound</c>: object reference.</summary>
    public const int AttachSound = 14;

    /// <summary><c>DefaultSceneRoot</c>: object reference.</summary>
    public const int DefaultSceneRoot = 15;

    /// <summary><c>InteractionProxyObject</c>: object reference.</summary>
    public const int InteractionProxyObject = 16;

    /// <summary><c>EndAttachPosition</c>: vector of three doubles, in the actor's space.</summary>
    public const int EndAttachPosition = 17;

    /// <summary><c>ZiplineEffect</c>: object reference.</summary>
    public const int ZiplineEffect = 23;

    /// <summary><c>AttachInteraction</c>: object reference.</summary>
    public const int AttachInteraction = 24;

    /// <summary><c>AttachmentInformationContainerComponent</c>: object reference.</summary>
    public const int AttachmentInformationContainerComponent = 25;

    /// <summary><c>bUseManualInteractionBoxSize</c>: one byte.</summary>
    public const int UseManualInteractionBoxSize = 28;

    /// <summary><c>ManualInteractionSizeZ</c>: double.</summary>
    public const int ManualInteractionSizeZ = 30;

    /// <summary>
    /// <c>ForcedTravelDirectionThresholdHome</c>: double. Fitted, not unique: it would be <c>InteractionSizeY</c> if
    /// <c>InteractionSizeY</c> and <c>InteractionSizeZ</c> were reflected.
    /// </summary>
    public const int ForcedTravelDirectionThresholdHome = 31;

    /// <summary><c>ExitPoints</c>: array of <see cref="ZiplineExitPointMembers"/> structs.</summary>
    public const int ExitPoints = 38;

    /// <summary><c>RootComponent</c>: object reference.</summary>
    public const int RootComponent = 105;

    /// <summary><c>BlueprintCreatedComponents</c>: array of object references.</summary>
    public const int BlueprintCreatedComponents = 127;
}

/// <summary>
/// The members a placed <c>BP_ActivatableZipline</c> (an <c>AActivatableStaticZipline</c>) may store, by chain
/// position (the Blueprint's own members first, then each script parent's and <c>AActor</c>'s).
/// </summary>
public static class ActivatableZiplineMembers
{
    /// <summary>First of the Blueprint's own component references (object references up to <see cref="LastComponent"/>).</summary>
    public const int FirstComponent = 1;

    /// <summary>Last of the Blueprint's own component references.</summary>
    public const int LastComponent = 17;

    /// <summary><c>bStartDeployed</c>: one byte.</summary>
    public const int StartDeployed = 18;

    /// <summary><c>bTwoSideActivation</c>: one byte.</summary>
    public const int TwoSideActivation = 19;

    /// <summary><c>ZiplineClass</c>: class reference (an import).</summary>
    public const int ZiplineClass = 20;

    /// <summary><c>bHasBeenDeployed</c>: one byte.</summary>
    public const int HasBeenDeployed = 24;

    /// <summary><c>ScatterBlocking</c>: object reference.</summary>
    public const int ScatterBlocking = 29;

    /// <summary><c>ActivationInteraction</c>: object reference.</summary>
    public const int ActivationInteraction = 30;

    /// <summary><c>AnchorLocation</c>: vector of three doubles, in the launcher's space.</summary>
    public const int AnchorLocation = 31;

    /// <summary><c>DefaultSceneRoot</c>: object reference.</summary>
    public const int DefaultSceneRoot = 34;

    /// <summary>Unknown member, object reference.</summary>
    public const int Unknown39 = 39;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown95 = 95;

    /// <summary><c>RootComponent</c>: object reference.</summary>
    public const int RootComponent = 108;

    /// <summary><c>BlueprintCreatedComponents</c>: array of object references.</summary>
    public const int BlueprintCreatedComponents = 130;
}

/// <summary>Members of the unversioned <c>FZiplineExitPoint</c> struct, in declaration order.</summary>
public static class ZiplineExitPointMembers
{
    /// <summary><c>LaunchTarget</c>: vector of three doubles.</summary>
    public const int LaunchTarget = 0;

    /// <summary><c>LaunchFrom</c>: vector of three doubles.</summary>
    public const int LaunchFrom = 1;

    /// <summary><c>LaunchSpeed</c>: double.</summary>
    public const int LaunchSpeed = 2;

    /// <summary><c>LaunchFromDistance</c>: double.</summary>
    public const int LaunchFromDistance = 3;

    /// <summary><c>bAdditive</c>: one byte.</summary>
    public const int Additive = 4;

    /// <summary><c>bMoreExact</c>: one byte.</summary>
    public const int MoreExact = 5;

    /// <summary><c>bAdvanced</c>: one byte.</summary>
    public const int Advanced = 6;

    /// <summary><c>TravelDirection</c>: enum, one byte (see <see cref="ZiplineTravelDirection"/>).</summary>
    public const int TravelDirection = 7;

    /// <summary><c>Fraction</c>: double.</summary>
    public const int Fraction = 8;

    /// <summary><c>DistanceAsFraction</c>: double.</summary>
    public const int DistanceAsFraction = 9;

    /// <summary><c>AbilityTag</c>: object reference.</summary>
    public const int AbilityTag = 10;

    /// <summary><c>bResetAndLaunchCharacterIfAbilityActivationSuccess</c>: one byte.</summary>
    public const int ResetAndLaunch = 11;

    /// <summary>Members of the struct.</summary>
    public const int MemberCount = 12;

    /// <summary>Most exit points a line may hold.</summary>
    public const int MaxExitPoints = 16;
}
