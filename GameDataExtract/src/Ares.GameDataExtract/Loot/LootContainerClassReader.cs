using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Properties;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Core.Assets;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.World;
using B = Ares.Structs.Game.Loot.LootContainerBlueprintMembers;

namespace Ares.GameDataExtract.Loot;

/// <summary>
/// Reads how a loot container class is searched (<see cref="LootContainerClass"/>) from its Blueprint chain, from
/// <see cref="LootContainerBlueprintMembers.BaseBlueprint"/> down to the class. Each Blueprint's class default holds its
/// component templates as differences from its parent's; they are applied parent first over the script defaults:
/// <list type="bullet">
/// <item>the search interaction and the item container, script components decoded against their script chains (scene
/// part included);</item>
/// <item>the root and socket meshes, static mesh components whose scene part is read from the end of their blocks (a
/// template whose scene part cannot be read leaves that component unreadable);</item>
/// <item>the quest component, which must decode.</item>
/// </list>
/// The prompt location is the interaction's location through its attach chain in the root's space; the search location
/// is that location floored to <see cref="LootContainerBlueprintMembers.SearchLocationStep"/>; the prompt distances come
/// from the standard preset (stationary while the character is immobilized). The actor class is the nearest script class
/// of the container actor's chain that declares replicated members.
/// </summary>
internal sealed class LootContainerClassReader
{
    private const int MaxChainLength = 16;
    private const string ClassSuffix = "_C";
    private const string DefaultPrefix = "Default__";

    private static readonly string[] s_parts = [B.InteractionComponent, B.ItemContainerComponent, B.QuestComponent, B.RootComponent, B.SocketMeshComponent];

    private static readonly Dictionary<string, string> s_partClasses = new(StringComparer.Ordinal)
    {
        [B.InteractionComponent] = B.InteractionClassPath,
        [B.ItemContainerComponent] = B.ItemContainerClassPath,
        [B.QuestComponent] = B.QuestClassPath,
        [B.RootComponent] = StaticMeshComponentMembers.ClassPath,
        [B.SocketMeshComponent] = StaticMeshComponentMembers.ClassPath,
    };

    private readonly PackageCache _packages;
    private readonly LootInteractionDefaults _defaults;
    private readonly StructSchema _interaction;
    private readonly StructSchema _container;
    private readonly StructSchema _quest;
    private readonly IReadOnlyList<string> _interactionTypes;
    private readonly IReadOnlyList<string> _presets;
    private readonly string _scriptActor;

    /// <summary>A reader over <paramref name="packages"/> with the script chains and defaults of <paramref name="cache"/>.</summary>
    /// <exception cref="InvalidDataException">A script class, enum or default the reader needs is missing.</exception>
    public LootContainerClassReader(PackageCache packages, ScriptCache cache, ScriptSchemas schemas)
    {
        _packages = packages;
        _defaults = LootScriptDefaults.Interaction(cache);
        _interaction = schemas.Of(B.InteractionScriptClass);
        _container = schemas.Of(ItemContainerComponentMembers.ScriptClass);
        _quest = schemas.Of(B.QuestScriptClass);
        _interactionTypes = cache.Enum(B.InteractionTypeEnum).Names;
        _presets = cache.Enum(B.PromptDistancePresetEnum).Names;
        _scriptActor = ScriptSchemas.ScriptPackage + B.ScriptActor[1..];
        ScriptClass replicated = cache.Chain(B.ScriptActor, out _).FirstOrDefault(type => type.Properties.Any(property => property.IsReplicated))
            ?? throw new InvalidDataException($"No class of the chain of {B.ScriptActor} declares replicated members");
        ActorClass = ScriptSchemas.ScriptPackage + replicated.Name[1..];
    }

    /// <summary>The replication layout every container actor uses.</summary>
    public string ActorClass { get; }

