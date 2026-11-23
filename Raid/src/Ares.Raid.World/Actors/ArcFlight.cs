using System.Numerics;
using Ares.Common.Game.Enemies;
using Ares.Raid.World.Collision;
using Ares.Structs.Core.Geometry;
using Ares.Structs.Game.Collision;

namespace Ares.Raid.World.Actors;

/// <summary>Where an ARC is and how it moves.</summary>
/// <param name="Location">World location, in centimetres.</param>
/// <param name="Velocity">Velocity, in centimetres per second.</param>
/// <param name="Yaw">Heading, in radians.</param>
/// <param name="Pitch">Pitch, in radians, positive nose up.</param>
/// <param name="Flying">Whether it has started flying (a freshly spawned ARC has not).</param>
/// <param name="Roll">Roll about its forward axis, in radians (its bank into a turn).</param>
public readonly record struct ArcPose(Vector3D Location, Vector3D Velocity, double Yaw, double Pitch, bool Flying, double Roll = 0);

/// <summary>
/// An idle flying ARC's movement (see <see cref="ArcMovementProfile"/>). It runs in fixed steps of
/// <see cref="StepSeconds"/>. It flies along its route from one home point to the next, steering for a point a few
/// metres ahead on the route; around the home point it heads for a point it picks there, probing a few directions for
/// the clearest, slows down as it gets close, pauses and turns to look where it goes next, then moves on along the
/// route, sometimes turning back. It keeps a slight vertical bob and its height above the ground. Away from the route
/// its collision sphere slides along what it touches, and it moves on along the route when it stays held back. It is
/// deterministic for its seed.
/// </summary>
public sealed class ArcFlight
{
    /// <summary>Length of one movement step, in seconds.</summary>
    private const double StepSeconds = 1.0 / 30;

    // Most steps one advance runs, so a stalled server does not spend the next tick catching up.
    private const int MaxStepsPerAdvance = 90;

    // How far ahead on the route it steers (centimetres, from its speed between these bounds), and the step it moves
    // that point by.
    private const double MinLookAhead = 200;
    private const double MaxLookAhead = 400;
    private const double RouteStep = 50;

    // Steps between the probes of the way ahead.
    private const int ProbeInterval = 5;

    // Sweeps of one step's move: to a surface, then sliding along up to two more.
    private const int SlidePasses = 3;

    // Gap kept from surfaces, in centimetres.
    private const double ContactGap = 1;

    // A step is held back when it covers less than this share of a wanted move longer than the minimum (centimetres),
    // and after this many held-back steps in a row (one second) it heads for another home point.
    private const double BlockedShare = 0.5;
    private const double MinBlockedStep = 0.5;
    private const int EscapeSteps = 30;

    // Outside a pause it must move this far (centimetres) within this many seconds.
    private const double MinProgress = 100;
    private const double ProgressSeconds = 3;

    // Slowest horizontal speed, in centimetres per second, at which the ARC turns to face its travel.
    private const double FacingSpeed = 30;

    // Steepest nose-down pitch, in degrees.
    private const double MaxPitch = 45;

    // Share of its turn rate it turns at while it pauses, looking toward where it goes next.
    private const double LookTurnShare = 0.5;

    // It eases its turns: it wants to close the angle to its heading over this many seconds (at most its turn rate), and
    // its turn rate moves this share of the way to that each step.
    private const double TurnSettleSeconds = 0.6;
    private const double TurnBlend = 0.15;

    // It eases its speed: it wants to close the gap to its wanted speed over this many seconds (within its acceleration
    // and deceleration), and its acceleration moves this share of the way to that each step. It counts the speed its
    // acceleration still adds while that catches up, so it settles on its wanted speed without overshooting it.
    private const double SpeedSettleSeconds = 0.3;
    private const double AccelerationBlend = 0.1;

    // It plans its braking at this share of its deceleration and as if the point were as much nearer as it covers in its
    // eased response's lag (seconds), so that it stops at the point instead of overshooting it, and it closes the last
    // of the way at most at the distance over these seconds.
    private const double BrakingShare = 0.5;
    private const double BrakingLagSeconds = 0.4;
    private const double FinalApproachSeconds = 0.5;

    // Share of the way to its wanted bank it rolls each step, so that it eases into and out of turns.
    private const double BankBlend = 0.15;

    // The vertical bob: two sines of these amplitudes (centimetres per second) and angular frequencies (radians per second).
    private const double BobAmplitude1 = 9;
    private const double BobFrequency1 = 3.14;
    private const double BobAmplitude2 = 11;
    private const double BobFrequency2 = 5.17;
    private const double MaxBobPhase = 859;

