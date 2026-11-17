using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;

namespace Ares.Raid.World.Loot;

/// <summary>Where a loot container of a raid comes from.</summary>
public enum LootContainerSource
{
    /// <summary>A container socket of a prop.</summary>
    Socket,

    /// <summary>A point of a dynamic loot spawner.</summary>
    Spawner,

    /// <summary>A container actor the map places.</summary>
    Placed,

    /// <summary>A container an operator spawned during the raid.</summary>
    Operator,
}

/// <summary>A loot container the server spawns: at a socket, at a spawner's point or where the map places it.</summary>
/// <param name="Index">Index in <see cref="LootCatalog.Containers"/>.</param>
/// <param name="Source">Where it comes from.</param>
/// <param name="Location">World location.</param>
/// <param name="Rotation">World rotation in degrees.</param>
/// <param name="Socket">The socket it stands on, for a socket container; null otherwise.</param>
/// <param name="Class">Its class and how it is searched.</param>
/// <param name="Guid">The container actor (dynamic id).</param>
/// <param name="Archetype">Exported id of its class default.</param>
/// <param name="SearchAnimation">Exported id of the animation set of its search, or 0 when it has none.</param>
/// <param name="Components">Its components (dynamic ids), in <see cref="LootContainerClass.Components"/> order.</param>
/// <param name="Tag">Its container tag: its socket's tag, else the tag of the container type that lists its class; null when none does.</param>
/// <param name="LootValue">Loot value of the area around it.</param>
/// <param name="Contents">What it holds from its creation.</param>
public sealed record LootContainer(int Index, LootContainerSource Source, Vector3D Location, Rotator Rotation, LootSocket? Socket, LootContainerClass Class, ulong Guid, ulong Archetype, ulong SearchAnimation, IReadOnlyList<ulong> Components, string? Tag, PointOfInterestLootValue LootValue, LootContents Contents)
{
    /// <summary>Short name of its class (the class name without its <c>_C</c> suffix).</summary>
    public string ClassName
    {
        get
        {
            string name = Class.Container.Class[(Class.Container.Class.LastIndexOf('.') + 1)..];
            return name.EndsWith("_C", StringComparison.Ordinal) ? name[..^2] : name;
        }
    }

    /// <summary>The search interaction.</summary>
    public ulong Interaction => Components[ComponentIndex(LootContainerBlueprintMembers.InteractionComponent)];

    /// <summary>The item container.</summary>
    public ulong ItemContainer => Components[ComponentIndex(LootContainerBlueprintMembers.ItemContainerComponent)];

    /// <summary>Class of the search interaction.</summary>
    public string InteractionClass => Class.Components[ComponentIndex(LootContainerBlueprintMembers.InteractionComponent)].Class;

    /// <summary>Class of the item container.</summary>
    public string ItemContainerClass => Class.Components[ComponentIndex(LootContainerBlueprintMembers.ItemContainerComponent)].Class;

    /// <summary>Where the container's prompt is: an open container measures the player's distance from it.</summary>
    public Vector3D Prompt
    {
        get
        {
            Vector3D offset = Rotation.Rotate(Class.PromptLocation);
            return new Vector3D(Location.X + offset.X, Location.Y + offset.Y, Location.Z + offset.Z);
        }
    }

    /// <summary>
    /// Distance from the prompt beyond which an open container closes: the prompt distance while the character is
    /// held at the container, plus the server's leeway.
    /// </summary>
    public double CloseDistance => Class.OpenedPromptDistance + LootInteractions.ServerPromptLeeway;

    private int ComponentIndex(string name)
    {
        for (int index = 0; index < Class.Components.Count; index++)
        {
            if (Class.Components[index].Name == name)
            {
                return index;
            }
        }

        throw new InvalidOperationException($"The container class has no {name}");
    }
}

/// <summary>What a player's client shows of a loot container.</summary>
/// <param name="State">State of the search interaction.</param>
/// <param name="HoldStartedAt">Server time the search hold starts, while a search of this player runs.</param>
/// <param name="OpenedAt">Server time the container was first opened, once it has been.</param>
/// <param name="ItemContainer">Id of the container among the player's containers, while this player has it open.</param>
public readonly record struct LootView(CurrentInteractionState State, double? HoldStartedAt, double? OpenedAt, int? ItemContainer);
