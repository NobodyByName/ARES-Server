using Ares.Common.Game.Enemies;
using Ares.Raid.World.Actors;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Core.Serialization;
using Ares.Structs.Game.Enemies;
using Ares.Structs.Game.Pendulum;
using Ares.Structs.Game.World;

namespace Ares.Raid.Tests.World;

public sealed class ArcFlightTests
{
    private static readonly ArcMovementProfile s_profile = new(CruiseSpeed: 320, Acceleration: 300, Deceleration: 200, VelocityBlend: 0.3, TurnRate: 1.2, PitchPerSpeed: 5.3,
        BankPerTurnRate: 0.2, MaxBank: 12, HoverHeight: 200, BodyRadius: 60, HomePointSpacing: 1.4, MinHomePointSpacing: 1000, ReplanSeconds: 10, ProbeDistance: 4000, ProbeAngles: [-50, -25, 0, 25],
        WanderShare: 0.3, MinDwellSeconds: 1, MaxDwellSeconds: 10, TurnBackChance: 0.25, ArriveDistance: 150);

    // A 200 m square loop at 50 m height.
    private static readonly SplinePathPlacement s_square = new("Square", true)
    {
        Points = [new Vector3D(0, 0, 5000), new Vector3D(20000, 0, 5000), new Vector3D(20000, 20000, 5000), new Vector3D(0, 20000, 5000)],
    };

    private static readonly ArcSpawner s_spawner = new("Spawner", "Spawner", "BP_SplineStaticSpawner_AirSmall", new Vector3D(19000, 1000, 4000), 0,
        "EGT_StaticWorld_WaspSingle", ["DA_EnemyType_LightDrone_02"], ["Square"], 4500, 2000);

    [Fact]
    public void Home_points_lie_evenly_along_the_route_s_curve_or_at_the_spawner_without_one()
    {
        ArcHomePoints homes = ArcRoutes.HomePoints(s_spawner, s_square, 6300);
        Assert.True(homes.Closed);
        // The curve runs through every route point, closes on its first, and rounds the corners (a bare square bulges most).
        Assert.All(s_square.Points, point => Assert.Contains(point, homes.Line));
        Assert.Equal(homes.Line[0], homes.Line[^1]);
        Assert.InRange(homes.Line.Max(DistanceToSquare), 1, 3000);
        Assert.InRange(homes.Length, 80000, 86000);
        // Home points from its start, 63 m apart along it; the last one is not crowded onto the first.
        Assert.Equal(s_square.Points[0], homes.Points[0]);
        Assert.All(homes.Distances.Select((distance, index) => distance - (index * 6300.0)), offset => Assert.Equal(0, offset, 1e-6));
        Assert.All(homes.Points.Zip(homes.Distances), pair => Assert.True(ArcRoutes.Distance(pair.First, homes.PointAt(pair.Second)) < 1e-6));
        Assert.True(homes.Length - homes.Distances[^1] >= 3150);
        int nearest = Enumerable.Range(0, homes.Points.Count).MinBy(index => ArcRoutes.Distance(homes.Points[index], s_spawner.Location));
        Assert.Equal(nearest, homes.Nearest(s_spawner.Location));

        ArcHomePoints alone = ArcRoutes.HomePoints(s_spawner, null, 6300);
        Assert.Equal([s_spawner.Location], alone.Points);
        Assert.False(alone.Closed);
    }

    [Fact]
    public void An_open_route_s_curve_ends_at_its_end_points_with_samples_at_most_3_m_apart()
    {
        List<Vector3D> curve = ArcRoutes.Curve([new Vector3D(0, 0, 0), new Vector3D(1000, 0, 0), new Vector3D(1000, 1000, 0)], false);
        Assert.Equal(new Vector3D(0, 0, 0), curve[0]);
        Assert.Equal(new Vector3D(1000, 1000, 0), curve[^1]);
        Assert.Contains(new Vector3D(1000, 0, 0), curve);
        Assert.All(curve.Zip(curve.Skip(1)), pair => Assert.InRange(ArcRoutes.Distance(pair.First, pair.Second), 1, ArcRoutes.SampleSpacing + 50));
    }

