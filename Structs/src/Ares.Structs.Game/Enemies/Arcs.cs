using Ares.Structs.Game.Characters;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;

namespace Ares.Structs.Game.Enemies;

/// <summary>An ARC enemy as a spawnable actor.</summary>
/// <param name="Name">Short lower-case name.</param>
/// <param name="Class">The Blueprint class and its class default, the archetype of the spawned actor.</param>
public sealed record ArcClass(string Name, ArchetypeClass Class);

/// <summary>
/// ARC enemies: Constructable pawns (<c>AConstructableBase</c>). A spawned ARC is the actor, then four attribute sets
/// created at runtime inside it (<see cref="AttributeSets"/>, in that order) and its ability system component, which
/// lists them. The client builds the ARC's parts and look from its class.
/// </summary>
public static class Arcs
{
    /// <summary>The Wasp, a light flying drone.</summary>
    public static ArcClass Wasp { get; } = new("wasp", new ArchetypeClass(
        "/Game/Pioneer/Enemies/LightDrone/Wasp/C_LightDrone_02.C_LightDrone_02_C",
        "/Game/Pioneer/Enemies/LightDrone/Wasp/C_LightDrone_02.Default__C_LightDrone_02_C"));

    /// <summary>Every ARC the server can spawn.</summary>
    public static IReadOnlyList<ArcClass> All { get; } = [Wasp];

    /// <summary>
    /// Class whose replication layout the actor's properties use. An ARC is a pawn, and a layout numbers its parent's
    /// properties first, so the actor and pawn properties keep their handles in every ARC class.
    /// </summary>
    public const string ActorClass = ClassPaths.Pawn;

    /// <summary>Damage model attribute set of the ARC's first part set (a native class without replicated properties).</summary>
    public const string PartSetAttributeSet = "/Script/EmbarkConstructable.ConstructableDamageModelAttributeSet_PartSet0";

    /// <summary>The attribute sets an ARC's ability system spawns, in the order they are created.</summary>
    public static IReadOnlyList<string> AttributeSets { get; } =
    [
        PartSetAttributeSet,
        ClassPaths.PioneerConstructableAttributeSet,
        ClassPaths.PioneerAttackerAttributeSet,
        ClassPaths.PioneerCharacterAttributeSet,
    ];

    /// <summary>Name of the ability system component.</summary>
    public const string AbilitySystemComponent = "AbilitySystem";

    /// <summary>Class whose replication layout the ability system component uses.</summary>
    public const string AbilitySystemClass = ClassPaths.EmbarkAbilitySystemComponent;

    /// <summary>Export checksum of the ability system component; the character's component has the same.</summary>
    public static uint AbilitySystemExportChecksum => CharacterDefaults.AbilitySystem.ExportChecksum;

    /// <summary>Distance within which an ARC is relevant to a player, in centimetres (the class default's net cull distance).</summary>
    public const double NetCullDistance = 30000;

    /// <summary>The ARC a name picks: its <see cref="ArcClass.Name"/>, ignoring case.</summary>
    /// <param name="name">The name.</param>
    /// <returns>The ARC, or null.</returns>
    public static ArcClass? Find(string name) => All.FirstOrDefault(arc => arc.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
