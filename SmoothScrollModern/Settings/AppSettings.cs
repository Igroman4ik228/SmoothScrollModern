namespace SmoothScrollModern.Settings;

public sealed class AppSettings
{
    public bool IsEnabled { get; set; } = true;

    public ScrollSettings Scroll { get; set; } = new();

    public TraySettings Tray { get; set; } = new();

    public List<ApplicationRule> ApplicationRules { get; set; } = [];

    public List<ScrollProfile> ScrollProfiles { get; set; } = [];

    public bool DefaultProfilesSeeded { get; set; }

    public bool AutoDetectExcludedApps { get; set; } = true;

    public string Theme { get; set; } = "System";

    public void Validate()
    {
        Scroll ??= new ScrollSettings();
        Tray ??= new TraySettings();
        ApplicationRules ??= [];
        ScrollProfiles ??= [];
        Scroll.Validate();

        foreach (var profile in ScrollProfiles.ToArray())
        {
            profile.Validate();
            if (ScrollProfiles.Count(item => string.Equals(item.Id, profile.Id, StringComparison.OrdinalIgnoreCase)) > 1)
            {
                profile.Id = Guid.NewGuid().ToString("N");
            }
        }

        Theme = Theme is "System" or "Light" or "Dark" ? Theme : "System";

        foreach (var rule in ApplicationRules.ToArray())
        {
            if (string.IsNullOrWhiteSpace(rule.ProcessName))
            {
                ApplicationRules.Remove(rule);
                continue;
            }

            rule.Validate();
            rule.IsUserRule = true;
            MigrateRuleCustomSettings(rule);
        }
    }

    private void MigrateRuleCustomSettings(ApplicationRule rule)
    {
        if (!rule.UseCustomScrollSettings || !string.IsNullOrWhiteSpace(rule.ScrollProfileId))
        {
            return;
        }

        var profile = new ScrollProfile
        {
            Name = $"{rule.DisplayName} профиль",
            Scroll = rule.Scroll
        };

        profile.Validate();
        ScrollProfiles.Add(profile);
        rule.ScrollProfileId = profile.Id;
        rule.UseCustomScrollSettings = false;
    }
}
