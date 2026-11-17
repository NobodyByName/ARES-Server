using System.Collections.Immutable;
using Ares.GameDataExtract.Components;
using Ares.GameDataExtract.Packages;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;
using Ares.Structs.Game.World;

namespace Ares.GameDataExtract.World;

/// <summary>
/// Reads the authored player starts of a map's persistent level (<see cref="PlayerStart"/>), in export order: the
/// placed <see cref="PlayerStartClassNames.Placed"/> and <see cref="PlayerStartClassNames.Group"/> actors, their spawn
/// tags, the root component their <c>RootComponent</c> names (one of their own components, without an attach parent or
/// scale, so its relative transform is the start's world transform) and, for a group start, its local spawn points.
/// </summary>
internal static class PlayerStartReader
{
    private const int MaxTags = 64;
    private const double MaxTilt = 1e-8;
    private const double UnitTolerance = 1e-5;

    private static readonly PackageObjectIndex s_placed = PackageObjectIndex.FromScriptPath(EmbarkPlayerStartMembers.ClassPath);
    private static readonly PackageObjectIndex s_group = PackageObjectIndex.FromScriptPath(PioneerPlayerStartMembers.ClassPath);
    private static readonly PackageObjectIndex s_capsule = PackageObjectIndex.FromScriptPath(CapsuleComponentMembers.ClassPath);
    private static readonly PackageObjectIndex s_groupComponent = PackageObjectIndex.FromScriptPath(PioneerPlayerStartComponentMembers.ClassPath);

    /// <summary>The player starts placed in <paramref name="level"/>'s persistent level.</summary>
    /// <exception cref="InvalidDataException">A start stores a member this reader does not know, or its root does not decode.</exception>
    public static ImmutableArray<PlayerStart> Read(CookedPackage level, SceneResolver scenes)
    {
        ImmutableArray<PlayerStart>.Builder starts = ImmutableArray.CreateBuilder<PlayerStart>();
        foreach (ZenExport actor in PlacedActors.Actors(level))
        {
            bool group = actor.Class == s_group;
            if (!group && actor.Class != s_placed)
            {
                continue;
            }

            try
            {
                starts.Add(ReadStart(level, actor, group, scenes));
            }
            catch (InvalidDataException error)
            {
                throw new InvalidDataException($"{level.Header.ExportPath(actor.Index)}: {error.Message}", error);
            }
        }

        return starts.ToImmutable();
    }

    private static PlayerStart ReadStart(CookedPackage level, ZenExport actor, bool group, SceneResolver scenes)
    {
        UnversionedHeader header = UnversionedHeader.Read(level.Data, CookedPackage.StartOf(actor));
        PlacedActorValues values = new(level, actor, header);
        string[] tags = [];
        Transform[] points = [];
        int root = 0;
        foreach (UnversionedMember member in header.Members)
        {
            Kind kind = group ? GroupMember(member.Index) : PlacedMember(member.Index);
            if (kind == Kind.Unknown)
            {
                throw new InvalidDataException($"unexpected member {member.Index}");
            }

            if (!member.IsNonZero)
            {
                continue;
            }

            switch (kind)
            {
                case Kind.Tags:
                    tags = values.GameplayTags(MaxTags);
                    break;
                case Kind.Reference:
                    _ = values.Reference();
                    break;
                case Kind.Root:
                    root = values.Reference();
                    break;
                case Kind.Byte:
                    _ = values.Byte();
                    break;
                case Kind.String:
                    _ = values.String();
                    break;
                case Kind.SpawnPoints:
                    points = SpawnPoints(values);
                    break;
                default:
                    throw new InvalidDataException($"member {member.Index} is not read");
            }
        }

        _ = values.ActorTrailer();
        ZenExport? component = root > 0 && root <= level.Exports.Count ? level.Exports[root - 1] : null;
        if (component is null || component.Outer.Kind != PackageObjectKind.Export || component.Outer.ExportIndex != actor.Index)
        {
            throw new InvalidDataException($"the root component reference {root} is not one of the actor's components");
        }

        SceneMembers scene = group
            ? component.Class == s_groupComponent
                ? ScenePart.Read(level, component, PioneerPlayerStartComponentMembers.ScenePart, NativeTail.None)
                : throw new InvalidDataException($"the root component {component.Name} is not a group start component")
            : component.Class == s_capsule
                ? scenes.Scene(level, component)
                : throw new InvalidDataException($"the root component {component.Name} is not a capsule");
        if (scene.Parent is not (null or 0) || scene.Scale is not null)
        {
            throw new InvalidDataException($"the root component {component.Name} has an attach parent or a scale");
        }

        Rotator rotation = scene.Rotation ?? default;
        if (Math.Abs(rotation.Pitch) + Math.Abs(rotation.Roll) > MaxTilt)
        {
            throw new InvalidDataException("tilted root rotation");
        }

        return new PlayerStart(
            actor.Name,
            group ? PlayerStartClassNames.Group : PlayerStartClassNames.Placed,
            tags,
            component.Name,
            scene.Location ?? default,
            rotation,
            points);
    }

