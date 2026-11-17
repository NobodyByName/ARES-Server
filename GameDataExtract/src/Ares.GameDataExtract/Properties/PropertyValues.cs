using Ares.GameDataExtract.Packages;

namespace Ares.GameDataExtract.Properties;

/// <summary>
/// An object reference as a property stores it: 0 none, a positive value export <c>value − 1</c> of the same package,
/// a negative value import <c>−value − 1</c> (resolve it with <see cref="PackageCache.Reference(CookedPackage, int)"/>).
/// </summary>
internal readonly record struct ObjectReference(int Value)
{
    public bool IsNull => Value == 0;
}

/// <summary>A soft object path: the package, the asset in it and an optional sub-object path.</summary>
internal sealed record SoftObjectPath(string PackageName, string AssetName, string SubPath)
{
    /// <summary>The path that points at nothing.</summary>
    public static SoftObjectPath Null { get; } = new(CookedPackage.NoneName, CookedPackage.NoneName, "");

    public bool IsNull => PackageName == CookedPackage.NoneName;

    /// <summary>The object path, for example <c>/Game/Items/DA_Item.DA_Item</c>; empty for <see cref="Null"/>.</summary>
    public string Path => IsNull ? "" : (AssetName == CookedPackage.NoneName ? PackageName : $"{PackageName}.{AssetName}") + (SubPath.Length > 0 ? ":" + SubPath : "");
}

/// <summary>How a localized text is stored.</summary>
internal enum TextHistory
{
    /// <summary>No history: at most a culture-invariant string (<see cref="TextValue.Source"/>).</summary>
    None = -1,

    /// <summary>A localizable string: namespace, key and source string.</summary>
    Base = 0,

    /// <summary>An entry of a string table: table name (<see cref="TextValue.Namespace"/>) and key.</summary>
    StringTableEntry = 11,
}

/// <summary>A localized text: its history and the strings it names.</summary>
internal sealed record TextValue(TextHistory History, string Namespace, string Key, string Source)
{
    /// <summary>The empty text.</summary>
    public static TextValue Empty { get; } = new(TextHistory.None, "", "", "");
}

/// <summary>A delegate: the object it is bound to and the function name.</summary>
internal sealed record DelegateValue(ObjectReference Object, string Function);
