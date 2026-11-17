using System.Collections.Immutable;
using System.Globalization;
using Ares.GameDataExtract.Animations;
using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;

namespace Ares.GameDataExtract.World;

/// <summary>The extraction tables of a map (see <see cref="ExtractionReader"/>).</summary>
/// <param name="Elevator">What the elevators share.</param>
/// <param name="Buttons">The buttons of each elevator, in <see cref="ExtractionClasses.Buttons"/> order.</param>
/// <param name="Points">The placed elevators, ordered by actor name (ordinal).</param>
internal sealed record ExtractionData(ExtractionElevator Elevator, ImmutableArray<ExtractionButton> Buttons, ImmutableArray<ExtractionPointPlacement> Points);

/// <summary>
/// Reads the elevator extraction points of a map's persistent level: the placed elevators (actors of
/// <see cref="ExtractionClasses.Elevator"/>) with their data asset and root transform, the buttons and the zone trigger
/// of the elevator Blueprint, the timings of the settings asset it names, the length of its door animation and the
/// script defaults the server needs.
/// </summary>
internal static class ExtractionReader
{
    private const string SceneComponentClass = "USceneComponent";
    private const string ScriptGlobalSeparator = "_";
    private const string TagSeparator = ".";
    private const double MaxCoordinate = 1e7;
    private const double MaxLevelAngle = 1e-6;
    private const string SingleDigits = "G7";
    private const int CurveTrailer = 8;
    private const int TracksIndex = 0;
    private const int PlaySpeedIndex = 1;
    private const string AudioFolder = "/Game/Pioneer/Audio/";
    private const double MaxPlaySpeed = 100;

    /// <summary>The extraction tables of map package <paramref name="mapPackage"/>.</summary>
    /// <exception cref="InvalidDataException">An elevator, the Blueprint, an asset or a script default does not decode as expected.</exception>
    public static ExtractionData Read(MapRuleSources sources, SceneResolver scenes, string mapPackage, MapWorldObjects world)
    {
        sources.CheckEnum(ExtractionClasses.StateEnum);
        sources.CheckEnum(new EnumDefinition(
            ScriptSchemasPath(ExtractionClasses.InteractionTypeEnum), Enum.GetValues<StandardInteractionType>().Length, Enum.GetNames<StandardInteractionType>()));
        string blueprintDefault = ExtractionClasses.Elevator.Archetype[(ExtractionClasses.Elevator.Archetype.LastIndexOf('.') + 1)..];
        ExportRef classDefault = sources.Export(ExtractionClasses.ElevatorPackage, "." + blueprintDefault);
        ExtractionElevator elevator = Elevator(sources, scenes, classDefault, blueprintDefault);
        ImmutableArray<ExtractionButton> buttons = Buttons(sources, blueprintDefault);
        ImmutableArray<ExtractionPointPlacement> points = Points(sources, scenes, mapPackage, world);
        return new ExtractionData(elevator, buttons, points);
    }

    private static ExtractionElevator Elevator(MapRuleSources sources, SceneResolver scenes, ExportRef classDefault, string blueprintDefault)
    {
        ObjectReference settingsReference = sources.Members(classDefault, ExtractionMembers.SettingsDataAsset).Get<ObjectReference>(ExtractionMembers.SettingsDataAsset);
        ExportRef settings = sources.Packages.Reference(classDefault.Package, settingsReference.Value)
            ?? throw new InvalidDataException($"{classDefault.Path} names no settings asset");
        if (UnversionedHeader.Read(settings.Package.Data, CookedPackage.StartOf(settings.Export)).Members.Count != 0)
        {
            throw new InvalidDataException($"{settings.Path} overrides its script defaults");
        }

        (_, string settingsClass) = sources.ScriptClassOf(settings.Package, settings.Export.Class, settings.Path);
        double[] timings = [.. ExtractionMembers.Settings.Select(member => Number(sources.Default(settingsClass, member)))];
        ExportRef zone = sources.Export(ExtractionClasses.ElevatorPackage, $".{blueprintDefault}:{ExtractionMembers.ExtractionZoneTrigger}");
        SceneMembers zoneScene = scenes.Scene(zone.Package, zone.Export);
        if (zoneScene is not { Location: { } zoneLocation, Rotation: null, Scale: null })
        {
            throw new InvalidDataException($"{zone.Path}: the zone trigger template sets no location, or a rotation or scale");
        }

        return new ExtractionElevator(
            ExtractionType(sources),
            settings.Package.Name,
            new ExtractionSettings(timings[0], timings[1], timings[2], timings[3], timings[4]),
            BoxExtent(zone),
            new Vector3D(zoneLocation.X + 0.0, zoneLocation.Y + 0.0, zoneLocation.Z + 0.0),
            new Rotator(0.0, 0.0, 0.0),
            sources.Default(ExtractionMembers.StandardInteractionClass, ExtractionMembers.InteractionServerHoldStartedTime).AsDouble,
            sources.Default(ExtractionMembers.ExtractionInfoStruct, ExtractionMembers.ExtractionStartedTS).AsDouble,
            DoorsSeconds(sources, blueprintDefault));
    }

