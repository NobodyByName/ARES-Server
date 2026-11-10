using Ares.Raid.Transport;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Net.Session;

namespace Ares.Raid.Tests.Transport;

public sealed class ReliableChannelTests
{
    private static ChannelBunch Control(string data = "x", int? sequence = null, bool opened = false) =>
        new(0, System.Text.Encoding.ASCII.GetBytes(data), data.Length * 8) { Name = ChannelName.Control, Sequence = sequence, Opened = opened };

    private static ChannelBunch Actor(int channel, string data = "", int? sequence = null, bool opened = false, int? close = null) =>
        new(channel, System.Text.Encoding.ASCII.GetBytes(data), data.Length * 8)
        {
            Name = sequence is not null || opened ? ChannelName.Actor : null,
            Sequence = sequence,
            Opened = opened,
            CloseReason = close,
        };

    // A bunch to queue: reliable, so it carries its channel type; the sender assigns the sequence.
    private static ChannelBunch Queued(int channel, string data = "", bool opened = false, int? close = null) =>
        Actor(channel, data, opened: opened, close: close) with { Name = ChannelName.Actor };

    private static NotificationHeader Ack(int sequence, int acknowledged, uint history) => new(sequence, acknowledged, [history]);

    [Fact]
    public void Lost_and_reordered_acknowledgements_complete_in_channel_order()
    {
        // Packet sequences wrap at 16384; each channel's reliable sequence starts from the seed.
        ReliableSender sender = new(16383);
        ReliableKey opened = sender.Queue(Queued(1, "A", opened: true));
        ReliableKey update = sender.Queue(Queued(1, "B"));
        ReliableKey other = sender.Queue(Control());
        Assert.Equal([0, 1, 0], sender.Bunches([opened, update, other]).Select(bunch => bunch.Sequence!.Value));
        Assert.Equal([opened], sender.Select(0, 1000, 1));
        sender.Sent(16383, [opened], 0);
        Assert.Equal([update], sender.Select(0.01, 1000, 1));
        sender.Sent(0, [update], 0.01);

        // The update is acknowledged but the open was lost: neither completes, the open is not acknowledged.
        Assert.Empty(sender.Acknowledge(Ack(0, 0, 1)));
        Assert.True(sender.Pending(opened) && sender.Pending(update));
        Assert.DoesNotContain(1, sender.OpenAcknowledged);
        Assert.Equal([opened], sender.Select(0.02, 1000, 1));
        sender.Sent(1, [opened], 0.02);
        Assert.Equal([opened, update], sender.Acknowledge(Ack(1, 1, 1)).Select(record => record.Key));
        Assert.Contains(1, sender.OpenAcknowledged);
        Assert.True(sender.Pending(other));
        Assert.Throws<InvalidPacketException>(() => sender.Acknowledge(Ack(2, 2, 1)));
        Assert.Throws<InvalidPacketException>(() => sender.Acknowledge(Ack(2, 0, 1)));
    }

    [Fact]
    public void Unreliable_bunches_are_sent_once_and_report_whether_they_arrived()
    {
        ReliableSender sender = new(10);
        ReliableKey open = sender.Queue(Queued(3, "A", opened: true));
        sender.Sent(10, [open], 0);
        sender.Acknowledge(Ack(0, 10, 1));

        ReliableKey lost = sender.QueueUnreliable(Queued(3, "B"));
        ReliableKey arrived = sender.QueueUnreliable(Queued(3, "C"));
        Assert.All(sender.Bunches([lost, arrived]), bunch => Assert.True(bunch.Sequence is null && bunch.Name is null));
        Assert.Equal([lost, arrived], sender.Select(0, 1000));
        sender.Sent(11, [lost], 0);
        Assert.Equal([arrived], sender.Select(5, 1000));
        sender.Sent(12, [arrived], 5);
        Assert.Null(sender.Arrived(arrived));

        // Packet 12 arrived, packet 11 did not: nothing is resent.
        sender.Acknowledge(Ack(1, 12, 1));
        Assert.Equal((false, true), (sender.Arrived(lost), sender.Arrived(arrived)));
        Assert.False(sender.Pending(lost) || sender.Pending(arrived));
        Assert.Empty(sender.Select(10, 1000));

        // Closing the channel drops unreliable data not sent yet.
        ReliableKey unsent = sender.QueueUnreliable(Queued(3, "D"));
        ReliableKey close = sender.Queue(Queued(3, close: 1));
        Assert.False(sender.Arrived(unsent));
        Assert.Equal([close], sender.Select(10, 1000));
        Assert.Throws<InvalidPacketException>(() => sender.QueueUnreliable(Queued(3, "E")));
    }

    [Fact]
    public void Retries_resend_the_same_bunch_in_new_packets()
    {
        ReliableSender sender = new(50);
        ReliableKey key = sender.Queue(Control("challenge"));
        ChannelBunch wire = sender.Bunches([key])[0];
        sender.Sent(50, [key], 100);
        Assert.Empty(sender.Select(100.9, 1000));
        Assert.Equal([key], sender.Select(101.1, 1000));
        sender.Sent(51, [key], 101.1);
        Assert.Same(wire, sender.Bunches([key])[0]);
        Assert.Equal([key], sender.Acknowledge(Ack(1, 51, 2)).Select(record => record.Key));
        Assert.Empty(sender.Select(102.2, 1000));
    }

