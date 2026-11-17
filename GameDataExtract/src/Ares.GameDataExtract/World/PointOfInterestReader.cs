using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads a map's points of interest (<see cref="PointOfInterest"/>): the <c>APioneerPointOfInterestVolume</c> actors of
/// the persistent level. The map marker component (<c>UPOIMapMarkerComponent</c>) gives the loot value
/// (<c>EnemyThreatLevel</c>) and the loot themes, the ping component (<c>UPingInfoComponent</c>) the string-table key
/// of the area's name (<c>PingInformation.PingDescription</c>), <c>ParentVolume</c> the enclosing area, and the brush
/// the hulls (see <see cref="LootVolumeReader"/>; flat elements, which hold no point, are left out). A volume whose
/// loot value can change at run time (<c>bDynamicThreatLevel</c>) is not supported.
/// </summary>
internal sealed partial class PointOfInterestReader
{
    private const string VolumeClass = "APioneerPointOfInterestVolume";
    private const string MarkerClass = "UPOIMapMarkerComponent";
    private const string PingClass = "UPingInfoComponent";
    private const string PingInformationStruct = "FPingInformation";
    private const string LootValueEnum = "EPointOfInterestEnemyThreatLevel";
    private const int MaxLootThemes = 16;
    private const sbyte StringTableHistory = 11;
    private const int PingInformationLeadingMembers = 2;

    private static readonly PackageObjectIndex s_volume = PackageObjectIndex.FromScriptPath("/Script/Angelscript.PioneerPointOfInterestVolume");
    private static readonly PackageObjectIndex s_marker = PackageObjectIndex.FromScriptPath("/Script/Angelscript.POIMapMarkerComponent");
    private static readonly PackageObjectIndex s_ping = PackageObjectIndex.FromScriptPath("/Script/Angelscript.PingInfoComponent");

    private readonly SceneResolver _scenes;
    private readonly ScriptMemberChain _volume;
    private readonly ScriptMemberChain _marker;
    private readonly HashSet<int> _oneByteVolumeMembers;
    private readonly HashSet<int> _oneByteMarkerMembers;
    private readonly int _pingInformationMembers;

    /// <summary>A reader with the member chains of the script cache.</summary>
    /// <exception cref="InvalidDataException">A class, struct or enum the reader needs changed.</exception>
    public PointOfInterestReader(SceneResolver scenes, ScriptCache cache)
    {
        _scenes = scenes;
        _volume = ScriptMemberChain.Of(cache, VolumeClass, "MapMarkerComponent", "AreaAnnouncementComponent");
        ScriptMemberChain.ExpectReflected(cache, "APointOfInterestVolume", "PingInfo", "bDebug", "ParentPointOfInterest", "bConsiderForLocationPing");
        ScriptMemberChain.ExpectReflected(cache, "AHierarchicalAreaVolume", "ParentVolume", "CachedHierarchyDepth");
        _marker = ScriptMemberChain.Of(cache, MarkerClass, "MarkerVisibilityType", "MapLayer", "LootThemes", "bCenterMapMarkerInVolume", "bDynamicThreatLevel",
            "EnemyThreatLevel", "LocationImage", "DialogueTag");
        ScriptMemberChain.ExpectReflected(cache, "UClientMapMarkerComponent", "Tag", "bIsVisible");
        ScriptMemberChain ping = ScriptMemberChain.Of(cache, PingClass, "PingInformation", "PingVisible_Server", "bAlwaysAttachToRoot", "bSkipPingAttachment",
            "bKeepRelativePositionWhenAttachingToRoot", "FluffyTrackedPrimitiveComponent");
        if (ping.Index("PingInformation") != 0)
        {
            throw new InvalidDataException($"{PingClass}.PingInformation is not the first member");
        }

        string[] information = [.. cache.Class(PingInformationStruct).ReflectedProperties.Select(member => member.Name)];
        _pingInformationMembers = information.Length;
        information = information[..Math.Min(information.Length, PingInformationLeadingMembers)];
        if (!information.SequenceEqual(["DescriptionCategory", "PingDescription"], StringComparer.Ordinal))
        {
            throw new InvalidDataException($"{PingInformationStruct} members are ({string.Join(", ", information)})");
        }

        ScriptMemberChain.ExpectEnum(cache, LootValueEnum, nameof(PointOfInterestLootValue.Unknown), nameof(PointOfInterestLootValue.Low),
            nameof(PointOfInterestLootValue.Medium), nameof(PointOfInterestLootValue.High));
        if (_volume.Count != _volume.Index("CachedHierarchyDepth") + 1)
        {
            throw new InvalidDataException($"The {VolumeClass} chain has script members after AHierarchicalAreaVolume");
        }

        _oneByteVolumeMembers = [_volume.Index("bDebug"), _volume.Index("bConsiderForLocationPing")];
        _oneByteMarkerMembers = [_marker.Index("MarkerVisibilityType"), _marker.Index("MapLayer"), _marker.Index("bCenterMapMarkerInVolume"), _marker.Index("EnemyThreatLevel")];
    }

