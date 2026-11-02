namespace Ares.Structs.Game.World;

/// <summary>Identity of the Dam map (Dam Battlegrounds) and its persistent-level actors.</summary>
public static class DamMap
{
    /// <summary>Matchmaking scenario name.</summary>
    public const string Scenario = "TheDam";

    /// <summary>Persistent map package.</summary>
    public const string MapPackage = "/Game/Pioneer/Maps/TheDam_02/TheDam_02_P";

    /// <summary>World object name in the map package.</summary>
    public const string WorldName = "TheDam_02_P";

    /// <summary>Persistent level object name in the world.</summary>
    public const string PersistentLevelName = "PersistentLevel";

    /// <summary>Level script actor name in the persistent level.</summary>
    public const string LevelScriptActorName = "TheDam_01_P_C_1";

    /// <summary>Level script actor class.</summary>
    public const string LevelScriptClass = "/Game/Pioneer/Maps/TheDam_02/TheDam_02_P.TheDam_02_P_C";

    /// <summary>World data layers actor name in the persistent level.</summary>
    public const string WorldDataLayersActorName = "WorldDataLayers";

    /// <summary>World data layers actor class.</summary>
    public const string WorldDataLayersClass = "/Script/Engine.WorldDataLayers";

    /// <summary>Game mode class sent in Welcome.</summary>
    public const string GameModeClass = "/Game/Pioneer/Core/GameModes/BP_ArcRaidersGameModeSalvage.BP_ArcRaidersGameModeSalvage_C";

    /// <summary>Game state class the Salvage game mode creates, and its class default object.</summary>
    public static ArchetypeClass GameState { get; } = new("/Script/Angelscript.PioneerGameStateSalvage", "/Script/Angelscript.Default__PioneerGameStateSalvage");

    /// <summary>Identifier tag of the Salvage game mode; player starts carry it when Salvage may use them.</summary>
    public const string GameModeTag = "GameMode.Salvage";

    /// <summary>Location tag of the map.</summary>
    public const string LocationTag = "GameMode.Location.TheDam";

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
