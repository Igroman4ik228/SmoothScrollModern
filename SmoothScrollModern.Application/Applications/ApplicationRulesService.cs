using System.IO;
using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Applications;

public sealed class ApplicationRulesService : IApplicationRulesService
{
    public ApplicationRule AddOrUpdateRule(AppSettings settings, ApplicationInfo application, bool disableSmoothScroll)
    {
        var processName = application.ProcessName;
        var displayName = string.IsNullOrWhiteSpace(application.DisplayName) ? processName : application.DisplayName;
        return string.IsNullOrWhiteSpace(application.ExecutablePath)
            ? AddManualRule(settings, processName, displayName, disableSmoothScroll)
            : AddApplicationPath(settings, application.ExecutablePath, displayName, disableSmoothScroll);
    }

    public ApplicationRule AddManualRule(AppSettings settings, string processName, string displayName, bool disableSmoothScroll)
    {
        var normalized = ApplicationRule.NormalizeProcessName(processName);
        var rule = settings.ApplicationRules.FirstOrDefault(item =>
            string.IsNullOrWhiteSpace(item.ExecutablePath)
            && string.Equals(item.ProcessName, normalized, StringComparison.OrdinalIgnoreCase));

        if (rule is not null)
        {
            rule.DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalized : displayName;
            rule.IsSmoothScrollDisabled = disableSmoothScroll;
            rule.IsRuleEnabled = true;
            return rule;
        }

        rule = new ApplicationRule
        {
            ProcessName = normalized,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? normalized : displayName.Trim(),
            IsSmoothScrollDisabled = disableSmoothScroll,
            IsRuleEnabled = true,
            IsUserRule = true
        };

        settings.ApplicationRules.Add(rule);
        return rule;
    }

    public ApplicationRule AddApplicationPath(AppSettings settings, string executablePath, string displayName, bool disableSmoothScroll)
    {
        var normalizedPath = ApplicationRule.NormalizeExecutablePath(executablePath);
        var processName = Path.GetFileName(normalizedPath);
        var rule = settings.ApplicationRules.FirstOrDefault(item =>
            string.Equals(item.ExecutablePath, normalizedPath, StringComparison.OrdinalIgnoreCase));

        if (rule is not null)
        {
            rule.DisplayName = string.IsNullOrWhiteSpace(displayName) ? processName : displayName;
            rule.IsSmoothScrollDisabled = disableSmoothScroll;
            rule.IsRuleEnabled = true;
            return rule;
        }

        rule = new ApplicationRule
        {
            ProcessName = processName,
            ExecutablePath = normalizedPath,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? processName : displayName.Trim(),
            IsSmoothScrollDisabled = disableSmoothScroll,
            IsRuleEnabled = true,
            IsUserRule = true
        };
        settings.ApplicationRules.Add(rule);
        return rule;
    }

    public static bool Matches(ApplicationRule rule, ApplicationInfo application)
    {
        if (!string.IsNullOrWhiteSpace(rule.ExecutablePath))
        {
            return !string.IsNullOrWhiteSpace(application.ExecutablePath)
                   && string.Equals(rule.ExecutablePath, application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(rule.ProcessName, application.ProcessName, StringComparison.OrdinalIgnoreCase);
    }
}
