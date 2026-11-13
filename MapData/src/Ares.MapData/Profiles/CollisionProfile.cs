using Ares.Structs.Game.Collision;

namespace Ares.MapData.Profiles;

/// <summary>A named collision setting from the game's config.</summary>
/// <param name="Name">The profile's name.</param>
/// <param name="CollisionEnabled">Which kinds of collision a body with this profile takes part in.</param>
/// <param name="ObjectType">The channel such a body is traced and swept as.</param>
/// <param name="Responses">The body's response to each channel, indexed by <see cref="CollisionChannel"/>.</param>
internal sealed record CollisionProfile(string Name, CollisionEnabled CollisionEnabled, CollisionChannel ObjectType, IReadOnlyList<CollisionResponse> Responses);

/// <summary>A component's own response to one channel, applied over the channel defaults.</summary>
/// <param name="Channel">The channel's name.</param>
/// <param name="Response">The response; a stored entry without one means <see cref="CollisionResponse.Block"/>.</param>
internal readonly record struct CollisionResponseOverride(string Channel, CollisionResponse Response = CollisionResponse.Block);

/// <summary>What a body's collision stops.</summary>
/// <param name="BlocksPawn">Whether it blocks the player character's movement capsule.</param>
/// <param name="BlocksClimbable">Whether it blocks the traces of vaulting, mantling and wall checks.</param>
internal readonly record struct CollisionClassification(bool BlocksPawn, bool BlocksClimbable);
