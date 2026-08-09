using System.Collections.Frozen;
using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Scroll;

/// <summary>
/// Полная неизменяемая конфигурация, используемая только во время обработки ввода.
/// </summary>
public sealed record ScrollConfigurationSnapshot(
    long Version,
    bool IsEnabled,
    DateTimeOffset? PausedUntilUtc,
    bool AutoDetectExcludedApps,
    ApplicationListMode ApplicationListMode,
    ScrollSettingsSnapshot DefaultSettings,
    FrozenDictionary<string, ApplicationRuleSnapshot> RulesByProcess,
    FrozenDictionary<string, ApplicationRuleSnapshot> RulesByExecutablePath);
