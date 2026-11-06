using Ares.Structs.Game.Movement;

namespace Ares.Raid.World.Movement;

/// <summary>Sustained sprint speed and the initial authored burst.</summary>
public static class SprintMovement
{
    /// <summary>Evaluates maximum speed at the supplied elapsed sprint time.</summary>
    /// <param name="runSpeed">Maximum speed without sprinting.</param>
    /// <param name="parameters">Sprint speed and burst modifiers.</param>
    /// <param name="seconds">Elapsed sprint time in seconds.</param>
    /// <returns>The maximum world speed.</returns>
    public static double MaximumSpeed(double runSpeed, SprintMovementParameters parameters, double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        if (!parameters.BurstAllowed)
        {
            return parameters.BaseSpeed;
        }

        double time = seconds;
        float peakTime = SprintMovementParameters.BurstCurve[1].Time;
        if (parameters.DurationBonus > 0 && time > peakTime)
        {
            time = Math.Max(peakTime, time - parameters.DurationBonus);
        }

        double alpha = BurstAlpha(time);
        double speed = runSpeed + ((parameters.BaseSpeed - runSpeed) * Math.Min(1, alpha));
        return speed + (((parameters.BaseSpeed * SprintMovementParameters.BurstSpeedMultiplier) - speed) * Math.Max(0, alpha - 1));
    }

    private static double BurstAlpha(double seconds)
    {
        if (!double.IsFinite(seconds))
        {
            throw new ArgumentOutOfRangeException(nameof(seconds));
        }

        IReadOnlyList<WeightedCurveKey> keys = SprintMovementParameters.BurstCurve;
        if (seconds <= keys[0].Time)
        {
            return keys[0].Value;
        }

        for (int index = 1; index < keys.Count; index++)
        {
            WeightedCurveKey end = keys[index];
            if (seconds >= end.Time)
            {
                continue;
            }

            WeightedCurveKey start = keys[index - 1];
            double leave = Math.Atan(start.LeaveTangent);
            double arrive = Math.Atan(end.ArriveTangent);
            double x1 = start.Time + (Math.Cos(leave) * start.LeaveWeight);
            double y1 = start.Value + (Math.Sin(leave) * start.LeaveWeight);
            double x2 = end.Time - (Math.Cos(arrive) * end.ArriveWeight);
            double y2 = end.Value - (Math.Sin(arrive) * end.ArriveWeight);
            double low = 0;
            double high = 1;
            for (int iteration = 0; iteration < 40; iteration++)
            {
                double middle = (low + high) / 2;
                if (Bezier(start.Time, x1, x2, end.Time, middle) < seconds)
                {
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            return Bezier(start.Value, y1, y2, end.Value, (low + high) / 2);
        }

        return keys[^1].Value;
    }

    private static double Bezier(double first, double second, double third, double fourth, double amount)
    {
        double inverse = 1 - amount;
        return (inverse * inverse * inverse * first) + (3 * inverse * inverse * amount * second)
            + (3 * inverse * amount * amount * third) + (amount * amount * amount * fourth);
    }
}
