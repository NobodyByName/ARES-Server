using Ares.Raid.Replication;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.World;

/// <summary>A placed ladder with the climbable actor the server spawns for it.</summary>
/// <param name="Index">Index in <see cref="DamLadders.All"/>.</param>
/// <param name="Cell">World Partition cell package holding the placed ladder.</param>
/// <param name="Parent">The placed ladder (static id).</param>
/// <param name="Guid">The climbable actor (dynamic id).</param>
/// <param name="Interactions">Its climb interactions (dynamic ids), in <see cref="DamLadders.InteractionComponents"/> order.</param>
/// <param name="Height">Its replicated climb height.</param>
public sealed record Ladder(int Index, string Cell, ulong Parent, ulong Guid, IReadOnlyList<ulong> Interactions, double Height);

/// <summary>
/// The Dam ladders. A placed ladder is only meshes; the game's server spawns a climbable actor for each one
/// (<see cref="DamLadders.Logic"/>), attached to it, with its climb height. A connection gets them in four
/// steps: the class default and the placed ladder are exported by path; the actor is spawned attached to the
/// placed ladder (it takes the ladder's location and rotation, so it is sent without one); its two climb
/// interactions are named; and they are enabled. The class default is found by path like the map objects.
/// </summary>
public sealed class LadderCatalog
{
    // Spawned-actor ids of the ladders: clear of the ids the world state hands out.
    private const ulong FirstGuid = 0x300000;

    private readonly ReplicationContext _replication;
    private readonly ObjectExports _exports;
    private readonly ObjectExports _interactionExports;
    private readonly ulong _archetype;

    /// <summary>Names the ladders.</summary>
    /// <param name="replication">Replication context (knows the interaction-state enum).</param>
    /// <param name="paths">Map path ids.</param>
    public LadderCatalog(ReplicationContext replication, MapPaths paths)
    {
        _replication = replication ?? throw new ArgumentNullException(nameof(replication));
        ArgumentNullException.ThrowIfNull(paths);
        // Found, never loaded: a load during play blocks the client's packet processing until it ends, and the
        // client drops the packets that arrive meanwhile. The placed ladders keep the class loaded (their logic
        // data refers to it), and a ladder is only sent once its cell is visible.
        string archetype = DamLadders.Logic.Archetype;
        int dot = archetype.LastIndexOf('.');
        _archetype = paths.MapObject(paths.MapObject(0, archetype[..dot]), archetype[(dot + 1)..]);

        List<Ladder> ladders = [];
        List<ObjectExport> interactions = [];
        int stride = 1 + DamLadders.InteractionComponents.Count;
        foreach (LadderPlacement placement in DamLadders.All)
        {
            ulong guid = FirstGuid + (2 * (ulong)(ladders.Count * stride));
            ulong[] components = [.. DamLadders.InteractionComponents.Select((_, index) => guid + (2 * (ulong)(index + 1)))];
            double height = LadderLogic.LadderHeight(placement.SplineTop, DamLadders.CycleHeight, DamLadders.EndInteractionDistance);
            ladders.Add(new Ladder(ladders.Count, placement.Cell, paths.MapObject(paths.Level(placement.Cell), placement.Actor), guid, components, height));
            interactions.Add(new DynamicActorExport(guid));
            interactions.AddRange(components.Select((component, index) => new StableSubobjectExport(component, guid, DamLadders.InteractionComponents[index], 0)));
        }

        _exports = paths.Exports();
        _interactionExports = new ObjectExports(interactions);
        Ladders = ladders;
    }

    /// <summary>All ladders.</summary>
    public IReadOnlyList<Ladder> Ladders { get; }

    /// <summary>Exports the class default and the placed ladder (with its cell).</summary>
    /// <param name="ladder">The ladder.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch ExportBunch(Ladder ladder)
    {
        ArgumentNullException.ThrowIfNull(ladder);
        return Bunches.Exports(_exports, [_archetype, ladder.Parent]);
    }

    /// <summary>Opens the ladder's channel: spawns the climbable actor attached to the placed ladder.</summary>
    /// <param name="ladder">The ladder.</param>
    /// <param name="channel">Channel index.</param>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch OpenBunch(Ladder ladder, int channel, int sequence)
    {
        ArgumentNullException.ThrowIfNull(ladder);
        const string Attachment = PropertyNames.AttachmentReplication;
        ActorContentBlock actor = _replication.Encode(DamLadders.ActorClass, new Dictionary<string, object?>
        {
            [PropertyNames.Role] = (ulong)NetRole.Authority,
            [PropertyNames.RemoteRole] = (ulong)NetRole.SimulatedProxy,
            [$"{Attachment}.{PropertyNames.AttachParent}"] = ladder.Parent,

            // The client applies the whole attachment; the default scale would be zero.
            [$"{Attachment}.{PropertyNames.RelativeScale3D}"] = Vector3D.One,
            [PropertyNames.LadderHeight] = ladder.Height,
        }, new Dictionary<ulong, string> { [ladder.Parent] = ClassPaths.Actor });
        return new ActorInitialization(new ActorSpawn(ladder.Guid, _archetype, 0, default), null, [actor]).ToBunch(channel, sequence, [ladder.Parent]);
    }

    /// <summary>Names the climb interactions inside the spawned actor.</summary>
    /// <param name="ladder">The ladder.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch InteractionExportBunch(Ladder ladder)
    {
        ArgumentNullException.ThrowIfNull(ladder);
        return Bunches.Exports(_interactionExports, ladder.Interactions);
    }

    /// <summary>Enables the climb interactions.</summary>
    /// <param name="ladder">The ladder.</param>
    /// <param name="channel">Channel index.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch InteractionBunch(Ladder ladder, int channel)
    {
        ArgumentNullException.ThrowIfNull(ladder);
        ActorContentBlock block = _replication.Encode(DamLadders.InteractionClass, new Dictionary<string, object?> { [PropertyNames.CurrentInteractionState] = (ulong)CurrentInteractionState.Idle });
        return Bunches.Actor(channel, writer =>
        {
            foreach (ulong interaction in ladder.Interactions)
            {
                new StableSubobjectContent(interaction, block).Write(writer);
            }
        });
    }
}
