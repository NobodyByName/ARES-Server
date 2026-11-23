using System.Collections.Immutable;
using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads the AI spline paths placed in a map's persistent level and generated cells (<see cref="SplinePathPlacement"/>):
/// actors of <see cref="SplinePathClasses.Blueprint"/>, each with its tags and world points (from the nearest object of
/// its archetype chain that sets them) and whether its <see cref="SplinePathClasses.SplineComponent"/> is a closed loop
/// (from the nearest object of the component's template chain that lists the flag). Read every level with
/// <see cref="ReadLevel"/>, then <see cref="Build"/>.
/// </summary>
internal sealed class SplinePathReader
{
    private static readonly string s_package = SplinePathClasses.Blueprint[..SplinePathClasses.Blueprint.LastIndexOf('.')];

    private static readonly PackageObjectIndex s_splineComponent = PackageObjectIndex.FromScriptPath(SplineComponentMembers.ClassPath);

    private readonly MapRuleSources _sources;
    private readonly SceneResolver _scenes;
    private readonly List<SplinePathPlacement> _paths = [];

    /// <summary>A reader over <paramref name="sources"/> that follows component templates with <paramref name="scenes"/>.</summary>
    public SplinePathReader(MapRuleSources sources, SceneResolver scenes)
    {
        _sources = sources;
        _scenes = scenes;
    }

    /// <summary>Reads the spline paths placed in the level package <paramref name="package"/> (the map package or a cell package).</summary>
    /// <exception cref="InvalidDataException">A path has no spline or no point, or its tags or points do not decode.</exception>
    public void ReadLevel(CookedPackage package)
    {
        foreach (ZenExport actor in PlacedActors.Actors(package).Where(actor => IsPath(package, actor.Class)))
        {
            string path = package.Header.ExportPath(actor.Index);
            try
            {
                ZenExport spline = package.Child(actor.Index, SplinePathClasses.SplineComponent) is { } found && found.Class == s_splineComponent
                    ? found
                    : throw new InvalidDataException($"The path has no {SplinePathClasses.SplineComponent} spline component");
                PropertyBag members = _sources.Members(new ExportRef(package, actor), SplinePathClasses.TagsMember, SplinePathClasses.PointsMember);
                ImmutableArray<Vector3D> points = [.. members.Get<object[]>(SplinePathClasses.PointsMember, []).Cast<Vector3D>()];
                _paths.Add(new SplinePathPlacement(actor.Name, IsClosedLoop(package, spline))
                {
                    Tags = [.. members.Get<string[]>(SplinePathClasses.TagsMember, [])],
                    Points = points.Length > 0 ? points : throw new InvalidDataException("The path has no point"),
                });
            }
            catch (InvalidDataException error)
            {
                throw new InvalidDataException($"{path}: {error.Message}", error);
            }
        }
    }

    /// <summary>The paths, ordered by name (ordinal).</summary>
    /// <exception cref="InvalidDataException">Two levels place a path of the same name.</exception>
    public ImmutableArray<SplinePathPlacement> Build()
    {
        return _paths.GroupBy(path => path.Name, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1) is { } duplicate
            ? throw new InvalidDataException($"{duplicate.Count()} levels place a spline path named {duplicate.Key}")
            : [.. _paths.OrderBy(path => path.Name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// The closed-loop flag of the spline component, as the nearest object of its template chain (the component itself
    /// first) that lists it stores it; false when none does.
    /// </summary>
    /// <exception cref="InvalidDataException">A template cannot be resolved or the chain is too long.</exception>
    private bool IsClosedLoop(CookedPackage package, ZenExport spline)
    {
        ExportRef? current = new ExportRef(package, spline);
        for (int depth = 0; depth <= SceneResolver.MaxTemplateDepth; depth++)
        {
            if (current is not { } found)
            {
                return false;
            }

            UnversionedHeader header = UnversionedHeader.Read(found.Package.Data, CookedPackage.StartOf(found.Export));
            if (header.Contains(SplineComponentMembers.ClosedLoop))
            {
                return header.IsNonZero(SplineComponentMembers.ClosedLoop);
            }

            current = _scenes.Template(found.Package, found.Export);
        }

        throw new InvalidDataException("The spline's template chain is too long");
    }

    /// <summary>Whether <paramref name="index"/> names the spline path class.</summary>
    private bool IsPath(CookedPackage package, PackageObjectIndex index)
    {
        return index.Kind == PackageObjectKind.PackageImport
            && index.ImportedPackageIndex < package.Header.ImportedPackageNames.Count
            && package.Header.ImportedPackageNames[index.ImportedPackageIndex] == s_package
            && _sources.Packages.Resolve(package, index) is { Path: SplinePathClasses.Blueprint };
    }
}
