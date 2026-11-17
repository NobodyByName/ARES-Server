using System.Diagnostics;
using System.Globalization;
using Ares.Common;
using Ares.Common.Archives;
using Ares.Common.CommandLine;
using Ares.Common.GameData;
using Ares.Common.Maps;
using Ares.GameDataExtract.Archives;
using Ares.GameDataExtract.Extraction;
using Ares.GameDataExtract.Output;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Profiles;
using Ares.GameDataExtract.World;
using Ares.Structs.Core;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract;

/// <summary>
/// <c>ARES-GameDataExtract extract</c>: reads the installed game read-only and writes the game data into the output
/// folder (created when missing, never inside the game folder): the Dam map's collision file and world tables (see
/// <see cref="MapTables"/>) under <c>maps/&lt;map&gt;/</c>, then <c>manifest.json</c> (layout in <see cref="GameDataFiles"/>). Each file replaces an
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
        manifest.json and the map files under maps/<map>/.
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
    internal int Run(ArchiveKeys keys, TocKey tocKey, TextWriter output, TextWriter error)
    {
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
            using (GameArchive archive = GameArchive.Open(GameDirectory, keys, tocKey))
            {
                CollisionProfiles profiles;
                using (PakFile configPak = PakFile.Open(Path.Combine(GameDirectory, CollisionProfileConfig.ConfigPak), keys))
                {
                    profiles = CollisionProfiles.Load(configPak);
                }

                PackageCache packages = PackageCache.FromArchive(archive);
                IReadOnlyList<string> cells = archive.PackagePathsUnder($"{DamMap.MapPackage}/{RuntimeCellMembers.GeneratedFolder}");
                (map, summary) = CollisionExtractor.Extract(packages, profiles, cells);
                world = MapWorldExtractor.Extract(packages, DamMap.MapPackage, cells, DamDataLayers.All);
            }

            GameDataWriter writer = new(OutputDirectory);
            string path = writer.WriteFile(GameDataFiles.CollisionPath(map.MapName), stream => MapCollisionFile.Write(stream, map));
            WriteWorld(writer, map.MapName, world);
            string manifest = writer.WriteManifest(GameBuild.SteamBuildId, AresVersion.Current);
            summary.Write(output);
            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"World tables: {world.StreamingCells.Length:N0} streaming cells, {world.LootSockets.Length:N0} loot sockets on {world.LootSocketMeshes.Length:N0} meshes, {world.PlacedLoot.Length:N0} placed loot containers, {world.PlayerStarts.Length:N0} player starts, {world.Ladders.Length:N0} ladders, {world.ZiplineLines.Length:N0} ziplines, {world.ZiplineLaunchers.Length:N0} zipline launchers"));
            output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"Wrote {path} ({new FileInfo(path).Length / (1024.0 * 1024.0):F1} MB), the world tables and {manifest} in {watch.Elapsed.TotalSeconds:F1} s"));
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
        writer.WriteTable(MapTables.PlacedLoot(mapName), world.PlacedLoot, json.PlacedLootContainer);
        writer.WriteTable(MapTables.PlayerStarts(mapName), world.PlayerStarts, json.PlayerStart);
        writer.WriteTable(MapTables.Ladders(mapName), world.Ladders, json.LadderPlacement);
        writer.WriteTable(MapTables.LadderLogic(mapName), world.LadderLogic is { } logic ? [logic] : [], json.ArchetypeClass);
        writer.WriteTable(MapTables.ZiplineLines(mapName), world.ZiplineLines, json.ZiplinePlacement);
        writer.WriteTable(MapTables.ZiplineLaunchers(mapName), world.ZiplineLaunchers, json.ActivatableZiplinePlacement);
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

        error.WriteLine($"ARES-GameDataExtract: error: archive keys file {KeysFile}: these keys do not match game build {GameBuild.SteamBuildId}");
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
