using System.Collections.Immutable;
using Ares.GameDataExtract.Packages;
using Ares.GameDataExtract.Scripts;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads the groups that pick which dynamic loot spawners run (<see cref="DynamicLootGroupEntry"/>) from the world
/// activity tree of a map's persistent level: the <c>UActivityNode</c> trees of the <c>ActivityArea</c> actors and
/// their <c>UActorSpawnerActivityContainer</c> activities. A container that lists loot spawners is a group: its
/// <c>SpawnersMin</c>/<c>SpawnersMax</c>, the spawners it lists (soft paths into the persistent level), the titles of
/// the node path from the root, the round configurations every node of the path applies to (<c>ContentTags</c>), and
/// the nodes above it that choose only some of their subnodes. Groups are ordered by container export.
/// </summary>
internal sealed class ActivityTreeReader
{
    /// <summary>Gameplay tag family of the round configurations a node applies to.</summary>
    private const string RoundPrefix = "RoundConfiguration.";

    private const string NodeClass = "UActivityNode";
    private const string ContainerClass = "UActorSpawnerActivityContainer";
    private const string ConfigStruct = "FActorSpawnerContainerConfig";
    private const string AreaPrefix = "ActivityArea";
    private const int NodeChainLength = 4;
    private const int ContainerChainLength = 5;
    private const int MaxTags = 64;
    private const int MaxReferences = 1024;
    private const int MaxSpawnerConfigs = 1024;
    private const int SoftPathNamesSize = 16;
    private const int MaxContainerTail = 4;

    private static readonly PackageObjectIndex s_node = PackageObjectIndex.FromScriptPath("/Script/Angelscript.ActivityNode");
    private static readonly PackageObjectIndex s_container = PackageObjectIndex.FromScriptPath("/Script/Angelscript.ActorSpawnerActivityContainer");

    private readonly ScriptMemberChain _node;
    private readonly ScriptMemberChain _container;
    private readonly ScriptMemberChain _config;

    /// <summary>A reader with the member chains of <paramref name="layouts"/>.</summary>
    /// <exception cref="InvalidDataException">A class or struct the reader needs changed.</exception>
    public ActivityTreeReader(ScriptLayouts layouts)
    {
        _node = ScriptMemberChain.OfLeading(layouts, NodeClass, "Title", "LocationTags", "ContentTags", "bInheritParentNodeLocationTags", "bForceNoDelayedActivation",
            "DelayedActivationEvent", "SubnodesMin", "SubnodesMax", "bUseWeightingForChildNodeSelection", "Weight", "NumSubnodesAvailable", "Subnodes", "Archetypes",
            "Activities");
        _container = ScriptMemberChain.OfLeading(layouts, ContainerClass, "SpawnersMin", "SpawnersMax", "SpawnerConfigs");
        _config = ScriptMemberChain.OfLeading(layouts, ConfigStruct, "Spawner");
    }

    /// <summary>
    /// The groups of the activity tree in <paramref name="level"/> over the spawner table <paramref name="spawners"/>
    /// (actor names must be unique), checking that no cell in <paramref name="cells"/> holds activity objects.
    /// </summary>
    /// <exception cref="InvalidDataException">A node or container does not decode, or a group is not as the spawners expect.</exception>
    public ImmutableArray<DynamicLootGroupEntry> Read(CookedPackage level, IEnumerable<CookedPackage> cells, IReadOnlyList<DynamicLootSpawner> spawners)
    {
        if (cells.FirstOrDefault(cell => cell.Exports.Any(export => export.Class == s_node || export.Class == s_container)) is { } outside)
        {
            throw new InvalidDataException($"{outside.Name}: activity nodes or containers outside the persistent level");
        }

        Dictionary<int, (string Area, string Name, Node Node)> nodes = [];
        SortedDictionary<int, (string Area, string Node, int Min, int Max, string[] Spawners)> containers = [];
        foreach (ZenExport export in level.Exports)
        {
            IReadOnlyList<string> chain = ExportChains.Chain(level, export);
            switch (chain.Count)
            {
                case NodeChainLength when chain[2].StartsWith(AreaPrefix, StringComparison.Ordinal) && export.Class == s_node:
                    nodes[export.Index] = (chain[2], chain[3], ReadNode(level, export));
                    break;
                case ContainerChainLength when chain[2].StartsWith(AreaPrefix, StringComparison.Ordinal) && export.Class == s_container:
                    (int min, int max, string[] names) = ReadContainer(level, export);
                    containers[export.Index] = (chain[2], chain[3], min, max, names);
                    break;
            }
        }

        Dictionary<string, int> spawnerIndex = new(StringComparer.Ordinal);
        for (int i = 0; i < spawners.Count; i++)
        {
            spawnerIndex[spawners[i].Actor] = i;
        }

        return [.. Groups(nodes, containers, spawnerIndex)];
    }

