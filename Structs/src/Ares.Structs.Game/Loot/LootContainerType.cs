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
/// <param name="MaxPerGroup">Limit on the containers of this type per group, or null when the asset does not set one.</param>
public sealed record LootContainerType(
    string Tag,
    IReadOnlyList<ArchetypeClass> Classes,
    IReadOnlyList<LootContainerVariant> Variants,
    int? MaxPerGroup);