    /// <summary>The group start's local spawn points: each stores its whole transform, unscaled, with a unit rotation.</summary>
    private static Transform[] SpawnPoints(PlacedActorValues values)
    {
        int count = values.Count(PioneerPlayerStartMembers.MaxSpawnPoints);
        Transform[] points = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            UnversionedHeader point = values.StructHeader(PlayerSpawnPointMembers.MemberCount);
            if (point.Members is not [{ Index: PlayerSpawnPointMembers.SpawnTransform, IsNonZero: true }])
            {
                throw new InvalidDataException($"spawn point {i} does not store only its transform");
            }

            UnversionedHeader transform = values.StructHeader(TransformMembers.MemberCount);
            if (transform.Members.Count != TransformMembers.MemberCount || transform.Members.Any(member => !member.IsNonZero))
            {
                throw new InvalidDataException($"spawn point {i} does not store its whole transform");
            }

            Quaternion rotation = values.Quaternion();
            Vector3D translation = values.Vector();
            Vector3D scale = values.Vector();
            double norm = (rotation.X * rotation.X) + (rotation.Y * rotation.Y) + (rotation.Z * rotation.Z) + (rotation.W * rotation.W);
            if (scale != Vector3D.One || Math.Abs(norm - 1) > UnitTolerance)
            {
                throw new InvalidDataException($"spawn point {i} is scaled or its rotation is not a unit quaternion");
            }

            points[i] = new Transform(rotation, translation, scale);
        }

        return points;
    }

    private static Kind PlacedMember(int index) => index switch
    {
        EmbarkPlayerStartMembers.SpawnTags => Kind.Tags,
        EmbarkPlayerStartMembers.CapsuleComponent or EmbarkPlayerStartMembers.GoodSprite or EmbarkPlayerStartMembers.BadSprite => Kind.Reference,
        EmbarkPlayerStartMembers.SpawnCollisionHandlingMethod => Kind.Byte,
        EmbarkPlayerStartMembers.RootComponent => Kind.Root,
        _ => Kind.Unknown,
    };

    private static Kind GroupMember(int index) => index switch
    {
        PioneerPlayerStartMembers.SpawnTags => Kind.Tags,
        PioneerPlayerStartMembers.SpawnPoints => Kind.SpawnPoints,
        PioneerPlayerStartMembers.NameForTelemetry => Kind.String,
        PioneerPlayerStartMembers.PioneerPlayerStartComponent => Kind.Reference,
        PioneerPlayerStartMembers.RootComponent => Kind.Root,
        _ => Kind.Unknown,
    };

    /// <summary>How a start member's value is read.</summary>
    private enum Kind
    {
        Unknown,
        Tags,
        Reference,
        Root,
        Byte,
        String,
        SpawnPoints,
    }
}
