using System.Diagnostics;
using SmoothScrollModern.Applications;
using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Scroll;

public sealed class ScrollDecisionService : IScrollDecisionService
{
    private static readonly string CurrentProcessName = $"{Process.GetCurrentProcess().ProcessName}.exe".ToLowerInvariant();
    private readonly IScrollConfigurationProvider _configurationProvider;
    private readonly IWindowIdentityResolver _windowIdentityResolver;

    public ScrollDecisionService(
        IScrollConfigurationProvider configurationProvider,
        IWindowIdentityResolver windowIdentityResolver)
    {
        _configurationProvider = configurationProvider;
        _windowIdentityResolver = windowIdentityResolver;
    }

    public ScrollDecision Decide(IntPtr targetWindowHandle)
    {
        var configuration = _configurationProvider.Current;
        if (!configuration.IsEnabled || IsPaused(configuration.PausedUntilUtc))
        {
            return ScrollDecision.Bypass;
        }

        var application = _windowIdentityResolver.Resolve(targetWindowHandle);
        if (application == WindowIdentity.Empty || IsCurrentApplication(application))
        {
            return ScrollDecision.Bypass;
        }

        if (configuration.AutoDetectExcludedApps && application.IsFullscreen)
        {
            return ScrollDecision.Bypass;
        }

        var hasRule = TryGetRule(configuration, application, out var rule);
        if (configuration.ApplicationListMode == ApplicationListMode.SelectedOnly
            && (!hasRule || !rule!.IsRuleEnabled))
        {
            return ScrollDecision.Bypass;
        }

        if (hasRule && rule!.IsRuleEnabled && rule.IsSmoothScrollDisabled)
        {
            return ScrollDecision.Bypass;
        }

        return hasRule && rule!.IsRuleEnabled
            ? ScrollDecision.Smooth(rule.Settings, rule.DeliveryMode)
            : ScrollDecision.Smooth(configuration.DefaultSettings, ScrollDeliveryMode.FineDelta);
    }

    private static bool IsPaused(DateTimeOffset? pausedUntilUtc)
    {
        return pausedUntilUtc is { } pausedUntil && pausedUntil > DateTimeOffset.UtcNow;
    }

    private static bool TryGetRule(
        ScrollConfigurationSnapshot configuration,
        WindowIdentity application,
        out ApplicationRuleSnapshot? rule)
    {
        if (!string.IsNullOrWhiteSpace(application.ExecutablePath)
            && configuration.RulesByExecutablePath.TryGetValue(application.ExecutablePath, out var pathRule))
        {
            rule = pathRule;
            return true;
        }

        if (configuration.RulesByProcess.TryGetValue(application.ProcessName, out var processRule))
        {
            rule = processRule;
            return true;
        }

        rule = null;
        return false;
    }

    private static bool IsCurrentApplication(WindowIdentity application)
    {
        return string.Equals(application.ProcessName, CurrentProcessName, StringComparison.OrdinalIgnoreCase);
    }
}