    [Fact]
    public void An_idle_flyer_wanders_from_home_point_to_home_point_near_its_route_at_cruise_speed()
    {
        ArcHomePoints homes = ArcRoutes.HomePoints(s_spawner, s_square, 6300);
        ArcFlight flight = new(s_profile, homes, 3, s_spawner.GroupSearchRadius, null, 7);
        Assert.False(flight.Pose.Flying);
        Assert.Equal(homes.Points[3], flight.Pose.Location);

        HashSet<int> visited = [];
        int still = 0;
        double fastest = 0;
        double farthest = 0;
        const int Seconds = 600;
        flight.Advance(0);
        for (int step = 1; step <= Seconds * 10; step++)
        {
            flight.Advance(step / 10.0);
            ArcPose pose = flight.Pose;
            double horizontal = Math.Sqrt((pose.Velocity.X * pose.Velocity.X) + (pose.Velocity.Y * pose.Velocity.Y));
            fastest = Math.Max(fastest, horizontal);
            still += horizontal < 10 ? 1 : 0;
            farthest = Math.Max(farthest, DistanceToSquare(pose.Location));
            visited.Add(flight.Home);
        }

        Assert.True(flight.Pose.Flying);
        Assert.InRange(fastest, 250, s_profile.CruiseSpeed + 1);
        // It never strays beyond its wander radius from the route, and spends some of its time paused.
        Assert.InRange(farthest, 100, s_spawner.GroupSearchRadius);
        Assert.InRange(still / (Seconds * 10.0), 0.03, 0.5);
        Assert.True(visited.Count >= 5, $"visited {visited.Count} home points");
        // With nothing in its way it never gives up on a home point.
        Assert.Equal(0, flight.Detours);
    }

    [Fact]
    public void A_flight_is_the_same_for_the_same_seed_and_steps_in_fixed_time()
    {
        ArcHomePoints homes = ArcRoutes.HomePoints(s_spawner, s_square, 6300);
        ArcFlight coarse = new(s_profile, homes, 3, 4500, null, 11);
        ArcFlight fine = new(s_profile, homes, 3, 4500, null, 11);
        ArcFlight other = new(s_profile, homes, 3, 4500, null, 12);
        coarse.Advance(0);
        fine.Advance(0);
        other.Advance(0);
        for (int step = 1; step <= 600; step++)
        {
            fine.Advance(step * 0.05);
            if (step % 20 != 0)
            {
                continue;
            }

            coarse.Advance(step * 0.05);
            other.Advance(step * 0.05);
        }

        Assert.Equal(fine.Pose, coarse.Pose);
        Assert.NotEqual(fine.Pose.Location, other.Pose.Location);
    }

    [Fact]
    public void A_fresh_arc_s_state_holds_its_initial_variables_and_only_its_position_in_the_transform()
    {
        ArcBlackboard wasp = Arcs.Wasp.Blackboard;
        IReadOnlyDictionary<int, PendulumSlot> state = ArcFrames.State(wasp, new ArcPose(new Vector3D(465150, 584650, -37718), default, 0, 0, false));
        Assert.Equal(Enumerable.Range(0, 9), state.Keys.Order());
        Assert.Equal(8390136UL, Field(state, 4, 0));
        Assert.Equal(80UL, Field(state, 4, 4));
        Assert.Equal(1UL, Field(state, 7, 0));
        Assert.Equal(PendulumCodec.Default.DefaultValue(17), state[0].Value);

        PendulumSlot transform = state[wasp.TransformSlot];
        byte[] defaults = PendulumCodec.Default.DefaultValue(ArcTransform.DescriptorId);
        IReadOnlyList<PendulumField> fields = PendulumRegistry.Descriptors[ArcTransform.DescriptorId].Fields;
        Assert.Equal(PendulumCodec.Quaternion((PendulumQuaternionField)fields[ArcTransform.RotationField], defaults),
            PendulumCodec.Quaternion((PendulumQuaternionField)fields[ArcTransform.RotationField], transform.Value));
        Assert.Equal([256UL, 256UL, 256UL], Enumerable.Range(ArcTransform.VelocityXField, 3).Select(field => Field(state, 6, field)));
        Assert.Equal(0UL, Field(state, 6, ArcTransform.UnknownFlightField));
        // Positions on the 24-bit range around the origin.
        Assert.Equal(13266058UL, Field(state, 6, ArcTransform.PositionXField));
    }

