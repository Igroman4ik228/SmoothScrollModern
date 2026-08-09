using System.Collections.Frozen;
using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Scroll;

public sealed class ScrollConfigurationSnapshotFactory
{
    private long _version;

    public ScrollConfigurationSnapshot Create(AppSettings settings, DateTimeOffset? pausedUntilUtc)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var defaultSettings = ScrollSettingsSnapshot.From(settings.Scroll ?? new ScrollSettings());
        var profilesById = BuildProfiles(settings.ScrollProfiles);
        var rulesByProcess = new Dictionary<string, ApplicationRuleSnapshot>(StringComparer.OrdinalIgnoreCase);
        var rulesByExecutablePath = new Dictionary<string, ApplicationRuleSnapshot>(StringComparer.OrdinalIgnoreCase);

        foreach (var rule in settings.ApplicationRules ?? [])
        {
            AddRule(rule, profilesById, defaultSettings, rulesByProcess, rulesByExecutablePath);
        }

        return new ScrollConfigurationSnapshot(
            Interlocked.Increment(ref _version),
            settings.IsEnabled,
            pausedUntilUtc?.ToUniversalTime(),
            settings.AutoDetectExcludedApps,
            Enum.IsDefined(settings.ApplicationListMode)
                ? settings.ApplicationListMode
                : ApplicationListMode.Exclusions,
            defaultSettings,
            rulesByProcess.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
            rulesByExecutablePath.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase));
    }

    private static Dictionary<string, ScrollSettingsSnapshot> BuildProfiles(
        IEnumerable<ScrollProfile>? profiles)
    {
        var profilesById = new Dictionary<string, ScrollSettingsSnapshot>(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in profiles ?? [])
        {
            var id = profile.Id?.Trim();
            if (string.IsNullOrWhiteSpace(id) || profilesById.ContainsKey(id))
            {
                continue;
            }

            profilesById.Add(id, ScrollSettingsSnapshot.From(profile.Scroll ?? new ScrollSettings()));
        }

        return profilesById;
    }

    private static void AddRule(
        ApplicationRule rule,
        IReadOnlyDictionary<string, ScrollSettingsSnapshot> profilesById,
        ScrollSettingsSnapshot defaultSettings,
        IDictionary<string, ApplicationRuleSnapshot> rulesByProcess,
        IDictionary<string, ApplicationRuleSnapshot> rulesByExecutablePath)
    {
        var processName = ApplicationRule.NormalizeProcessName(rule.ProcessName);
        var executablePath = ApplicationRule.NormalizeExecutablePath(rule.ExecutablePath);
        var settings = !string.IsNullOrWhiteSpace(rule.ScrollProfileId)
                       && profilesById.TryGetValue(rule.ScrollProfileId, out var profileSettings)
            ? profileSettings
            : defaultSettings;
        var deliveryMode = Enum.IsDefined(rule.DeliveryMode)
            ? rule.DeliveryMode
            : ScrollDeliveryMode.FineDelta;
        var snapshot = new ApplicationRuleSnapshot(
            rule.IsRuleEnabled,
            rule.IsSmoothScrollDisabled,
            deliveryMode,
            settings);

        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            // Правило с путём намеренно не попадает в индекс процессов: оно не должно
            // применяться к другой копии одноимённого приложения.
            rulesByExecutablePath.TryAdd(executablePath, snapshot);
            return;
        }

        rulesByProcess.TryAdd(processName, snapshot);
    }
}
