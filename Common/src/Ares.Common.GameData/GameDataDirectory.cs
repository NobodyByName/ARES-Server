using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Ares.Structs.Core;

namespace Ares.Common.GameData;

/// <summary>
/// An operator's game data directory, opened and checked: its manifest was written for this game build
/// (<see cref="GameBuild.GameDataBuildId"/>) and lists only well-formed names of files that exist. Each file's SHA-256 is
/// checked against the manifest when the file is read.
/// </summary>
public sealed class GameDataDirectory
{
    private const string RerunHint = "Run ARES-GameDataExtract extract again to rewrite the game data.";

    private readonly Dictionary<string, GameDataFileEntry> _files;

    private GameDataDirectory(string fullPath, GameDataManifest manifest)
    {
        FullPath = fullPath;
        Manifest = manifest;
        _files = manifest.Files.ToDictionary(file => file.Name, StringComparer.Ordinal);
    }

    /// <summary>Full path of the directory.</summary>
    public string FullPath { get; }

    /// <summary>The directory's manifest.</summary>
    public GameDataManifest Manifest { get; }

    /// <summary>Opens a game data directory and checks its manifest.</summary>
    /// <param name="directory">The configured game data directory.</param>
    /// <exception cref="GameDataException">
    /// The directory or its manifest is missing, the manifest is malformed or for another game build, or it lists a
    /// file that does not exist.
    /// </exception>
    public static GameDataDirectory Open(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        string fullPath = Path.GetFullPath(directory);
        if (!Directory.Exists(fullPath))
        {
            throw new GameDataException($"Game data directory not found: {fullPath}. Write it with ARES-GameDataExtract extract and point game-data-dir at its output folder.");
        }

        string manifestPath = Path.Combine(fullPath, GameDataFiles.ManifestFileName);
        GameDataManifest manifest = ReadManifest(manifestPath);
        if (manifest.GameBuildId != GameBuild.GameDataBuildId)
        {
            throw new GameDataException($"The game data in {fullPath} was extracted from game build {manifest.GameBuildId}, but this ARES requires game data from build {GameBuild.GameDataBuildId}. Point game-data-dir at an extract from that build.");
        }

        CheckEntries(manifestPath, manifest);
        if (manifest.Files.Select(file => PathOf(fullPath, file.Name)).FirstOrDefault(path => !File.Exists(path)) is { } missing)
        {
            throw new GameDataException($"Game data file {missing} is listed in {manifestPath} but missing. {RerunHint}");
        }

        return new GameDataDirectory(fullPath, manifest);
    }

    /// <summary>Whether the manifest lists a file of this name.</summary>
    public bool Contains(string name) => _files.ContainsKey(name);

    /// <summary>Full path of a file of the directory (whether or not it is listed).</summary>
    public string PathOf(string name) => PathOf(FullPath, name);

    /// <summary>Reads a JSON table: checks its SHA-256 and row count against the manifest and returns its rows in file order.</summary>
    /// <param name="name">The table's file name, for example <c>items.json</c> or <c>maps/TheDam_02_P/sockets.json</c>.</param>
    /// <param name="rowType">Source-generated metadata of the row type.</param>
    /// <exception cref="GameDataException">The table is missing, changed since extraction or malformed.</exception>
    public ImmutableArray<TRow> ReadTable<TRow>(string name, JsonTypeInfo<TRow> rowType)
    {
        ArgumentNullException.ThrowIfNull(rowType);
        GameDataFileEntry entry = Entry(name);
        string path = PathOf(name);
        if (entry.RowCount is not { } expectedRows)
        {
            throw new GameDataException($"Game data file {path} is not a table (the manifest lists no row count). {RerunHint}");
        }

        byte[] content = ReadAllBytes(path);
        CheckHash(path, entry, SHA256.HashData(content));
        ImmutableArray<TRow> rows;
        try
        {
            rows = ParseRows(content, rowType);
        }
        catch (JsonException error)
        {
            throw new GameDataException($"Game data table {path} is malformed: {error.Message} {RerunHint}", error);
        }

        return rows.Length == expectedRows
            ? rows
            : throw new GameDataException($"Game data table {path} holds {rows.Length} rows, but the manifest lists {expectedRows}. {RerunHint}");
    }

