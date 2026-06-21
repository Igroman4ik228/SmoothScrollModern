using System.IO;
using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Applications;

public sealed class ApplicationRulesService : IApplicationRulesService
{
    public bool ShouldBypass(ApplicationInfo application, AppSettings settings)
    {
        if (application == ApplicationInfo.Empty || string.IsNullOrWhiteSpace(application.ProcessName))
        {
            return false;
        }

        if (settings.AutoDetectExcludedApps && application.IsFullscreen)
        {
            return true;
        }

        return settings.ApplicationRules.Any(rule =>
            rule.IsRuleEnabled
            && rule.IsSmoothScrollDisabled
            && Matches(rule, application));
    }

    public ApplicationRule AddOrUpdateRule(AppSettings settings, ApplicationInfo application)
    {
        var processName = application.ProcessName;
        var displayName = string.IsNullOrWhiteSpace(application.DisplayName) ? processName : application.DisplayName;
        var rule = AddManualRule(settings, processName, displayName);
        rule.ExecutablePath = application.ExecutablePath;
        return rule;
    }

    public ApplicationRule AddManualRule(AppSettings settings, string processName, string displayName)
    {
        var normalized = ApplicationRule.NormalizeProcessName(processName);
        var rule = settings.ApplicationRules.FirstOrDefault(item =>
            string.Equals(item.ProcessName, normalized, StringComparison.OrdinalIgnoreCase));

        if (rule is not null)
        {
            rule.DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalized : displayName;
            rule.IsSmoothScrollDisabled = true;
            rule.IsRuleEnabled = true;
            return rule;
        }

        rule = new ApplicationRule
        {
            ProcessName = normalized,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalized : displayName.Trim(),
            IsSmoothScrollDisabled = true,
            IsRuleEnabled = true,
            IsUserRule = true
        };

        settings.ApplicationRules.Add(rule);
        return rule;
    }

    public ApplicationRule AddApplicationPath(AppSettings settings, string executablePath, string displayName)
    {
        var normalizedPath = ApplicationRule.NormalizeExecutablePath(executablePath);
        var processName = Path.GetFileName(normalizedPath);
        var rule = AddManualRule(
            settings,
            processName,
            string.IsNullOrWhiteSpace(displayName) ? processName : displayName);
        rule.ExecutablePath = normalizedPath;
        return rule;
    }

    public static bool Matches(ApplicationRule rule, ApplicationInfo application)
    {
        if (!string.IsNullOrWhiteSpace(rule.ExecutablePath)
            && !string.IsNullOrWhiteSpace(application.ExecutablePath)
            && string.Equals(rule.ExecutablePath, application.ExecutablePath, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(rule.ProcessName, application.ProcessName, StringComparison.OrdinalIgnoreCase);
    }
}
