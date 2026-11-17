using Ares.Structs.Core.Assets;

namespace Ares.Structs.Game.World;

/// <summary>
/// The cooked world item class list (<see cref="WorldItemList"/>): a <c>UPreloadableEmbarkActorListAsset</c> that
/// stores one member, its array of soft class paths.
/// </summary>
public static class WorldItemListAsset
{
    /// <summary>Package of the list.</summary>
    public const string Package = "/CookedOnly/WorldItemList";

    /// <summary>Name given here to the array member (chain index 0); the game's member name is not needed to read it.</summary>
    public const string ClassesMember = "Classes";

    /// <summary>The chain of the list's class as far as its cooked export uses it.</summary>
    public static StructSchema Schema { get; } = new("PreloadableEmbarkActorListAsset",
    [
        new PropertyField(ClassesMember, PropertyType.ArrayOf(PropertyType.StructOf(EngineStructs.SoftObjectPath))),
    ]);
}
