using SmoothScrollModern.Scroll;

namespace SmoothScrollModern.Settings;

public sealed class ScrollSettings
{
    public const double ScrollMultiplierMin = 0.2;
    public const double ScrollMultiplierMax = 6.0;
    public const double ScrollMultiplierStep = 0.05;

    public const int DurationMin = 30;
    public const int DurationMax = 900;
    public const int DurationStep = 10;

    public const double SmoothnessMin = 0.0;
    public const double SmoothnessMax = 1.0;
    public const double SmoothnessStep = 0.025;

    public const double AccelerationMin = 0.2;
    public const double AccelerationMax = 4.0;
    public const double AccelerationStep = 0.05;

    public double ScrollMultiplier { get; set; } = 1.4;

    public int DurationMs { get; set; } = 160;

    public double Smoothness { get; set; } = 0.75;

    public double Acceleration { get; set; } = 1.0;

    public EasingType EasingType { get; set; } = EasingType.EaseOutCubic;

    public bool EnableHorizontalScroll { get; set; } = true;

    public void Validate()
    {
        ScrollMultiplier = Snap(ScrollMultiplier, ScrollMultiplierMin, ScrollMultiplierMax, ScrollMultiplierStep);
        DurationMs = Snap(DurationMs, DurationMin, DurationMax, DurationStep);
        Smoothness = Snap(Smoothness, SmoothnessMin, SmoothnessMax, SmoothnessStep);
        Acceleration = Snap(Acceleration, AccelerationMin, AccelerationMax, AccelerationStep);
    }

    private static double Snap(double value, double minimum, double maximum, double step)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return minimum;
        }

        var clamped = Math.Clamp(value, minimum, maximum);
        var steps = Math.Round((clamped - minimum) / step, MidpointRounding.AwayFromZero);
        var snapped = minimum + (steps * step);
        return Math.Round(Math.Clamp(snapped, minimum, maximum), 3, MidpointRounding.AwayFromZero);
    }

    private static int Snap(int value, int minimum, int maximum, int step)
    {
        var clamped = Math.Clamp(value, minimum, maximum);
        var steps = (int)Math.Round((clamped - minimum) / (double)step, MidpointRounding.AwayFromZero);
        return Math.Clamp(minimum + (steps * step), minimum, maximum);
    }
}
