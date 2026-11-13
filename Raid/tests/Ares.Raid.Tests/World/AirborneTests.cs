using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Movement;

namespace Ares.Raid.Tests.World;

public sealed class AirborneTests
{
    [Fact]
    public void A_frame_waits_until_a_falling_character_has_landed()
    {
        ScriptedPlayer session = new();
        session.Possess();
        (uint frame, double at) = session.Settle(30, 100);
        // Doors are usable once the client has them.
        foreach (double later in (double[])[at + 6, at + 21, at + 21.5, at + 22])
        {
            session.Service(later);
        }

        at += 22.5;
        ulong button = session.Door.Buttons[0];
        Vector3D position = session.Member.Spawn.Location;
        double speed = 0;
        for (int tick = 0; tick < 12; tick++)
        {
            speed += AirMovementParameters.FallAcceleration * PawnBlackboard.TickSeconds * PawnBlackboard.TickSeconds;
            position = position with { Z = position.Z - speed };
            Report(tick >= 4 ? button : 0);
        }

        Assert.Equal(ScriptedPlayer.SettledFrames, session.Channels.BlackboardFrames);
        Assert.True(session.Channels.SimulatedCharacter?.Unsettled);

        for (int tick = 0; tick < 6; tick++)
        {
            Report(button);
        }

        Assert.Equal(ScriptedPlayer.SettledFrames + 1, session.Channels.BlackboardFrames);
        Assert.False(session.Channels.SimulatedCharacter?.Unsettled);
        return;

        void Report(ulong focus)
        {
            session.Input(frame, frame + 1, focus: focus, position: position);
            session.Service(at);
            (frame, at) = (frame + 1, at + 0.05);
        }
    }
}
