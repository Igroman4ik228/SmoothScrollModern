namespace SmoothScrollModern.Scroll;

public sealed record ApplicationRuleSnapshot(
    bool IsRuleEnabled,
    bool IsSmoothScrollDisabled,
    ScrollDeliveryMode DeliveryMode,
    ScrollSettingsSnapshot Settings);
