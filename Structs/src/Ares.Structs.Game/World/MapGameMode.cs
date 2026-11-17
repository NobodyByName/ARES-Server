using Ares.Structs.Core.Assets;

namespace Ares.Structs.Game.World;

/// <summary>The game mode a map runs and the map's identity in it.</summary>
/// <param name="GameModeClass">Game mode class the map's world settings name (sent in Welcome).</param>
/// <param name="GameState">Game state class the game mode creates, and its class default object.</param>
/// <param name="LocationTag">Location tag of the map (its level script's <c>LocationTag</c>).</param>
public sealed record MapGameMode(string GameModeClass, ArchetypeClass GameState, string LocationTag);

/// <summary>
/// The native members a map's world settings actor (<c>AEmbarkWorldSettings</c>, which declares no script members)
/// stores before its game mode, by chain position: fitted to the Dam world settings, whose values decode consistently
/// with these kinds (the object references name its own subobjects, the data layer array names data layer assets and
/// <see cref="DefaultGameMode"/> names a game mode class). Members after the game mode are not read.
/// </summary>
public static class WorldSettingsMembers
{
    /// <summary>Name of the native world settings base class (the script cache's native super of the world settings class).</summary>
    public const string NativeBase = "AEmbarkWorldSettingsBase";

    /// <summary>Unknown member 11: int32.</summary>
    public const int Unknown11 = 11;

    /// <summary>Unknown member 20: object reference (a world settings extension subobject).</summary>
    public const int Unknown20 = 20;

    /// <summary>Unknown member 25: bool.</summary>
    public const int Unknown25 = 25;

    /// <summary>Unknown member 29: bool.</summary>
    public const int Unknown29 = 29;

    /// <summary>Unknown member 35: bool.</summary>
    public const int Unknown35 = 35;

    /// <summary>Unknown member 37: object reference (a navigation configuration subobject).</summary>
    public const int Unknown37 = 37;

    /// <summary>Unknown member 38: object reference (the world partition subobject).</summary>
    public const int Unknown38 = 38;

    /// <summary>Unknown member 40: array of object references (data layer assets).</summary>
    public const int Unknown40 = 40;

    /// <summary>Unknown member 46: float (the kill height).</summary>
    public const int Unknown46 = 46;

    /// <summary><c>DefaultGameMode</c> (the game mode override): a class reference.</summary>
    public const int DefaultGameMode = 68;

    /// <summary>The chain up to <see cref="DefaultGameMode"/>, with the members above typed and the others unknown.</summary>
    public static StructSchema Schema { get; } = new(NativeBase, Members());

    private static PropertyField?[] Members()
    {
        PropertyField?[] members = new PropertyField?[DefaultGameMode + 1];
        members[Unknown11] = new PropertyField(nameof(Unknown11), PropertyType.Int32);
        members[Unknown20] = new PropertyField(nameof(Unknown20), PropertyType.Object);
        members[Unknown25] = new PropertyField(nameof(Unknown25), PropertyType.Bool);
        members[Unknown29] = new PropertyField(nameof(Unknown29), PropertyType.Bool);
        members[Unknown35] = new PropertyField(nameof(Unknown35), PropertyType.Bool);
        members[Unknown37] = new PropertyField(nameof(Unknown37), PropertyType.Object);
        members[Unknown38] = new PropertyField(nameof(Unknown38), PropertyType.Object);
        members[Unknown40] = new PropertyField(nameof(Unknown40), PropertyType.ArrayOf(PropertyType.Object));
        members[Unknown46] = new PropertyField(nameof(Unknown46), PropertyType.Float);
        members[DefaultGameMode] = new PropertyField(nameof(DefaultGameMode), PropertyType.Object);
        return members;
    }
}

/// <summary>Members of the game mode classes the map tables read.</summary>
public static class GameModeMembers
{
    /// <summary>
    /// Byte offset of <c>AGameModeBase.GameStateClass</c> (a native class reference) in a game mode object: the script
    /// default statements assign the game state class there (<c>opAssign</c> of a script type's static class).
    /// </summary>
    public const int GameStateClassOffset = 0x3D8;

    /// <summary>Prefix of the script globals that hold a script class's static class (followed by the class name with its prefix).</summary>
    public const string StaticTypePrefix = "__StaticType_";
}

/// <summary>Members of a map's level script class (<c>APioneerLevelScript</c>) and engine structs its Blueprint holds.</summary>
public static class LevelScriptMembers
{
    /// <summary><c>LevelLightingScenariosDataAsset</c>: the map's lighting lookup asset.</summary>
    public const string LevelLightingScenariosDataAsset = "LevelLightingScenariosDataAsset";

    /// <summary><c>LocationTag</c>: the map's location tag (a gameplay tag).</summary>
    public const string LocationTag = "LocationTag";

    /// <summary><c>RoundConfigurationLightingScenarios</c>: the asset of the round lighting groups.</summary>
    public const string RoundConfigurationLightingScenarios = "RoundConfigurationLightingScenarios";

    /// <summary>Script path of <c>FTimerHandle</c>.</summary>
    public const string TimerHandlePath = "/Script/Engine.TimerHandle";

    /// <summary><c>FTimerHandle</c>: a property block holding <c>Handle</c>.</summary>
    public static StructSchema TimerHandle { get; } = new("TimerHandle", [new PropertyField("Handle", PropertyType.UInt64)]);
}
