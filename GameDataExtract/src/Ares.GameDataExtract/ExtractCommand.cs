using System.Diagnostics;
using System.Globalization;
using Ares.Common;
using Ares.Common.Archives;
using Ares.Common.CommandLine;
using Ares.Common.GameData;
using Ares.Common.Maps;
using Ares.GameDataExtract.Archives;
using Ares.GameDataExtract.Extraction;
using Ares.GameDataExtract.Items;
using Ares.GameDataExtract.Loot;
using Ares.GameDataExtract.Movement;
using Ares.GameDataExtract.Output;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Profiles;
using Ares.GameDataExtract.Registry;
using Ares.GameDataExtract.Scripts;
using Ares.GameDataExtract.World;
using Ares.Structs.Core;
using Ares.Structs.Game;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.Movement;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract;

/// <summary>
/// <c>ARES-GameDataExtract extract</c>: reads the installed game read-only and writes the game data into the output
/// folder (created when missing, never inside the game folder): the Dam map's collision file, world, loot area and gameplay
/// tables (see <see cref="MapTables"/>) under <c>maps/&lt;map&gt;/</c>, the global item, movement and loot definition tables (see
/// <see cref="GameTables"/>), then <c>manifest.json</c> (layout in <see cref="GameDataFiles"/>). Each file replaces an
/// earlier one only once complete. Prints what was written and which components could not be decoded.
/// </summary>
internal sealed class ExtractCommand
{
    /// <summary>The command name.</summary>
    public const string Name = "extract";

    /// <summary>Exit code of a successful run.</summary>
    public const int Success = 0;

    /// <summary>Exit code when the game's files cannot be read with the given keys.</summary>
    public const int ReadError = 1;

    /// <summary>Exit code of invalid usage, a missing game folder or an unusable or wrong keys file.</summary>
    public const int UsageError = 2;

    /// <summary>Command-line help.</summary>
    public static readonly string Usage = $"""
        Usage: ARES-GameDataExtract extract --game <dir> --keys <file> --toc-key <file> --out <dir>

        Reads an installed game (never changing its files) and writes the game data ARES loads:
        manifest.json, the global tables and the map files under maps/<map>/.
        Run it again after every game update.

          --game <dir>       Game folder, the one holding {GameBuild.ProjectName}/
          --keys <file>      Archive keys file (the same file as ARES-Host's archive-keys-file)
          --toc-key <file>   TOC key file: the key of the game's .utoc files as 64 hex digits
                             (optional 0x prefix; whitespace and # comments are ignored)
          --out <dir>        Folder for the game data (created when missing; not inside the game folder)
          --help             Show this help
          --version          Show the ARES and game build versions

        Exit codes: {Success} success, {ReadError} the game's files could not be read, {UsageError} invalid usage or inputs.
        """;

    private static readonly HashSet<string> s_flags = ["help"];

    private ExtractCommand(string gameDirectory, string keysFile, string tocKeyFile, string outputDirectory)
    {
        GameDirectory = gameDirectory;
        KeysFile = keysFile;
        TocKeyFile = tocKeyFile;
        OutputDirectory = outputDirectory;
    }

    private string GameDirectory { get; }

    private string KeysFile { get; }

    private string TocKeyFile { get; }

    private string OutputDirectory { get; }

    /// <summary>Parses the options after the command name.</summary>
    /// <exception cref="HelpRequestedException">--help was given.</exception>
    /// <exception cref="CommandLineException">An option is missing, unknown or repeated.</exception>
    public static ExtractCommand Parse(IReadOnlyList<string> args)
    {
        CommandLineArguments arguments = CommandLineArguments.Parse(args, s_flags);
        if (arguments.HelpRequested)
        {
            throw new HelpRequestedException();
        }

        string game = Required(arguments, "game");
        string keys = Required(arguments, "keys");
        string tocKey = Required(arguments, "toc-key");
        string output = Required(arguments, "out");
        arguments.RejectUnknown();
        return new ExtractCommand(Path.GetFullPath(game), Path.GetFullPath(keys), Path.GetFullPath(tocKey), Path.GetFullPath(output));
    }

