namespace Ares.MapData.Packages;

/// <summary>Package ids: the id of a package's chunks in the I/O store containers.</summary>
internal static class PackageId
{
    /// <summary>The id of the package at <paramref name="packagePath"/> (for example <c>/Game/Maps/Example</c>): CityHash64 of its lower-cased path.</summary>
    public static ulong FromPath(string packagePath) => CityHash.HashLowerUtf16(packagePath);
}
