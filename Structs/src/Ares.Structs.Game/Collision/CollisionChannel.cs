namespace Ares.Structs.Game.Collision;

/// <summary>
/// A collision channel: the object type of a body or the channel of a trace. The first eight are the engine's object
/// types; the game names its own channels in the collision profile config (<see cref="CollisionProfileConfig"/>).
/// </summary>
public enum CollisionChannel : byte
{
    WorldStatic = 0,
    WorldDynamic = 1,
    Pawn = 2,
    Visibility = 3,
    Camera = 4,
    PhysicsBody = 5,
    Vehicle = 6,
    Destructible = 7,
    EngineTraceChannel1 = 8,
    EngineTraceChannel2 = 9,
    EngineTraceChannel3 = 10,
    EngineTraceChannel4 = 11,
    EngineTraceChannel5 = 12,
    EngineTraceChannel6 = 13,
    GameTraceChannel1 = 14,
    GameTraceChannel2 = 15,
    GameTraceChannel3 = 16,
    GameTraceChannel4 = 17,
    GameTraceChannel5 = 18,
    GameTraceChannel6 = 19,
    GameTraceChannel7 = 20,
    GameTraceChannel8 = 21,
    GameTraceChannel9 = 22,
    GameTraceChannel10 = 23,
    GameTraceChannel11 = 24,
    GameTraceChannel12 = 25,
    GameTraceChannel13 = 26,
    GameTraceChannel14 = 27,
    GameTraceChannel15 = 28,
    GameTraceChannel16 = 29,
    GameTraceChannel17 = 30,
    GameTraceChannel18 = 31,
}

/// <summary>Which kinds of collision a body takes part in: queries (traces, sweeps, overlaps), physics or probes.</summary>
public enum CollisionEnabled : byte
{
    NoCollision = 0,
    QueryOnly = 1,
    PhysicsOnly = 2,
    QueryAndPhysics = 3,
    ProbeOnly = 4,
    QueryAndProbe = 5,
}

/// <summary>How a body responds to a channel.</summary>
public enum CollisionResponse : byte
{
    Ignore = 0,
    Overlap = 1,
    Block = 2,
}