    /// <summary>
    /// Opens a listed file for reading after checking its SHA-256 against the manifest; the stream is positioned at the
    /// start. Use it for files that are not JSON tables, such as map collision.
    /// </summary>
    /// <exception cref="GameDataException">The file is not listed, is missing or changed since extraction.</exception>
    public FileStream OpenFile(string name)
    {
        GameDataFileEntry entry = Entry(name);
        string path = PathOf(name);
        FileStream stream = OpenRead(path);
        try
        {
            CheckHash(path, entry, SHA256.HashData(stream));
            stream.Position = 0;
            return stream;
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    private static string PathOf(string directory, string name) => Path.Combine(directory, name.Replace('/', Path.DirectorySeparatorChar));

    private static GameDataManifest ReadManifest(string path)
    {
        if (!File.Exists(path))
        {
            throw new GameDataException($"Game data manifest not found: {path}. The folder was not written by ARES-GameDataExtract, or an extraction did not finish. {RerunHint}");
        }

        try
        {
            return JsonSerializer.Deserialize(ReadAllBytes(path), GameDataJsonContext.Default.GameDataManifest)
                ?? throw new GameDataException($"Game data manifest {path} is empty (null). {RerunHint}");
        }
        catch (JsonException error)
        {
            throw new GameDataException($"Game data manifest {path} is malformed: {error.Message} {RerunHint}", error);
        }
    }

    private static void CheckEntries(string manifestPath, GameDataManifest manifest)
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        foreach (GameDataFileEntry file in manifest.Files)
        {
            string? problem = !GameDataFiles.IsValidName(file.Name) ? "is not a valid file name"
                : !names.Add(file.Name) ? "is listed twice"
                : !IsSha256(file.Sha256) ? "has a SHA-256 that is not 64 lowercase hex digits"
                : file.RowCount < 0 ? "has a negative row count"
                : null;
            if (problem is not null)
            {
                throw new GameDataException($"Game data manifest {manifestPath} is malformed: '{file.Name}' {problem}. {RerunHint}");
            }
        }
    }

    private static bool IsSha256(string value) => value.Length == SHA256.HashSizeInBytes * 2 && value.All(char.IsAsciiHexDigitLower);

    private static void CheckHash(string path, GameDataFileEntry entry, byte[] hash)
    {
        if (!string.Equals(Convert.ToHexStringLower(hash), entry.Sha256, StringComparison.Ordinal))
        {
            throw new GameDataException($"Game data file {path} does not match its SHA-256 in the manifest: it changed after extraction. {RerunHint}");
        }
    }

    private static ImmutableArray<TRow> ParseRows<TRow>(byte[] content, JsonTypeInfo<TRow> rowType)
    {
        Utf8JsonReader reader = new(content);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("a table is a JSON array of rows.");
        }

        ImmutableArray<TRow>.Builder rows = ImmutableArray.CreateBuilder<TRow>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            rows.Add(JsonSerializer.Deserialize(ref reader, rowType) ?? throw new JsonException($"row {rows.Count} is null."));
        }

        if (reader.TokenType != JsonTokenType.EndArray || reader.Read())
        {
            throw new JsonException("unexpected content after the rows.");
        }

        return rows.DrainToImmutable();
    }

    private GameDataFileEntry Entry(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        return _files.TryGetValue(name, out GameDataFileEntry? entry)
            ? entry
            : throw new GameDataException($"The game data in {FullPath} has no file '{name}'; it was written by an ARES-GameDataExtract that does not extract it. Run the ARES-GameDataExtract of this ARES version again.");
    }

    private static byte[] ReadAllBytes(string path)
    {
        try
        {
            return File.ReadAllBytes(path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new GameDataException($"Game data file {path} cannot be read: {error.Message}", error);
        }
    }

    private static FileStream OpenRead(string path)
    {
        try
        {
            return File.OpenRead(path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new GameDataException($"Game data file {path} cannot be read: {error.Message}", error);
        }
    }
}
