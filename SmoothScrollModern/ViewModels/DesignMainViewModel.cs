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

    public string NewScrollProfileName { get; set; } = "Рабочие приложения";

    public string ScrollProfilesCountText { get; set; } = "2 пользовательских";

    public string CurrentApplicationText { get; set; } = "Visual Studio (devenv.exe)";

    public string CurrentWindowTitle { get; set; } = "SmoothScroll - MainWindow.xaml";

    public bool IsCurrentApplicationBypassed { get; set; }

    public ApplicationRule? SelectedRule { get; set; }

    public IReadOnlyList<SelectionOption<EasingType>> EasingOptions { get; } =
    [
        new(EasingType.Linear, "Линейная"),
        new(EasingType.EaseOutCubic, "Мягкая"),
        new(EasingType.EaseOutQuart, "Очень мягкая"),
        new(EasingType.EaseOutQuint, "Максимально мягкая")
    ];

    public IReadOnlyList<SelectionOption<ScrollDeliveryMode>> DeliveryModeOptions { get; } =
    [
        new(ScrollDeliveryMode.FineDelta, "Плавный режим"),
        new(ScrollDeliveryMode.WheelStep, "Режим совместимости")
    ];

    public IReadOnlyList<SelectionOption<string>> ThemeOptions { get; } =
    [
        new("System", "Системная"),
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
        Name = "Глобальный профиль",
        IsGlobal = true
    };

    public ObservableCollection<ScrollProfile> UserScrollProfiles { get; } =
    [
        new()
        {
            Id = "work",
            Name = "Рабочие приложения",
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
