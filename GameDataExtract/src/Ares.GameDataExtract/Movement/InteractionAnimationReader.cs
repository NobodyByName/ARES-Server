using System.Collections.Immutable;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Scripts;
using Ares.GameDataExtract.World;
using Ares.Structs.Game.Movement;

namespace Ares.GameDataExtract.Movement;

/// <summary>The interaction animation tables (see <see cref="InteractionAnimationReader"/>).</summary>
/// <param name="Containers">The containers, ordered by the last segment of the path their source names (ordinal).</param>
/// <param name="TagBased">The character's fallback list, in list order.</param>
internal sealed record InteractionAnimationData(ImmutableArray<InteractionAnimationContainer> Containers, ImmutableArray<TaggedInteractionAnimationContainer> TagBased);

/// <summary>
/// Reads the interaction animation containers (<c>UInteractionAnimationContainer</c> data assets) that change character
/// movement, from these sources: the character Blueprint's interactor template (its tag-based fallback list) and its
/// knockout interaction template, the door Blueprints' breach interaction templates, and the search animations and
/// elevator button containers the loot and extraction tables found. Each container's unversioned members are read
/// against the script class's reflected members; a member it does not set keeps the script constructor's literal.
/// </summary>
internal static class InteractionAnimationReader
{
    private const string Character = "/Game/Pioneer/Core/Blueprints/Player/BP_PioneerCharacter";
    private const string ContainerClass = "UInteractionAnimationContainer";
    private const string TagBasedClass = "UTagBasedInteractionAnimationContainers";
    private const string Interactor = "InteractorComponent";
    private const string TagBasedMember = "TagBasedInteractionAnimationContainer";
    private const string KnockoutInteraction = "KnockoutInteraction";
    private const string BreachInteraction = "BreachInteraction";
    private const string AnimationContainer = "AnimationContainer";
    private const string InteractionContainers = "InteractionContainers";
    private const string SemanticTags = "SemanticTags";
    private const string EntryContainer = "InteractionAnimationContainer";
    private const int DataTrailer = 4;

    // Container members read: (name, literal size of their script default).
    private const string StopInteractResponse = "StopInteractResponse";
    private const string AttachmentAlignmentType = "AttachmentAlignmentType";
    private const string AllowFloatingAlignment = "bAllowFloatingAlignment";
    private const string FullBodyInteraction = "bFullBodyInteraction";
    private const string HeightThresholdToTriggerCrouch = "HeightThresholdToTriggerCrouch";
    private const string FinishWhenAnimationsFinished = "bFinishInteractionWhenAnimationsFinished";
    private const string UnendingInteraction = "bUnendingInteraction";

    // EStopInteractResponse: AbortImmediately, FinishAlignmentAnimation.
    private const byte FinishAlignmentAnimation = 1;

    private static readonly string[] s_doors =
    [
        "/Game/Pioneer/Interactables/Doors/BP_InteractiveDoor_Rsr_x350z300_01",
        "/Game/Pioneer/Interactables/Doors/BP_InteractiveDoor_Wrh_x300z325_01",
    ];

    private static readonly (string Name, int Size)[] s_members =
    [
        (StopInteractResponse, 1), (AttachmentAlignmentType, 1), (AllowFloatingAlignment, 1), (FullBodyInteraction, 1),
        (HeightThresholdToTriggerCrouch, sizeof(double)), (FinishWhenAnimationsFinished, 1), (UnendingInteraction, 1),
    ];

    /// <summary>Reads the tables.</summary>
    /// <param name="packages">The game's packages.</param>
    /// <param name="scripts">The game's compiled scripts.</param>
    /// <param name="searchAnimations">The loot container classes' search animations (object or package paths).</param>
    /// <param name="buttonContainers">The elevator buttons' own containers (object or package paths).</param>
    /// <exception cref="InvalidDataException">A template, list or container is missing or does not decode as expected.</exception>
    public static InteractionAnimationData Read(PackageCache packages, ScriptCache scripts, IEnumerable<string> searchAnimations, IEnumerable<string> buttonContainers)
    {
        MapRuleSources sources = new(packages, scripts);
        ScriptSchemas schemas = new(scripts);
        Dictionary<string, double> defaults = s_members.ToDictionary(member => member.Name, member => Default(sources, member.Name, member.Size), StringComparer.Ordinal);

        string tagBasedPath = Reference(sources, Template(sources, Character, Interactor), TagBasedMember);
        (PropertyBag tagBased, CookedPackage tagPackage) = DataAsset(sources, schemas, tagBasedPath, TagBasedClass);
        ImmutableArray<TaggedInteractionAnimationContainer> entries =
        [
            .. tagBased.Get<object[]>(InteractionContainers, []).Cast<PropertyBag>().Select(entry => new TaggedInteractionAnimationContainer(
                [.. entry.Get<string[]>(SemanticTags, [])],
                ReferencePath(sources, tagPackage, entry.Get<ObjectReference>(EntryContainer)))),
        ];
        if (entries.IsEmpty)
        {
            throw new InvalidDataException($"{tagBasedPath}: the tag-based list is empty");
        }

        // Source path -> container; later sources keep the first path seen.
        SortedSet<string> sourced = new(StringComparer.Ordinal) { Reference(sources, Template(sources, Character, KnockoutInteraction), AnimationContainer) };
        foreach (string door in s_doors)
        {
            sourced.Add(Reference(sources, Template(sources, door, BreachInteraction), AnimationContainer));
        }

        sourced.UnionWith(searchAnimations);
        sourced.UnionWith(buttonContainers);
        sourced.UnionWith(entries.Select(entry => entry.Container));

        ImmutableArray<InteractionAnimationContainer> containers =
        [
            .. sourced.Select(path => (Path: path, Key: path[(path.LastIndexOf('/') + 1)..]))
                .OrderBy(source => source.Key, StringComparer.Ordinal)
                .Select(source => ObjectPath(source.Path))
                .Distinct(StringComparer.Ordinal)
                .Select(path => Container(sources, schemas, path, defaults)),
        ];
        return new InteractionAnimationData(containers, entries);
    }

