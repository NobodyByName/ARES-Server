using System.Collections.Immutable;

namespace Ares.Common.Maps;

/// <summary>A streaming cell of the map, which owns placements and height-field tiles.</summary>
/// <param name="PackageName">The cell's package name.</param>
/// <param name="DataLayers">Names of the data layers the cell belongs to; empty for the base map, which is always loaded.</param>
public sealed record MapCell(string PackageName, ImmutableArray<string> DataLayers)
{
    /// <summary>Whether the cell belongs to the base map rather than to optional data layers.</summary>
    public bool IsBaseMap => DataLayers.IsEmpty;
}
