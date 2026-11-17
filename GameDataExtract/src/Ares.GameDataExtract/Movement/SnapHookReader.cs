using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Game.Movement;

namespace Ares.GameDataExtract.Movement;

/// <summary>
/// Reads the snap hook item Blueprint's class default (<see cref="MovementAssets.SnapHookBlueprint"/>): its header must
/// list exactly the members of <see cref="MovementAssetSchemas.SnaphookItemChain"/> it is known to set, at the chain
/// indices the script cache's reflected chain of <c>USnaphookItem</c> gives them, and its item data asset must be
/// <see cref="MovementAssets.SnapHookDataAsset"/>.
/// </summary>
internal static class SnapHookReader
{
    private const string ScriptClass = "USnaphookItem";

    /// <summary>Reads the class default.</summary>
    /// <exception cref="InvalidDataException">The class default sets other members, does not decode or names another data asset.</exception>
    public static SnapHookItemDefaults Read(PackageCache packages, ScriptCache scripts)
    {
        ScriptSchemaCheck.Chain(scripts, ScriptClass, MovementAssetSchemas.SnaphookItemChain);
        CookedPackage package = MovementPackages.Load(packages, MovementAssets.SnapHookBlueprint);
        ZenExport defaults = MovementPackages.Named(package, $"Default__{MovementPackages.ShortName(MovementAssets.SnapHookBlueprint)}_C");
        UnversionedHeader header = UnversionedHeader.Read(package.Data, CookedPackage.StartOf(defaults));
        if (!header.Members.Where(member => member.IsNonZero).Select(member => member.Index).SequenceEqual(MovementAssetSchemas.SnaphookItemStoredMembers)
            || !header.Members.Where(member => !member.IsNonZero).Select(member => member.Index).SequenceEqual(MovementAssetSchemas.SnaphookItemZeroMembers))
        {
            throw new InvalidDataException($"{package.Name}: unexpected class default header {string.Join(", ", header.Members.Select(member => member.Index))}");
        }

        PropertyBag values = PropertyReader.ReadExport(package, defaults, MovementAssetSchemas.SnaphookItemChain, MovementPackages.Trailer);
        string? dataAsset = MovementPackages.ImportedPackage(package, values.Get<ObjectReference>("ItemDataAsset"), "the item data asset");
        return dataAsset == MovementAssets.SnapHookDataAsset
            ? new SnapHookItemDefaults(dataAsset, values.Get<double>("SnaphookRange"), values.Get<double>("ActivationCost"))
            : throw new InvalidDataException($"{package.Name}: item data asset {dataAsset}");
    }
}
