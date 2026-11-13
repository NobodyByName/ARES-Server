using System.Collections.Immutable;
using Ares.Common.Maps;
using Ares.MapData.Packages;
using Ares.Structs.Game.World;

namespace Ares.MapData.Cells;

/// <summary>
/// Reads which data layers the streaming cells of a World Partition map belong to, from the runtime cells stored in
/// the map's persistent level. A cell without data layers belongs to the base map.
/// </summary>
internal static class MapCellReader
{
    private static readonly PackageObjectIndex s_runtimeCell = PackageObjectIndex.FromScriptPath(RuntimeCellMembers.ClassPath);

    /// <summary>The data layer names of each runtime cell of <paramref name="level"/>, by cell name.</summary>
    /// <exception cref="InvalidDataException">A runtime cell does not decode, or two share a name.</exception>
    public static IReadOnlyDictionary<string, ImmutableArray<string>> ReadRuntimeCells(CookedPackage level)
    {
        Dictionary<string, ImmutableArray<string>> cells = new(StringComparer.Ordinal);
        foreach (ZenExport export in level.Exports)
        {
            if (export.Class != s_runtimeCell)
            {
                continue;
            }

            ImmutableArray<string> layers;
            try
            {
                layers = ReadDataLayers(level, export);
            }
            catch (InvalidDataException error)
            {
                throw new InvalidDataException($"{level.Header.ExportPath(export.Index)}: {error.Message}", error);
            }

            if (!cells.TryAdd(export.Name, layers))
            {
                throw new InvalidDataException($"{level.Name}: two runtime cells are named {export.Name}");
            }
        }

        return cells;
    }

    /// <summary>
    /// The cells <paramref name="packages"/> lists, in that order, with their data layers' names
    /// (<see cref="DataLayer.Name"/>). The persistent level itself belongs to the base map; every other package must be
    /// a generated cell package of the map with a runtime cell.
    /// </summary>
    /// <param name="level">The map's persistent level package.</param>
    /// <param name="packages">Package names of the cells.</param>
    /// <param name="dataLayers">The map's data layers; every layer a cell names must be one of them.</param>
    /// <exception cref="InvalidDataException">A package is not a cell of the map, a runtime cell does not decode, or a cell names an unknown data layer.</exception>
    public static IReadOnlyList<MapCell> ReadCells(CookedPackage level, IReadOnlyList<string> packages, IReadOnlyList<DataLayer> dataLayers)
    {
        IReadOnlyDictionary<string, ImmutableArray<string>> runtimeCells = ReadRuntimeCells(level);
        HashSet<string> known = [.. dataLayers.Select(layer => layer.Name)];
        string prefix = $"{level.Name}/{RuntimeCellMembers.GeneratedFolder}/";
        List<MapCell> cells = new(packages.Count);
        foreach (string package in packages)
        {
            if (package == level.Name)
            {
                cells.Add(new MapCell(package, []));
                continue;
            }

            if (!package.StartsWith(prefix, StringComparison.Ordinal)
                || !runtimeCells.TryGetValue(package[prefix.Length..], out ImmutableArray<string> layers))
            {
                throw new InvalidDataException($"{package} has no runtime cell in {level.Name}");
            }

            if (layers.FirstOrDefault(layer => !known.Contains(layer)) is { } unknown)
            {
                throw new InvalidDataException($"{package} belongs to data layer {unknown}, which the map does not define");
            }

            cells.Add(new MapCell(package, layers));
        }

        return cells;
    }

    private static ImmutableArray<string> ReadDataLayers(CookedPackage level, ZenExport export)
    {
        UnversionedHeader header = UnversionedHeader.Read(level.Data, CookedPackage.StartOf(export));
        if (!header.IsNonZero(RuntimeCellMembers.DataLayers))
        {
            return [];
        }

        int at = header.ValuesOffset + header.Members
            .TakeWhile(member => member.Index != RuntimeCellMembers.DataLayers)
            .Where(member => member.IsNonZero)
            .Sum(member => RuntimeCellMembers.FixedValueSize(member.Index)
                ?? throw new InvalidDataException($"Runtime cell member {member.Index} has an unknown size"));

        return ReadNames(level, at, CookedPackage.EndOf(export));
    }

    private static ImmutableArray<string> ReadNames(CookedPackage level, int start, int end)
    {
        UnversionedHeader header = UnversionedHeader.Read(level.Data, start);
        int at = header.ValuesOffset;
        ImmutableArray<string> names = [];
        foreach (UnversionedMember member in header.Members)
        {
            if (!member.IsNonZero)
            {
                continue;
            }

            switch (member.Index)
            {
                case DataLayerNamesMembers.Unknown0:
                    at++;
                    break;
                case DataLayerNamesMembers.Names:
                    int count = level.ReadInt32(at);
                    at += sizeof(int);
                    if (count < 0 || (long)count * DataLayerNamesMembers.NameSize > end - at)
                    {
                        throw new InvalidDataException($"Invalid data layer count {count}");
                    }

                    ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>(count);
                    for (int i = 0; i < count; i++)
                    {
                        builder.Add(level.ReadName(at));
                        at += DataLayerNamesMembers.NameSize;
                    }

                    names = builder.MoveToImmutable();
                    break;
                default:
                    throw new InvalidDataException($"Data layer names member {member.Index} is not known");
            }
        }

        return at <= end ? names : throw new InvalidDataException("The data layer names run past the export");
    }
}
