using System.Collections.Immutable;
using Ares.Common.Maps;
using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads the loot container actors a map places itself (<see cref="PlacedLootContainer"/>): placed actors whose class
/// is the generated class of a <c>BP_SalvageContainer_*</c> or <c>BP_SocketContainer_*</c> Blueprint, at the world
/// transform of their root <c>Mesh</c> component. Locations are rounded to 2 decimal places, rotations to 3 (each angle
/// in (−180, 180]) and scales to 4. Read every cell with <see cref="ReadCell"/>, then <see cref="Build"/>.
/// </summary>
internal sealed class PlacedLootReader
{
    /// <summary>Name of a container actor's root component.</summary>
    public const string RootComponent = "Mesh";

    private const int LocationDigits = 2;
    private const int RotationDigits = 3;
    private const int ScaleDigits = 4;

    private static readonly string[] s_containerBlueprintPrefixes = ["BP_SalvageContainer_", "BP_SocketContainer_"];

    private readonly SceneResolver _scenes;
    private readonly string _mapPackage;
    private readonly List<PlacedLootContainer> _containers = [];

    /// <summary>A reader for the cells of map package <paramref name="mapPackage"/>.</summary>
    public PlacedLootReader(SceneResolver scenes, string mapPackage)
    {
        _scenes = scenes;
        _mapPackage = mapPackage;
    }

    /// <summary>Reads the container actors placed in <paramref name="cell"/>.</summary>
    /// <exception cref="InvalidDataException">A container actor has no root component, or its transform does not decode.</exception>
    public void ReadCell(MapCell cell, CookedPackage package)
    {
        string cellName = MapCells.Name(cell, _mapPackage);
        string? dataLayer = MapCells.DataLayer(cell);
        foreach (ZenExport actor in PlacedActors.Actors(package))
        {
            if (actor.Class.Kind != PackageObjectKind.PackageImport)
            {
                continue;
            }

            string blueprint = package.Header.ImportedPackageNames[actor.Class.ImportedPackageIndex];
            string leaf = blueprint[(blueprint.LastIndexOf('/') + 1)..];
            if (!s_containerBlueprintPrefixes.Any(prefix => leaf.StartsWith(prefix, StringComparison.Ordinal)))
            {
                continue;
            }

            string path = package.Header.ExportPath(actor.Index);
            ZenExport mesh = package.Child(actor.Index, RootComponent) ?? throw new InvalidDataException($"{path} has no {RootComponent} component");
            Transform world;
            try
            {
                world = _scenes.WorldTransform(package, mesh);
            }
            catch (InvalidDataException error)
            {
                throw new InvalidDataException($"{path}: {error.Message}", error);
            }

            _containers.Add(new PlacedLootContainer(
                cellName,
                actor.Name,
                $"{blueprint}.{leaf}_C",
                DecimalRounding.Round(world.Translation, LocationDigits),
                DecimalRounding.WrappedAngles(TransformMath.ToRotator(world.Rotation), RotationDigits),
                DecimalRounding.Round(world.Scale3D, ScaleDigits),
                dataLayer));
        }
    }

    /// <summary>The containers ordered by cell and actor name (ordinal).</summary>
    public ImmutableArray<PlacedLootContainer> Build()
    {
        return [.. _containers.OrderBy(row => row.Cell, StringComparer.Ordinal).ThenBy(row => row.Actor, StringComparer.Ordinal)];
    }
}