    /// <summary>
    /// The doors' open or close time: the latest key of the curve assets the door animation's tracks refer to, as a
    /// float, over the animation class's script default play speed. The template sets its tracks but not its play speed.
    /// The tracks hold a native struct of unknown layout, so the curves are the packages the tracks' bytes refer to
    /// (four-byte package import references), audio left out; each must be a rich-curve asset.
    /// </summary>
    private static double DoorsSeconds(MapRuleSources sources, string blueprintDefault)
    {
        string[] reflected = [.. sources.Scripts.Class(ExtractionMembers.AnimationComponentClass).ReflectedProperties.Take(2).Select(member => member.Name)];
        if (!reflected.SequenceEqual([ExtractionMembers.AnimationTracks, ExtractionMembers.StandardPlaySpeed], StringComparer.Ordinal))
        {
            throw new InvalidDataException($"{ExtractionMembers.AnimationComponentClass} members start ({string.Join(", ", reflected)})");
        }

        double speed = sources.Default(ExtractionMembers.AnimationComponentClass, ExtractionMembers.StandardPlaySpeed).AsDouble;
        if (speed is not (> 0 and <= MaxPlaySpeed))
        {
            throw new InvalidDataException($"Unexpected {ExtractionMembers.AnimationComponentClass} play speed {speed}");
        }

        ExportRef doors = sources.Export(ExtractionClasses.ElevatorPackage, $".{blueprintDefault}:{ExtractionMembers.DoorAnimation}");
        CookedPackage package = doors.Package;
        int start = CookedPackage.StartOf(doors.Export);
        UnversionedHeader header = UnversionedHeader.Read(package.Data, start);
        if (!header.IsNonZero(TracksIndex) || header.Contains(PlaySpeedIndex) || ExportBytes.Int32(package.Data, header.ValuesOffset) <= 0)
        {
            throw new InvalidDataException($"{doors.Path} has no tracks, or its own play speed");
        }

        ReadOnlySpan<byte> body = package.Data.AsSpan(header.ValuesOffset, start + (int)doors.Export.SerialSize - header.ValuesOffset);
        SortedSet<string> curves = new(StringComparer.Ordinal);
        for (int at = 0; at + sizeof(int) <= body.Length; at++)
        {
            if (ImportedPackage(package, ExportBytes.Int32(body, at)) is { } name && !name.StartsWith(AudioFolder, StringComparison.Ordinal))
            {
                curves.Add(name);
            }
        }

        if (curves.Count == 0)
        {
            throw new InvalidDataException($"{doors.Path} refers to no curve");
        }

        float latest = 0;
        foreach (string name in curves)
        {
            CookedPackage curve = sources.Load(name);
            latest = curve.Exports.Count == 1
                ? Math.Max(latest, CurveKeyReader.LatestKeyTime(curve, curve.Exports[0], CurveTrailer))
                : throw new InvalidDataException($"{name}: expected one curve export, found {curve.Exports.Count}");
        }

        return Single(latest) / speed;
    }

    /// <summary>The package a reference value of <paramref name="package"/> imports, when it is a package import.</summary>
    private static string? ImportedPackage(CookedPackage package, int value)
    {
        return package.IsImportReference(value) && package.Header.Imports[-value - 1] is { Kind: PackageObjectKind.PackageImport } import
            ? package.Header.ImportedPackageNames[import.ImportedPackageIndex]
            : null;
    }

    /// <summary>The extraction type tag the elevator's default statements assign last (a tag global, named with <c>_</c> for <c>.</c>).</summary>
    private static string ExtractionType(MapRuleSources sources)
    {
        int offset = sources.Offset(ExtractionMembers.ExtractionPointBaseClass, ExtractionMembers.ExtractionType);
        string[] globals =
        [
            .. ScriptPatterns.DefaultFunctions(sources.Scripts, ExtractionClasses.ScriptClass)
                .SelectMany(function => ScriptPatterns.GlobalsAssignedTo(sources.Scripts, function, offset)),
        ];
        return globals.Length > 0
            ? globals[^1].Replace(ScriptGlobalSeparator, TagSeparator, StringComparison.Ordinal)
            : throw new InvalidDataException($"The script defaults of {ExtractionClasses.ScriptClass} assign no extraction type");
    }

