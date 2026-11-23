using Ares.Structs.Core.Geometry;

namespace Ares.Common.Game.Enemies;

/// <summary>A place on a map where ARCs spawn, and the routes they keep to while idle.</summary>
/// <param name="Id">The spawner's name, unique on its map.</param>
/// <param name="Label">A descriptive name, which several spawners may share.</param>
/// <param name="Class">The kind of spawner (its Blueprint class name).</param>
/// <param name="Location">Where it stands, in world centimetres.</param>
/// <param name="Yaw">Its heading in degrees.</param>
/// <param name="EnemyTable">The enemy table it spawns from.</param>
/// <param name="EnemyTypes">The enemy types its table can spawn.</param>
/// <param name="Routes">Names of the map's spline paths its ARCs keep to while idle; empty when they stay around the spawner.</param>
/// <param name="GroupSearchRadius">How far, in centimetres, its ARCs wander around each home point.</param>
/// <param name="SpawnAreaRadius">Radius of the area it spawns in, in centimetres.</param>
public sealed record ArcSpawner(string Id, string Label, string Class, Vector3D Location, double Yaw, string EnemyTable, IReadOnlyList<string> EnemyTypes,
    IReadOnlyList<string> Routes, double GroupSearchRadius, double SpawnAreaRadius);

/// <summary>
/// How an ARC moves while idle. It wanders from home point to home point along its spawner's route: it heads for a
/// point drawn around the current home point, pauses there, then moves on to the next home point, sometimes turning back.
/// </summary>
/// <param name="CruiseSpeed">Fastest idle speed, in centimetres per second.</param>
/// <param name="Acceleration">Fastest gain of speed, in centimetres per second per second.</param>
/// <param name="Deceleration">Fastest loss of speed, in centimetres per second per second; it brakes at this rate to stop at the point it heads for.</param>
/// <param name="VelocityBlend">Share of the way from its velocity to the one it wants that it covers each thirtieth of a second (0 to 1).</param>
/// <param name="TurnRate">Fastest turn, in radians per second.</param>
/// <param name="PitchPerSpeed">Nose-down pitch while flying, in degrees per metre per second of horizontal speed.</param>
/// <param name="BankPerTurnRate">Bank into a turn, in degrees per degree per second of turn rate.</param>
/// <param name="MaxBank">Steepest bank, in degrees.</param>
/// <param name="HoverHeight">Least height above the ground, in centimetres.</param>
/// <param name="BodyRadius">Radius of the sphere that collides with the world, in centimetres.</param>
/// <param name="HomePointSpacing">Home points are this many times the spawner's group search radius apart along the route.</param>
/// <param name="MinHomePointSpacing">Least distance between home points, in centimetres.</param>
/// <param name="ReplanSeconds">Seconds between picks of the next point to head for.</param>
/// <param name="ProbeDistance">How far ahead, in centimetres, the candidate directions are probed.</param>
/// <param name="ProbeAngles">Directions probed around the way to the home point, in degrees.</param>
/// <param name="WanderShare">Share of the way from the home point to the best probed point that the point it heads for lies (0 to 1).</param>
/// <param name="MinDwellSeconds">Shortest pause at a home point, in seconds.</param>
/// <param name="MaxDwellSeconds">Longest pause at a home point, in seconds.</param>
/// <param name="TurnBackChance">Chance (0 to 1) that it heads back along the route instead of on at each home point.</param>
/// <param name="ArriveDistance">Distance, in centimetres, at which it has reached the point it heads for.</param>
public sealed record ArcMovementProfile(double CruiseSpeed, double Acceleration, double Deceleration, double VelocityBlend, double TurnRate, double PitchPerSpeed,
    double BankPerTurnRate, double MaxBank, double HoverHeight, double BodyRadius, double HomePointSpacing, double MinHomePointSpacing, double ReplanSeconds, double ProbeDistance,
    IReadOnlyList<double> ProbeAngles, double WanderShare, double MinDwellSeconds, double MaxDwellSeconds, double TurnBackChance, double ArriveDistance);
