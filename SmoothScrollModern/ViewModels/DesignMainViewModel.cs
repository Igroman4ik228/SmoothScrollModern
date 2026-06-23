using System.Collections.ObjectModel;
using System.Windows.Input;
using SmoothScrollModern.Scroll;
using SmoothScrollModern.Settings;

namespace SmoothScrollModern.ViewModels;

public sealed class DesignMainViewModel
{
    public DesignMainViewModel()
    {
        foreach (var rule in ApplicationRules)
        {
            FilteredApplicationRules.Add(rule);
        }

        foreach (var profile in UserScrollProfiles)
        {
            FilteredUserScrollProfiles.Add(profile);
        }

        ScrollProfileChoices.Add(GlobalScrollProfile);
        foreach (var profile in UserScrollProfiles)
        {
            ScrollProfileChoices.Add(profile);
        }
    }

    public bool IsEnabled { get; set; } = true;

    public string StatusText { get; set; } = "Плавная прокрутка включена";

    public double ScrollMultiplier { get; set; } = 1.4;

    public int DurationMs { get; set; } = 160;

    public double Smoothness { get; set; } = 0.75;

    public double Acceleration { get; set; } = 1.2;

    public EasingType EasingType { get; set; } = EasingType.EaseOutCubic;

    public bool EnableHorizontalScroll { get; set; } = true;

    public bool AutoDetectExcludedApps { get; set; } = true;

    public bool StartWithWindows { get; set; }

    public bool StartMinimizedToTray { get; set; }

    public bool CloseToTray { get; set; } = true;

    public string Theme { get; set; } = "Dark";

    public string ManualProcessName { get; set; } = "chrome.exe";

    public string ApplicationSearchQuery { get; set; } = string.Empty;

    public string ProfileCountText { get; set; } = "4 правила";

    public string ScrollProfileSearchQuery { get; set; } = string.Empty;

    public string NewScrollProfileName { get; set; } = "Рабочие исключения";

    public string ScrollProfilesCountText { get; set; } = "2 из 2";

    public bool HasVisibleUserScrollProfiles { get; set; } = true;

    public bool IsUserScrollProfilesEmpty { get; set; }

    public bool IsScrollProfileSearchEmpty { get; set; }

    public int ScrollProfilesPageIndex { get; set; }

    public int ScrollProfilesPageCount { get; set; } = 1;

    public bool HasScrollProfilesPagination { get; set; }

    public string ScrollProfilesPageText { get; set; } = "Страница 1 из 1";

    public bool HasVisibleApplicationRules { get; set; } = true;

    public bool IsApplicationRulesEmpty { get; set; }

    public bool IsApplicationRuleSearchEmpty { get; set; }

    public int ApplicationRulesPageIndex { get; set; }

    public int ApplicationRulesPageCount { get; set; } = 1;

    public bool HasApplicationRulesPagination { get; set; }

    public string ApplicationRulesPageText { get; set; } = "Страница 1 из 1";

    public string CurrentApplicationText { get; set; } = "Visual Studio (devenv.exe)";

    public string CurrentWindowTitle { get; set; } = "SmoothScroll - MainWindow.xaml";

    public ApplicationRule? CurrentApplicationRule => ApplicationRules.FirstOrDefault(rule => rule.ProcessName == "devenv.exe");

    public bool HasCurrentApplicationRule => CurrentApplicationRule is not null;

    public bool IsCurrentApplicationRuleMissing => !HasCurrentApplicationRule;

    public bool IsCurrentApplicationExcluded => CurrentApplicationRule is { IsRuleEnabled: true, IsSmoothScrollDisabled: true };

    public bool IsCurrentApplicationNotExcluded => !IsCurrentApplicationExcluded;

    public bool IsCurrentApplicationBypassed { get; set; }

    public ApplicationRule? SelectedRule { get; set; }

