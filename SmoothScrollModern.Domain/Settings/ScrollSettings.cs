using Windows.System;

namespace SmoothScrollModern.Settings;

public sealed class ScrollSettings
{
    public const double DistanceMultiplierMin = 0.1;
    public const double DistanceMultiplierMax = 10.0;
    public const double DistanceMultiplierStep = 0.05;

    public const double FrictionMin = 1.5;
    public const double FrictionMax = 60.0;
    public const double FrictionStep = 0.1;

    public const double BurstAccelerationMin = 0.0;
    public const double BurstAccelerationMax = 6.0;
    public const double BurstAccelerationStep = 0.05;

    public const double DirectionChangeDampingMin = 0.02;
    public const double DirectionChangeDampingMax = 1.0;
    public const double DirectionChangeDampingStep = 0.01;

    public const double MaxVelocityMin = 500.0;
    public const double MaxVelocityMax = 40000.0;
    public const double MaxVelocityStep = 50.0;

    public const double StopVelocityThresholdMin = 1.0;
    public const double StopVelocityThresholdMax = 120.0;
    public const double StopVelocityThresholdStep = 1.0;

    public const double PrecisionMultiplierMin = 0.1;
    public const double PrecisionMultiplierMax = 3.0;
    public const double PrecisionMultiplierStep = 0.05;

    public double DistanceMultiplier { get; set; } = 0.6;

    public double Friction { get; set; } = 7.0;

    public double BurstAcceleration { get; set; } = 1.0;

    public double DirectionChangeDamping { get; set; } = 0.18;

    public double MaxVelocity { get; set; } = 40000.0;

    public double StopVelocityThreshold { get; set; } = 8.0;

    public double PrecisionMultiplier { get; set; } = 1.0;

    public bool EnableHorizontalScroll { get; set; } = true;

    public List<VirtualKey> BypassSmoothingVirtualKeys { get; set; } = [ShortcutKeys.LeftControl, ShortcutKeys.RightControl];

    public void Validate()
    {
        DistanceMultiplier = Snap(DistanceMultiplier, DistanceMultiplierMin, DistanceMultiplierMax, DistanceMultiplierStep);
        Friction = Snap(Friction, FrictionMin, FrictionMax, FrictionStep);
        BurstAcceleration = Snap(BurstAcceleration, BurstAccelerationMin, BurstAccelerationMax, BurstAccelerationStep);
        DirectionChangeDamping = Snap(DirectionChangeDamping, DirectionChangeDampingMin, DirectionChangeDampingMax, DirectionChangeDampingStep);
        MaxVelocity = Snap(MaxVelocity, MaxVelocityMin, MaxVelocityMax, MaxVelocityStep);
        StopVelocityThreshold = Snap(StopVelocityThreshold, StopVelocityThresholdMin, StopVelocityThresholdMax, StopVelocityThresholdStep);
        PrecisionMultiplier = Snap(PrecisionMultiplier, PrecisionMultiplierMin, PrecisionMultiplierMax, PrecisionMultiplierStep);

        BypassSmoothingVirtualKeys = BypassSmoothingVirtualKeys
            ?.SelectMany(ShortcutKeys.ExpandGenericModifier)
            .Where(ShortcutKeys.IsValid)
            .Distinct()
            .ToList() ?? [];
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
}
