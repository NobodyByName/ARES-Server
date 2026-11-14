namespace Ares.Structs.Game.Collision;

/// <summary>
/// The surface type of a physical material (<c>EPhysicalSurface</c>), which a character's movement state reports for
/// the ground under it. <see cref="Default"/> is the engine's own type, also that of the engine's default physical
/// material; the game names <c>SurfaceType1</c> to <c>SurfaceType38</c> in its physics settings.
/// </summary>
public enum PhysicalSurfaceType : byte
{
    Default = 0,
    Stone = 1,
    Sand = 2,
    Mud = 3,
    Grass = 4,
    Gravel = 5,
    Metal = 6,
    Glass = 7,
    Plastic = 8,
    Water = 9,
    Snow = 10,
    Wood = 11,
    GravelRough = 12,
    Core = 13,
    MetalArmored = 14,
    MetalLight = 15,
    PowerCell = 16,
    GrassTall = 17,
    GravelFine = 18,
    GravelVolcanic = 19,
    GravelVolcanicFine = 20,
    Dirt = 21,
    ForestPine = 22,
    SaltFlat = 23,
    Shrubbery = 24,
    Concrete = 25,
    StoneVolcanic = 26,
    ChainLinkFence = 27,
    PlayerBody = 28,
    PlayerHead = 29,
    TreeLog = 30,
    Fabric = 31,
    RoofTile = 32,
    WoodenFloor = 33,
    Tile = 34,
    Plaster = 35,
    ForestLeaves = 36,
    GlassIndestructibleTranslucent = 37,
    Ice = 38,
}
