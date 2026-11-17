using System.Collections.Immutable;

namespace Ares.Common.GameData;

/// <summary>
/// <see cref="GameDataFiles.ManifestFileName"/>: which game build the data was extracted from, by which
/// ARES-GameDataExtract, and every file it wrote.
/// </summary>
/// <param name="GameBuildId">Steam build id of the game the data was extracted from.</param>
/// <param name="ExtractorVersion">ARES version of the ARES-GameDataExtract that wrote the data.</param>
/// <param name="Files">The files, ordered by name (ordinal).</param>
public sealed record GameDataManifest(int GameBuildId, string ExtractorVersion, ImmutableArray<GameDataFileEntry> Files);

/// <summary>One file of a game data directory.</summary>
/// <param name="Name">Name relative to the directory (see <see cref="GameDataFiles"/>).</param>
/// <param name="Sha256">SHA-256 of the file's content, lowercase hex.</param>
/// <param name="RowCount">Number of rows of a JSON table; null for other files.</param>
public sealed record GameDataFileEntry(string Name, string Sha256, int? RowCount = null);