    /// <summary>The box extent a zone trigger template stores first (<see cref="BoxComponentMembers.BoxExtent"/>).</summary>
    private static Vector3D BoxExtent(ExportRef zone)
    {
        UnversionedHeader header = UnversionedHeader.Read(zone.Package.Data, CookedPackage.StartOf(zone.Export));
        return header.Members is [{ Index: BoxComponentMembers.BoxExtent, IsNonZero: true }, ..]
            ? zone.Package.ReadVector(header.ValuesOffset)
            : throw new InvalidDataException($"{zone.Path}: the zone trigger template sets no box extent");
    }

    /// <summary>The elevator buttons: start state and prompt from the elevator's default statements, interaction settings from the Blueprint's templates.</summary>
    private static ImmutableArray<ExtractionButton> Buttons(MapRuleSources sources, string blueprintDefault)
    {
        Dictionary<int, byte> enabled = [];
        Dictionary<int, string> prompts = [];
        int startEnabled = sources.Offset(ExtractionMembers.BaseInteractionClass, ExtractionMembers.StartEnabled);
        foreach (ScriptFunction function in ScriptPatterns.DefaultFunctions(sources.Scripts, ExtractionClasses.ScriptClass))
        {
            foreach ((int member, byte value) in ScriptPatterns.BytesStoredInMembers(function, startEnabled))
            {
                enabled[member] = value;
            }

            foreach ((int member, string prompt) in ScriptPatterns.GlobalArguments(sources.Scripts, function, ExtractionMembers.SetPromptText))
            {
                prompts[member] = prompt;
            }
        }

        ImmutableArray<ExtractionButton>.Builder rows = ImmutableArray.CreateBuilder<ExtractionButton>(ExtractionClasses.Buttons.Count);
        foreach ((string component, ExtractionButtonRole role) in ExtractionClasses.Buttons)
        {
            ExportRef template = sources.Export(ExtractionClasses.ElevatorPackage, $".{blueprintDefault}:{component}");
            (string classPath, string scriptClass) = sources.ScriptClassOf(template.Package, template.Export.Class, template.Path);
            int member = sources.Offset(ExtractionClasses.ScriptClass, component);
            bool start = enabled.TryGetValue(member, out byte value) ? value != 0 : sources.Default(scriptClass, ExtractionMembers.StartEnabled).AsByte != 0;
            string? prompt = prompts.GetValueOrDefault(member);
            if (role == ExtractionButtonRole.Repair)
            {
                rows.Add(new ExtractionButton(component, role, classPath, start, prompt, null, 0.0, 0.0, null));
                continue;
            }

            PropertyBag settings = sources.Members(template,
                ExtractionMembers.InteractionType, ExtractionMembers.HoldDuration, ExtractionMembers.AlignmentTime, ExtractionMembers.AnimationContainer);
            StandardInteractionType type = (StandardInteractionType)settings.Get(ExtractionMembers.InteractionType, sources.Default(scriptClass, ExtractionMembers.InteractionType).AsByte);
            float hold = settings.Get(ExtractionMembers.HoldDuration, sources.Default(scriptClass, ExtractionMembers.HoldDuration).AsSingle);
            float alignment = settings.Get(ExtractionMembers.AlignmentTime, sources.Default(scriptClass, ExtractionMembers.AlignmentTime).AsSingle);
            string? container = settings.Get(ExtractionMembers.AnimationContainer, default(ObjectReference)) is { IsNull: false } reference
                ? (sources.Packages.Reference(template.Package, reference.Value) ?? throw new InvalidDataException($"{template.Path}: the animation container is a script object")).Package.Name
                : null;
            rows.Add(new ExtractionButton(component, role, classPath, start, prompt, type, Single(hold), Single(alignment), container));
        }

        return rows.MoveToImmutable();
    }

    /// <summary>The elevators placed in the persistent level, with their data asset and root transform.</summary>
    private static ImmutableArray<ExtractionPointPlacement> Points(MapRuleSources sources, SceneResolver scenes, string mapPackage, MapWorldObjects world)
    {
        CookedPackage map = sources.Load(mapPackage);
        ZenExport level = map.Exports.Single(export => map.Header.ExportPath(export.Index) == $"{map.Name}.{world.WorldName}:{world.PersistentLevelName}");
        List<ExtractionPointPlacement> rows = [];
        foreach (ZenExport actor in map.ChildrenOf(level.Index))
        {
            if (actor.Class.Kind != PackageObjectKind.PackageImport || sources.Packages.Resolve(map, actor.Class)?.Path != ExtractionClasses.Elevator.Class)
            {
                continue;
            }

            ExportRef placed = new(map, actor);
            try
            {
                rows.Add(Point(sources, scenes, placed));
            }
            catch (InvalidDataException error)
            {
                throw new InvalidDataException($"{placed.Path}: {error.Message}", error);
            }
        }

        if (rows.Select(row => row.DataAsset).Distinct(StringComparer.Ordinal).Count() != rows.Count)
        {
            throw new InvalidDataException($"{mapPackage}: elevators share a data asset");
        }

        return [.. rows.OrderBy(row => row.Actor, StringComparer.Ordinal)];
    }

