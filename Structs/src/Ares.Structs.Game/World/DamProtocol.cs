namespace Ares.Structs.Game.World;

/// <summary>
/// Protocol facts of a Dam raid that the installed game's files do not hold: the game mode identifier tag the server
/// sends and matches player starts with, and the global actors it replicates.
/// </summary>
public static class DamProtocol
{
    /// <summary>Identifier tag of the Salvage game mode; player starts carry it when Salvage may use them.</summary>
    public const string GameModeTag = "GameMode.Salvage";

    /// <summary>Global actors the server replicates, in channel order.</summary>
    public static IReadOnlyList<GlobalActor> GlobalActors { get; } =
    [
        new("PioneerCharacterMovementGlobalActor", "/Script/Angelscript.PioneerCharacterMovementGlobalActor"),
        new("PioPendulumQueryGlobalActor", "/Script/Angelscript.PioPendulumQueryGlobalActor"),
        new("EmbarkFastReplicatorTransform", "/Script/EmbarkFastReplicator.EmbarkFastReplicatorTransform"),
        new("EmbarkFastReplicatorTransformInterpolator", "/Script/EmbarkGameplay.EmbarkFastReplicatorTransformInterpolator"),
        new("EmbarkActorBlackBoardFastReplicatorTransformInterpolator", "/Script/EmbarkGameplay.EmbarkActorBlackBoardFastReplicatorTransformInterpolator"),
        new("TetherSystem", "/Script/Angelscript.TetherSystem"),
    ];
}
