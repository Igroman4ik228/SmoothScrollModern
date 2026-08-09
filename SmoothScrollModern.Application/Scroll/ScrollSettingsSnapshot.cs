using System.Collections.Immutable;
using SmoothScrollModern.Settings;
using Windows.System;

namespace SmoothScrollModern.Scroll;

/// <summary>
/// Неизменяемая конфигурация физики прокрутки, безопасная для чтения из hook-потока.
/// </summary>
public sealed record ScrollSettingsSnapshot(
    double DistanceMultiplier,
    double Friction,
    double BurstAcceleration,
    double DirectionChangeDamping,
    double MaxVelocity,
    double StopVelocityThreshold,
    double PrecisionMultiplier,
    bool EnableHorizontalScroll,
    ImmutableArray<VirtualKey> BypassSmoothingVirtualKeys)
{
    public static ScrollSettingsSnapshot From(ScrollSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new ScrollSettingsSnapshot(
            Snap(settings.DistanceMultiplier, ScrollSettings.DistanceMultiplierMin, ScrollSettings.DistanceMultiplierMax, ScrollSettings.DistanceMultiplierStep),
            Snap(settings.Friction, ScrollSettings.FrictionMin, ScrollSettings.FrictionMax, ScrollSettings.FrictionStep),
            Snap(settings.BurstAcceleration, ScrollSettings.BurstAccelerationMin, ScrollSettings.BurstAccelerationMax, ScrollSettings.BurstAccelerationStep),
            Snap(settings.DirectionChangeDamping, ScrollSettings.DirectionChangeDampingMin, ScrollSettings.DirectionChangeDampingMax, ScrollSettings.DirectionChangeDampingStep),
            Snap(settings.MaxVelocity, ScrollSettings.MaxVelocityMin, ScrollSettings.MaxVelocityMax, ScrollSettings.MaxVelocityStep),
            Snap(settings.StopVelocityThreshold, ScrollSettings.StopVelocityThresholdMin, ScrollSettings.StopVelocityThresholdMax, ScrollSettings.StopVelocityThresholdStep),
            Snap(settings.PrecisionMultiplier, ScrollSettings.PrecisionMultiplierMin, ScrollSettings.PrecisionMultiplierMax, ScrollSettings.PrecisionMultiplierStep),
            settings.EnableHorizontalScroll,
            (settings.BypassSmoothingVirtualKeys ?? [])
                .SelectMany(ShortcutKeys.ExpandGenericModifier)
                .Where(ShortcutKeys.IsValid)
                .Distinct()
                .ToImmutableArray());
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