    [Fact]
    public void Closed_channels_need_a_new_open_and_keep_their_counter()
    {
        ReliableSender sender = new(10);
        ReliableKey open = sender.Queue(Queued(2, opened: true));
        sender.Sent(10, [open], 0);
        sender.Acknowledge(Ack(0, 10, 1));
        ReliableKey close = sender.Queue(Queued(2, close: 0));
        Assert.Throws<InvalidPacketException>(() => sender.Queue(Queued(2)));
        sender.Sent(11, [close], 0.1);
        sender.Acknowledge(Ack(1, 11, 3));
        Assert.Throws<InvalidPacketException>(() => sender.Queue(Queued(2)));
        ReliableKey reopened = sender.Queue(Queued(2, opened: true));
        Assert.Equal(13, sender.Bunches([reopened])[0].Sequence);
        Assert.DoesNotContain(2, sender.OpenAcknowledged);
        Assert.Throws<InvalidPacketException>(() => sender.Queue(Queued(2, opened: true)));
        Assert.Throws<InvalidPacketException>(() => sender.Queue(Control(sequence: 999)));
        Assert.Throws<InvalidPacketException>(() => sender.Queue(Control(new string('x', 1000))));
    }

    [Fact]
    public void Incoming_bunches_are_ordered_deduplicated_and_owned_by_the_server()
    {
        ReliableReceiver receiver = new(16383);
        ChannelBunch hello = Control("hello", 0, true);
        ChannelBunch login = Control("login", 1);
        Assert.Empty(receiver.Receive(login));
        Assert.Empty(receiver.Receive(login));
        Assert.Throws<InvalidPacketException>(() => receiver.Receive(login with { Payload = [.. "other"u8], PayloadBits = 40 }));
        Assert.Equal([hello, login], receiver.Receive(hello));
        Assert.Empty(receiver.Receive(hello));
        Assert.Throws<InvalidPacketException>(() => receiver.Receive(hello with { Payload = [.. "wrong"u8] }));

        receiver.RegisterActor(1);
        ChannelBunch actor0 = Actor(1, "a", 0);
        Assert.Throws<InvalidPacketException>(() => receiver.Receive(actor0 with { MustBeMapped = true }));
        ChannelBunch actor1 = Actor(1, "b", 1);
        ChannelBunch actor2 = Actor(1, "c", 2);
        Assert.Empty(receiver.Receive(actor2));
        Assert.Equal([actor0], receiver.Receive(actor0));
        Assert.Equal([actor1, actor2], receiver.Receive(actor1));
        ChannelBunch unreliable = Actor(1, "u");
        Assert.Equal([unreliable], receiver.Receive(unreliable));
        Assert.Throws<InvalidPacketException>(() => receiver.Receive(Actor(2, sequence: 0, opened: true)));
        Assert.Throws<InvalidPacketException>(() => receiver.Receive(Actor(1, sequence: 3, opened: true)));
        receiver.CloseActor(1);
        Assert.Throws<InvalidPacketException>(() => receiver.Receive(Actor(1, sequence: 3)));
        receiver.RegisterActor(1);
        Assert.NotEmpty(receiver.Receive(Actor(1, sequence: 3)));
    }

    [Fact]
    public void A_peer_close_is_delivered_once()
    {
        ReliableReceiver receiver = new(0);
        receiver.RegisterActor(3);
        ChannelBunch close = Actor(3, sequence: 1, close: 0);
        Assert.Equal([close], receiver.Receive(close));
        Assert.Empty(receiver.Receive(close));
        Assert.Throws<InvalidPacketException>(() => receiver.Receive(Actor(3, sequence: 2)));
        receiver.RegisterActor(3);
        Assert.NotEmpty(receiver.Receive(Actor(3, sequence: 2)));
    }

    [Fact]
    public void Buffers_and_retries_are_bounded()
    {
        ReliableReceiver receiver = new(0);
        for (int sequence = 2; sequence < 257; sequence++)
        {
            Assert.Empty(receiver.Receive(Control(sequence: sequence)));
        }

        Assert.Equal(256, receiver.Receive(Control(sequence: 1, opened: true)).Count);
        Assert.Throws<ReliableOverflowException>(() => new ReliableReceiver(0).Receive(Control(sequence: 257)));

        ReliableSender sender = new(0);
        for (int index = 0; index < 255; index++)
        {
            sender.Queue(Control());
        }

        Assert.Throws<ReliableOverflowException>(() => sender.Queue(Control()));

        sender = new ReliableSender(0);
        ReliableKey key = sender.Queue(Control());
        for (int attempt = 0; attempt < 64; attempt++)
        {
            sender.Sent(attempt, [key], attempt);
        }

        Assert.Throws<ReliableOverflowException>(() => sender.Select(65, 1000));
    }

    [Fact]
    public void The_packet_window_rejects_replays_and_unsent_acknowledgements()
    {
        SequenceSeeds seeds = SequenceSeeds.FromCookie([0xFF, 0xFF, 0x01, 0x40, .. new byte[16]]);
        Assert.Equal(new SequenceSeeds(16383, 1), seeds);
        PacketWindow window = new(seeds.ClientPacket, seeds.ServerPacket);
        NotificationHeader first = new(1, 16382, [0]);
        Assert.True(window.Receive(first));
        Assert.False(window.Receive(first));
        Assert.False(window.Receive(new NotificationHeader(2, 16383, [0])));
        NotificationHeader next = window.NextHeader();
        Assert.Equal((16383, 1, 1U), (next.Sequence, next.Acknowledged, next.History[0]));
        Assert.Equal(16383, window.CommitSent());
        Assert.Equal(0, window.NextOutgoing);
        Assert.True(window.Receive(new NotificationHeader(3, 16383, [1])));
        Assert.Equal(5U, window.NextHeader().History[0]);
        Assert.False(window.Receive(new NotificationHeader(2, 16383, [1])));
    }
}
