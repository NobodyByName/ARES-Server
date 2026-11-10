using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Replication;

/// <summary>Reliable bunches the server queues; the sender assigns their sequence numbers.</summary>
public static class Bunches
{
    /// <summary>Channel close reason when the actor no longer exists.</summary>
    public const int Destroyed = 0;

    /// <summary>Channel close reason when the actor's level is no longer visible.</summary>
    public const int LevelUnloaded = 2;

    /// <summary>Object exports on the control channel.</summary>
    /// <param name="exports">The export records.</param>
    /// <param name="roots">Ids to export with their outers.</param>
    /// <returns>The bunch.</returns>
    public static ChannelBunch Exports(ObjectExports exports, IReadOnlyList<ulong> roots)
    {
        ArgumentNullException.ThrowIfNull(exports);
        BitWriter writer = new();
        exports.Write(writer, roots);
        return new ChannelBunch(0, writer.ToArray(), writer.Length) { Name = ChannelName.Control, PackageExports = true };
    }

    /// <summary>Content on an open actor channel.</summary>
    /// <param name="channel">Channel index.</param>
    /// <param name="write">Writes the content.</param>
    /// <returns>The bunch.</returns>
    public static ChannelBunch Actor(int channel, Action<BitWriter> write)
    {
        ArgumentNullException.ThrowIfNull(write);
        BitWriter writer = new();
        write(writer);
        return new ChannelBunch(channel, writer.ToArray(), writer.Length) { Name = ChannelName.Actor };
    }

    /// <summary>Content blocks on an open actor channel.</summary>
    /// <param name="channel">Channel index.</param>
    /// <param name="blocks">The blocks.</param>
    /// <returns>The bunch.</returns>
    public static ChannelBunch Actor(int channel, params IReadOnlyList<ActorContentBlock> blocks) =>
        Actor(channel, writer =>
        {
            foreach (ActorContentBlock block in blocks)
            {
                block.Write(writer);
            }
        });

    /// <summary>Closes an actor channel.</summary>
    /// <param name="channel">Channel index.</param>
    /// <param name="reason">Close reason.</param>
    /// <returns>The bunch.</returns>
    public static ChannelBunch Close(int channel, int reason) =>
        new(channel, [], 0) { Name = ChannelName.Actor, CloseReason = reason };
}
