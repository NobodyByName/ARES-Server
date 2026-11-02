using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Replication;

/// <summary>A map actor the server replicates on its own channel (it already exists on the client).</summary>
/// <param name="Channel">Channel index.</param>
/// <param name="Guid">Its static id (exported with the map imports).</param>
/// <param name="ClassPath">Its class.</param>
/// <param name="Content">Initial content.</param>
public sealed record StaticWorldActor(int Channel, ulong Guid, string ClassPath, IReadOnlyList<ActorContentBlock> Content)
{
    /// <summary>The channel's opening bunch.</summary>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch Open(int sequence) => new StaticActorInitialization(new StaticActorReference(Guid), Content).ToBunch(Channel, sequence);
}

/// <summary>A server-spawned world actor (not owned by a player).</summary>
/// <param name="Channel">Channel index.</param>
/// <param name="Guid">Its dynamic id.</param>
/// <param name="Archetype">Exported archetype id.</param>
/// <param name="RequiredGuids">Other exported ids the client resolves first.</param>
/// <param name="Content">Initial content.</param>
public sealed record SpawnedWorldActor(int Channel, ulong Guid, ulong Archetype, IReadOnlyList<ulong> RequiredGuids, IReadOnlyList<ActorContentBlock> Content)
{
    /// <summary>The channel's opening bunch.</summary>
    /// <param name="sequence">Reliable sequence number.</param>
    /// <returns>The bunch.</returns>
    public ChannelBunch Open(int sequence) =>
        new ActorInitialization(new ActorSpawn(Guid, Archetype, 0, default), null, Content).ToBunch(Channel, sequence, RequiredGuids);
}
