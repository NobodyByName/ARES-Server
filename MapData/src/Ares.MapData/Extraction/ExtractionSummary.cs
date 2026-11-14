using System.Globalization;
using System.Text.RegularExpressions;
using Ares.Structs.Game.Collision;

namespace Ares.MapData.Extraction;

/// <summary>Counts of one component class during an extraction.</summary>
internal sealed class ComponentCounts
{
    /// <summary>Components read.</summary>
    public int Components { get; set; }

    /// <summary>Components (instanced classes: instances) whose collision blocks the player or vaulting.</summary>
    public int Blocking { get; set; }

    /// <summary>Placements written.</summary>
    public int Placements { get; set; }

    /// <summary>Blocking components (or instances) left out because their body has no geometry.</summary>
    public int WithoutGeometry { get; set; }
}

/// <summary>Components of one failure category that could not be placed.</summary>
internal sealed class FailureCounts
{
    /// <summary>Components with this failure.</summary>
    public int Components { get; set; }

    /// <summary>Of those, components whose collision settings decode and block something with a non-empty body.</summary>
    public int WouldBlock { get; set; }

    /// <summary>Of those, components whose collision settings or body do not decode either.</summary>
    public int Unknown { get; set; }
}

/// <summary>What an extraction read, wrote and could not decode.</summary>
internal sealed partial class ExtractionSummary
{
    private readonly SortedDictionary<string, FailureCounts> _failures = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, int> _surfaceFailures = new(StringComparer.Ordinal);
    private readonly SortedDictionary<PhysicalSurfaceType, int> _surfaces = [];

    /// <summary>Counts per component class, by class name.</summary>
    private readonly SortedDictionary<string, ComponentCounts> _classes = new(StringComparer.Ordinal);

    /// <summary>Cells read (the persistent level included).</summary>
    public int Cells { get; set; }

    /// <summary>Cells that belong to the base map.</summary>
    public int BaseMapCells { get; set; }

    /// <summary>Landscape height-field tiles written.</summary>
    public int HeightFields { get; set; }

    /// <summary>Distinct bodies written.</summary>
    public int Bodies { get; set; }

    /// <summary>Static mesh components that show no mesh.</summary>
    public int WithoutMesh { get; set; }

    /// <summary>Instanced components whose override materials are stored but not read (their mesh's materials apply).</summary>
    public int InstancedOverrideMaterialsNotRead { get; set; }

    /// <summary>Total placements written.</summary>
    private int Placements => _classes.Values.Sum(counts => counts.Placements);

    /// <summary>The counts of a component class, created on first use.</summary>
    public ComponentCounts Class(string name)
    {
        if (_classes.TryGetValue(name, out ComponentCounts? counts))
        {
            return counts;
        }

        counts = new ComponentCounts();
        _classes.Add(name, counts);
        return counts;
    }

    /// <summary>Records a component that could not be placed, with what its collision would have been when known.</summary>
    /// <param name="reason">Why it failed.</param>
    /// <param name="wouldBlock">Whether it would have blocked something, or <see langword="null"/> when that is not known.</param>
    public void AddFailure(string reason, bool? wouldBlock)
    {
        string category = AssetPath().Replace(reason, "<asset>");
        if (!_failures.TryGetValue(category, out FailureCounts? counts))
        {
            counts = new FailureCounts();
            _failures.Add(category, counts);
        }

        counts.Components++;
        if (wouldBlock is null)
        {
            counts.Unknown++;
        }
        else if (wouldBlock.Value)
        {
            counts.WouldBlock++;
        }
    }

    /// <summary>Counts a component (instanced: a component of instances) whose surface type was decoded.</summary>
    public void AddSurface(PhysicalSurfaceType surface) => _surfaces[surface] = _surfaces.GetValueOrDefault(surface) + 1;

    /// <summary>Records a component whose surface type could not be decoded; it gets the default surface type.</summary>
    public void AddSurfaceFailure(string reason)
    {
        string category = AssetPath().Replace(reason, "<asset>");
        _surfaceFailures[category] = _surfaceFailures.GetValueOrDefault(category) + 1;
    }

    /// <summary>Writes the summary for operators.</summary>
    public void Write(TextWriter output)
    {
        CultureInfo culture = CultureInfo.InvariantCulture;
        output.WriteLine(string.Create(culture, $"Cells: {Cells:N0} ({BaseMapCells:N0} base map, {Cells - BaseMapCells:N0} in data layers)"));
        output.WriteLine(string.Create(culture, $"Landscape tiles: {HeightFields:N0}"));
        output.WriteLine(string.Create(culture, $"Placements: {Placements:N0} of {Bodies:N0} bodies"));
        foreach ((string name, ComponentCounts counts) in _classes)
        {
            string missing = counts.WithoutGeometry > 0 ? string.Create(culture, $", {counts.WithoutGeometry:N0} without geometry") : "";
            output.WriteLine(string.Create(culture,
                $"  {name,-22} {counts.Placements,9:N0} placed  ({counts.Blocking:N0} blocking of {counts.Components:N0} read{missing})"));
        }

        if (WithoutMesh > 0)
        {
            output.WriteLine(string.Create(culture, $"Static mesh components without a mesh: {WithoutMesh:N0}"));
        }

        output.WriteLine(string.Create(culture, $"Surface types (components): {string.Join(", ", _surfaces.Select(entry => $"{entry.Key} {entry.Value:N0}"))}"));
        if (InstancedOverrideMaterialsNotRead > 0)
        {
            output.WriteLine(string.Create(culture, $"Instanced components with override materials not read: {InstancedOverrideMaterialsNotRead:N0}"));
        }

        if (_surfaceFailures.Count > 0)
        {
            output.WriteLine(string.Create(culture, $"Surface types not decoded (default used): {_surfaceFailures.Values.Sum():N0} components"));
            foreach ((string category, int count) in _surfaceFailures.OrderByDescending(entry => entry.Value))
            {
                output.WriteLine(string.Create(culture, $"  {count,6:N0}  {category}"));
            }
        }

        if (_failures.Count == 0)
        {
            return;
        }

        output.WriteLine(string.Create(culture,
            $"Components not decoded: {_failures.Values.Sum(counts => counts.Components):N0} (would block / not known)"));
        foreach ((string category, FailureCounts counts) in _failures.OrderByDescending(entry => entry.Value.Components))
        {
            output.WriteLine(string.Create(culture, $"  {counts.Components,6:N0}  {counts.WouldBlock:N0} / {counts.Unknown:N0}  {category}"));
        }
    }

    [GeneratedRegex(@"/(?!Script/)[A-Za-z0-9_]+/\S+")]
    private static partial Regex AssetPath();
}