    /// <summary>Reads the container class at <paramref name="classPath"/> (<c>Package.Name_C</c>).</summary>
    /// <exception cref="InvalidDataException">The class is not a container Blueprint, or a template the row needs does not decode.</exception>
    public LootContainerClass Read(string classPath)
    {
        Templates templates = new();
        foreach ((CookedPackage package, ZenExport type) in BlueprintChain(classPath))
        {
            Dictionary<string, ZenExport> parts = Parts(package, type);
            foreach (string part in s_parts)
            {
                ZenExport export = parts.GetValueOrDefault(part) ?? throw new InvalidDataException($"{package.Name}: no {part} template");
                templates.Classes[part] = _packages.ScriptPath(export.Class) ?? throw new InvalidDataException($"{package.Name}: {part} is not of a script class");
            }

            Merge(templates.Interaction, package, PropertyReader.ReadExport(package, parts[B.InteractionComponent], _interaction, B.ScriptComponentTrailer));
            Merge(templates.Container, package, PropertyReader.ReadExport(package, parts[B.ItemContainerComponent], _container, B.ScriptComponentTrailer));
            PropertyReader.ReadExport(package, parts[B.QuestComponent], _quest, B.ScriptComponentTrailer);
            foreach (string mesh in (string[])[B.RootComponent, B.SocketMeshComponent])
            {
                MergeMesh(templates, mesh, package, parts[mesh]);
            }
        }

        foreach ((string part, string expected) in s_partClasses)
        {
            if (templates.Classes[part] != expected)
            {
                throw new InvalidDataException($"{classPath}: {part} is a {templates.Classes[part]}");
            }
        }

        return Row(classPath, templates);
    }

    private LootContainerClass Row(string classPath, Templates templates)
    {
        Dictionary<string, Dictionary<string, object?>> values = new(StringComparer.Ordinal)
        {
            [B.InteractionComponent] = templates.Interaction,
            [B.ItemContainerComponent] = templates.Container,
            [B.RootComponent] = templates.Meshes[B.RootComponent],
            [B.SocketMeshComponent] = templates.Meshes[B.SocketMeshComponent],
        };
        Dictionary<string, string?> parents = values.ToDictionary(pair => pair.Key, pair => pair.Value.GetValueOrDefault(ScriptSchemas.AttachParent) as string, StringComparer.Ordinal);
        if (parents[B.RootComponent] is { } rootParent)
        {
            throw new InvalidDataException($"{classPath}: the root is attached to {rootParent}");
        }

        if (!templates.Unreadable.Contains(B.SocketMeshComponent) && parents[B.SocketMeshComponent] != B.RootComponent)
        {
            throw new InvalidDataException($"{classPath}: the socket mesh is attached to {parents[B.SocketMeshComponent] ?? "nothing"}");
        }

        foreach (string part in (string[])[B.InteractionComponent, B.ItemContainerComponent])
        {
            if (values[part].GetValueOrDefault(ScriptSchemas.AttachSocketName) is string socket && socket.Length > 0 && socket != CookedPackage.NoneName)
            {
                throw new InvalidDataException($"{classPath}: {part} is attached to a socket");
            }
        }

        Vector3D point = Relative(values[B.InteractionComponent]).Location;
        string? parent = parents[B.InteractionComponent];
        while (parent is not null && parent != B.RootComponent)
        {
            if (!values.ContainsKey(parent) || templates.Unreadable.Contains(parent))
            {
                throw new InvalidDataException($"{classPath}: the interaction is attached to {parent}");
            }

            point = TransformPoint(Relative(values[parent]), point);
            parent = parents[parent];
        }

        if (parent != B.RootComponent)
        {
            throw new InvalidDataException($"{classPath}: the interaction's attach chain does not reach the root");
        }

        Dictionary<string, object?> interaction = templates.Interaction;
        byte preset = Value(interaction, B.PromptDistancePreset, _defaults.PromptDistancePreset);
        if (preset >= _presets.Count || _presets[preset] != B.StandardPreset)
        {
            throw new InvalidDataException($"{classPath}: prompt distance preset {preset}");
        }

        byte type = Value(interaction, B.InteractionType, _defaults.InteractionType);
        double hold = Seconds(interaction, B.HoldDuration, _defaults.HoldSeconds);
        bool immobilized = Value(interaction, B.ForceImmobilize, _defaults.ForceImmobilize) || (type < _interactionTypes.Count && _interactionTypes[type] == B.ContinuousInteraction);
        string package = classPath[..classPath.LastIndexOf('.')];
        return new LootContainerClass(
            new ArchetypeClass(classPath, $"{package}.{DefaultPrefix}{classPath[(classPath.LastIndexOf('.') + 1)..]}"),
            ActorClass,
            Components(templates.Classes, parents),
            new Vector3D(PositiveZero(point.X), PositiveZero(point.Y), PositiveZero(point.Z)),
            new Vector3D(Floor(point.X), Floor(point.Y), Floor(point.Z)),
            interaction.GetValueOrDefault(B.AnimationContainer) as string,
            Seconds(interaction, B.AlignmentTime, _defaults.AlignmentSeconds),
            hold,
            Value(interaction, B.EquipRaiderTool, _defaults.EquipsRaiderTool),
            immobilized || hold > 0 ? _defaults.StationaryPromptDistance : _defaults.StandardPromptDistance,
            immobilized ? _defaults.StationaryPromptDistance : _defaults.StandardPromptDistance,
            Value(templates.Container, ItemContainerComponentMembers.ItemLimit, _defaults.SlotCount));
    }