    /// <summary>Runs the command and returns its exit code.</summary>
    public int Run(TextWriter output, TextWriter error)
    {
        if (!Directory.Exists(GameDirectory))
        {
            error.WriteLine($"ARES-GameDataExtract: error: game folder not found: {GameDirectory}");
            return UsageError;
        }

        if (ReadKeys(error) is not { } keys || ReadTocKey(error) is not { } tocKey)
        {
            return UsageError;
        }

        return Run(keys, tocKey, output, error);
    }

    /// <summary>Runs the command with the keys already read and returns its exit code.</summary>
    /// <param name="keys">The archive keys.</param>
    /// <param name="tocKey">The TOC key.</param>
    /// <param name="output">Standard output.</param>
    /// <param name="error">Standard error.</param>
    /// <param name="gameAssetTables">
    /// Whether to extract the item tables and the map gameplay tables; tests with a synthetic game leave them out, since
    /// they need the game's whole item catalogue and its gameplay assets (game mode, rounds, lighting, extraction).
    /// </param>
    /// <param name="layouts">The script layouts cooked blocks are read by; <see cref="ScriptLayouts.Current"/> by default.</param>
    internal int Run(ArchiveKeys keys, TocKey tocKey, TextWriter output, TextWriter error, bool gameAssetTables = true, ScriptLayouts? layouts = null)
    {
        layouts ??= ScriptLayouts.Current;
        if (IsInside(OutputDirectory, GameDirectory))
        {
            error.WriteLine($"ARES-GameDataExtract: error: the output folder {OutputDirectory} is inside the game folder");
            return UsageError;
        }

        Stopwatch watch = Stopwatch.StartNew();
        try
        {
            MapCollision map;
            ExtractionSummary summary;
            MapWorldData world;
            ItemTables? items = null;
            TimeSpan itemTime = TimeSpan.Zero;
            MovementData movement;
            TimeSpan movementTime;
            LootDefinitionData loot;
            TimeSpan lootTime;
            MapLootAreaData lootAreas;
            TimeSpan lootAreaTime;
            MapRulesData? rules = null;
            TimeSpan rulesTime = TimeSpan.Zero;
            InteractionAnimationData? animations = null;
            TimeSpan sharedTime;
            using (GameArchive archive = GameArchive.Open(GameDirectory, keys, tocKey))
            {
                CollisionProfiles profiles;
                AssetRegistry registry;

                // The collision config pak is the main pak: it also holds the asset registry, read once and shared by the
                // tables that need it.
                using (PakFile configPak = PakFile.Open(Path.Combine(GameDirectory, CollisionProfileConfig.ConfigPak), keys))
                {
                    profiles = CollisionProfiles.Load(configPak);
                    long sharedStart = Stopwatch.GetTimestamp();
                    registry = AssetRegistry.Load(configPak);
                    sharedTime = Stopwatch.GetElapsedTime(sharedStart);
                }

                PackageCache packages = PackageCache.FromArchive(archive);
                string mapPackage = archive.MapPackage(GameNames.Maps.Dam);
                IReadOnlyList<string> cells = archive.PackagePathsUnder($"{mapPackage}/{RuntimeCellMembers.GeneratedFolder}");
                (map, summary) = CollisionExtractor.Extract(packages, profiles, mapPackage, cells);
                world = MapWorldExtractor.Extract(packages, mapPackage, cells, DamDataLayers.All);
                if (gameAssetTables)
                {
                    long itemStart = Stopwatch.GetTimestamp();
                    items = ItemExtractor.Extract(packages, registry, layouts);
                    itemTime = Stopwatch.GetElapsedTime(itemStart);
                }

                long movementStart = Stopwatch.GetTimestamp();
                movement = MovementExtractor.Extract(packages);
                movementTime = Stopwatch.GetElapsedTime(movementStart);
                long lootStart = Stopwatch.GetTimestamp();
                loot = LootDefinitionExtractor.Extract(packages, registry, layouts, [mapPackage, .. cells]);
                lootTime = Stopwatch.GetElapsedTime(lootStart);
                long lootAreaStart = Stopwatch.GetTimestamp();
                lootAreas = MapWorldExtractor.ExtractLootAreas(packages, layouts, mapPackage, cells, DamDataLayers.All);
                lootAreaTime = Stopwatch.GetElapsedTime(lootAreaStart);
                if (gameAssetTables)
                {
                    long rulesStart = Stopwatch.GetTimestamp();
                    rules = MapRulesExtractor.Extract(packages, layouts, mapPackage, world.World, cells, RoundConfigurationAssets.Dam);
                    rulesTime = Stopwatch.GetElapsedTime(rulesStart);
                    animations = InteractionAnimationReader.Read(packages, layouts,
                        loot.ContainerClasses.Select(row => row.SearchAnimation).OfType<string>(),
                        rules.Extraction.Buttons.Select(button => button.AnimationContainer).OfType<string>());
                }
            }

            GameDataWriter writer = new(OutputDirectory);
            string path = writer.WriteFile(GameDataFiles.CollisionPath(map.MapName), stream => MapCollisionFile.Write(stream, map));
            WriteWorld(writer, map.MapName, world);
            WriteLootAreas(writer, map.MapName, lootAreas);
            if (items is not null)
            {
                WriteItems(writer, items);
            }

            WriteMovement(writer, movement);
            WriteLoot(writer, loot);
            if (rules is not null)
            {
                WriteRules(writer, map.MapName, rules);
            }

            if (animations is not null)
            {
                writer.WriteTable(GameTables.InteractionAnimationContainers, animations.Containers, GameDataJsonContext.Default.InteractionAnimationContainer);
                writer.WriteTable(GameTables.TaggedInteractionAnimationContainers, animations.TagBased, GameDataJsonContext.Default.TaggedInteractionAnimationContainer);
            }

            string manifest = writer.WriteManifest(GameBuild.GameDataBuildId, AresVersion.Current);
            summary.Write(output);
            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"World tables: {world.StreamingCells.Length:N0} streaming cells, {world.LootSockets.Length:N0} loot sockets on {world.LootSocketMeshes.Length:N0} meshes, {world.LooseItemSockets.Length:N0} loose item sockets, {world.PlacedLoot.Length:N0} placed loot containers, {world.PlayerStarts.Length:N0} player starts, {world.Ladders.Length:N0} ladders, {world.ZiplineLines.Length:N0} ziplines, {world.ZiplineLaunchers.Length:N0} zipline launchers"));
            if (items is not null)
            {
                output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"Item tables: {items.UsableItems.Length:N0} usable items, {items.ItemValues.Length:N0} item values, {items.WorldItemList.Length:N0} world item classes, {items.Firearms.Length:N0} firearms, {items.Shields.Length:N0} shields, {items.Augments.Length:N0} augments, {items.LoadoutFrames.Length:N0} loadout frames (read in {itemTime.TotalSeconds:F1} s)"));
            }

            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"Movement tables: {movement.DodgeRolls.Count} dodge rolls, {movement.VaultDefinitions.Count} vault definition sets, {movement.HeavyLanding.Heavy.Count + movement.HeavyLanding.Recovery.Count} landing levels, {movement.GraphStateTags.Count} graph tags, {movement.SprintBurstCurve.Count} sprint curve keys (read in {movementTime.TotalSeconds:F1} s)"));
            if (animations is not null)
            {
                output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"Interaction animation tables: {animations.Containers.Length:N0} containers ({animations.Containers.Count(container => container.Alignment != InteractionAlignment.None):N0} aligned), {animations.TagBased.Length:N0} tag-based entries"));
            }

            WriteLootSummary(output, loot, lootTime);
            if (rules is not null)
            {
                output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"Gameplay tables: {rules.Doors.Length:N0} doors, {rules.ExploitPreventionAreas.Length:N0} exploit prevention areas, {rules.SplinePaths.Length:N0} spline paths, {rules.Rounds.Length:N0} round configurations, {rules.LightingScenarios.Length:N0} lighting scenarios, {rules.RoundLightingGroups.Length:N0} round lighting groups, {rules.Extraction.Points.Length:N0} extraction points (read in {rulesTime.TotalSeconds:F1} s)"));
            }

            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"Loot area tables: {lootAreas.PointsOfInterest.Length:N0} points of interest, {lootAreas.LootLimiters.Length:N0} loot limiters, {lootAreas.DynamicLootSpawners.Length:N0} dynamic loot spawners in {lootAreas.DynamicLootGroups.Length:N0} groups (read in {lootAreaTime.TotalSeconds:F1} s)"));
            string tables = gameAssetTables ? "the world, gameplay, item, movement, interaction animation, loot and loot area tables" : "the world, movement, loot and loot area tables";
            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"Wrote {path} ({new FileInfo(path).Length / (1024.0 * 1024.0):F1} MB), {tables} and {manifest} in {watch.Elapsed.TotalSeconds:F1} s (asset registry read once in {sharedTime.TotalSeconds:F1} s)"));
            return Success;
        }
        catch (WrongTocKeyException)
        {
            error.WriteLine($"ARES-GameDataExtract: error: TOC key file {TocKeyFile}: the key does not decrypt the game's .utoc files (wrong key, or a key for another game build)");
            return UsageError;
        }
        catch (DirectoryNotFoundException failure)
        {
            error.WriteLine($"ARES-GameDataExtract: error: {failure.Message}. Is {GameDirectory} the game folder?");
            return UsageError;
        }
        catch (Exception failure) when (failure is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"ARES-GameDataExtract: error: cannot read the game's archives or write the game data: {failure.Message}");
            return ReadError;
        }
    }

    private static void WriteWorld(GameDataWriter writer, string mapName, MapWorldData world)
    {
        GameDataJsonContext json = GameDataJsonContext.Default;
        writer.WriteTable(MapTables.World(mapName), [world.World], json.MapWorldObjects);
        writer.WriteTable(MapTables.StreamingCells(mapName), world.StreamingCells, json.String);
        writer.WriteTable(MapTables.LootSocketMeshes(mapName), world.LootSocketMeshes, json.LootSocketMesh);
        writer.WriteTable(MapTables.LootSockets(mapName), world.LootSockets, json.LootSocket);
        writer.WriteTable(MapTables.LooseItemSockets(mapName), world.LooseItemSockets, json.LooseItemSocket);
        writer.WriteTable(MapTables.PlacedLoot(mapName), world.PlacedLoot, json.PlacedLootContainer);
        writer.WriteTable(MapTables.PlayerStarts(mapName), world.PlayerStarts, json.PlayerStart);
        writer.WriteTable(MapTables.Ladders(mapName), world.Ladders, json.LadderPlacement);
        writer.WriteTable(MapTables.LadderLogic(mapName), world.LadderLogic is { } logic ? [logic] : [], json.ArchetypeClass);
        writer.WriteTable(MapTables.ZiplineLines(mapName), world.ZiplineLines, json.ZiplinePlacement);
        writer.WriteTable(MapTables.ZiplineLaunchers(mapName), world.ZiplineLaunchers, json.ActivatableZiplinePlacement);
    }

    private static void WriteItems(GameDataWriter writer, ItemTables items)
    {
        GameDataJsonContext json = GameDataJsonContext.Default;
        writer.WriteTable(GameTables.UsableItems, items.UsableItems, json.ItemDefinition);
        writer.WriteTable(GameTables.ItemValues, items.ItemValues, json.ItemValue);
        writer.WriteTable(GameTables.WorldItemList, items.WorldItemList, json.String);
        writer.WriteTable(GameTables.Augments, items.Augments, json.AugmentDefinition);
        writer.WriteTable(GameTables.LoadoutFrames, items.LoadoutFrames, json.LoadoutFrameDefinition);
        writer.WriteTable(GameTables.Firearms, items.Firearms, json.FirearmDefinition);
        writer.WriteTable(GameTables.Shields, items.Shields, json.ShieldDefinition);
        writer.WriteTable(GameTables.MenuAugments, items.Menu.Augments, json.MenuItem);
        writer.WriteTable(GameTables.MenuCurrency, items.Menu.Currency, json.MenuItem);
        writer.WriteTable(GameTables.MenuGadgets, items.Menu.Gadgets, json.MenuItem);
        writer.WriteTable(GameTables.MenuSalvage, items.Menu.Salvage, json.MenuItem);
        writer.WriteTable(GameTables.MenuShields, items.Menu.Shields, json.MenuItem);
        writer.WriteTable(GameTables.MenuWeapons, items.Menu.Weapons, json.MenuItem);
    }

    private static void WriteMovement(GameDataWriter writer, MovementData movement)
    {
        GameDataJsonContext json = GameDataJsonContext.Default;
        writer.WriteTable(GameTables.DodgeRolls, movement.DodgeRolls, json.DodgeRollSequence);
        writer.WriteTable(GameTables.VaultDefinitions, movement.VaultDefinitions, json.VaultDefinitionSet);
        writer.WriteTable(GameTables.HeavyLandingLevels, movement.HeavyLanding.Heavy, json.HeavyLandingLevel);
        writer.WriteTable(GameTables.RecoveryLandingLevels, movement.HeavyLanding.Recovery, json.HeavyLandingLevel);
        writer.WriteTable(GameTables.GraphStateTags, movement.GraphStateTags, json.String);
        writer.WriteTable(GameTables.SnapHookItem, [movement.SnapHook], json.SnapHookItemDefaults);
        writer.WriteTable(GameTables.SprintBurstCurve, movement.SprintBurstCurve, json.WeightedCurveKey);
    }

    private static void WriteLoot(GameDataWriter writer, LootDefinitionData loot)
    {
        GameDataJsonContext json = GameDataJsonContext.Default;
        writer.WriteTable(GameTables.LootContainerTypes, loot.ContainerTypes, json.LootContainerType);
        writer.WriteTable(GameTables.LootContainerClasses, loot.ContainerClasses, json.LootContainerClass);
        writer.WriteTable(GameTables.StaticValueLootTables, loot.ValueTables, json.StaticValueLootTable);
        writer.WriteTable(GameTables.SkillLootTables, loot.SkillTables, json.SkillLootTable);
    }

    private static void WriteLootSummary(TextWriter output, LootDefinitionData loot, TimeSpan time)
    {
        output.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"Loot tables: {loot.ContainerTypes.Length:N0} container types, {loot.ContainerClasses.Length:N0} container classes, {loot.ValueTables.Length:N0} value loot tables, {loot.SkillTables.Length:N0} skill loot tables (read in {time.TotalSeconds:F1} s)"));
        foreach ((string classPath, string reason) in loot.UndecodedClasses)
        {
            output.WriteLine($"Container class not decoded: {classPath}: {reason}");
        }
    }

    private static void WriteLootAreas(GameDataWriter writer, string mapName, MapLootAreaData areas)
    {
        GameDataJsonContext json = GameDataJsonContext.Default;
        writer.WriteTable(MapTables.PointsOfInterest(mapName), areas.PointsOfInterest, json.PointOfInterest);
        writer.WriteTable(MapTables.LootLimiters(mapName), areas.LootLimiters, json.LootLimiter);
        writer.WriteTable(MapTables.DynamicLootSpawners(mapName), areas.DynamicLootSpawners, json.DynamicLootSpawner);
        writer.WriteTable(MapTables.DynamicLootGroups(mapName), areas.DynamicLootGroups, json.DynamicLootGroupEntry);
    }

    private static void WriteRules(GameDataWriter writer, string mapName, MapRulesData rules)
    {
        GameDataJsonContext json = GameDataJsonContext.Default;
        writer.WriteTable(MapTables.GameMode(mapName), [rules.GameMode], json.MapGameMode);
        writer.WriteTable(MapTables.Doors(mapName), rules.Doors, json.DoorPlacement);
        writer.WriteTable(MapTables.ExploitPreventionAreas(mapName), rules.ExploitPreventionAreas, json.ExploitPreventionAreaPlacement);
        writer.WriteTable(MapTables.SplinePaths(mapName), rules.SplinePaths, json.SplinePathPlacement);
        writer.WriteTable(MapTables.Rounds(mapName), rules.Rounds, json.RoundConfiguration);
        writer.WriteTable(MapTables.LightingScenarios(mapName), rules.LightingScenarios, json.LightingScenario);
        writer.WriteTable(MapTables.RoundLightingGroups(mapName), rules.RoundLightingGroups, json.RoundLightingGroup);
        writer.WriteTable(MapTables.ExtractionElevator(mapName), [rules.Extraction.Elevator], json.ExtractionElevator);
        writer.WriteTable(MapTables.ExtractionButtons(mapName), rules.Extraction.Buttons, json.ExtractionButton);
        writer.WriteTable(MapTables.ExtractionPoints(mapName), rules.Extraction.Points, json.ExtractionPointPlacement);
        writer.WriteTable(MapTables.ZiplineDefaults(mapName), [rules.ZiplineDefaults], json.ZiplineLineDefaults);
        writer.WriteTable(MapTables.PlayerStartCapsule(mapName), [rules.PlayerStartCapsule], json.PlayerStartCapsule);
    }

    /// <summary>Whether <paramref name="directory"/> is <paramref name="parent"/> or inside it.</summary>
    private static bool IsInside(string directory, string parent)
    {
        string relative = Path.GetRelativePath(parent, directory);
        return relative == "." || (relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !Path.IsPathRooted(relative));
    }

    private static string Required(CommandLineArguments arguments, string name)
    {
        return arguments.GetString(name) is { Length: > 0 } value ? value : throw new CommandLineException($"--{name} is required");
    }

    private ArchiveKeys? ReadKeys(TextWriter error)
    {
        if (!File.Exists(KeysFile))
        {
            error.WriteLine($"ARES-GameDataExtract: error: archive keys file not found: {KeysFile}");
            return null;
        }

        ArchiveKeys keys;
        try
        {
            keys = ArchiveKeys.Parse(File.ReadAllText(KeysFile));
        }
        catch (Exception failure) when (failure is IOException or FormatException or UnauthorizedAccessException)
        {
            error.WriteLine($"ARES-GameDataExtract: error: archive keys file {KeysFile}: {failure.Message}");
            return null;
        }

        if (keys.Fingerprint() == GameBuild.ArchiveKeysSha256)
        {
            return keys;
        }

        error.WriteLine($"ARES-GameDataExtract: error: archive keys file {KeysFile}: these keys do not match game build {GameBuild.GameDataBuildId}");
        return null;
    }

    /// <summary>Reads <see cref="TocKeyFile"/>, or writes why it cannot be used and returns <see langword="null"/>.</summary>
    internal TocKey? ReadTocKey(TextWriter error)
    {
        if (!File.Exists(TocKeyFile))
        {
            error.WriteLine($"ARES-GameDataExtract: error: TOC key file not found: {TocKeyFile}");
            return null;
        }

        try
        {
            return TocKey.Parse(File.ReadAllText(TocKeyFile));
        }
        catch (Exception failure) when (failure is IOException or FormatException or UnauthorizedAccessException)
        {
            error.WriteLine($"ARES-GameDataExtract: error: TOC key file {TocKeyFile}: {failure.Message}");
            return null;
        }
    }
}