    /// <summary>The points of interest of the persistent level <paramref name="level"/>, ordered by actor name (ordinal).</summary>
    /// <exception cref="InvalidDataException">A volume, its components or its brush do not decode as expected.</exception>
    public ImmutableArray<PointOfInterest> Read(CookedPackage level)
    {
        List<(string Actor, string? Name, string? Parent, PointOfInterestLootValue Value, string[] Themes)> rows = [];
        foreach (ZenExport export in level.Exports.Where(export => export.Class == s_volume))
        {
            string actor = ExportChains.PlacedActorName(level, export) ?? throw new InvalidDataException($"{level.Header.ExportPath(export.Index)} is not a placed actor");
            try
            {
                (ZenExport marker, ZenExport? ping, string? parent) = ReadVolume(level, export);
                (PointOfInterestLootValue value, string[] themes) = ReadMarker(level, marker);
                rows.Add((actor, ping is null ? null : ReadPingName(level, ping), parent, value, themes));
            }
            catch (InvalidDataException error)
            {
                throw new InvalidDataException($"{actor}: {error.Message}", error);
            }
        }

        rows.Sort((a, b) => string.CompareOrdinal(a.Actor, b.Actor));
        HashSet<string> actors = [.. rows.Select(row => row.Actor)];
        if (rows.FirstOrDefault(row => row.Parent is not null && !actors.Contains(row.Parent)) is { Actor: { } orphan })
        {
            throw new InvalidDataException($"{orphan}: the parent volume is not a point of interest");
        }

        IReadOnlyDictionary<string, LootVolumeHulls> volumes = LootVolumeReader.Read(_scenes, level, actors, LootVolumeReader.PointOfInterestMaxElements, skipFlat: true);
        return [.. rows.Select(row => new PointOfInterest(
            row.Actor,
            row.Name,
            row.Parent,
            row.Value,
            row.Themes,
            volumes[row.Actor].Hulls is { Length: > 0 } hulls ? hulls : throw new InvalidDataException($"{row.Actor}: no hull with volume")))];
    }

    /// <summary>The marker and ping components and the parent volume of a volume actor.</summary>
    private (ZenExport Marker, ZenExport? Ping, string? Parent) ReadVolume(CookedPackage level, ZenExport export)
    {
        UnversionedHeader header = UnversionedHeader.Read(level.Data, CookedPackage.StartOf(export));
        PlacedActorValues values = new(level, export, header);
        Dictionary<int, int> stored = [];
        foreach (UnversionedMember member in header.Members.Where(member => member.Index < _volume.Count))
        {
            stored[member.Index] = !member.IsNonZero ? 0 : _oneByteVolumeMembers.Contains(member.Index) ? values.Byte() : values.Int32();
        }

        int markerValue = stored.GetValueOrDefault(_volume.Index("MapMarkerComponent"));
        if (markerValue == 0)
        {
            throw new InvalidDataException("no map marker component");
        }

        string? parent = ParentVolume(level, stored.GetValueOrDefault(_volume.Index("ParentVolume")), "ParentVolume");
        if (ParentVolume(level, stored.GetValueOrDefault(_volume.Index("ParentPointOfInterest")), "ParentPointOfInterest") != parent)
        {
            throw new InvalidDataException("ParentPointOfInterest differs from ParentVolume");
        }

        ZenExport marker = OwnExport(level, markerValue, s_marker, "MapMarkerComponent");
        int pingValue = stored.GetValueOrDefault(_volume.Index("PingInfo"));
        return (marker, pingValue != 0 ? OwnExport(level, pingValue, s_ping, "PingInfo") : null, parent);
    }

