using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.World;

namespace Ares.Structs.Game.Loot;

/// <summary>A component of a loot container the client knows by name: replicated, or the attach parent of one.</summary>
/// <param name="Name">Component name (a stable subobject of the container).</param>
/// <param name="Class">Component class.</param>
/// <param name="AttachParent">Name of the component it is attached to, or null for the root.</param>
public sealed record LootContainerComponent(string Name, string Class, string? AttachParent);

/// <summary>How one loot container class is searched.</summary>
/// <param name="Container">Container class, with its class default.</param>
/// <param name="ActorClass">Replication layout of the container actor.</param>
/// <param name="Components">Components the client knows by name, in the order the container sends them.</param>
/// <param name="PromptLocation">The search prompt (the interaction component) relative to the container.</param>
/// <param name="SearchLocation">Where the character's search starts, in the space of the container, as the interactor reports it.</param>
/// <param name="SearchAnimation">Animation set the character plays while searching, or null.</param>
/// <param name="AlignmentSeconds">Time the character takes to line up with the container before the hold starts.</param>
/// <param name="HoldSeconds">Hold time of a first search.</param>
/// <param name="OpenDelaySeconds">
/// Wait after a first search's hold before the contents show (the lid opening); the hidden items start revealing then.
/// </param>
/// <param name="EquipsRaiderTool">Whether a first search draws the raider tool (breach and search).</param>
/// <param name="PromptDistance">Prompt distance while the character searches.</param>
/// <param name="OpenedPromptDistance">Prompt distance once the container has been opened (no hold).</param>
/// <param name="SlotCount">Slots of the container's item container.</param>
public sealed record LootContainerClass(
    ArchetypeClass Container,
    string ActorClass,
    IReadOnlyList<LootContainerComponent> Components,
    Vector3D PromptLocation,
    Vector3D SearchLocation,
    string? SearchAnimation,
    double AlignmentSeconds,
    double HoldSeconds,
    double OpenDelaySeconds,
    bool EquipsRaiderTool,
    double PromptDistance,
    double OpenedPromptDistance,
    int SlotCount);
