using Ares.Raid.World;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Loot;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World;

public sealed class AdminTests
{
    [Fact]
    public void A_teleport_sends_a_frame_at_the_destination_and_holds_frames_until_the_client_reports_from_there()
    {
        ScriptedPlayer session = new();
        Vector3D spawn = session.Member.Spawn.Location;
        (Dictionary<int, PendulumSlot> state, uint frame, double at) = Settled(session, spawn);
        ulong button = session.Door.Buttons[0];

        Vector3D destination = new(spawn.X + 5000, spawn.Y - 2000, spawn.Z + 300);
        Assert.Equal(AdminRefusal.None, session.Channels.Teleport(destination, Math.PI / 2));
        Assert.Equal(AdminRefusal.TeleportPending, session.Channels.Teleport(destination));
        Assert.Equal(destination, session.Channels.CharacterPosition);

        session.Sent.Clear();
        session.Input(frame, frame + 1, position: spawn);
        session.Service(at);
        state = session.PawnFrame(state);
        Assert.Equal(1, session.Channels.Teleports);
        Assert.Equal(Quantized(destination), Position(state));
        (frame, at) = (frame + 1, at + 0.1);

        // Reports the client sent before it took the frame come from the old place: no frame may move it back.
        int frames = session.Channels.BlackboardFrames;
        session.Input(frame, frame + 3, focus: button, position: spawn);
        session.Service(at);
        Assert.Equal(frames, session.Channels.BlackboardFrames);
        Assert.True(session.Channels.TeleportPending);
        (frame, at) = (frame + 3, at + 0.1);

        // Once its reports come from the destination, frames follow them again.
        session.Sent.Clear();
        session.Input(frame, frame + 3, focus: button, position: destination);
        session.Service(at);
        Assert.False(session.Channels.TeleportPending);
        Assert.Equal(frames + 1, session.Channels.BlackboardFrames);
        // That frame carries the reported position, which the client's input quantizes more coarsely.
        (ulong X, ulong Y, ulong Z) reported = Position(session.PawnFrame(state));
        (ulong X, ulong Y, ulong Z) expected = Quantized(destination);
        Assert.All([(reported.X, expected.X), (reported.Y, expected.Y), (reported.Z, expected.Z)], axis => Assert.InRange((long)axis.Item1 - (long)axis.Item2, -10, 10));
        Assert.Equal(destination.X, session.Channels.CharacterPosition!.Value.X, 0);
    }

    [Fact]
    public void A_teleport_to_a_container_stands_the_character_in_front_of_its_prompt_facing_it()
    {
        ScriptedPlayer session = new();
        Settled(session, session.Member.Spawn.Location);
        LootContainer container = session.Raid.Loot.Containers[0];

        Assert.Equal(AdminRefusal.NoSuchContainer, session.Channels.TeleportToLoot(session.Raid.Loot.Containers.Count));
        Assert.Equal(AdminRefusal.None, session.Channels.TeleportToLoot(container.Index));

        Vector3D at = session.Channels.CharacterPosition!.Value;
        Vector3D prompt = container.Prompt;
        Assert.Equal(150, Math.Sqrt(((at.X - prompt.X) * (at.X - prompt.X)) + ((at.Y - prompt.Y) * (at.Y - prompt.Y))), 3);
        Assert.Equal(container.Location.Z + 50, at.Z, 3);
        Assert.Equal(Math.Atan2(prompt.Y - at.Y, prompt.X - at.X), session.Channels.CharacterYaw!.Value, 6);
    }

    [Fact]
    public void A_spawned_container_gets_new_ids_and_reaches_the_player()
    {
        ScriptedPlayer session = new();
        Vector3D spawn = session.Member.Spawn.Location;
        session.Possess();
        session.Service(100);
        session.Service(106);
        (uint frame, double at) = (30, 121.0);
        session.Input(frame, frame + 3, position: spawn);
        frame += 3;

        LootContainer[] before = [.. session.Raid.Loot.Containers];
        HashSet<ulong> used = [.. before.SelectMany(container => container.Components.Append(container.Guid))];
        LootContainerClass type = LootContainerClasses.All[0];
        Assert.Equal(AdminRefusal.None, session.Channels.SpawnLoot(type, out LootContainer? spawned));
        Assert.Equal(AdminRefusal.None, session.Channels.SpawnLoot(type, out LootContainer? second));
        Assert.NotNull(spawned);
        Assert.NotNull(second);
        Assert.Equal((before.Length, before.Length + 1), (spawned.Index, second.Index));
        Assert.Equal(LootContainerSource.Operator, spawned.Source);
        Assert.Same(type, spawned.Class);
        Assert.All(spawned.Components.Append(spawned.Guid).Concat(second.Components.Append(second.Guid)), id => Assert.DoesNotContain(id, used));
        Assert.NotEqual(spawned.Guid, second.Guid);
        Assert.Same(spawned, session.Raid.Loot.ByInteraction(spawned.Interaction));
        Assert.Equal(200, Math.Sqrt(((spawned.Location.X - spawn.X) * (spawned.Location.X - spawn.X)) + ((spawned.Location.Y - spawn.Y) * (spawned.Location.Y - spawn.Y))), 0);

        for (int step = 0; step < 300 && !session.Channels.HasLoot(spawned.Index); step++)
        {
            session.Input(frame, frame + 3, position: spawn);
            session.Service(at);
            (frame, at) = (frame + 3, at + 0.3);
        }

        Assert.True(session.Channels.HasLoot(spawned.Index));
        Assert.NotNull(session.Raid.LootStates.Items(spawned.Index)[0]);
    }

    // Possession, the map and doors, then standing inputs until the held items are linked; returns the character's
    // latest frame state, the next input frame and the next service time.
    private static (Dictionary<int, PendulumSlot> State, uint Frame, double At) Settled(ScriptedPlayer session, Vector3D position)
    {
        session.Possess();
        session.Sent.Clear();
        foreach (double time in (double[])[100.0, 106.0, 121.0, 121.5, 122.0])
        {
            session.Service(time);
        }

        Dictionary<int, PendulumSlot> state = session.SpawnState;
        (uint frame, double at) = (30, 122.5);
        for (int step = 0; step < 20 && session.Channels.BlackboardFrames < ScriptedPlayer.SettledFrames; step++)
        {
            int frames = session.Channels.BlackboardFrames;
            session.Sent.Clear();
            session.Input(frame, frame + 3, position: position);
            session.Service(at);
            if (session.Channels.BlackboardFrames > frames)
            {
                state = session.PawnFrame(state);
            }

            (frame, at) = (frame + 3, at + 0.5);
        }

        Assert.Equal(ScriptedPlayer.SettledFrames, session.Channels.BlackboardFrames);
        return (state, frame, at);
    }

    private static (ulong X, ulong Y, ulong Z) Position(Dictionary<int, PendulumSlot> state)
    {
        PendulumSlot movement = state[PawnBlackboard.MovementStateSlot];
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[Assert.NotNull(movement.DescriptorId)].Fields;
        return (Field(PawnBlackboard.PositionXField), Field(PawnBlackboard.PositionYField), Field(PawnBlackboard.PositionZField));

        ulong Field(int field) => PendulumCodec.Integer((PendulumScalarField)fields[field], movement.Value);
    }

    private static (ulong X, ulong Y, ulong Z) Quantized(Vector3D world) => (Axis(world.X), Axis(world.Y), Axis(world.Z));

    private static ulong Axis(double world) => (ulong)Math.Round((world + PawnBlackboard.PositionRange) * ((1 << 24) - 1) / (2 * PawnBlackboard.PositionRange));
}
