using System.Collections.Immutable;
using Ares.Common.Maps;
using Ares.MapData.Components;
using Ares.MapData.Packages;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Bodies;

/// <summary>The collision of one asset: a static mesh's body setup, or a body setup a component owns.</summary>
/// <param name="Path">Object path of the static mesh or body setup.</param>
/// <param name="Body">The shapes a moving character collides with, or <see langword="null"/> when the asset has none.</param>
/// <param name="TraceFlag">Which collision the body offers to simple and complex queries.</param>
/// <param name="DefaultInstance">Collision settings of components that use the body's defaults, or <see langword="null"/>
/// when the body setup stores none (the class defaults apply) or the asset has no body setup.</param>
internal sealed record AssetBody(string Path, CollisionBody? Body, CollisionTraceFlag TraceFlag, BodyInstanceValues? DefaultInstance)
{
    /// <summary>Whether the asset has no collision geometry; placements of it collide with nothing.</summary>
    public bool IsEmpty => Body is null;
}

/// <summary>
/// The collision bodies of the assets one extraction places, each decoded once. A body keeps the simple shapes (boxes,
/// spheres, capsules and the cooked convex hulls, which already include each element's transform) unless its trace flag
/// is <see cref="CollisionTraceFlag.ComplexAsSimple"/>, in which case it keeps only the cooked triangle meshes. A body
/// whose trace flag is <see cref="CollisionTraceFlag.UseDefault"/> takes the project's default shape complexity.
/// Mirrored hulls are not kept: placements whose scale flips handedness mirror the hulls through their scale.
/// </summary>
internal sealed class BodyLibrary
{
    private static readonly PackageObjectIndex s_bodySetup = PackageObjectIndex.FromScriptPath(CollisionObjectClasses.BodySetup);
    private static readonly PackageObjectIndex s_staticMesh = PackageObjectIndex.FromScriptPath(CollisionObjectClasses.StaticMesh);

    private readonly PackageCache _packages;
    private readonly CollisionTraceFlag _defaultShapeComplexity;
    private readonly Dictionary<string, AssetBody> _meshes = new(StringComparer.Ordinal);
    private readonly Dictionary<(CookedPackage Package, int Export), AssetBody> _cellBodies = [];

    /// <summary>
    /// A library over the packages of <paramref name="packages"/>, in which bodies whose trace flag is
    /// <see cref="CollisionTraceFlag.UseDefault"/> offer <paramref name="defaultShapeComplexity"/>.
    /// </summary>
    public BodyLibrary(PackageCache packages, CollisionTraceFlag defaultShapeComplexity)
    {
        _packages = packages;
        _defaultShapeComplexity = defaultShapeComplexity;
    }

    /// <summary>The body of the static mesh at object path <paramref name="meshPath"/> (for example <c>/Game/Meshes/Rock.Rock</c>).</summary>
    /// <exception cref="InvalidDataException">The mesh does not exist, has more than one body setup, or its body setup cannot be decoded.</exception>
    public AssetBody GetMeshBody(string meshPath)
    {
        if (_meshes.TryGetValue(meshPath, out AssetBody? body))
        {
            return body;
        }

        body = ReadMeshBody(meshPath);
        _meshes[meshPath] = body;
        return body;
    }

    /// <summary>The body of a body setup export a component owns (a spline mesh's, a brush's or a model's).</summary>
    /// <exception cref="InvalidDataException">The export is not a body setup or cannot be decoded.</exception>
    public AssetBody GetCellBody(ExportRef bodySetup)
    {
        if (_cellBodies.TryGetValue((bodySetup.Package, bodySetup.Export.Index), out AssetBody? body))
        {
            return body;
        }

        if (bodySetup.Export.Class != s_bodySetup)
        {
            throw new InvalidDataException($"{bodySetup.Path} is not a body setup");
        }

        body = FromContents(bodySetup.Path, BodySetupReader.Read(bodySetup.Package, bodySetup.Export), _defaultShapeComplexity);
        _cellBodies[(bodySetup.Package, bodySetup.Export.Index)] = body;
        return body;
    }

    /// <summary>
    /// The asset body of decoded body setup <paramref name="contents"/>, named <paramref name="path"/>; a trace flag of
    /// <see cref="CollisionTraceFlag.UseDefault"/> stands for <paramref name="defaultShapeComplexity"/>.
    /// </summary>
    public static AssetBody FromContents(string path, BodySetupContents contents, CollisionTraceFlag defaultShapeComplexity)
    {
        CollisionTraceFlag traceFlag = contents.TraceFlag == CollisionTraceFlag.UseDefault ? defaultShapeComplexity : contents.TraceFlag;
        return new AssetBody(path, Shapes(path, contents, traceFlag), traceFlag, contents.DefaultInstance);
    }

    /// <summary>The shapes of a body the character collides with, or <see langword="null"/> when there are none.</summary>
    private static CollisionBody? Shapes(string name, BodySetupContents contents, CollisionTraceFlag traceFlag)
    {
        CollisionBody body;
        if (traceFlag == CollisionTraceFlag.ComplexAsSimple)
        {
            body = new CollisionBody(name, [], [], [], [], contents.Cooked?.TriangleMeshes ?? []);
        }
        else
        {
            ImmutableArray<ConvexHull> hulls = contents.Cooked is { } cooked
                ? [.. cooked.ConvexHulls.OfType<ConvexHull>()]
                : [];
            body = new CollisionBody(name, contents.Boxes, contents.Spheres, contents.Capsules, hulls, []);
        }

        return body.Boxes.IsEmpty && body.Spheres.IsEmpty && body.Capsules.IsEmpty && body.ConvexHulls.IsEmpty && body.TriangleMeshes.IsEmpty
            ? null
            : body;
    }

    private AssetBody ReadMeshBody(string meshPath)
    {
        int dot = meshPath.IndexOf('.', meshPath.LastIndexOf('/') + 1);
        if (dot < 0)
        {
            throw new InvalidDataException($"{meshPath} is not an object path");
        }

        CookedPackage package = _packages.Load(meshPath[..dot]) ?? throw new InvalidDataException($"{meshPath[..dot]} is not in the game archives");
        ZenExport mesh = package.Exports.FirstOrDefault(export => export.Class == s_staticMesh && package.Header.ExportPath(export.Index) == meshPath)
            ?? throw new InvalidDataException($"{package.Name} has no static mesh {meshPath}");
        ZenExport[] bodySetups = [.. package.ChildrenOf(mesh.Index).Where(child => child.Class == s_bodySetup)];
        return bodySetups.Length switch
        {
            0 => new AssetBody(meshPath, null, CollisionTraceFlag.UseDefault, null),
            1 => FromContents(meshPath, BodySetupReader.Read(package, bodySetups[0]), _defaultShapeComplexity),
            _ => throw new InvalidDataException($"{meshPath} has {bodySetups.Length} body setups"),
        };
    }
}