    private static ExtractionPointPlacement Point(MapRuleSources sources, SceneResolver scenes, ExportRef placed)
    {
        CookedPackage map = placed.Package;
        foreach ((string component, _) in ExtractionClasses.Buttons)
        {
            ZenExport button = map.Child(placed.Export.Index, component) ?? throw new InvalidDataException($"no {component}");
            if (!SetsOnlyAttachParent(sources, new ExportRef(map, button)))
            {
                throw new InvalidDataException($"{component} sets more than its attach parent");
            }
        }

        ZenExport zone = map.Child(placed.Export.Index, ExtractionMembers.ExtractionZoneTrigger) ?? throw new InvalidDataException("no extraction zone trigger");
        int parent = scenes.Scene(map, zone).Parent ?? 0;
        if (UnversionedHeader.Read(map.Data, CookedPackage.StartOf(zone)).Members is not [{ Index: BoxComponentMembers.ScenePart + SceneComponentMembers.AttachParent, IsNonZero: true }]
            || parent <= 0 || parent > map.Exports.Count)
        {
            throw new InvalidDataException("the zone trigger sets more than its attach parent, or no export");
        }

        ZenExport root = map.Exports[parent - 1];
        if (root.Outer != PackageObjectIndex.FromExport(placed.Export.Index)
            || scenes.Scene(map, root) is not { Location: { } location, Rotation: { } rotation, Parent: null or 0, Scale: null }
            || !(Math.Abs(location.X) < MaxCoordinate && Math.Abs(location.Y) < MaxCoordinate && Math.Abs(location.Z) < MaxCoordinate)
            || Math.Abs(rotation.Pitch) + Math.Abs(rotation.Roll) > MaxLevelAngle)
        {
            throw new InvalidDataException($"unexpected root component {root.Name}");
        }

        ObjectReference reference = sources.Members(placed, ExtractionMembers.ExtractionDataAsset).Get<ObjectReference>(ExtractionMembers.ExtractionDataAsset);
        ExportRef asset = sources.Packages.Reference(map, reference.Value) is { } found && reference.Value < 0
            ? found
            : throw new InvalidDataException("no imported extraction data asset");
        PropertyBag texts = sources.Members(asset, ExtractionMembers.TitleText, ExtractionMembers.DescriptiveText, ExtractionMembers.DialogueTag);
        return new ExtractionPointPlacement(
            placed.Export.Name,
            asset.Package.Name,
            TableKey(texts, ExtractionMembers.TitleText, asset),
            TableKey(texts, ExtractionMembers.DescriptiveText, asset),
            MapRuleSources.Tag(texts.Get<PropertyBag>(ExtractionMembers.DialogueTag)),
            new Vector3D(location.X + 0.0, location.Y + 0.0, location.Z + 0.0),
            new Rotator(rotation.Pitch + 0.0, rotation.Yaw + 0.0, rotation.Roll + 0.0));
    }

    /// <summary>Whether a placed button stores only its attach parent (the first scene member after its script chain).</summary>
    private static bool SetsOnlyAttachParent(MapRuleSources sources, ExportRef button)
    {
        (_, string scriptClass) = sources.ScriptClassOf(button.Package, button.Export.Class, button.Path);
        int scenePart = sources.Scripts.ReflectedChain(scriptClass, out string? native).Count;
        return native == SceneComponentClass
            && UnversionedHeader.Read(button.Package.Data, CookedPackage.StartOf(button.Export)).Members
                is [{ IsNonZero: true } member] && member.Index == scenePart + SceneComponentMembers.AttachParent;
    }

    private static string TableKey(PropertyBag texts, string member, ExportRef asset)
    {
        return texts.Get<TextValue>(member) is { History: TextHistory.StringTableEntry, Key.Length: > 0 } text
            ? text.Key
            : throw new InvalidDataException($"{asset.Path}: {member} is not a string-table entry");
    }

    /// <summary>A settings literal: a double as stored, a float as <see cref="Single"/> gives it.</summary>
    private static double Number(ScriptConstant constant) => constant.Size == sizeof(float) ? Single(constant.AsSingle) : constant.AsDouble;

    /// <summary>A float read from the game as the shortest double with the same seven significant digits (0.2, not 0.20000000298…).</summary>
    private static double Single(float value) => double.Parse(((double)value).ToString(SingleDigits, CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    private static string ScriptSchemasPath(string enumName) => ScriptSchemas.ScriptPackage + enumName;
}