    private static IEnumerable<DynamicLootGroupEntry> Groups(
        Dictionary<int, (string Area, string Name, Node Node)> nodes,
        SortedDictionary<int, (string Area, string Node, int Min, int Max, string[] Spawners)> containers,
        Dictionary<string, int> spawnerIndex)
    {
        Dictionary<int, int> parents = [];
        foreach ((int index, (_, _, Node node)) in nodes)
        {
            foreach (int child in node.Subnodes)
            {
                parents[child] = index;
            }
        }

        Dictionary<(string, string), int> byName = nodes.ToDictionary(pair => (pair.Value.Area, pair.Value.Name), pair => pair.Key);
        foreach ((int containerIndex, (string area, string nodeName, int spawnersMin, int spawnersMax, string[] names)) in containers)
        {
            int min = spawnersMin;
            int max = spawnersMax;
            int?[] listed = [.. names.Select(name => spawnerIndex.TryGetValue(name, out int index) ? index : (int?)null)];
            if (listed.All(entry => entry is null))
            {
                continue;
            }

            int nodeIndex = byName.TryGetValue((area, nodeName), out int found) ? found : throw new InvalidDataException($"{nodeName}: the container's node is not an activity node");
            int position = Array.IndexOf(nodes[nodeIndex].Node.Activities, containerIndex);
            if (position < 0)
            {
                throw new InvalidDataException($"{nodeName}: the container is not one of the node's activities");
            }

            List<int> path = [];
            for (int? at = nodeIndex; at is { } current; at = parents.TryGetValue(current, out int parent) ? parent : null)
            {
                path.Add(current);
            }

            path.Reverse();
            if (path.Any(index => nodes[index].Node.Partial))
            {
                throw new InvalidDataException($"{nodeName}: a node on its path is only partly read");
            }

            HashSet<string> rounds = new(nodes[path[0]].Node.Content, StringComparer.Ordinal);
            foreach (int index in path.Skip(1))
            {
                rounds.IntersectWith(nodes[index].Node.Content);
            }

            if (rounds.Count == 0 || rounds.Any(tag => !tag.StartsWith(RoundPrefix, StringComparison.Ordinal)))
            {
                throw new InvalidDataException($"{nodeName}: content tags ({string.Join(", ", rounds.Order(StringComparer.Ordinal))})");
            }

            int[] groupSpawners = [.. listed.OfType<int>()];
            if (groupSpawners.Length != listed.Length)
            {
                // Only fully always-on groups may drop their spawners of other classes.
                if (min != listed.Length || max != listed.Length)
                {
                    throw new InvalidDataException($"{nodeName}: partly container group with a random pick");
                }

                min = max = groupSpawners.Length;
            }

            List<DynamicLootSelection> selections = [];
            foreach (int index in path.SkipLast(1))
            {
                (_, string name, Node node) = nodes[index];
                int count = node.Subnodes.Length;
                if (node.SubnodesMin is not { } low || node.SubnodesMax is not { } high)
                {
                    throw new InvalidDataException($"{name}: subnodes without a selection range");
                }

                if (low != count || high != count || node.Weighted)
                {
                    selections.Add(new DynamicLootSelection(name, low, high, count, node.Weighted));
                }
            }

            if (groupSpawners.Length < max || min > max || min < 0)
            {
                throw new InvalidDataException($"{nodeName}: container range {min}-{max} of {groupSpawners.Length}");
            }

            yield return new DynamicLootGroupEntry(
                $"{nodeName}/{position}",
                [.. path.Select(index => nodes[index].Node.Title is { Length: > 0 } title ? title : nodes[index].Name)],
                [.. rounds.Select(tag => tag[RoundPrefix.Length..]).Order(StringComparer.Ordinal)],
                min,
                max,
                [.. groupSpawners.Distinct().Order()],
                selections);
        }
    }

