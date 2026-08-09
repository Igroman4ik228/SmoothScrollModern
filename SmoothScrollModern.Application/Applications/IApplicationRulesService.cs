using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Applications;

public interface IApplicationRulesService
{
    ApplicationRule AddOrUpdateRule(AppSettings settings, ApplicationInfo application, bool disableSmoothScroll);

    ApplicationRule AddManualRule(AppSettings settings, string processName, string displayName, bool disableSmoothScroll);

    ApplicationRule AddApplicationPath(AppSettings settings, string executablePath, string displayName, bool disableSmoothScroll);
}
