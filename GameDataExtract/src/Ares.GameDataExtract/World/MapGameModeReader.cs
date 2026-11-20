using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Game;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads a map's game mode (<see cref="MapGameMode"/>) and its level script's lighting assets: the game mode class its
/// world settings name, the game state class that game mode creates (<see cref="ScriptWiring"/>), and the location tag
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
            || sources.Layouts.NameOf(path, ActorPrefix) is not { } name)
        {
            return false;
        }

        _ = sources.Layouts.Chain(name, out string? native);
        return native == WorldSettingsMembers.NativeBase;
    }

    /// <summary>
    /// The game state class <paramref name="gameModeClass"/> creates, with its class default object: the first script
    /// class of the game mode's chain must be <see cref="ScriptWiring.SalvageGameMode"/>, which creates
    /// <see cref="ScriptWiring.SalvageGameState"/>.
    /// </summary>
    private static ArchetypeClass GameState(MapRuleSources sources, string gameModeClass)
    {
        int split = gameModeClass.LastIndexOf('.');
        ExportRef type = sources.Export(gameModeClass[..split], gameModeClass[split..]);
        (_, string script) = sources.ScriptClassOf(type.Package, PackageObjectIndex.FromExport(type.Export.Index), gameModeClass);
        if (script != ScriptWiring.SalvageGameMode || !sources.Layouts.Contains(ScriptWiring.SalvageGameState))
        {
            throw new InvalidDataException($"No game state class is known for the game mode {gameModeClass} (script class {script})");
        }

        string name = ScriptWiring.SalvageGameState[1..];
        return new ArchetypeClass(ScriptLayouts.ScriptPackage + name, ScriptLayouts.ScriptPackage + ClassDefaultPrefix + name);
    }

    private static ExportRef Referenced(MapRuleSources sources, CookedPackage map, PropertyBag bag, string member)
    {
        return sources.Packages.Reference(map, bag.Get<ObjectReference>(member).Value)
            ?? throw new InvalidDataException($"{map.Name}: the level script's {member} is none");
    }
}
