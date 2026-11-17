using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Ares.Common.GameData;

namespace Ares.GameDataExtract.Output;

/// <summary>
/// Writes a game data directory (layout in <see cref="GameDataFiles"/>): the files of one extraction, then
/// <see cref="GameDataFiles.ManifestFileName"/> listing them. The output is deterministic: the same input gives the same
/// bytes.
/// </summary>
/// <remarks>
/// <para>
/// JSON is UTF-8 without a byte order mark, indented by two spaces, with <c>\n</c> newlines and a final newline.
/// <see cref="Utf8JsonWriter"/> formats numbers culture-invariantly, floating-point values in their shortest
/// round-trip form. Rows are written in the order given and members in declaration order, so tables must come in a
/// stable order and row types must not hold unordered collections. Null members are written as <c>null</c>
/// (<see cref="GameDataJsonContext"/> never leaves a member out), so a nullable constructor parameter without a default
/// (such as <c>PointOfInterest.Name</c>), which the reader requires, reads back. The manifest lists the files ordered by name.
/// </para>
/// <para>
/// Each file is written to a temporary file and moved into place once complete. The first write removes an earlier
/// manifest, so an extraction that stops halfway leaves a directory the servers refuse instead of one that mixes old
/// and new files.
/// </para>
/// </remarks>
internal sealed class GameDataWriter
{
    private const string TemporaryExtension = ".tmp";
    private const int WriteBufferSize = 1 << 20;

    private static readonly JsonWriterOptions s_jsonOptions = new()
    {
        Indented = true,
        IndentSize = 2,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly SortedDictionary<string, GameDataFileEntry> _files = new(StringComparer.Ordinal);
    private readonly string _fullPath;
    private bool _started;

    /// <summary>Creates a writer into <paramref name="directory"/> (created when missing).</summary>
    public GameDataWriter(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        _fullPath = Path.GetFullPath(directory);
    }

    /// <summary>Writes a JSON table: an array of rows in the given order.</summary>
    /// <param name="name">The table's file name (see <see cref="GameDataFiles"/>).</param>
    /// <param name="rows">The rows, in a stable order.</param>
    /// <param name="rowType">Source-generated metadata of the row type (from <see cref="GameDataJsonContext"/>).</param>
    public void WriteTable<TRow>(string name, IEnumerable<TRow> rows, JsonTypeInfo<TRow> rowType)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(rowType);
        int count = 0;
        Write(name, stream =>
        {
            using (Utf8JsonWriter writer = new(stream, s_jsonOptions))
            {
                writer.WriteStartArray();
                foreach (TRow row in rows)
                {
                    JsonSerializer.Serialize(writer, row, rowType);
                    count++;
                }

                writer.WriteEndArray();
            }

            stream.Write("\n"u8);
        }, () => count);
    }

    /// <summary>Writes a file that is not a JSON table, such as a map's collision.</summary>
    /// <param name="name">The file's name (see <see cref="GameDataFiles"/>).</param>
    /// <param name="write">Writes the content into the given stream.</param>
    /// <returns>The written file's full path.</returns>
    public string WriteFile(string name, Action<Stream> write)
    {
        ArgumentNullException.ThrowIfNull(write);
        return Write(name, write, static () => null);
    }

    /// <summary>Writes the manifest of the files written so far.</summary>
    /// <param name="gameBuildId">Steam build id of the game the data was extracted from.</param>
    /// <param name="extractorVersion">ARES version of this extractor.</param>
    /// <returns>The manifest's full path.</returns>
    public string WriteManifest(int gameBuildId, string extractorVersion)
    {
        ArgumentException.ThrowIfNullOrEmpty(extractorVersion);
        GameDataManifest manifest = new(gameBuildId, extractorVersion, [.. _files.Values]);
        Directory.CreateDirectory(_fullPath);
        string path = Path.Combine(_fullPath, GameDataFiles.ManifestFileName);
        WriteAtomically(path, stream =>
        {
            using (Utf8JsonWriter writer = new(stream, s_jsonOptions))
            {
                JsonSerializer.Serialize(writer, manifest, GameDataJsonContext.Default.GameDataManifest);
            }

            stream.Write("\n"u8);
        });
        return path;
    }

    private string Write(string name, Action<Stream> write, Func<int?> rowCount)
    {
        if (!GameDataFiles.IsValidName(name))
        {
            throw new ArgumentException($"'{name}' is not a valid game data file name.", nameof(name));
        }

        if (!_started)
        {
            Directory.CreateDirectory(_fullPath);
            File.Delete(Path.Combine(_fullPath, GameDataFiles.ManifestFileName));
            _started = true;
        }

        string path = Path.Combine(_fullPath, name.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string hash = WriteAtomically(path, write);
        _files[name] = new GameDataFileEntry(name, hash, rowCount());
        return path;
    }

    /// <summary>Writes a file through a temporary file and returns the SHA-256 (lowercase hex) of its content.</summary>
    private static string WriteAtomically(string path, Action<Stream> write)
    {
        string temporary = path + TemporaryExtension;
        try
        {
            string hash;
            using (FileStream stream = new(temporary, FileMode.Create, FileAccess.ReadWrite, FileShare.None, WriteBufferSize))
            {
                write(stream);
                stream.Position = 0;
                hash = Convert.ToHexStringLower(SHA256.HashData(stream));
            }

            File.Move(temporary, path, overwrite: true);
            return hash;
        }
        catch
        {
            File.Delete(temporary);
            throw;
        }
    }
}