    /// <summary>The loot value and loot theme tags of a map marker component.</summary>
    private (PointOfInterestLootValue Value, string[] Themes) ReadMarker(CookedPackage level, ZenExport export)
    {
        UnversionedHeader header = UnversionedHeader.Read(level.Data, CookedPackage.StartOf(export));
        PlacedActorValues values = new(level, export, header);
        int lootValue = 0;
        string[] themes = [];
        int stop = _marker.Index("LocationImage");
        foreach (UnversionedMember member in header.Members.Where(member => member.IsNonZero))
        {
            if (member.Index >= stop)
            {
                break;
            }

            if (member.Index == _marker.Index("bDynamicThreatLevel"))
            {
                throw new InvalidDataException("bDynamicThreatLevel is set; the loot value can change at run time");
            }

            if (member.Index == _marker.Index("LootThemes"))
            {
                themes = values.GameplayTags(MaxLootThemes);
            }
            else if (_oneByteMarkerMembers.Contains(member.Index))
            {
                byte value = values.Byte();
                lootValue = member.Index == _marker.Index("EnemyThreatLevel") ? value : lootValue;
            }
        }

        return lootValue <= (int)PointOfInterestLootValue.High
            ? ((PointOfInterestLootValue)lootValue, themes)
            : throw new InvalidDataException($"loot value {lootValue}");
    }

    /// <summary>The string-table key of a ping component's <c>PingInformation.PingDescription</c>, or <see langword="null"/>.</summary>
    private string? ReadPingName(CookedPackage level, ZenExport export)
    {
        UnversionedHeader header = UnversionedHeader.Read(level.Data, CookedPackage.StartOf(export));
        if (!header.IsNonZero(0))
        {
            return null;
        }

        PlacedActorValues values = new(level, export, header);
        UnversionedHeader information = values.StructHeader(_pingInformationMembers);
        if (information.IsNonZero(0))
        {
            values.Byte();
        }

        if (!information.IsNonZero(1))
        {
            return null;
        }

        values.Int32();
        if ((sbyte)values.Byte() != StringTableHistory)
        {
            throw new InvalidDataException("the name is not a string-table text");
        }

        (uint table, uint number) = values.NameWords();
        string key = values.String();
        return number == 0 && table < level.Header.Names.Count && NameKey().IsMatch(key)
            ? key
            : throw new InvalidDataException($"unexpected name text {key}");
    }

    /// <summary>The actor name of a point-of-interest volume a member refers to, or <see langword="null"/> for none.</summary>
    private static string? ParentVolume(CookedPackage level, int value, string member)
    {
        if (value == 0)
        {
            return null;
        }

        ZenExport? target = value > 0 && value <= level.Exports.Count ? level.Exports[value - 1] : null;
        return target is not null && target.Class == s_volume && ExportChains.PlacedActorName(level, target) is { } name
            ? name
            : throw new InvalidDataException($"{member} is not a point of interest volume of the level");
    }

    private static ZenExport OwnExport(CookedPackage level, int value, PackageObjectIndex type, string member)
    {
        ZenExport? target = value > 0 && value <= level.Exports.Count ? level.Exports[value - 1] : null;
        return target is not null && target.Class == type ? target : throw new InvalidDataException($"{member} is not the expected export");
    }

    [GeneratedRegex("^[A-Z0-9_]{1,100}$", RegexOptions.CultureInvariant)]
    private static partial Regex NameKey();
}
