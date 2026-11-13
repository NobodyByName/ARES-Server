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

/// <summary>Raid game phase (<c>EGamePhase</c>).</summary>
public enum GamePhase : byte
{
    /// <summary>The raid's first phase, when players arrive.</summary>
    PreGame = 0,

    EarlyGame = 1,
    MidGame = 2,
    LateGame = 3,

    /// <summary>The raid has not been initialized yet.</summary>
    NotInitialized = 4,
}

/// <summary>Match states of the engine's game state (<c>MatchState</c> names).</summary>
public static class MatchStates
{
    /// <summary>The match is being played.</summary>
    public const string InProgress = "InProgress";
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

/// <summary>How a camera blends to a new view target (<c>EViewTargetBlendFunction</c>).</summary>
public enum ViewTargetBlendFunction : byte
{
    Linear = 0,
    Cubic = 1,
    EaseIn = 2,
    EaseOut = 3,
    PreBlended = 4,
}

/// <summary>Replicated enums whose wire width is not given by the replication schema alone.</summary>
public static class KnownEnums
{
    /// <summary>Network roles; the engine's maximum is <c>ROLE_MAX</c> (4).</summary>
    public static EnumDefinition NetRole { get; } =
        new(EnumPaths.ENetRole, 4, ["ROLE_None", "ROLE_SimulatedProxy", "ROLE_AutonomousProxy", "ROLE_Authority"]);

    /// <summary>
    /// Game phases. The script enum's largest value is NOT_INITIALIZED (4); it and an appended
    /// native maximum (5) both need three bits.
    /// </summary>
    public static EnumDefinition GamePhase { get; } =
        new(EnumPaths.EGamePhase, 4, ["PreGame", "EarlyGame", "MidGame", "LateGame", "NOT_INITIALIZED"]);

    /// <summary>View target blend functions; the engine's maximum is <c>VTBlend_MAX</c> (5).</summary>
    public static EnumDefinition ViewTargetBlendFunction { get; } =
        new(EnumPaths.EViewTargetBlendFunction, 5, ["VTBlend_Linear", "VTBlend_Cubic", "VTBlend_EaseIn", "VTBlend_EaseOut", "VTBlend_PreBlended"]);

    /// <summary>
    /// Item sources (<c>EItemSources</c>): five bits on the wire. The value names and the enum's own maximum are not
    /// known; the maximum here is the largest value five bits hold.
    /// </summary>
    public static EnumDefinition ItemSources { get; } = new(EnumPaths.EItemSources, 31, []);
}
