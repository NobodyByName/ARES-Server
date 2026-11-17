using Ares.GameDataExtract.Packages;
using Ares.Structs.Core.Assets;

namespace Ares.GameDataExtract.Properties;

/// <summary>
/// Resolves a cooked object's properties along its archetype chain: a class default object over its super class's
/// default object, a component over the component template it was created from (a Blueprint's construction-script
/// template or a parent class default's component). Each object's block lists only its differences from its archetype,
/// so its properties are its own over the archetype's, merged (see <see cref="PropertyBag.Over"/>). The chain ends at an
/// archetype that is a script object (a native or script class default, whose values are not stored in packages).
/// Each object is read with the chain of its own class. Results are cached per object.
/// </summary>
internal sealed class ObjectDefaults
{
    private const int MaxArchetypes = 32;

    private readonly PackageCache _packages;
    private readonly Func<ExportRef, StructSchema> _schemaOf;
    private readonly Dictionary<(CookedPackage, int), PropertyBag> _resolved = [];

    /// <summary>Resolves objects of <paramref name="packages"/>, reading each with the chain <paramref name="schemaOf"/> gives for it.</summary>
    public ObjectDefaults(PackageCache packages, Func<ExportRef, StructSchema> schemaOf)
    {
        _packages = packages;
        _schemaOf = schemaOf;
    }

    /// <summary>The archetype chain of <paramref name="target"/>: the object itself, then each archetype stored in packages.</summary>
    /// <exception cref="InvalidDataException">An archetype cannot be resolved, or the chain is too long.</exception>
    public IReadOnlyList<ExportRef> Archetypes(ExportRef target)
    {
        List<ExportRef> chain = [target];
        while (Archetype(chain[^1]) is { } archetype)
        {
            if (chain.Count >= MaxArchetypes)
            {
                throw new InvalidDataException($"{target.Path}: the archetype chain is too long");
            }

            chain.Add(archetype);
        }

        return chain;
    }

    /// <summary>The properties of <paramref name="target"/> over those of its archetypes.</summary>
    /// <exception cref="InvalidDataException">An object of the chain cannot be read with its class's chain.</exception>
    public PropertyBag Resolve(ExportRef target)
    {
        IReadOnlyList<ExportRef> chain = Archetypes(target);
        PropertyBag? merged = null;
        for (int i = chain.Count - 1; i >= 0; i--)
        {
            merged = Resolve(chain[i], merged);
        }

        return merged ?? throw new InvalidOperationException("An archetype chain holds at least the object itself");
    }

    /// <summary>The properties of <paramref name="current"/> over <paramref name="inherited"/>, its archetype's resolved ones.</summary>
    private PropertyBag Resolve(ExportRef current, PropertyBag? inherited)
    {
        if (_resolved.TryGetValue((current.Package, current.Export.Index), out PropertyBag? bag))
        {
            return bag;
        }

        PropertyBag own = PropertyReader.Read(current.Package, CookedPackage.StartOf(current.Export), _schemaOf(current), out _);
        bag = inherited is null ? own : own.Over(inherited);
        _resolved[(current.Package, current.Export.Index)] = bag;
        return bag;
    }

    private ExportRef? Archetype(ExportRef target)
    {
        return target.Export.Template.Kind is PackageObjectKind.Null or PackageObjectKind.ScriptImport
            ? null
            : _packages.Resolve(target.Package, target.Export.Template) ?? throw new InvalidDataException($"{target.Path}: the archetype cannot be resolved");
    }
}
