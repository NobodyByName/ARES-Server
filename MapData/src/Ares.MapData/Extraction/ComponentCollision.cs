using Ares.MapData.Bodies;
using Ares.MapData.Components;
using Ares.MapData.Profiles;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Extraction;

/// <summary>What a component's collision blocks, from the settings it and its templates store.</summary>
internal static class ComponentCollision
{
    /// <summary>
    /// What a placed component blocks. A static mesh component that uses its mesh's default collision (its own or its
    /// class default's <c>bUseDefaultCollision</c>) takes the mesh body setup's default instance; every other component
    /// takes its stored settings over its class default's profile. <see langword="null"/> when the component stores no
    /// profile and its class default is not known.
    /// </summary>
    /// <param name="profiles">The game's collision profiles.</param>
    /// <param name="settings">The component's collision settings.</param>
    /// <param name="mesh">Static mesh components: their mesh's body; <see langword="null"/> otherwise.</param>
    public static CollisionClassification? Classify(CollisionProfiles profiles, CollisionSettings settings, AssetBody? mesh)
    {
        bool useDefault = settings.UseDefaultCollision ?? settings.ClassDefault?.UseDefaultCollision ?? false;
        if (useDefault && mesh is not null)
        {
            return Classify(profiles, mesh.DefaultInstance, null);
        }

        if (settings.ProfileName is null && settings.ClassDefault is null)
        {
            return null;
        }

        BodyInstanceValues stored = new(settings.ObjectType, settings.CollisionEnabled, settings.ProfileName, settings.Responses);
        return Classify(profiles, stored, settings.ClassDefault?.ProfileName);
    }

    /// <summary>
    /// What a body instance blocks. A known profile (the stored one, else <paramref name="fallbackProfile"/>) decides
    /// everything. Otherwise each stored member applies, and a member not stored comes from the fallback profile, else
    /// from the body instance defaults; stored response overrides apply over the channel defaults.
    /// </summary>
    /// <param name="profiles">The game's collision profiles.</param>
    /// <param name="stored">The members the body instance stores, or <see langword="null"/> when it stores none.</param>
    /// <param name="fallbackProfile">The profile of the class default the body instance's component is created from.</param>
    public static CollisionClassification Classify(CollisionProfiles profiles, BodyInstanceValues? stored, string? fallbackProfile)
    {
        if (profiles.Find(stored?.ProfileName ?? fallbackProfile) is { } profile)
        {
            return profiles.Classify(profile.CollisionEnabled, profile.ObjectType, profile.Responses);
        }

        CollisionProfile? fallback = profiles.Find(fallbackProfile);
        CollisionEnabled enabled = stored?.CollisionEnabled ?? fallback?.CollisionEnabled ?? BodyInstanceMembers.DefaultCollisionEnabled;
        CollisionChannel objectType = stored?.ObjectType ?? fallback?.ObjectType ?? BodyInstanceMembers.DefaultObjectType;
        if (stored?.Responses is { } overrides)
        {
            return profiles.Classify(null, enabled, objectType, overrides);
        }

        return fallback is not null
            ? profiles.Classify(enabled, objectType, fallback.Responses)
            : profiles.Classify(null, enabled, objectType, []);
    }
}
