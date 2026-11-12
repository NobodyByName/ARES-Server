using Ares.Structs.Core.Geometry;

namespace Ares.Structs.Game.World;

/// <summary>A loot container the server spawns: its class and where it stands.</summary>
/// <param name="Container">Container class, with its class default.</param>
/// <param name="Location">World location.</param>
/// <param name="Rotation">World rotation in degrees.</param>
public readonly record struct LootPlacement(ArchetypeClass Container, Vector3D Location, Rotator Rotation);

/// <summary>
/// Loot containers of the Dam map. Socket containers are not placed in the map: the server spawns them at
/// their sockets, each with a search interaction and an item container.
/// </summary>
public static class DamLoot
{
    /// <summary>Server-room cabinet door, a socket container of the government server racks.</summary>
    public static ArchetypeClass ServerCabinet { get; } = new(
        "/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door.BP_SocketContainer_Gvt_Server_01_x400z250_Door_C",
        "/Game/Pioneer/Environment/Props/Government/Server_01/BP_SocketContainer_Gvt_Server_01_x400z250_Door.Default__BP_SocketContainer_Gvt_Server_01_x400z250_Door_C");

    /// <summary>Script class of the socket containers (their Blueprints add no replicated properties).</summary>
    public const string ActorClass = "/Script/Angelscript.LootContainerSingle";

    /// <summary>Class of the search interaction.</summary>
    public const string InteractionClass = "/Script/Angelscript.LootInteractionComponent";

    /// <summary>Class of the item container.</summary>
    public const string ContainerClass = "/Script/Angelscript.ItemContainerComponent";

    /// <summary>Name of the search interaction component.</summary>
    public const string InteractionComponent = "LootInteractionComponent";

    /// <summary>Name of the item container component.</summary>
    public const string ContainerComponent = "ItemContainerComponent";

    /// <summary>The container's replicated components, by name.</summary>
    public static IReadOnlyList<string> Components { get; } = [InteractionComponent, "Mesh", ContainerComponent, "SearchInteractQuestComponent"];

    /// <summary>Animation set the character plays while searching a server cabinet.</summary>
    public const string SearchAnimation =
        "/Game/Pioneer/Characters/Avatar/Animations/DataAssets/DA_InteractionAnimation_PryFromAbove.DA_InteractionAnimation_PryFromAbove";

    /// <summary>Where the character's search of a server cabinet starts, in the space of the cabinet's prompt.</summary>
    public static Vector3D SearchLocation { get; } = new(76.3, 6.2, -0.1);

    /// <summary>
    /// The server cabinet's prompt (its search interaction component) relative to the cabinet: an open cabinet
    /// measures the player's distance from it.
    /// </summary>
    public static Vector3D PromptLocation { get; } = new(76.37448040273613, 6.232721592968787, -1.147489288226943e-06);

    /// <summary>
    /// Prompt distance of an opened container: the standard prompt distance preset, since an opened container
    /// has no hold and so leaves the player free to move.
    /// </summary>
    public const double OpenedPromptDistance = 135;

    /// <summary>Distance the server adds to a prompt distance before an interaction under way ends.</summary>
    public const double ServerPromptLeeway = 75;

    /// <summary>Hold start time of a search interaction nobody holds.</summary>
    public const double NoHoldStartedTime = -1;

    /// <summary>Slots of a searched container's item container.</summary>
    public const int SlotCount = 8;

    /// <summary>Time the character takes to line up with a container before the search hold starts.</summary>
    public const double SearchAlignmentSeconds = 1.0;

    /// <summary>Hold time of a first search (the breach), after which the container opens.</summary>
    public const double SearchHoldSeconds = 3.5;

    /// <summary>Hold time of a container that has been opened: it opens again as soon as the character is lined up.</summary>
    public const float OpenedHoldSeconds = 0;

    /// <summary>The containers the server spawns.</summary>
    public static IReadOnlyList<LootPlacement> All { get; } =
    [
        new(ServerCabinet, new Vector3D(452930.7, 559705.2, -38043.2), new Rotator(0, 30, 180)),
    ];
}
