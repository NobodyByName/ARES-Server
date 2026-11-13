namespace Ares.Structs.Game.Collision;

/// <summary>
/// The native data that closes a cooked scene component export after its properties: a zero word, a count and that many
/// entries (each starting with an import reference). Static mesh (and spline mesh) components follow it with their LOD
/// info array (a count and that many <see cref="StaticMeshLodInfoNativeData"/> entries) and <see cref="StaticMeshTrailer"/>.
/// </summary>
public static class ComponentNativeData
{
    /// <summary>Bytes of the zero word and the entry count.</summary>
    public const int HeadSize = 8;

    /// <summary>Bytes of one entry.</summary>
    public const int EntrySize = 28;

    /// <summary>The largest entry count a component stores.</summary>
    public const int MaxEntries = 63;

    /// <summary>Bytes of the LOD info count of a static mesh component.</summary>
    public const int LodCountSize = 4;

    /// <summary>The largest LOD info count a static mesh component stores.</summary>
    public const int MaxLodInfos = 8;

    /// <summary>The words after a static mesh (or spline mesh) component's LOD info array.</summary>
    public static ReadOnlySpan<byte> StaticMeshTrailer => [1, 0, 0, 0, 0, 0, 0, 0];
}

/// <summary>
/// One entry of a static mesh component's native LOD info array: strip flags (a global and a class byte), a fixed-size
/// block, then, unless the class flags strip them, a byte telling whether override vertex colors follow and, when it is
/// set, a vertex color buffer: its own strip flags, the stride, the vertex count and, for a non-zero count, the element
/// size, the element count and one color per vertex.
/// </summary>
public static class StaticMeshLodInfoNativeData
{
    /// <summary>Bytes of the strip flags (global byte, then class byte).</summary>
    public const int StripFlagsSize = 2;

    /// <summary>Global strip flag: editor-only data (painted vertices) is not stored. Cooked entries always set it.</summary>
    public const byte EditorDataStripped = 1;

    /// <summary>Class strip flag: the override vertex color part is not stored.</summary>
    public const byte OverrideColorsStripped = 1;

    /// <summary>Bytes of the fixed-size block after the strip flags.</summary>
    public const int FixedSize = 32;

    /// <summary>Bytes of a vertex color buffer before its colors: strip flags, stride and vertex count.</summary>
    public const int ColorBufferHeaderSize = 10;

    /// <summary>Bytes of the element size and element count that precede a non-empty color array.</summary>
    public const int ColorArrayHeaderSize = 8;

    /// <summary>Bytes of one vertex color.</summary>
    public const int ColorSize = 4;
}

/// <summary>Script paths of other cooked object classes the map's collision refers to.</summary>
public static class CollisionObjectClasses
{
    /// <summary>Script path of a static mesh asset.</summary>
    public const string StaticMesh = "/Script/Engine.StaticMesh";

    /// <summary>Script path of a body setup (a mesh's or component's collision shapes).</summary>
    public const string BodySetup = "/Script/Engine.BodySetup";
}