    private const CollisionChannel Channel = GameCollisionChannels.Minion;

    private readonly ArcMovementProfile _profile;
    private readonly ArcHomePoints _route;
    private readonly double _radius;
    private readonly CollisionWorld? _collision;
    private readonly Random _random;
    private readonly double _bobPhase;

    private Double3 _position;
    private Double3 _velocity;
    private Double3 _flightVelocity;
    private Double3 _previousPosition;
    private double _previousYaw;
    private double _now;
    private Double3 _target;
    private double _yaw;
    private double _pitch;
    private double _roll;
    private double _turnRate;
    private double _speed;
    private double _acceleration;
    private double _time;
    private double? _clock;
    private double _replanAt;
    private double? _dwellUntil;
    private double? _lookAt;
    private int _direction;
    private int _steps;
    private int _blockedSteps;
    private Double3 _progressFrom;
    private double _progressAt;

    // The way along the route to the current home point while it flies there: how far along it has come and where the
    // home point lies (distances along the route line, beyond its length or below zero across a loop's start).
    private double _along;
    private double? _legEnd;

    /// <summary>Places a flying ARC at a home point.</summary>
    /// <param name="profile">How it moves.</param>
    /// <param name="route">The home points it moves between and the route they lie on; one point keeps it around that point.</param>
    /// <param name="home">The home point it starts at.</param>
    /// <param name="radius">How far it wanders around a home point, in centimetres.</param>
    /// <param name="collision">The map's collision, or null to fly through everything.</param>
    /// <param name="seed">Seed of its choices.</param>
    public ArcFlight(ArcMovementProfile profile, ArcHomePoints route, int home, double radius, CollisionWorld? collision, int seed)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _route = route ?? throw new ArgumentNullException(nameof(route));
        IReadOnlyList<Vector3D> homes = route.Points;
        ArgumentOutOfRangeException.ThrowIfZero(homes.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(home);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(home, homes.Count);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
        Home = home;
        _radius = radius;
        _collision = collision;
        _random = new Random(seed);
        _bobPhase = _random.NextDouble() * MaxBobPhase;
        _position = Double3.From(homes[home]);
        _previousPosition = _position;
        _progressFrom = _position;
        _target = _position;
        _direction = _random.NextDouble() < 0.5 ? 1 : -1;
    }

    /// <summary>
    /// Where it is at the time of its last <see cref="Advance"/>: between its last two steps, one step behind, so that
    /// poses read at any times follow its movement evenly. Its velocity is its steered velocity with its bob.
    /// </summary>
    public ArcPose Pose
    {
        get
        {
            double share = _clock is { } clock ? Math.Clamp((_now - clock) / StepSeconds, 0, 1) : 1;
            Double3 location = _previousPosition + ((_position - _previousPosition) * share);
            double yaw = _previousYaw + (Math.IEEERemainder(_yaw - _previousYaw, 2 * Math.PI) * share);
            return new ArcPose(location.ToVector3D(), _flightVelocity.ToVector3D(), Math.IEEERemainder(yaw, 2 * Math.PI), _pitch, _steps > 0, _roll);
        }
    }

    /// <summary>How often it has given up on a home point it could not reach.</summary>
    public int Detours { get; private set; }

    /// <summary>The home point it is heading for or pausing at.</summary>
    public int Home { get; private set; }

    /// <summary>Moves it on to a time on the raid clock; the first call only sets its clock.</summary>
    /// <param name="now">Raid clock, in seconds.</param>
    public void Advance(double now)
    {
        _now = now;
        if (_clock is not { } clock)
        {
            _clock = now;
            return;
        }

        int steps = 0;
        while (clock + StepSeconds <= now && steps < MaxStepsPerAdvance)
        {
            Step();
            clock += StepSeconds;
            steps++;
        }

        _clock = steps == MaxStepsPerAdvance ? now : clock;
    }