    public IReadOnlyList<SelectionOption<EasingType>> EasingOptions { get; } =
    [
        new(EasingType.Linear, "Ровно"),
        new(EasingType.EaseOutCubic, "Мягко"),
        new(EasingType.EaseOutQuart, "Очень мягко"),
        new(EasingType.EaseOutQuint, "Максимально мягко")
    ];

    public IReadOnlyList<SelectionOption<ScrollDeliveryMode>> DeliveryModeOptions { get; } =
    [
        new(ScrollDeliveryMode.FineDelta, "Плавно"),
        new(ScrollDeliveryMode.WheelStep, "Как обычно")
    ];

    public IReadOnlyList<SelectionOption<string>> ThemeOptions { get; } =
    [
        new("System", "Как в Windows"),
        new("Light", "Светлая"),
        new("Dark", "Темная")
    ];

    public ObservableCollection<ApplicationRule> ApplicationRules { get; } =
    [
        new()
        {
            ProcessName = "explorer.exe",
            ExecutablePath = @"C:\Windows\explorer.exe",
            DisplayName = "Проводник",
            IsRuleEnabled = true,
            IsSmoothScrollDisabled = false,
            IsUserRule = true,
            DeliveryMode = ScrollDeliveryMode.WheelStep
        },
        new()
        {
            ProcessName = "steam.exe",
            DisplayName = "Steam",
            IsRuleEnabled = true,
            IsSmoothScrollDisabled = true,
            IsUserRule = true
        },
        new()
        {
            ProcessName = "devenv.exe",
            ExecutablePath = @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe",
            DisplayName = "Visual Studio",
            IsRuleEnabled = true,
            IsSmoothScrollDisabled = true,
            IsUserRule = true,
            ScrollProfileId = "work"
        },
        new()
        {
            ProcessName = "chrome.exe",
            DisplayName = "Google Chrome",
            IsRuleEnabled = false,
            IsSmoothScrollDisabled = true,
            IsUserRule = true
        }
    ];

    public ObservableCollection<ApplicationRule> FilteredApplicationRules { get; } = [];

    public ScrollProfile GlobalScrollProfile { get; } = new()
    {
        Id = string.Empty,
        Name = "Основной профиль",
        IsGlobal = true
    };

    public ObservableCollection<ScrollProfile> UserScrollProfiles { get; } =
    [
        new()
        {
            Id = "work",
            Name = "Рабочие исключения",
            ProfileScrollMultiplier = 1.1,
            ProfileDurationMs = 110,
            ProfileSmoothness = 0.55
        },
        new()
        {
            Id = "compat",
            Name = "Совместимость",
            ProfileScrollMultiplier = 1.0,
            ProfileDurationMs = 90,
            ProfileSmoothness = 0.35
        }
    ];

    public ObservableCollection<ScrollProfile> FilteredUserScrollProfiles { get; } = [];

    public ObservableCollection<ScrollProfile> ScrollProfileChoices { get; } = [];

    public ICommand ToggleEnabledCommand { get; } = new RelayCommand(() => { });

    public ICommand DisableCurrentApplicationCommand { get; } = new RelayCommand(() => { });

    public ICommand AddManualRuleCommand { get; } = new RelayCommand(() => { });

    public ICommand BrowseApplicationCommand { get; } = new RelayCommand(() => { });

    public ICommand AddScrollProfileCommand { get; } = new RelayCommand(() => { });

    public ICommand RemoveScrollProfileCommand { get; } = new RelayCommand(_ => { });

    public ICommand RemoveSelectedRuleCommand { get; } = new RelayCommand(() => { });

    public ICommand RemoveRuleCommand { get; } = new RelayCommand(_ => { });

    public ICommand ResetDefaultsCommand { get; } = new RelayCommand(() => { });

    public ICommand ExportSettingsCommand { get; } = new RelayCommand(() => { });

    public ICommand ImportSettingsCommand { get; } = new RelayCommand(() => { });
}