    /// <summary>
    /// The components the client knows by name, in the order the container sends them: each replicated component (the
    /// interaction, the item container, the quest component), then its attach parents not listed yet.
    /// </summary>
    private static LootContainerComponent[] Components(Dictionary<string, string> classes, Dictionary<string, string?> parents)
    {
        List<string> order = [];
        foreach (string replicated in (string[])[B.InteractionComponent, B.ItemContainerComponent, B.QuestComponent])
        {
            for (string? part = replicated; part is not null && !order.Contains(part); part = parents.GetValueOrDefault(part))
            {
                order.Add(part);
            }
        }

        return [.. order.Select(part => new LootContainerComponent(part, classes[part], parents.GetValueOrDefault(part)))];
    }

    /// <summary>[(package, class export)] from the base Blueprint down to the class.</summary>
    private List<(CookedPackage Package, ZenExport Class)> BlueprintChain(string classPath)
    {
        List<(CookedPackage, ZenExport)> chain = [];
        string packageName = classPath[..classPath.LastIndexOf('.')];
        while (true)
        {
            CookedPackage package = _packages.Load(packageName) ?? throw new InvalidDataException($"{packageName} is not in the game archives");
            string name = packageName[(packageName.LastIndexOf('/') + 1)..] + ClassSuffix;
            ZenExport type = package.Exports.SingleOrDefault(export => export.Name == name && export.Outer.IsNull)
                ?? throw new InvalidDataException($"{packageName}: no class export {name}");
            chain.Add((package, type));
            if (type.Super.Kind == PackageObjectKind.ScriptImport)
            {
                chain.Reverse();
                return chain[0].Item1.Name == B.BaseBlueprint && _packages.ScriptPath(type.Super) == _scriptActor
                    ? chain
                    : throw new InvalidDataException($"{classPath} is not a Blueprint of {B.BaseBlueprint}");
            }

            packageName = (_packages.Resolve(package, type.Super) ?? throw new InvalidDataException($"{packageName}: the super class cannot be resolved")).Package.Name;
            if (chain.Count > MaxChainLength)
            {
                throw new InvalidDataException($"{classPath}: the Blueprint chain is too long");
            }
        }
    }

    /// <summary>The subobjects of the class default of <paramref name="type"/>, by name.</summary>
    private static Dictionary<string, ZenExport> Parts(CookedPackage package, ZenExport type)
    {
        string name = DefaultPrefix + type.Name;
        ZenExport archetype = package.Exports.SingleOrDefault(export => export.Name == name && export.Outer.IsNull)
            ?? throw new InvalidDataException($"{package.Name}: no class default {name}");
        return package.ChildrenOf(archetype.Index).ToDictionary(export => export.Name, StringComparer.Ordinal);
    }

    /// <summary>Applies a template's own values; references become the attach parent's name and the animation set's path.</summary>
    private void Merge(Dictionary<string, object?> values, CookedPackage package, PropertyBag own)
    {
        foreach (string name in own.Names)
        {
            object value = own.Get<object>(name);
            values[name] = name switch
            {
                ScriptSchemas.AttachParent => AttachName(package, (ObjectReference)value),
                B.AnimationContainer => _packages.Reference(package, ((ObjectReference)value).Value)?.Path,
                _ => value,
            };
        }
    }

    private void MergeMesh(Templates templates, string part, CookedPackage package, ZenExport export)
    {
        SceneMembers scene;
        try
        {
            scene = ScenePart.Read(package, export, StaticMeshComponentMembers.ScenePart, NativeTail.StaticMesh);
        }
        catch (InvalidDataException)
        {
            templates.Unreadable.Add(part);
            return;
        }

        Dictionary<string, object?> values = templates.Meshes[part];
        if (scene.Socket is { } socket && socket != CookedPackage.NoneName)
        {
            templates.Unreadable.Add(part);
        }

        if (scene.Location is { } location)
        {
            values[ScriptSchemas.RelativeLocation] = location;
        }

        if (scene.Rotation is { } rotation)
        {
            values[ScriptSchemas.RelativeRotation] = rotation;
        }

        if (scene.Scale is { } scale)
        {
            values[ScriptSchemas.RelativeScale3D] = scale;
        }

        if (scene.Parent is { } reference)
        {
            values[ScriptSchemas.AttachParent] = AttachName(package, new ObjectReference(reference));
        }
    }