    private void Step()
    {
        (_previousPosition, _previousYaw) = (_position, _yaw);
        _time += StepSeconds;
        _steps++;
        if (_dwellUntil is { } until)
        {
            if (_lookAt is { } look && _time >= look)
            {
                // Partway through the pause it picks where it goes next and starts turning to look there.
                _lookAt = null;
                NextHome(true);
            }

            if (_time >= until)
            {
                _dwellUntil = null;
                if (_lookAt is not null)
                {
                    _lookAt = null;
                    NextHome(true);
                }

                Replan();
            }
        }
        else if (_time >= _replanAt)
        {
            Replan();
        }

        Double3 aim = _dwellUntil is null ? OnRoute() ?? _target : _target;
        // Its next leg may be picked while it still pauses; it only flies it once the pause is over.
        bool onLeg = _dwellUntil is null && _legEnd is not null;
        Double3 toTarget = aim - _position;
        double distance = toTarget.Length();
        if (_dwellUntil is null && _legEnd is null && (distance <= _profile.ArriveDistance
            || (Horizontal(toTarget) <= _profile.ArriveDistance && Math.Abs(toTarget.Z) <= 2 * _profile.HoverHeight)))
        {
            double dwell = _profile.MinDwellSeconds + (_random.NextDouble() * (_profile.MaxDwellSeconds - _profile.MinDwellSeconds));
            _dwellUntil = _time + dwell;
            _lookAt = _time + (_random.NextDouble() * dwell);
        }

        // Along a leg it cruises; toward a point it brakes in time to stop there.
        double wantedSpeed = onLeg ? _profile.CruiseSpeed
            : BrakingSpeed(Math.Max(0, distance - (_speed * BrakingLagSeconds)));
        double coming = _acceleration * StepSeconds / AccelerationBlend;
        double wantedAcceleration = Math.Clamp((wantedSpeed - _speed - coming) / SpeedSettleSeconds, -_profile.Deceleration, _profile.Acceleration);
        _acceleration += (wantedAcceleration - _acceleration) * AccelerationBlend;
        _speed += _acceleration * StepSeconds;
        if (_speed <= 0)
        {
            (_speed, _acceleration) = (0, Math.Max(_acceleration, 0));
        }
        else if (_speed >= _profile.CruiseSpeed)
        {
            (_speed, _acceleration) = (_profile.CruiseSpeed, Math.Min(_acceleration, 0));
        }

        Double3 desired = distance > 1e-6 ? toTarget * (_speed / distance) : default;
        _velocity += (desired - _velocity) * _profile.VelocityBlend;
        double bob = (BobAmplitude1 * Math.Sin((BobFrequency1 * _time) + _bobPhase)) + (BobAmplitude2 * Math.Sin((BobFrequency2 * _time) + _bobPhase));
        Double3 wanted = (_velocity + new Double3(0, 0, bob)) * StepSeconds;
        // Its route is laid out to fly along (it passes through foliage), so only its picks away from the route collide.
        Double3 move = onLeg ? wanted : Collide(wanted);
        if (wanted.Length() > MinBlockedStep && move.LengthSquared() < wanted.LengthSquared() * BlockedShare * BlockedShare)
        {
            // Held back by the world: it slows to what it managed, picks another way at once, and gives up on this home
            // point when that does not free it.
            _velocity = move * (1 / StepSeconds);
            (_speed, _acceleration) = (Math.Min(_speed, _velocity.Length()), 0);
            _blockedSteps++;
            switch (_blockedSteps)
            {
                case >= EscapeSteps:
                    _blockedSteps = 0;
                    (_dwellUntil, _lookAt) = (null, null);
                    Detours++;
                    NextHome(false);
                    Replan();
                    break;
                case 1:
                    _replanAt = _time;
                    break;
            }
        }
        else
        {
            _blockedSteps = 0;
        }

        // It does not sink below its hover height over what is under it (it is never lifted, so foliage above its way
        // does not hold it up).
        double floor = Floor(_position + move);
        if (_position.Z + move.Z < floor && move.Z < 0)
        {
            move = move with { Z = Math.Min(0, Math.Max(move.Z, floor - _position.Z)) };
        }

        _position += move;
        _flightVelocity = _velocity + new Double3(0, 0, bob);
        Watch();
        Face(move, StepSeconds);
        if (_steps % ProbeInterval == 0 && _dwellUntil is null && _legEnd is null && Blocked(_position, _target))
        {
            _replanAt = _time;
        }
    }

    // Picks the point to head for: the clearest of the probed directions toward the home point, kept within the wander
    // radius of it, then most of the way back to the home point.
    private void Replan()
    {
        Double3 home = Double3.From(_route.Points[Home]);
        Double3 toHome = new(home.X - _position.X, home.Y - _position.Y, 0);
        double heading = toHome.Length() > 1 ? Math.Atan2(toHome.Y, toHome.X) : _yaw;
        Double3 best = home;
        double bestScore = double.NegativeInfinity;
        foreach (double angle in _profile.ProbeAngles)
        {
            double direction = heading + (angle * Math.PI / 180);
            Double3 candidate = new(_position.X + (_profile.ProbeDistance * Math.Cos(direction)), _position.Y + (_profile.ProbeDistance * Math.Sin(direction)), home.Z);
            Double3 fromHome = new(candidate.X - home.X, candidate.Y - home.Y, 0);
            if (fromHome.Length() > _radius)
            {
                fromHome *= _radius / fromHome.Length();
                candidate = new Double3(home.X + fromHome.X, home.Y + fromHome.Y, home.Z);
            }

            // How far the way is clear, plus a random share standing in for the game's own preference between directions.
            double score = Clearance(_position, candidate) + _random.NextDouble();
            if (score > bestScore)
            {
                (best, bestScore) = (candidate, score);
            }
        }

        Double3 target = home + ((best - home) * _profile.WanderShare);
        _target = target with { Z = Math.Max(target.Z, Floor(target)) };
        _replanAt = _time + _profile.ReplanSeconds;
    }

