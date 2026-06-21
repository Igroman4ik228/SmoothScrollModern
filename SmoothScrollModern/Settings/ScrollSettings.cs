using SmoothScrollModern.Scroll;

namespace SmoothScrollModern.Settings;

public sealed class ScrollSettings
{
    public double ScrollMultiplier { get; set; } = 1.4;

    public int DurationMs { get; set; } = 160;

    public double Smoothness { get; set; } = 0.75;

    public double Acceleration { get; set; } = 1.0;

    public EasingType EasingType { get; set; } = EasingType.EaseOutCubic;

    public bool EnableHorizontalScroll { get; set; } = true;

    public void Validate()
    {
        ScrollMultiplier = Math.Clamp(ScrollMultiplier, 0.2, 6.0);
        DurationMs = Math.Clamp(DurationMs, 30, 900);
        Smoothness = Math.Clamp(Smoothness, 0.0, 1.0);
        Acceleration = Math.Clamp(Acceleration, 0.2, 4.0);
    }
}
