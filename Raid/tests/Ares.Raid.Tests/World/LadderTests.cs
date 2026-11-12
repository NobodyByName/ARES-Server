using Ares.Raid.World;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Replication;
using Ares.Structs.Game.World;
using Ares.Structs.Net.Replication;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.World;

public sealed class LadderTests
{
    [Fact]
    public void A_ladder_spawns_attached_to_its_placed_ladder_with_its_climb_height()
    {
        ScriptedPlayer session = new();
        LadderCatalog catalog = session.Raid.Ladders;
        Ladder ladder = catalog.Ladders.Single(item => DamLadders.All[item.Index].Actor == "BP_Ladder_WallEnd_01_C_1");
        Assert.Equal(DamLadders.All.Count, catalog.Ladders.Count);
        Assert.Equal(824.0, ladder.Height, 9);

        ChannelBunch bunch = catalog.OpenBunch(ladder, 300, 1);
        Assert.True(bunch is { Opened: true, MustBeMapped: true });
        BitReader reader = new(bunch.Payload, bunch.PayloadBits);
        IReadOnlyList<ulong> required = MustMapGuids.Read(reader).Guids;
        Assert.Equal(ladder.Parent, required[1]);

        // The spawn: the actor, its class default, the persistent level and no location of its own.
        Assert.Equal((ladder.Guid, required[0], 0UL), (NetGuid.Read(reader), NetGuid.Read(reader), NetGuid.Read(reader)));
        Assert.Equal((false, 0UL), (reader.ReadBit(), reader.ReadBits(3)));

        ValueCodec values = new(GameReplication.Catalog, [KnownEnums.NetRole], new Dictionary<ulong, string> { [ladder.Parent] = ClassPaths.Actor });
        Dictionary<string, object?> content = new PropertyCodec(values).Decode(new PropertyLayout(GameReplication.Catalog, DamLadders.ActorClass), ActorContentBlock.Read(reader), swapActorRoles: false);
        Assert.Equal(0, reader.Remaining);
        Assert.Equal(ladder.Parent, content[$"{PropertyNames.AttachmentReplication}.{PropertyNames.AttachParent}"]);
        Assert.Equal(Vector3D.One, content[$"{PropertyNames.AttachmentReplication}.{PropertyNames.RelativeScale3D}"]);
        Assert.Equal(ladder.Height, content[PropertyNames.LadderHeight]);
        Assert.Equal((ulong)NetRole.SimulatedProxy, content[PropertyNames.RemoteRole]);
    }

    [Fact]
    public void Ladder_exports_never_ask_the_client_to_load()
    {
        // A load during play blocks the client's packet processing and it drops the packets meanwhile.
        ScriptedPlayer session = new();
        Ladder ladder = session.Raid.Ladders.Ladders[0];
        ChannelBunch bunch = session.Raid.Ladders.ExportBunch(ladder);
        (ObjectExports exports, IReadOnlyList<ulong> roots) = ObjectExports.Read(new BitReader(bunch.Payload, bunch.PayloadBits));
        Assert.Contains(ladder.Parent, roots);
        StaticObjectExport[] records = [.. exports.Records.Values.OfType<StaticObjectExport>()];
        Assert.Contains(records, record => record.Path == DamLadders.Logic.Archetype.Split('.')[1]);
        Assert.All(records, record => Assert.True(record.NoLoad, record.Path));
    }

    [Fact]
    public void Ladders_of_visible_cells_get_their_actor_and_climb_interactions_and_keep_them()
    {
        ScriptedPlayer session = new();
        session.Possess();
        foreach (double at in (double[])[100.0, 106.0, 121.0, 121.5, 122.0, 122.5, 123.0, 123.5, 124.0, 124.5])
        {
            session.Service(at);
        }

        Ladder[] visible = [.. session.Raid.Ladders.Ladders.Where(ladder => ladder.Cell == session.Door.Cell)];
        Assert.NotEmpty(visible);
        Assert.Equal(visible.Length, session.Channels.LaddersReady);
        int firstChannel = 64 + session.Raid.Doors.Doors.Count;
        Assert.Equal(
            visible.Select(ladder => firstChannel + ladder.Index).Order(),
            session.Sent.Where(bunch => bunch is { Opened: true } && bunch.Channel >= firstChannel && bunch.Channel < firstChannel + session.Raid.Ladders.Ladders.Count).Select(bunch => bunch.Channel).Order());

        // Leaving the cell keeps the ladders: the game's own ladder actors outlive their cell.
        session.Sent.Clear();
        session.ReportVisible((session.Door.Cell, false, false));
        session.Service(125.0);
        Assert.DoesNotContain(session.Sent, bunch => bunch.Channel >= firstChannel);
        Assert.Equal(visible.Length, session.Channels.LaddersReady);

        // The client sends only channel bookkeeping on a ladder channel; a failed ladder is not offered again.
        session.Channels.Receive(session.Io, new ChannelBunch(firstChannel + visible[0].Index, [], 0) { Name = ChannelName.Actor });
        Ladder elsewhere = session.Raid.Ladders.Ladders.First(ladder => ladder.Cell != session.Door.Cell);
        session.Channels.ActorChannelFailed(firstChannel + elsewhere.Index);
        session.ReportVisible((elsewhere.Cell, true, false));
        foreach (double at in (double[])[126.0, 126.5, 127.0, 127.5])
        {
            session.Service(at);
        }

        int[] opened = [.. session.Sent.Where(bunch => bunch is { Opened: true }).Select(bunch => bunch.Channel)];
        Assert.Contains(session.Raid.Ladders.Ladders, ladder => ladder != elsewhere && ladder.Cell == elsewhere.Cell && opened.Contains(firstChannel + ladder.Index));
        Assert.DoesNotContain(firstChannel + elsewhere.Index, opened);
    }
}