    // Sets off along the route for the next home point, sometimes turning back when it may; an open route turns back at
    // its ends. A detour (no turning back) keeps its way past the home point it could not reach.
    private void NextHome(bool mayTurnBack)
    {
        int count = _route.Points.Count;
        if (count == 1)
        {
            return;
        }

        bool starting = _legEnd is null;
        double from = starting ? Nearest(_route.Distances[Home]) : _along;
        if (mayTurnBack && _random.NextDouble() < _profile.TurnBackChance)
        {
            _direction = -_direction;
        }

        int next = Home + _direction;

        if (_route.Closed)
        {
            next = (next + count) % count;
        }
        else if (next < 0 || next >= count)
        {
            _direction = -_direction;
            next = Home + _direction;
        }

        Home = next;
        double to = _route.Distances[next];
        double length = _route.Length;
        if (_route.Closed && length > 0)
        {
            from = ((from % length) + length) % length;
            to += _direction > 0 ? (to < from ? length : 0) : (to > from ? -length : 0);
        }

        if (starting)
        {
            // Away from the route it rejoins it ahead, as far along as it lies off it, instead of heading straight at it.
            double off = Horizontal(Double3.From(_route.PointAt(from)) - _position);
            from += Math.Sign(to - from) * Math.Min(off, Math.Abs(to - from) / 2);
        }

        (_along, _legEnd) = (from, to);
    }

    // The distance along the route, within its wander radius of a distance, of the route point nearest it.
    private double Nearest(double around)
    {
        double best = around;
        double bestDistance = double.PositiveInfinity;
        for (double along = around - _radius; along <= around + _radius; along += RouteStep)
        {
            double distance = Horizontal(Double3.From(_route.PointAt(along)) - _position);
            if (distance < bestDistance)
            {
                (best, bestDistance) = (along, distance);
            }
        }

        return best;
    }

    // While it flies to a home point: the point a few metres ahead of it on the route, or null once it reaches the
    // home point's neighbourhood and heads for its own pick there.
    private Double3? OnRoute()
    {
        if (_legEnd is not { } end)
        {
            return null;
        }

        double ahead = Math.Clamp(_velocity.Length(), MinLookAhead, MaxLookAhead);
        int sign = end >= _along ? 1 : -1;
        Double3 point = Double3.From(_route.PointAt(_along));
        while (sign * (end - _along) > 0 && Horizontal(point - _position) < ahead)
        {
            _along = sign > 0 ? Math.Min(end, _along + RouteStep) : Math.Max(end, _along - RouteStep);
            point = Double3.From(_route.PointAt(_along));
        }

        if (sign * (end - _along) > 0 || Horizontal(point - _position) >= Math.Max(ahead, _profile.ArriveDistance))
        {
            return point with { Z = Math.Max(point.Z, Floor(point)) };
        }

        // At the home point: a fresh pick from here, so it carries on ahead instead of turning to an old one.
        _legEnd = null;
        Replan();
        return null;
    }

    // Turns toward its travel (or, pausing, where it goes next), easing in and out of the turn up to the turn rate,
    // pitching its nose down with its horizontal speed and banking into the turn with its turn rate.
    private void Face(Double3 move, double dt)
    {
        double horizontal = Math.Sqrt((move.X * move.X) + (move.Y * move.Y)) / dt;
        double wantedRate = 0;
        Double3? look = horizontal >= FacingSpeed ? move : Look();
        if (look is { } toward)
        {
            double turn = Math.IEEERemainder(Math.Atan2(toward.Y, toward.X) - _yaw, 2 * Math.PI);
            double limit = _profile.TurnRate * (horizontal >= FacingSpeed ? 1 : LookTurnShare);
            wantedRate = Math.Clamp(turn / TurnSettleSeconds, -limit, limit);
        }

        _turnRate += (wantedRate - _turnRate) * TurnBlend;
        double turned = _turnRate * dt;
        _yaw = Math.IEEERemainder(_yaw + turned, 2 * Math.PI);

        _pitch = -Math.Min(_profile.PitchPerSpeed * horizontal / 100, MaxPitch) * Math.PI / 180;
        // It banks as its turn swings its travel around, so with its speed as well as its turn rate: turning on the spot
        // it stays level.
        double turnRate = turned / dt * 180 / Math.PI * Math.Min(1, horizontal / _profile.CruiseSpeed);
        double bank = Math.Clamp(_profile.BankPerTurnRate * turnRate, -_profile.MaxBank, _profile.MaxBank) * Math.PI / 180;
        _roll += (bank - _roll) * BankBlend;
    }

