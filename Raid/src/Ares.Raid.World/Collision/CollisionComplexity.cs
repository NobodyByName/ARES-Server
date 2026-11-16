namespace Ares.Raid.World.Collision;

/// <summary>Which collision of the bodies a query hits.</summary>
public enum CollisionComplexity
{
    /// <summary>Each body's simple collision: its boxes, spheres, capsules, convex hulls and simple triangle meshes.</summary>
    Simple,

    /// <summary>
    /// What each body offers complex queries: its complex triangle meshes, or its simple collision when it uses simple
    /// collision as complex or complex as simple.
    /// </summary>
    Complex,
}
