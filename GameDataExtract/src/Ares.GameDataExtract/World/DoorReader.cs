using System.Collections.Immutable;
using Ares.GameDataExtract.Packages;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads the regular doors placed in a map's generated cells (<see cref="DoorPlacement"/>): actors of a
/// <see cref="DoorClasses.Blueprints"/> class named after it (actors of the class placed under another name are puzzle
/// doors), each with both <see cref="DoorClasses.InteractionComponents"/>. A door's
/// start flags (<see cref="DoorStartFlagMembers"/>) are the ones its header or its Blueprint defaults' headers list,
/// then the class defaults (<see cref="DoorStartFlagMembers.Defaults"/>). Read every cell with <see cref="ReadCell"/>,
/// then <see cref="Build"/>.
/// </summary>
internal sealed class DoorReader
{
    private const char NameSeparator = '_';

    private static readonly HashSet<string> s_packages = [.. DoorClasses.Blueprints.Select(path => path[..path.LastIndexOf('.')])];

    private static readonly string[] s_flags = [.. DoorStartFlagMembers.Defaults.Select(flag => flag.Member)];

    private static readonly Dictionary<string, bool> s_defaults = DoorStartFlagMembers.Defaults.ToDictionary(flag => flag.Member, flag => flag.Default, StringComparer.Ordinal);

    private readonly MapRuleSources _sources;
    private readonly List<DoorPlacement> _doors = [];

    /// <summary>A reader over <paramref name="sources"/>.</summary>
    /// <exception cref="InvalidDataException">The door script's interaction-state enum differs.</exception>
    public DoorReader(MapRuleSources sources)
    {
        _sources = sources;
        sources.CheckEnum(DoorClasses.InteractionState);
    }

    /// <summary>Reads the regular doors placed in the cell package <paramref name="cell"/>.</summary>
    /// <exception cref="InvalidDataException">A door lacks a button, or its start flags do not decode.</exception>
    public void ReadCell(string cell, CookedPackage package)
    {
        foreach (ZenExport actor in PlacedActors.Actors(package))
        {
            if (DoorClass(package, actor.Class) is not { } type || !actor.Name.StartsWith(type[(type.LastIndexOf('.') + 1)..] + NameSeparator, StringComparison.Ordinal))
            {
                continue;
            }

            string path = package.Header.ExportPath(actor.Index);
            if (DoorClasses.InteractionComponents.Any(component => package.Child(actor.Index, component) is null))
            {
                throw new InvalidDataException($"{path}: a regular door without both open/close buttons");
            }

            IReadOnlyDictionary<string, bool> flags;
            try
            {
                flags = _sources.Flags(new ExportRef(package, actor), s_flags);
            }
            catch (InvalidDataException error)
            {
                throw new InvalidDataException($"{path}: {error.Message}", error);
            }

            _doors.Add(new DoorPlacement(cell, actor.Name)
            {
                StartAsOpened = Flag(flags, DoorStartFlagMembers.StartAsOpened),
                StartAsLocked = Flag(flags, DoorStartFlagMembers.StartAsLocked),
                StartAsSealed = Flag(flags, DoorStartFlagMembers.StartAsSealed),
                StartAsPowered = Flag(flags, DoorStartFlagMembers.StartAsPowered),
                BreachOpensDoor = Flag(flags, DoorStartFlagMembers.BreachOpensDoor),
            });
        }
    }

    /// <summary>The doors, ordered by cell and actor name (ordinal).</summary>
    public ImmutableArray<DoorPlacement> Build() =>
        [.. _doors.OrderBy(door => door.Cell, StringComparer.Ordinal).ThenBy(door => door.Actor, StringComparer.Ordinal)];

    private static bool Flag(IReadOnlyDictionary<string, bool> flags, string name) => flags.GetValueOrDefault(name, s_defaults[name]);

    /// <summary>The regular door class <paramref name="index"/> names, or <see langword="null"/>.</summary>
    private string? DoorClass(CookedPackage package, PackageObjectIndex index)
    {
        return index.Kind == PackageObjectKind.PackageImport
            && index.ImportedPackageIndex < package.Header.ImportedPackageNames.Count
            && s_packages.Contains(package.Header.ImportedPackageNames[index.ImportedPackageIndex])
            && _sources.Packages.Resolve(package, index) is { } type
            && DoorClasses.Blueprints.Contains(type.Path, StringComparer.Ordinal)
                ? type.Path
                : null;
    }
}
