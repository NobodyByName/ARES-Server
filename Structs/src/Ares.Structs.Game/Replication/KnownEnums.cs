using Ares.Structs.Net.Replication;

namespace Ares.Structs.Game.Replication;

/// <summary>Network role of an actor (<c>ENetRole</c>).</summary>
public enum NetRole : byte
{
    None = 0,
    SimulatedProxy = 1,
    AutonomousProxy = 2,
    Authority = 3,
}

/// <summary>Raid game phase (<c>EGamePhase</c>). Only the phase used before a raid starts is decoded.</summary>
public enum GamePhase : byte
{
    Unknown0 = 0,
    Unknown1 = 1,
    Unknown2 = 2,
    Unknown3 = 3,

    /// <summary>The raid has not been initialized yet.</summary>
    NotInitialized = 4,
}

/// <summary>Character movement mode (<c>EMovementMode</c>).</summary>
public enum MovementMode : byte
{
    None = 0,
    Walking = 1,
    NavWalking = 2,
    Falling = 3,
    Swimming = 4,
    Flying = 5,
    Custom = 6,
}

/// <summary>Replicated enums whose wire width is not given by the replication schema alone.</summary>
public static class KnownEnums
{
    /// <summary>Network roles; the engine's maximum is <c>ROLE_MAX</c> (4).</summary>
    public static EnumDefinition NetRole { get; } =
        new(EnumPaths.ENetRole, 4, ["ROLE_None", "ROLE_SimulatedProxy", "ROLE_AutonomousProxy", "ROLE_Authority"]);

    /// <summary>
    /// Game phases. The script enum's largest value is NOT_INITIALIZED (4); it and an appended
    /// native maximum (5) both need three bits. Names of the other values are not decoded.
    /// </summary>
    public static EnumDefinition GamePhase { get; } =
        new(EnumPaths.EGamePhase, 4, ["Unknown0", "Unknown1", "Unknown2", "Unknown3", "NOT_INITIALIZED"]);
}