    [Fact]
    public void A_flying_frame_carries_rotation_velocity_and_position_and_reads_back_as_the_client_reads_it()
    {
        ArcBlackboard wasp = Arcs.Wasp.Blackboard;
        IReadOnlyDictionary<int, PendulumSlot> fresh = ArcFrames.State(wasp, new ArcPose(new Vector3D(1000, 2000, 300), default, 0, 0, false));
        ArcPose flying = new(new Vector3D(1100, 2000, 300), new Vector3D(320, 0, -15.6), Math.PI / 2, -17 * Math.PI / 180, true);
        IReadOnlyDictionary<int, PendulumSlot> state = ArcFrames.State(wasp, flying);
        Assert.Equal([297UL, 256UL, 254UL], Enumerable.Range(ArcTransform.VelocityXField, 3).Select(field => Field(state, 6, field)));
        Assert.Equal(ArcTransform.UnknownFlightValue, Field(state, 6, ArcTransform.UnknownFlightField));
        PendulumQuaternionField rotation = (PendulumQuaternionField)PendulumRegistry.Descriptors[ArcTransform.DescriptorId].Fields[ArcTransform.RotationField];
        PendulumQuaternion quaternion = PendulumCodec.Quaternion(rotation, state[6].Value);
        Assert.Equal(90, PendulumRotations.Yaw(quaternion, rotation) * 180 / Math.PI, 0.5);
        Assert.Equal(-17, PendulumRotations.Pitch(quaternion, rotation) * 180 / Math.PI, 0.5);

        // Reset, then a delta against it: the client reads both with the field set their header names.
        Dictionary<int, PendulumSlot> received = Read(0, 1, fresh, new Dictionary<int, PendulumSlot>(), out BlackboardUpdateHeader reset);
        Assert.Equal((0U, 1U, true, PendulumFieldFlags.Others), (reset.Baseline, reset.Latest, reset.Reset, reset.Fields));
        Assert.Equal(fresh[6].Value, received[6].Value);
        received = Read(1, 2, state, fresh, out BlackboardUpdateHeader delta, received);
        Assert.Equal((1U, 2U, false), (delta.Baseline, delta.Latest, delta.Reset));
        Assert.Equal(state[6].Value, received[6].Value);
    }

    [Fact]
    public void Frames_go_less_often_the_farther_the_client_is()
    {
        Assert.Equal(1.0 / 30, ArcFrames.Interval(0));
        Assert.Equal(0.07, ArcFrames.Interval(5000));
        Assert.Equal(0.47, ArcFrames.Interval(14999));
        Assert.Equal(0.7, ArcFrames.Interval(19999));
        Assert.Equal(1.0, ArcFrames.Interval(30000));
    }

    private static ulong Field(IReadOnlyDictionary<int, PendulumSlot> state, int slot, int field) =>
        PendulumCodec.Integer(PendulumRegistry.Descriptors[state[slot].DescriptorId!.Value].Fields[field], state[slot].Value);

    // Writes a frame and reads it back: its header, then its body with the Others codec against the client's baseline.
    private static Dictionary<int, PendulumSlot> Read(uint baseline, uint frame, IReadOnlyDictionary<int, PendulumSlot> state, IReadOnlyDictionary<int, PendulumSlot> previous,
        out BlackboardUpdateHeader header, Dictionary<int, PendulumSlot>? clientBaseline = null)
    {
        BitWriter writer = new();
        ArcFrames.Write(writer, baseline, frame, state, previous);
        BitReader reader = new(writer.ToArray(), writer.Length);
        Assert.True(reader.ReadBit());
        header = BlackboardUpdate.ReadHeader(reader);
        Dictionary<int, PendulumSlot> result = PendulumCodec.Others.ReadFrame(reader, header.Reset ? new Dictionary<int, PendulumSlot>() : clientBaseline!);
        Assert.Equal(0, reader.Remaining);
        return result;
    }

    // Horizontal distance from a point to the square loop's edges.
    private static double DistanceToSquare(Vector3D at)
    {
        double x = Math.Clamp(at.X, 0, 20000);
        double y = Math.Clamp(at.Y, 0, 20000);
        double inside = Math.Min(Math.Min(x, 20000 - x), Math.Min(y, 20000 - y));
        bool outside = at.X is < 0 or > 20000 || at.Y is < 0 or > 20000;
        return outside ? Math.Sqrt(((at.X - x) * (at.X - x)) + ((at.Y - y) * (at.Y - y))) : inside;
    }
}