    private static InteractionAnimationContainer Container(MapRuleSources sources, ScriptSchemas schemas, string path, Dictionary<string, double> defaults)
    {
        (PropertyBag values, _) = DataAsset(sources, schemas, path, ContainerClass);
        double Value(string member) => values.Contains(member)
            ? values.Get<object>(member) switch
            {
                bool flag => flag ? 1 : 0,
                byte value => value,
                double value => value,
                object other => throw new InvalidDataException($"{path}: {member} holds a {other.GetType().Name}"),
            }
            : defaults[member];

        double alignment = Value(AttachmentAlignmentType);
        double stop = Value(StopInteractResponse);
        if (!Enum.IsDefined((InteractionAlignment)(byte)alignment) || stop is not (0 or FinishAlignmentAnimation))
        {
            throw new InvalidDataException($"{path}: alignment {alignment} or stop response {stop} is unknown");
        }

        return new InteractionAnimationContainer(
            path,
            (InteractionAlignment)(byte)alignment,
            stop == FinishAlignmentAnimation,
            Value(AllowFloatingAlignment) != 0,
            Value(FullBodyInteraction) != 0,
            Value(HeightThresholdToTriggerCrouch),
            Value(FinishWhenAnimationsFinished) != 0,
            Value(UnendingInteraction) != 0);
    }

    /// <summary>The literal the container class's script defaults store into <paramref name="member"/>, of <paramref name="size"/> bytes.</summary>
    private static double Default(MapRuleSources sources, string member, int size)
    {
        ScriptConstant constant = sources.Default(ContainerClass, member);
        return constant.Size != size ? throw new InvalidDataException($"{ContainerClass}.{member}: a {constant.Size}-byte default, expected {size}")
            : size == sizeof(double) ? constant.AsDouble
            : constant.AsByte;
    }

    /// <summary>The only export named <paramref name="name"/> of Blueprint <paramref name="blueprint"/>, a component template.</summary>
    private static ExportRef Template(MapRuleSources sources, string blueprint, string name)
    {
        CookedPackage package = sources.Load(blueprint);
        ZenExport[] found = [.. package.Exports.Where(export => export.Name == name).Take(2)];
        return found.Length == 1 ? new ExportRef(package, found[0]) : throw new InvalidDataException($"{blueprint}: {name} template not found once");
    }

    /// <summary>The object path of the object member <paramref name="member"/> of <paramref name="template"/> refers to.</summary>
    private static string Reference(MapRuleSources sources, ExportRef template, string member) =>
        ReferencePath(sources, template.Package, sources.Own(template, [member]).Get<ObjectReference>(member));

    private static string ReferencePath(MapRuleSources sources, CookedPackage package, ObjectReference reference) =>
        sources.Packages.Reference(package, reference.Value) is { } found ? found.Path : throw new InvalidDataException($"{package.Name}: reference {reference.Value} names nothing");

    /// <summary>The object path of a data asset named like its package (a package path gets the asset's name).</summary>
    private static string ObjectPath(string path)
    {
        string name = path[(path.LastIndexOf('/') + 1)..];
        return name.Contains('.', StringComparison.Ordinal) ? path : $"{path}.{name}";
    }

    /// <summary>The members a data asset of script class <paramref name="scriptClass"/> sets, read to its trailer, with its package.</summary>
    private static (PropertyBag Values, CookedPackage Package) DataAsset(MapRuleSources sources, ScriptSchemas schemas, string path, string scriptClass)
    {
        int dot = path.LastIndexOf('.');
        CookedPackage package = sources.Load(path[..dot]);
        string name = path[(dot + 1)..];
        ZenExport[] found = [.. package.Exports.Where(export => export.Name == name).Take(2)];
        return found.Length == 1
            ? (PropertyReader.ReadExport(package, found[0], schemas.Of(scriptClass), DataTrailer), package)
            : throw new InvalidDataException($"{path}: export {name} not found once");
    }
}