    /// <summary>The members of an activity node block up to the ones this reader needs (see <see cref="Node.Partial"/>).</summary>
    private Node ReadNode(CookedPackage level, ZenExport export)
    {
        UnversionedHeader header = UnversionedHeader.Read(level.Data, CookedPackage.StartOf(export));
        PlacedActorValues values = new(level, export, header);
        Node node = new();
        HashSet<int> flags = [_node.Index("bInheritParentNodeLocationTags"), _node.Index("bForceNoDelayedActivation"), _node.Index("bUseWeightingForChildNodeSelection")];
        foreach ((int index, bool isNonZero) in header.Members)
        {
            if (index == _node.Index("DelayedActivationEvent") || (index == _node.Index("LocationTags") && isNonZero))
            {
                node.Partial = true;
                break;
            }

            if (index == _node.Index("Title"))
            {
                node.Title = isNonZero ? values.Name() : node.Title;
            }
            else if (index == _node.Index("ContentTags"))
            {
                node.Content = isNonZero ? values.GameplayTags(MaxTags) : node.Content;
            }
            else if (flags.Contains(index))
            {
                bool set = isNonZero && values.Byte() != 0;
                node.Weighted |= set && index == _node.Index("bUseWeightingForChildNodeSelection");
            }
            else if (index == _node.Index("SubnodesMin"))
            {
                node.SubnodesMin = isNonZero ? values.Int32() : 0;
            }
            else if (index == _node.Index("SubnodesMax"))
            {
                node.SubnodesMax = isNonZero ? values.Int32() : 0;
            }
            else if (index == _node.Index("Weight"))
            {
                if (isNonZero)
                {
                    values.Skip(sizeof(double));
                }
            }
            else if (index == _node.Index("Subnodes") || index == _node.Index("Activities"))
            {
                if (!isNonZero)
                {
                    continue;
                }

                int[] references = [.. values.References(MaxReferences).Select(reference => reference - 1)];
                node.Subnodes = index == _node.Index("Subnodes") ? references : node.Subnodes;
                node.Activities = index == _node.Index("Activities") ? references : node.Activities;
            }
            else if (index != _node.Index("LocationTags"))
            {
                throw new InvalidDataException($"{export.Name}: node member {index} (not read)");
            }
        }

        return node;
    }

    /// <summary>SpawnersMin, SpawnersMax and the persistent-level actor names of a container's spawner configs.</summary>
    private (int Min, int Max, string[] Spawners) ReadContainer(CookedPackage level, ZenExport export)
    {
        UnversionedHeader header = UnversionedHeader.Read(level.Data, CookedPackage.StartOf(export));
        int[] expected = [_container.Index("SpawnersMin"), _container.Index("SpawnersMax"), _container.Index("SpawnerConfigs")];
        if (!header.Members.Select(member => member.Index).SequenceEqual(expected) || header.Members.Any(member => !member.IsNonZero))
        {
            throw new InvalidDataException($"{export.Name}: container members ({string.Join(", ", header.Members.Select(member => member.Index))})");
        }

        PlacedActorValues values = new(level, export, header);
        int min = values.Int32();
        int max = values.Int32();
        int count = values.Count(MaxSpawnerConfigs);
        string[] names = new string[count];
        for (int i = 0; i < count; i++)
        {
            UnversionedHeader config = values.StructHeader(_config.Count);
            if (config.Members is not [{ IsNonZero: true } spawner] || spawner.Index != _config.Index("Spawner"))
            {
                throw new InvalidDataException($"{export.Name}: spawner config members");
            }

            values.Skip(SoftPathNamesSize);
            string path = LootLimiterReader.SubPath(level, values);
            int dot = path.IndexOf('.', StringComparison.Ordinal);
            names[i] = dot >= 0 && path[..dot] == ExportChains.PersistentLevel && dot + 1 < path.Length
                ? path[(dot + 1)..]
                : throw new InvalidDataException($"{export.Name}: unexpected spawner path {path}");
        }

        return values.Remaining <= MaxContainerTail && !level.Data.AsSpan(values.Offset, values.Remaining).ContainsAnyExcept((byte)0)
            ? (min, max, names)
            : throw new InvalidDataException($"{export.Name}: the container block has trailing data");
    }

    /// <summary>The members of an activity node this reader uses.</summary>
    private sealed class Node
    {
        public string? Title { get; set; }

        public IReadOnlyList<string> Content { get; set; } = [];

        public int? SubnodesMin { get; set; }

        public int? SubnodesMax { get; set; }

        public bool Weighted { get; set; }

        public int[] Subnodes { get; set; } = [];

        public int[] Activities { get; set; } = [];

        /// <summary>True when the block stores members after the ones read (from <c>DelayedActivationEvent</c>, or location tags).</summary>
        public bool Partial { get; set; }
    }
}
