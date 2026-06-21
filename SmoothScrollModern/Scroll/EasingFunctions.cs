namespace SmoothScrollModern.Scroll;

public static class EasingFunctions
{
    public static double Apply(EasingType easingType, double progress)
    {
        var t = Math.Clamp(progress, 0.0, 1.0);

        return easingType switch
        {
            EasingType.Linear => t,
            EasingType.EaseOutQuart => 1.0 - Math.Pow(1.0 - t, 4.0),
            EasingType.EaseOutQuint => 1.0 - Math.Pow(1.0 - t, 5.0),
            _ => 1.0 - Math.Pow(1.0 - t, 3.0)
        };
    }
}