    // The speed it wants at a distance from the point it heads for, to stop there.
    private double BrakingSpeed(double distance) =>
        Math.Min(Math.Min(Math.Sqrt(2 * BrakingShare * _profile.Deceleration * distance), distance / FinalApproachSeconds), _profile.CruiseSpeed);

    // While it pauses with its next leg picked: the way to where that leg sets off along the route.
    private Double3? Look()
    {
        if (_dwellUntil is null || _legEnd is not { } end)
        {
            return null;
        }

        Double3 point = Double3.From(_route.PointAt(_along));
        if (Horizontal(point - _position) < MaxLookAhead)
        {
            point = Double3.From(_route.PointAt(_along + (Math.Sign(end - _along) * MaxLookAhead)));
        }

        Double3 toward = point - _position;
        return Horizontal(toward) > 1 ? toward : null;
    }

    // The lowest height it may fly at above a point: the hover height over the first surface below it within that height.
    private double Floor(Double3 at)
    {
        if (_collision is null || _profile.HoverHeight <= 0)
        {
            return double.NegativeInfinity;
        }

        CollisionHit? ground = _collision.Raycast(at.ToVector3D(), -Vector3.UnitZ, _profile.HoverHeight, Channel, CollisionComplexity.Simple);
        return ground is { } hit ? hit.Point.Z + _profile.HoverHeight : double.NegativeInfinity;
    }

    // Share of the way from one point to another that its sphere can fly (1 without collision).
    private double Clearance(Double3 from, Double3 to)
    {
        Double3 way = to - from;
        double length = way.Length();
        if (_collision is null || length < 1)
        {
            return 1;
        }

        return Sweep(from, to) is { } hit ? (hit.StartPenetrating ? 0 : hit.Distance / length) : 1;
    }

    private bool Blocked(Double3 from, Double3 to) => Clearance(from, to) < 1;

    private static double Horizontal(Double3 offset) => Math.Sqrt((offset.X * offset.X) + (offset.Y * offset.Y));

    // Outside a pause it must get somewhere: after a while without, it moves on along the route.
    private void Watch()
    {
        if (_dwellUntil is not null || (_position - _progressFrom).Length() >= MinProgress)
        {
            (_progressFrom, _progressAt) = (_position, _time);
            return;
        }

        if (_time - _progressAt < ProgressSeconds)
        {
            return;
        }

        (_progressFrom, _progressAt) = (_position, _time);
        Detours++;
        NextHome(false);
        Replan();
    }

    // How far it gets of a wanted move: up to the first surface its sphere touches, then sliding along it; out of a
    // surface it starts inside.
    private Double3 Collide(Double3 wanted)
    {
        if (_collision is null)
        {
            return wanted;
        }

        Double3 total = default;
        Double3 at = _position;
        Double3 rest = wanted;
        for (int pass = 0; pass < SlidePasses && rest.LengthSquared() > 1e-6; pass++)
        {
            if (Sweep(at, at + rest) is not { } hit)
            {
                return total + rest;
            }

            Double3 normal = Double3.From(hit.Normal);
            if (hit.StartPenetrating)
            {
                Double3 push = normal * (hit.PenetrationDepth + ContactGap);
                (total, at) = (total + push, at + push);
                continue;
            }

            double length = rest.Length();
            Double3 travel = rest * (Math.Max(0, hit.Distance - ContactGap) / length);
            (total, at, rest) = (total + travel, at + travel, rest - travel);
            double into = Double3.Dot(rest, normal);
            if (into < 0)
            {
                rest -= normal * into;
            }
        }

        return total;
    }

    private CollisionHit? Sweep(Double3 from, Double3 to) =>
        _collision!.SweepSphere(from.ToVector3D(), to.ToVector3D(), _profile.BodyRadius, Channel, CollisionComplexity.Simple);
}
