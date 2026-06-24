using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmoothScrollModern.Shared.Presentation;
using SmoothScrollModern.Settings;
using SmoothScrollModern.Startup;
using Forms = System.Windows.Forms;

namespace SmoothScrollModern.Features.Settings.ViewModels;

public sealed class ApplicationSettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly IStartupService _startupService;
    private readonly Func<AppSettings> _settingsAccessor;
    private readonly Action<AppSettings> _replaceSettings;
    private readonly Action _requestSave;
    private readonly Action _stateChanged;
    private readonly Action<string> _themeChanged;
    private DateTimeOffset? _pausedUntil;

    public ApplicationSettingsViewModel(
        ISettingsService settingsService,
        IStartupService startupService,
        Func<AppSettings> settingsAccessor,
        Action<AppSettings> replaceSettings,
        Action requestSave,
        Action stateChanged,
        Action<string> themeChanged)
    {
        _settingsService = settingsService;
        _startupService = startupService;
        _settingsAccessor = settingsAccessor;
        _replaceSettings = replaceSettings;
        _requestSave = requestSave;
        _stateChanged = stateChanged;
        _themeChanged = themeChanged;

        ToggleEnabledCommand = new RelayCommand(() => IsEnabled = !IsEnabled);
        PauseCommand = new RelayCommand(() => PauseFor(TimeSpan.FromMinutes(Core.Constants.TrayPauseMinutes)));
        ResetDefaultsCommand = new RelayCommand(ResetDefaults);
        ExportSettingsCommand = new RelayCommand(ExportSettings);
        ImportSettingsCommand = new RelayCommand(ImportSettings);
    }

    public IReadOnlyList<SelectionOption<string>> ThemeOptions { get; } =
    [
        new("System", "Как в Windows"),
        new("Light", "Светлая"),
        new("Dark", "Темная")
    ];

    public IRelayCommand ToggleEnabledCommand { get; }

    public IRelayCommand PauseCommand { get; }

    public IRelayCommand ResetDefaultsCommand { get; }

    public IRelayCommand ExportSettingsCommand { get; }

    public IRelayCommand ImportSettingsCommand { get; }

    public bool IsEnabled
    {
        get => Settings.IsEnabled;
        set
        {
            if (Settings.IsEnabled == value)
            {
                return;
            }

            Settings.IsEnabled = value;
            SaveAndNotify(nameof(IsEnabled), nameof(StatusText));
        }
    }

    public bool IsPaused => _pausedUntil is not null && _pausedUntil > DateTimeOffset.Now;

    public string StatusText => IsPaused
        ? $"Пауза до {_pausedUntil:HH:mm}"
        : IsEnabled ? "Плавная прокрутка включена" : "Плавная прокрутка выключена";

    public bool AutoDetectExcludedApps
    {
        get => Settings.AutoDetectExcludedApps;
        set
        {
            if (Settings.AutoDetectExcludedApps == value)
            {
                return;
            }

            Settings.AutoDetectExcludedApps = value;
            SaveAndNotify(nameof(AutoDetectExcludedApps));
        }
    }

    public bool StartWithWindows
    {
        get => Settings.Tray.StartWithWindows;
        set
        {
            if (Settings.Tray.StartWithWindows == value)
            {
                return;
            }

            Settings.Tray.StartWithWindows = value;
            SyncStartup();
            SaveAndNotify(nameof(StartWithWindows));
        }
    }

    public bool StartMinimizedToTray
    {
        get => Settings.Tray.StartMinimizedToTray;
        set
        {
            if (Settings.Tray.StartMinimizedToTray == value)
            {
                return;
            }

            Settings.Tray.StartMinimizedToTray = value;
            SaveAndNotify(nameof(StartMinimizedToTray));
        }
    }

    public bool CloseToTray
    {
        get => Settings.Tray.CloseToTray;
        set
        {
            if (Settings.Tray.CloseToTray == value)
            {
                return;
            }

            Settings.Tray.CloseToTray = value;
            SaveAndNotify(nameof(CloseToTray));
        }
    }

    public string Theme
    {
        get => Settings.Theme;
        set
        {
            if (Settings.Theme == value)
            {
                return;
            }

            Settings.Theme = value;
            _themeChanged(Settings.Theme);
            SaveAndNotify(nameof(Theme));
        }
    }

    private AppSettings Settings => _settingsAccessor();

    public void LoadSettings()
    {
        SyncStartup();
        _themeChanged(Settings.Theme);
        OnPropertyChanged(string.Empty);
        _stateChanged();
    }

    public void PauseFor(TimeSpan duration)
    {
        _pausedUntil = DateTimeOffset.Now.Add(duration);
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(StatusText));
        _stateChanged();
    }

    public void RefreshPauseState()
    {
        if (_pausedUntil is null || _pausedUntil > DateTimeOffset.Now)
        {
            return;
        }

        _pausedUntil = null;
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(StatusText));
        _stateChanged();
    }

    private void ResetDefaults()
    {
        _replaceSettings(new AppSettings());
        _settingsService.Save(Settings);
    }

    private void ExportSettings()
    {
        using var dialog = new Forms.SaveFileDialog
        {
            Title = "Экспорт настроек",
            Filter = "JSON (*.json)|*.json",
            FileName = "smoothscroll-settings.json"
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK)
        {
            _settingsService.Export(Settings, dialog.FileName);
        }
    }

    private void ImportSettings()
    {
        using var dialog = new Forms.OpenFileDialog
        {
            Title = "Импорт настроек",
            Filter = "JSON (*.json)|*.json"
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return;
        }

        _replaceSettings(_settingsService.Import(dialog.FileName));
        _settingsService.Save(Settings);
    }

    private void SaveAndNotify(params string[] propertyNames)
    {
        _requestSave();
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }

    private void SyncStartup()
    {
        _startupService.SetEnabled(Settings.Tray.StartWithWindows);
    }
}
