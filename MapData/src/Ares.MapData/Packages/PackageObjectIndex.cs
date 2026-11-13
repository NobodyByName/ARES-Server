namespace Ares.MapData.Packages;

/// <summary>What a <see cref="PackageObjectIndex"/> refers to.</summary>
internal enum PackageObjectKind
{
    /// <summary>An export of the same package.</summary>
    Export = 0,

    /// <summary>A native script object, named by its global id.</summary>
    ScriptImport = 1,

    /// <summary>A public export of another package.</summary>
    PackageImport = 2,

    /// <summary>No object.</summary>
    Null = 3,
}

/// <summary>
/// A 64-bit object reference in a cooked package: the top two bits are the <see cref="PackageObjectKind"/>, the rest
/// an export index, a script object id, or an imported package index and public export hash index.
/// </summary>
internal readonly record struct PackageObjectIndex(ulong Value)
{
    private const int KindShift = 62;
    private const ulong IndexMask = (1UL << KindShift) - 1;

    /// <summary>The null reference.</summary>
    public static PackageObjectIndex Null { get; } = new(ulong.MaxValue);

    public PackageObjectKind Kind => (PackageObjectKind)(Value >> KindShift);

    public bool IsNull => Kind == PackageObjectKind.Null;

    /// <summary>The export index of an <see cref="PackageObjectKind.Export"/> reference.</summary>
    public int ExportIndex => (int)(Value & IndexMask);

    /// <summary>Index into the package's imported package names, for a <see cref="PackageObjectKind.PackageImport"/>.</summary>
    public int ImportedPackageIndex => (int)((Value & IndexMask) >> 32);

    /// <summary>Index into the package's imported public export hashes, for a <see cref="PackageObjectKind.PackageImport"/>.</summary>
    public int ImportedPublicExportHashIndex => (int)(uint)Value;

    /// <summary>
    /// The global id of a script object from its path (for example <c>/Script/Engine.StaticMeshComponent</c>): CityHash64 of
    /// the lower-cased path with <c>.</c> and <c>:</c> replaced by <c>/</c>, tagged as a script import.
    /// </summary>
    public static PackageObjectIndex FromScriptPath(string path)
    {
        string normalized = path.Replace('.', '/').Replace(':', '/');
        return new PackageObjectIndex(((ulong)PackageObjectKind.ScriptImport << KindShift) | (CityHash.HashLowerUtf16(normalized) & IndexMask));
    }

    /// <summary>A reference to export <paramref name="index"/> of the same package.</summary>
    public static PackageObjectIndex FromExport(int index) => new((ulong)index);

    public override string ToString() => $"{Kind}:{Value & IndexMask:x}";
}
