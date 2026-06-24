using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Applications;

public interface IApplicationRulesService
{
    bool ShouldBypass(ApplicationInfo application, AppSettings settings);

    ApplicationRule AddOrUpdateRule(AppSettings settings, ApplicationInfo application);

    ApplicationRule AddManualRule(AppSettings settings, string processName, string displayName);

    ApplicationRule AddApplicationPath(AppSettings settings, string executablePath, string displayName);
}
