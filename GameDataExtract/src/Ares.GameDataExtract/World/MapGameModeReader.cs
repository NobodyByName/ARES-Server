using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads a map's game mode (<see cref="MapGameMode"/>) and its level script's lighting assets: the game mode class its
/// world settings name, the game state class that game mode's script default statements assign, and the location tag
/// and lighting asset references of its level script class default.
/// </summary>
internal static class MapGameModeReader
{
    private const string ClassDefaultPrefix = "Default__";
    private const string ActorPrefix = "A";

    /// <summary>The game mode of <paramref name="mapPackage"/> and its level script class default's lighting assets.</summary>
    /// <exception cref="InvalidDataException">A value is missing or does not decode.</exception>
    public static (MapGameMode GameMode, ExportRef Lighting, ExportRef RoundLighting) Read(MapRuleSources sources, string mapPackage, MapWorldObjects world)
    {
        CookedPackage map = sources.Load(mapPackage);
        string gameMode = GameModeClass(sources, map, world);
        PropertyBag level = sources.Members(
            sources.Export(mapPackage, "." + ClassDefaultPrefix + world.LevelScriptClass[(world.LevelScriptClass.LastIndexOf('.') + 1)..]),
            LevelScriptMembers.LevelLightingScenariosDataAsset, LevelScriptMembers.LocationTag, LevelScriptMembers.RoundConfigurationLightingScenarios);
        ExportRef lighting = Referenced(sources, map, level, LevelScriptMembers.LevelLightingScenariosDataAsset);
        ExportRef roundLighting = Referenced(sources, map, level, LevelScriptMembers.RoundConfigurationLightingScenarios);
        string location = MapRuleSources.Tag(level.Get<PropertyBag>(LevelScriptMembers.LocationTag));
        return (new MapGameMode(gameMode, GameState(sources, gameMode), location), lighting, roundLighting);
    }

    /// <summary>The game mode class the persistent level's world settings actor names.</summary>
    private static string GameModeClass(MapRuleSources sources, CookedPackage map, MapWorldObjects world)
    {
        ZenExport level = map.Exports.Single(export => map.Header.ExportPath(export.Index) == $"{map.Name}.{world.WorldName}:{world.PersistentLevelName}");
        ZenExport[] settings = [.. map.ChildrenOf(level.Index).Where(actor => IsWorldSettings(sources, actor.Class)).Take(2)];
        if (settings.Length != 1)
        {
            throw new InvalidDataException($"{map.Name}: the persistent level has {settings.Length} world settings actors");
        }

        PropertyBag bag = sources.Own(new ExportRef(map, settings[0]), [nameof(WorldSettingsMembers.DefaultGameMode)]);
        ObjectReference reference = bag.Get<ObjectReference>(nameof(WorldSettingsMembers.DefaultGameMode));
        return sources.Packages.Reference(map, reference.Value) is { } mode && reference.Value < 0
            ? mode.Path
            : throw new InvalidDataException($"{map.Name}: the world settings name no imported game mode class");
    }

    private static bool IsWorldSettings(MapRuleSources sources, PackageObjectIndex index)
    {
        if (index.Kind != PackageObjectKind.ScriptImport || sources.Packages.ScriptPath(index) is not { } path
            || !path.StartsWith(ScriptSchemas.ScriptPackage, StringComparison.Ordinal)
            || !sources.Scripts.TryGetClass(ActorPrefix + path[ScriptSchemas.ScriptPackage.Length..], out ScriptClass? type))
        {
            return false;
        }

        _ = sources.Scripts.Chain(type.Name, out string? native);
        return native == WorldSettingsMembers.NativeBase;
    }

    /// <summary>
    /// The game state class the script default statements of <paramref name="gameModeClass"/>'s script class chain assign
    /// last to <see cref="GameModeMembers.GameStateClassOffset"/>, with its class default object.
    /// </summary>
    private static ArchetypeClass GameState(MapRuleSources sources, string gameModeClass)
    {
        int split = gameModeClass.LastIndexOf('.');
        ExportRef type = sources.Export(gameModeClass[..split], gameModeClass[split..]);
        (_, string script) = sources.ScriptClassOf(type.Package, PackageObjectIndex.FromExport(type.Export.Index), gameModeClass);
        string? global = ScriptPatterns.DefaultFunctions(sources.Scripts, script)
            .SelectMany(function => ScriptPatterns.GlobalsAssignedTo(sources.Scripts, function, GameModeMembers.GameStateClassOffset))
            .LastOrDefault();
        if (global is null || !global.StartsWith(GameModeMembers.StaticTypePrefix, StringComparison.Ordinal)
            || global[GameModeMembers.StaticTypePrefix.Length..] is not ['A', _, ..] stateName || !sources.Scripts.TryGetClass(stateName, out _))
        {
            throw new InvalidDataException($"The script defaults of {script} assign no script game state class");
        }

        string name = stateName[1..];
        return new ArchetypeClass(ScriptSchemas.ScriptPackage + name, ScriptSchemas.ScriptPackage + ClassDefaultPrefix + name);
    }

    private static ExportRef Referenced(MapRuleSources sources, CookedPackage map, PropertyBag bag, string member)
    {
        return sources.Packages.Reference(map, bag.Get<ObjectReference>(member).Value)
            ?? throw new InvalidDataException($"{map.Name}: the level script's {member} is none");
    }
}
