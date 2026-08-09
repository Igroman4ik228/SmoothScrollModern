namespace SmoothScrollModern.Scroll;

public sealed record ScrollDecision(
    bool ShouldSmooth,
    ScrollSettingsSnapshot? Settings,
    ScrollDeliveryMode DeliveryMode)
{
    public static ScrollDecision Bypass { get; } = new(false, null, ScrollDeliveryMode.FineDelta);

    public static ScrollDecision Smooth(ScrollSettingsSnapshot settings, ScrollDeliveryMode deliveryMode)
    {
        return new ScrollDecision(true, settings, deliveryMode);
    }
}
