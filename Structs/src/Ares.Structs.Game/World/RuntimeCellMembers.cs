namespace Ares.Structs.Game.World;

/// <summary>
/// Members of a <c>UWorldPartitionRuntimeLevelStreamingCell</c> in a World Partition map's persistent level: one
/// runtime cell, named like the leaf of the generated cell package it streams in.
/// </summary>
public static class RuntimeCellMembers
{
    /// <summary>Script path of the class.</summary>
    public const string ClassPath = "/Script/Engine.WorldPartitionRuntimeLevelStreamingCell";

    /// <summary>Folder, under the map package, of the generated cell packages.</summary>
    public const string GeneratedFolder = "_Generated_";

    /// <summary>Unknown member, int32.</summary>
    public const int Unknown0 = 0;

    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown2 = 2;

    /// <summary>
    /// <c>DataLayers</c>: the cell's data layer names as a struct framed by its own unversioned header (see
    /// <see cref="DataLayerNamesMembers"/>). Absent or empty for cells of the base map.
    /// </summary>
    public const int DataLayers = 3;

    /// <summary>Bytes of the value of a fixed-size member before <see cref="DataLayers"/>; <see langword="null"/> when unknown.</summary>
    public static int? FixedValueSize(int member) => member switch
    {
        Unknown0 => 4,
        Unknown2 => 1,
        _ => null,
    };
}

/// <summary>Members of the data layer names struct of a runtime cell (<see cref="RuntimeCellMembers.DataLayers"/>).</summary>
public static class DataLayerNamesMembers
{
    /// <summary>Unknown member, one byte.</summary>
    public const int Unknown0 = 0;

    /// <summary><c>DataLayers</c>: array of names (an int32 count, then 8 bytes per name), the layers' instance names.</summary>
    public const int Names = 1;

    /// <summary>Bytes of a name.</summary>
    public const int NameSize = 8;
}
