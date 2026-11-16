namespace MeterFlow.Simulator;

/// <summary>Household consumption per 5-minute interval, peaking at 18:00 local time.</summary>
public static class MeterProfile
{
    private const double BaseKwh = 0.05;
    private const double PeakKwh = 0.25;
    private const double PeakHour = 18;
    private const double NoiseFraction = 0.2;

    public static decimal KwhAt(DateTimeOffset moment, Random rng)
    {
        double hour = moment.Hour + moment.Minute / 60.0;
        double angle = (hour - PeakHour) / 24 * 2 * Math.PI;
        double dailyShape = (1 + Math.Cos(angle)) / 2;
        double kwh = BaseKwh + (PeakKwh - BaseKwh) * dailyShape;

        double noise = (rng.NextDouble() * 2 - 1) * NoiseFraction;
        return Math.Round((decimal)(kwh * (1 + noise)), 3);
    }
}