    /// <summary>The name of the component an attach parent reference names: a subobject of a class default, or <see langword="null"/> for none.</summary>
    private string? AttachName(CookedPackage package, ObjectReference reference)
    {
        if (reference.IsNull)
        {
            return null;
        }

        ExportRef target = _packages.Reference(package, reference.Value) ?? throw new InvalidDataException($"{package.Name}: a component is attached to a script object");
        return target.Export.Outer is { Kind: PackageObjectKind.Export } outer
               && target.Package.Exports[outer.ExportIndex] is { Outer.IsNull: true } owner && owner.Name.StartsWith(DefaultPrefix, StringComparison.Ordinal)
            ? target.Export.Name
            : throw new InvalidDataException($"{package.Name}: a component is attached to {target.Path}");
    }

    private static (Vector3D Location, Rotator Rotation, Vector3D Scale) Relative(Dictionary<string, object?> values)
    {
        return (values.GetValueOrDefault(ScriptSchemas.RelativeLocation) as Vector3D? ?? default,
            values.GetValueOrDefault(ScriptSchemas.RelativeRotation) as Rotator? ?? default,
            values.GetValueOrDefault(ScriptSchemas.RelativeScale3D) as Vector3D? ?? Vector3D.One);
    }

    /// <summary><paramref name="point"/> scaled, rotated (pitch, yaw, roll in degrees) and moved by a relative transform.</summary>
    private static Vector3D TransformPoint((Vector3D Location, Rotator Rotation, Vector3D Scale) transform, Vector3D point)
    {
        const double Radians = Math.PI / 180.0;
        double pitch = transform.Rotation.Pitch * Radians;
        double yaw = transform.Rotation.Yaw * Radians;
        double roll = transform.Rotation.Roll * Radians;
        double sp = Math.Sin(pitch);
        double cp = Math.Cos(pitch);
        double sy = Math.Sin(yaw);
        double cy = Math.Cos(yaw);
        double sr = Math.Sin(roll);
        double cr = Math.Cos(roll);
        double[] x = [cp * cy, cp * sy, sp];
        double[] y = [(sr * sp * cy) - (cr * sy), (sr * sp * sy) + (cr * cy), -sr * cp];
        double[] z = [-((cr * sp * cy) + (sr * sy)), (cy * sr) - (cr * sp * sy), cr * cp];
        double px = point.X * transform.Scale.X;
        double py = point.Y * transform.Scale.Y;
        double pz = point.Z * transform.Scale.Z;
        double[] location = [transform.Location.X, transform.Location.Y, transform.Location.Z];
        double At(int i) => location[i] + (px * x[i]) + (py * y[i]) + (pz * z[i]);
        return new Vector3D(At(0), At(1), At(2));
    }

    private static double Floor(double value) => PositiveZero(Math.Floor(value / B.SearchLocationStep) * B.SearchLocationStep);

    /// <summary>The value with a zero always positive (the tables never hold −0).</summary>
    private static double PositiveZero(double value) => value == 0 ? 0.0 : value;

    /// <summary>The value a template set for <paramref name="name"/>, or the script default <paramref name="fallback"/>.</summary>
    private static T Value<T>(Dictionary<string, object?> values, string name, T fallback)
    {
        if (!values.TryGetValue(name, out object? value))
        {
            return fallback;
        }

        return value is T typed ? typed : throw new InvalidDataException($"{name} holds a {value?.GetType().Name}, not a {typeof(T).Name}");
    }

    /// <summary>The float a template set for <paramref name="name"/> (see <see cref="LootNumbers.Single"/>), or <paramref name="fallback"/>.</summary>
    private static double Seconds(Dictionary<string, object?> values, string name, double fallback)
    {
        return values.ContainsKey(name) ? LootNumbers.Single(Value(values, name, 0f)) : fallback;
    }

    /// <summary>The component values of a class, applied parent Blueprint first.</summary>
    private sealed class Templates
    {
        public Dictionary<string, string> Classes { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, object?> Interaction { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, object?> Container { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, Dictionary<string, object?>> Meshes { get; } = new(StringComparer.Ordinal)
        {
            [B.RootComponent] = new(StringComparer.Ordinal),
            [B.SocketMeshComponent] = new(StringComparer.Ordinal),
        };

        public HashSet<string> Unreadable { get; } = new(StringComparer.Ordinal);
    }
}
