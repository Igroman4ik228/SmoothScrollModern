using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Microsoft.UI.Dispatching;
using SmoothScrollModern.Applications;
using SmoothScrollModern.Core;
using SmoothScrollModern.Scroll;
using SmoothScrollModern.Settings;
using SmoothScrollModern.Startup;
using Forms = System.Windows.Forms;

namespace SmoothScrollModern.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private const int ListPageSize = 8;
    private static readonly TimeSpan SearchDebounceInterval = TimeSpan.FromMilliseconds(300);
    private static readonly string CurrentProcessName = $"{Process.GetCurrentProcess().ProcessName}.exe".ToLowerInvariant();
    private readonly ISettingsService _settingsService;
    private readonly IActiveWindowService _activeWindowService;
    private readonly IApplicationRulesService _applicationRulesService;
    private readonly IStartupService _startupService;
    private readonly DispatcherQueueTimer _activeApplicationTimer;
    private readonly DispatcherQueueTimer _saveTimer;
    private readonly DispatcherQueueTimer _applicationSearchTimer;
    private readonly DispatcherQueueTimer _scrollProfileSearchTimer;
    private readonly List<ApplicationRule> _filteredApplicationRuleMatches = [];
    private readonly List<ScrollProfile> _filteredScrollProfileMatches = [];
    private ApplicationInfo _currentApplication = ApplicationInfo.Empty;
    private string _manualProcessName = string.Empty;
    private string _applicationSearchQuery = string.Empty;
    private string _appliedApplicationSearchQuery = string.Empty;
    private string _scrollProfileSearchQuery = string.Empty;
    private string _appliedScrollProfileSearchQuery = string.Empty;
    private string _newScrollProfileName = string.Empty;
    private ApplicationRule? _selectedRule;
    private DateTimeOffset? _pausedUntil;
    private int _applicationRulesPageIndex;
    private int _scrollProfilesPageIndex;
    private bool _disposed;

    public MainViewModel(
        AppSettings settings,
        ISettingsService settingsService,
        IActiveWindowService activeWindowService,
        IApplicationRulesService applicationRulesService,
        IStartupService startupService)
    {
        Settings = settings;
        _settingsService = settingsService;
        _activeWindowService = activeWindowService;
        _applicationRulesService = applicationRulesService;
        _startupService = startupService;
        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        ApplicationRules = new ObservableCollection<ApplicationRule>(Settings.ApplicationRules);

        GlobalScrollProfile = new ScrollProfile
        {
            Id = string.Empty,
            Name = "Основной профиль",
            Scroll = Settings.Scroll,
            IsGlobal = true
        };
        GlobalScrollProfile.PropertyChanged += OnGlobalScrollProfilePropertyChanged;

        UserScrollProfiles = new ObservableCollection<ScrollProfile>(Settings.ScrollProfiles);
        foreach (var profile in UserScrollProfiles)
        {
            profile.PropertyChanged += OnScrollProfilePropertyChanged;
        }

        ScrollProfileChoices = new ObservableCollection<ScrollProfile>();
        RebuildScrollProfileChoices();
        NormalizeApplicationRuleProfileReferences();

        foreach (var rule in ApplicationRules)
        {
            rule.PropertyChanged += OnRulePropertyChanged;
        }

        ToggleEnabledCommand = new RelayCommand(() => IsEnabled = !IsEnabled);
        DisableCurrentApplicationCommand = new RelayCommand(DisableCurrentApplication, CanDisableCurrentApplication);
        AddManualRuleCommand = new RelayCommand(AddManualRule, () => !string.IsNullOrWhiteSpace(ManualProcessName));
        BrowseApplicationCommand = new RelayCommand(BrowseApplication);
        AddScrollProfileCommand = new RelayCommand(AddScrollProfile, () => !string.IsNullOrWhiteSpace(NewScrollProfileName));
        RemoveScrollProfileCommand = new RelayCommand(RemoveScrollProfile, parameter => parameter is ScrollProfile { IsGlobal: false });
        RemoveSelectedRuleCommand = new RelayCommand(RemoveSelectedRule, CanRemoveSelectedRule);
        RemoveRuleCommand = new RelayCommand(RemoveRule, CanRemoveRule);
        ResetDefaultsCommand = new RelayCommand(ResetDefaults);
        ExportSettingsCommand = new RelayCommand(ExportSettings);
        ImportSettingsCommand = new RelayCommand(ImportSettings);
        PauseCommand = new RelayCommand(() => PauseFor(TimeSpan.FromMinutes(Constants.TrayPauseMinutes)));

        _applicationSearchTimer = dispatcherQueue.CreateTimer();
        _applicationSearchTimer.Interval = SearchDebounceInterval;
        _applicationSearchTimer.Tick += OnApplicationSearchTimerTick;

        _scrollProfileSearchTimer = dispatcherQueue.CreateTimer();
        _scrollProfileSearchTimer.Interval = SearchDebounceInterval;
        _scrollProfileSearchTimer.Tick += OnScrollProfileSearchTimerTick;

        FilteredApplicationRules = [];
        RefreshApplicationRulesFilter();
        FilteredUserScrollProfiles = [];
        RefreshScrollProfilesFilter();

        _activeApplicationTimer = dispatcherQueue.CreateTimer();
        _activeApplicationTimer.Interval = TimeSpan.FromMilliseconds(Constants.ActiveApplicationRefreshMs);
        _activeApplicationTimer.Tick += OnActiveApplicationTimerTick;
        _activeApplicationTimer.Start();

        _saveTimer = dispatcherQueue.CreateTimer();
        _saveTimer.Interval = TimeSpan.FromMilliseconds(350);
        _saveTimer.Tick += OnSaveTimerTick;

        RefreshCurrentApplication();
        ApplyTheme();
        SyncStartup();
    }

    public event Action? StateChanged;

    public event Action<string>? ThemeChanged;

    public AppSettings Settings { get; private set; }

    public ObservableCollection<ApplicationRule> ApplicationRules { get; }

    public ObservableCollection<ApplicationRule> FilteredApplicationRules { get; }

    public ScrollProfile GlobalScrollProfile { get; }

    public ObservableCollection<ScrollProfile> UserScrollProfiles { get; }

    public ObservableCollection<ScrollProfile> FilteredUserScrollProfiles { get; }

    public ObservableCollection<ScrollProfile> ScrollProfileChoices { get; }

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

    public ICommand ToggleEnabledCommand { get; }

    public ICommand DisableCurrentApplicationCommand { get; }

    public ICommand AddManualRuleCommand { get; }

    public ICommand BrowseApplicationCommand { get; }

    public ICommand AddScrollProfileCommand { get; }

    public ICommand RemoveScrollProfileCommand { get; }

    public ICommand RemoveSelectedRuleCommand { get; }

    public ICommand RemoveRuleCommand { get; }

    public ICommand ResetDefaultsCommand { get; }

    public ICommand ExportSettingsCommand { get; }

    public ICommand ImportSettingsCommand { get; }

    public ICommand PauseCommand { get; }

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

    public double ScrollMultiplier
    {
        get => Settings.Scroll.ScrollMultiplier;
        set
        {
            var previousValue = Settings.Scroll.ScrollMultiplier;
            if (Math.Abs(previousValue - value) < 0.0005)
            {
                return;
            }

            Settings.Scroll.ScrollMultiplier = value;
            Settings.Scroll.Validate();
            if (Math.Abs(previousValue - Settings.Scroll.ScrollMultiplier) < 0.0005)
            {
                OnPropertyChanged(nameof(ScrollMultiplier));
                return;
            }

            SaveAndNotify(nameof(ScrollMultiplier));
        }
    }

    public int DurationMs
    {
        get => Settings.Scroll.DurationMs;
        set
        {
            var previousValue = Settings.Scroll.DurationMs;
            if (previousValue == value)
            {
                return;
            }

            Settings.Scroll.DurationMs = value;
            Settings.Scroll.Validate();
            if (previousValue == Settings.Scroll.DurationMs)
            {
                OnPropertyChanged(nameof(DurationMs));
                return;
            }

            SaveAndNotify(nameof(DurationMs));
        }
    }

    public double Smoothness
    {
        get => Settings.Scroll.Smoothness;
        set
        {
            var previousValue = Settings.Scroll.Smoothness;
            if (Math.Abs(previousValue - value) < 0.0005)
            {
                return;
            }

            Settings.Scroll.Smoothness = value;
            Settings.Scroll.Validate();
            if (Math.Abs(previousValue - Settings.Scroll.Smoothness) < 0.0005)
            {
                OnPropertyChanged(nameof(Smoothness));
                return;
            }

            SaveAndNotify(nameof(Smoothness));
        }
    }

    public double Acceleration
    {
        get => Settings.Scroll.Acceleration;
        set
        {
            var previousValue = Settings.Scroll.Acceleration;
            if (Math.Abs(previousValue - value) < 0.0005)
            {
                return;
            }

            Settings.Scroll.Acceleration = value;
            Settings.Scroll.Validate();
            if (Math.Abs(previousValue - Settings.Scroll.Acceleration) < 0.0005)
            {
                OnPropertyChanged(nameof(Acceleration));
                return;
            }

            SaveAndNotify(nameof(Acceleration));
        }
    }

    public EasingType EasingType
    {
        get => Settings.Scroll.EasingType;
        set
        {
            if (Settings.Scroll.EasingType == value)
            {
                return;
            }

            Settings.Scroll.EasingType = value;
            SaveAndNotify(nameof(EasingType));
        }
    }

    public bool EnableHorizontalScroll
    {
        get => Settings.Scroll.EnableHorizontalScroll;
        set
        {
            if (Settings.Scroll.EnableHorizontalScroll == value)
            {
                return;
            }

            Settings.Scroll.EnableHorizontalScroll = value;
            SaveAndNotify(nameof(EnableHorizontalScroll));
        }
    }

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
            ApplyTheme();
            SaveAndNotify(nameof(Theme));
        }
    }

    public string ManualProcessName
    {
        get => _manualProcessName;
        set
        {
            if (SetField(ref _manualProcessName, value))
            {
                (AddManualRuleCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string ApplicationSearchQuery
    {
        get => _applicationSearchQuery;
        set
        {
            if (SetField(ref _applicationSearchQuery, value))
            {
                QueueApplicationRulesSearch();
            }
        }
    }

    public string ScrollProfileSearchQuery
    {
        get => _scrollProfileSearchQuery;
        set
        {
            if (SetField(ref _scrollProfileSearchQuery, value))
            {
                QueueScrollProfilesSearch();
            }
        }
    }

    public string NewScrollProfileName
    {
        get => _newScrollProfileName;
        set
        {
            if (SetField(ref _newScrollProfileName, value))
            {
                (AddScrollProfileCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string ScrollProfilesCountText => BuildListCountText(
        _filteredScrollProfileMatches.Count,
        UserScrollProfiles.Count,
        ScrollProfilesPageIndex,
        !string.IsNullOrWhiteSpace(_appliedScrollProfileSearchQuery));

    public bool HasVisibleUserScrollProfiles => FilteredUserScrollProfiles.Count > 0;

    public bool IsUserScrollProfilesEmpty => UserScrollProfiles.Count == 0;

    public bool IsScrollProfileSearchEmpty => UserScrollProfiles.Count > 0
        && !string.IsNullOrWhiteSpace(_appliedScrollProfileSearchQuery)
        && _filteredScrollProfileMatches.Count == 0;

    public int ScrollProfilesPageIndex
    {
        get => _scrollProfilesPageIndex;
        set
        {
            var pageIndex = CoercePageIndex(value, ScrollProfilesPageCount);
            if (SetField(ref _scrollProfilesPageIndex, pageIndex))
            {
                RefreshScrollProfilesPage();
            }
        }
    }

    public int ScrollProfilesPageCount => GetPageCount(_filteredScrollProfileMatches.Count);

    public bool HasScrollProfilesPagination => _filteredScrollProfileMatches.Count > ListPageSize;

    public string ScrollProfilesPageText => $"Страница {ScrollProfilesPageIndex + 1} из {ScrollProfilesPageCount}";

    public string ProfileCountText
    {
        get
        {
            return BuildListCountText(
                _filteredApplicationRuleMatches.Count,
                ApplicationRules.Count,
                ApplicationRulesPageIndex,
                !string.IsNullOrWhiteSpace(_appliedApplicationSearchQuery));
        }
    }

    public bool HasVisibleApplicationRules => FilteredApplicationRules.Count > 0;

    public bool IsApplicationRulesEmpty => ApplicationRules.Count == 0;

    public bool IsApplicationRuleSearchEmpty => ApplicationRules.Count > 0
        && !string.IsNullOrWhiteSpace(_appliedApplicationSearchQuery)
        && _filteredApplicationRuleMatches.Count == 0;

    public int ApplicationRulesPageIndex
    {
        get => _applicationRulesPageIndex;
        set
        {
            var pageIndex = CoercePageIndex(value, ApplicationRulesPageCount);
            if (SetField(ref _applicationRulesPageIndex, pageIndex))
            {
                RefreshApplicationRulesPage();
            }
        }
    }

    public int ApplicationRulesPageCount => GetPageCount(_filteredApplicationRuleMatches.Count);

    public bool HasApplicationRulesPagination => _filteredApplicationRuleMatches.Count > ListPageSize;

    public string ApplicationRulesPageText => $"Страница {ApplicationRulesPageIndex + 1} из {ApplicationRulesPageCount}";

    public ApplicationRule? SelectedRule
    {
        get => _selectedRule;
        set
        {
            if (SetField(ref _selectedRule, value))
            {
                (RemoveSelectedRuleCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }
    }

    public string CurrentApplicationText => _currentApplication == ApplicationInfo.Empty
        ? "Приложение не определено"
        : $"{GetApplicationDisplayName(_currentApplication)} ({GetApplicationProcessName(_currentApplication)})";

    public string CurrentApplicationDisplayNameText => GetApplicationDisplayName(_currentApplication);

    public string CurrentApplicationProcessNameText => GetApplicationProcessName(_currentApplication);

    public string CurrentApplicationExecutablePathText => string.IsNullOrWhiteSpace(_currentApplication.ExecutablePath)
        ? string.Empty
        : _currentApplication.ExecutablePath;

    public string CurrentWindowTitle => string.IsNullOrWhiteSpace(_currentApplication.WindowTitle)
        ? "Без заголовка окна"
        : _currentApplication.WindowTitle;

    public ApplicationRule? CurrentApplicationRule => FindCurrentApplicationRule();

    public bool HasCurrentApplicationRule => CurrentApplicationRule is not null;

    public bool IsCurrentApplicationRuleMissing => !HasCurrentApplicationRule;

    public bool IsCurrentApplicationExcluded => IsExcludedRule(CurrentApplicationRule);

    public bool IsCurrentApplicationNotExcluded => !IsCurrentApplicationExcluded;

    public bool IsCurrentApplicationBypassed
    {
        get => _applicationRulesService.ShouldBypass(_currentApplication, Settings);
        set => SetCurrentApplicationBypass(value);
    }

    public bool IsOwnApplicationActive => IsOwnApplication(_currentApplication);

    public void PauseFor(TimeSpan duration)
    {
        _pausedUntil = DateTimeOffset.Now.Add(duration);
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(StatusText));
        StateChanged?.Invoke();
    }

    public void RefreshCurrentApplication()
    {
        var activeApplication = _activeWindowService.GetActiveApplication();
        if (!IsOwnApplication(activeApplication))
        {
            _currentApplication = activeApplication;
        }

        if (IsPaused && _pausedUntil <= DateTimeOffset.Now)
        {
            _pausedUntil = null;
        }

        RefreshApplicationRulesFilter();
        OnPropertyChanged(nameof(CurrentApplicationText));
        OnPropertyChanged(nameof(CurrentApplicationDisplayNameText));
        OnPropertyChanged(nameof(CurrentApplicationProcessNameText));
        OnPropertyChanged(nameof(CurrentApplicationExecutablePathText));
        OnPropertyChanged(nameof(CurrentWindowTitle));
        OnPropertyChanged(nameof(CurrentApplicationRule));
        OnPropertyChanged(nameof(HasCurrentApplicationRule));
        OnPropertyChanged(nameof(IsCurrentApplicationRuleMissing));
        OnPropertyChanged(nameof(IsCurrentApplicationExcluded));
        OnPropertyChanged(nameof(IsCurrentApplicationNotExcluded));
        OnPropertyChanged(nameof(IsCurrentApplicationBypassed));
        OnPropertyChanged(nameof(IsOwnApplicationActive));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(StatusText));
        (DisableCurrentApplicationCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RemoveRuleCommand as RelayCommand)?.RaiseCanExecuteChanged();
        (RemoveSelectedRuleCommand as RelayCommand)?.RaiseCanExecuteChanged();
        StateChanged?.Invoke();
    }

    public void DisableCurrentApplication()
    {
        RefreshCurrentApplication();
        if (!CanDisableCurrentApplication())
        {
            return;
        }

        var rule = _applicationRulesService.AddOrUpdateRule(Settings, _currentApplication);
        AddRuleToCollection(rule);
        SaveAndNotifyCurrentApplicationRuleState();
    }

    public bool ShouldHandleScroll()
    {
        return TryGetScrollProfile(out _, out _);
    }

    public bool TryGetScrollProfile(out ScrollSettings scrollSettings, out ScrollDeliveryMode deliveryMode)
    {
        scrollSettings = Settings.Scroll;
        deliveryMode = ScrollDeliveryMode.FineDelta;

        if (!IsEnabled || IsPaused)
        {
            return false;
        }

        var activeApplication = _activeWindowService.GetActiveApplication();
        if (IsOwnApplication(activeApplication))
        {
            return false;
        }

        var rule = FindApplicationRule(activeApplication);
        if (rule is { IsRuleEnabled: true, IsSmoothScrollDisabled: true }
            || _applicationRulesService.ShouldBypass(activeApplication, Settings))
        {
            return false;
        }

        if (rule is { IsRuleEnabled: true })
        {
            scrollSettings = GetScrollSettingsForRule(rule);
            deliveryMode = rule.DeliveryMode;
        }

        return true;
    }

    public void Save()
    {
        _saveTimer.Stop();
        SyncRulesToSettings();
        _settingsService.Save(Settings);
        StateChanged?.Invoke();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _activeApplicationTimer.Stop();
        _activeApplicationTimer.Tick -= OnActiveApplicationTimerTick;
        _applicationSearchTimer.Stop();
        _applicationSearchTimer.Tick -= OnApplicationSearchTimerTick;
        _scrollProfileSearchTimer.Stop();
        _scrollProfileSearchTimer.Tick -= OnScrollProfileSearchTimerTick;
        _saveTimer.Stop();
        _saveTimer.Tick -= OnSaveTimerTick;

        UnsubscribeApplicationRules();
        GlobalScrollProfile.PropertyChanged -= OnGlobalScrollProfilePropertyChanged;
        UnsubscribeScrollProfiles();

        _disposed = true;
    }

    private void AddManualRule()
    {
        var rule = _applicationRulesService.AddManualRule(Settings, ManualProcessName, ManualProcessName);
        AddRuleToCollection(rule);
        ManualProcessName = string.Empty;
        SaveAndNotifyCurrentApplicationRuleState();
    }

    private void BrowseApplication()
    {
        using var dialog = new Forms.OpenFileDialog
        {
            Title = "Выберите приложение",
            Filter = "EXE-файлы (*.exe)|*.exe|Все файлы (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != Forms.DialogResult.OK)
        {
            return;
        }

        var processName = Path.GetFileName(dialog.FileName);
        var displayName = TryGetFileDescription(dialog.FileName) ?? processName;
        var rule = _applicationRulesService.AddApplicationPath(Settings, dialog.FileName, displayName);
        AddRuleToCollection(rule);
        ManualProcessName = processName;
        SaveAndNotifyCurrentApplicationRuleState();
    }

    private void OnActiveApplicationTimerTick(DispatcherQueueTimer sender, object args)
    {
        RefreshCurrentApplication();
    }

    private void OnSaveTimerTick(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        Save();
    }

    private void OnApplicationSearchTimerTick(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        _appliedApplicationSearchQuery = ApplicationSearchQuery;
        RefreshApplicationRulesFilter(resetPage: true);
    }

    private void OnScrollProfileSearchTimerTick(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        _appliedScrollProfileSearchQuery = ScrollProfileSearchQuery;
        RefreshScrollProfilesFilter(resetPage: true);
    }

    private void AddScrollProfile()
    {
        var profile = new ScrollProfile
        {
            Name = NewScrollProfileName,
            Scroll = new ScrollSettings
            {
                ScrollMultiplier = Settings.Scroll.ScrollMultiplier,
                DurationMs = Settings.Scroll.DurationMs,
                Smoothness = Settings.Scroll.Smoothness,
                Acceleration = Settings.Scroll.Acceleration,
                EasingType = Settings.Scroll.EasingType,
                EnableHorizontalScroll = Settings.Scroll.EnableHorizontalScroll
            }
        };

        profile.Validate();
        AddScrollProfileToCollection(profile);
        NewScrollProfileName = string.Empty;
        RebuildScrollProfileChoices();
        RefreshScrollProfilesFilter(resetPage: true);
        SaveAndNotify(nameof(ScrollProfilesCountText));
    }

    private void RemoveScrollProfile(object? parameter)
    {
        if (parameter is not ScrollProfile profile)
        {
            return;
        }

        profile.PropertyChanged -= OnScrollProfilePropertyChanged;
        UserScrollProfiles.Remove(profile);
        RefreshScrollProfilesFilter(resetPage: true);
        foreach (var rule in ApplicationRules.Where(rule =>
                     string.Equals(rule.ScrollProfileId, profile.Id, StringComparison.OrdinalIgnoreCase)))
        {
            rule.ScrollProfileId = string.Empty;
        }

        RebuildScrollProfileChoices();
        SaveAndNotify(nameof(ScrollProfilesCountText), nameof(IsCurrentApplicationBypassed));
    }

    private void RemoveSelectedRule()
    {
        RemoveRule(SelectedRule);
    }

    private void RemoveRule(object? parameter)
    {
        if (parameter is not ApplicationRule rule)
        {
            return;
        }

        rule.PropertyChanged -= OnRulePropertyChanged;
        Settings.ApplicationRules.Remove(rule);
        ApplicationRules.Remove(rule);
        RefreshApplicationRulesFilter(resetPage: true);
        if (ReferenceEquals(SelectedRule, rule))
        {
            SelectedRule = null;
        }

        SaveAndNotifyCurrentApplicationRuleState();
    }

    private bool CanRemoveSelectedRule()
    {
        return SelectedRule is not null;
    }

    private static bool CanRemoveRule(object? parameter)
    {
        return parameter is ApplicationRule;
    }

    private bool CanDisableCurrentApplication()
    {
        return _currentApplication != ApplicationInfo.Empty && !IsOwnApplicationActive;
    }

    private void SetCurrentApplicationBypass(bool isBypassed)
    {
        if (!CanDisableCurrentApplication())
        {
            OnPropertyChanged(nameof(IsCurrentApplicationBypassed));
            return;
        }

        var rule = FindCurrentApplicationRule();
        if (isBypassed)
        {
            rule ??= _applicationRulesService.AddOrUpdateRule(Settings, _currentApplication);
            rule.IsRuleEnabled = true;
            rule.IsSmoothScrollDisabled = true;
            AddRuleToCollection(rule);
        }
        else if (rule is not null)
        {
            rule.IsRuleEnabled = false;
        }

        SaveAndNotify(nameof(IsCurrentApplicationBypassed));
    }

    private ApplicationRule? FindCurrentApplicationRule()
    {
        return FindApplicationRule(_currentApplication);
    }

    private ApplicationRule? FindApplicationRule(ApplicationInfo application)
    {
        return Settings.ApplicationRules.FirstOrDefault(rule => ApplicationRulesService.Matches(rule, application));
    }

    private static bool IsOwnApplication(ApplicationInfo application)
    {
        return string.Equals(
            application.ProcessName,
            CurrentProcessName,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExcludedRule(ApplicationRule? rule)
    {
        return rule is { IsRuleEnabled: true, IsSmoothScrollDisabled: true };
    }

    private void ResetDefaults()
    {
        ReplaceSettings(new AppSettings());
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

        ReplaceSettings(_settingsService.Import(dialog.FileName));
        _settingsService.Save(Settings);
    }

    private void AddRuleToCollection(ApplicationRule rule)
    {
        if (!ApplicationRules.Contains(rule))
        {
            ApplicationRules.Add(rule);
            rule.PropertyChanged += OnRulePropertyChanged;
            RefreshApplicationRulesFilter();
            OnPropertyChanged(nameof(ProfileCountText));
        }
    }

    private void AddScrollProfileToCollection(ScrollProfile profile)
    {
        profile.PropertyChanged += OnScrollProfilePropertyChanged;
        UserScrollProfiles.Add(profile);
        RefreshScrollProfilesFilter();
    }

    private void ReplaceSettings(AppSettings settings)
    {
        UnsubscribeApplicationRules();
        UnsubscribeScrollProfiles();

        Settings = settings;
        ApplicationRules.Clear();
        UserScrollProfiles.Clear();
        GlobalScrollProfile.Scroll = Settings.Scroll;

        foreach (var rule in Settings.ApplicationRules)
        {
            AddRuleToCollection(rule);
        }

        foreach (var profile in Settings.ScrollProfiles)
        {
            AddScrollProfileToCollection(profile);
        }

        RebuildScrollProfileChoices();
        NormalizeApplicationRuleProfileReferences();
        RefreshApplicationRulesFilter(resetPage: true);
        RefreshScrollProfilesFilter(resetPage: true);
        SyncStartup();
        ApplyTheme();
        OnPropertyChanged(string.Empty);
        StateChanged?.Invoke();
    }

    private void UnsubscribeApplicationRules()
    {
        foreach (var rule in ApplicationRules)
        {
            rule.PropertyChanged -= OnRulePropertyChanged;
        }
    }

    private void UnsubscribeScrollProfiles()
    {
        foreach (var profile in UserScrollProfiles)
        {
            profile.PropertyChanged -= OnScrollProfilePropertyChanged;
        }
    }

    private void SyncRulesToSettings()
    {
        Settings.ApplicationRules = ApplicationRules.ToList();
        Settings.ScrollProfiles = UserScrollProfiles.ToList();
    }

    private void OnRulePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (RulePropertyAffectsSearch(e.PropertyName))
        {
            RefreshApplicationRulesFilter();
            OnPropertyChanged(nameof(ProfileCountText));
        }

        SaveAndNotifyCurrentApplicationRuleState();
    }

    private void OnGlobalScrollProfilePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Settings.Scroll = GlobalScrollProfile.Scroll;
        SaveAndNotify(
            nameof(ScrollMultiplier),
            nameof(DurationMs),
            nameof(Smoothness),
            nameof(Acceleration),
            nameof(EasingType),
            nameof(EnableHorizontalScroll));
    }

    private void OnScrollProfilePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ScrollProfile.Name) or nameof(ScrollProfile.Id))
        {
            RebuildScrollProfileChoices();
            RefreshScrollProfilesFilter();
        }

        SaveAndNotify(nameof(ScrollProfilesCountText), nameof(IsCurrentApplicationBypassed));
    }

    private ScrollSettings GetScrollSettingsForRule(ApplicationRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.ScrollProfileId))
        {
            return Settings.Scroll;
        }

        return UserScrollProfiles.FirstOrDefault(profile =>
                string.Equals(profile.Id, rule.ScrollProfileId, StringComparison.OrdinalIgnoreCase))
            ?.Scroll ?? Settings.Scroll;
    }

    private void RebuildScrollProfileChoices()
    {
        ScrollProfileChoices.Clear();
        ScrollProfileChoices.Add(GlobalScrollProfile);
        foreach (var profile in UserScrollProfiles)
        {
            ScrollProfileChoices.Add(profile);
        }
    }

    private void NormalizeApplicationRuleProfileReferences()
    {
        foreach (var rule in ApplicationRules)
        {
            if (string.IsNullOrWhiteSpace(rule.ScrollProfileId))
            {
                continue;
            }

            var hasProfile = UserScrollProfiles.Any(profile =>
                string.Equals(profile.Id, rule.ScrollProfileId, StringComparison.OrdinalIgnoreCase));
            if (!hasProfile)
            {
                rule.ScrollProfileId = string.Empty;
            }
        }
    }

    private void RefreshApplicationRulesFilter(bool resetPage = false)
    {
        var visibleRules = ApplicationRules
            .Where(FilterApplicationRule)
            .ToList();

        _filteredApplicationRuleMatches.Clear();
        _filteredApplicationRuleMatches.AddRange(visibleRules);
        ApplyApplicationRulesPageIndex(resetPage);
        RefreshApplicationRulesPage();
    }

    private void RefreshApplicationRulesPage()
    {
        var pageItems = GetPageItems(_filteredApplicationRuleMatches, ApplicationRulesPageIndex);
        SyncCollection(FilteredApplicationRules, pageItems);

        OnPropertyChanged(nameof(ProfileCountText));
        OnPropertyChanged(nameof(HasVisibleApplicationRules));
        OnPropertyChanged(nameof(IsApplicationRulesEmpty));
        OnPropertyChanged(nameof(IsApplicationRuleSearchEmpty));
        OnPropertyChanged(nameof(ApplicationRulesPageCount));
        OnPropertyChanged(nameof(HasApplicationRulesPagination));
        OnPropertyChanged(nameof(ApplicationRulesPageText));
    }

    private void RefreshScrollProfilesFilter(bool resetPage = false)
    {
        var visibleProfiles = UserScrollProfiles
            .Where(FilterScrollProfile)
            .ToList();

        _filteredScrollProfileMatches.Clear();
        _filteredScrollProfileMatches.AddRange(visibleProfiles);
        ApplyScrollProfilesPageIndex(resetPage);
        RefreshScrollProfilesPage();
    }

    private void RefreshScrollProfilesPage()
    {
        var pageItems = GetPageItems(_filteredScrollProfileMatches, ScrollProfilesPageIndex);
        SyncCollection(FilteredUserScrollProfiles, pageItems);

        OnPropertyChanged(nameof(ScrollProfilesCountText));
        OnPropertyChanged(nameof(HasVisibleUserScrollProfiles));
        OnPropertyChanged(nameof(IsUserScrollProfilesEmpty));
        OnPropertyChanged(nameof(IsScrollProfileSearchEmpty));
        OnPropertyChanged(nameof(ScrollProfilesPageCount));
        OnPropertyChanged(nameof(HasScrollProfilesPagination));
        OnPropertyChanged(nameof(ScrollProfilesPageText));
    }

    private static bool RulePropertyAffectsSearch(string? propertyName)
    {
        return propertyName is nameof(ApplicationRule.DisplayName)
            or nameof(ApplicationRule.ProcessName)
            or nameof(ApplicationRule.ExecutablePath);
    }

    private bool FilterApplicationRule(ApplicationRule rule)
    {
        if (string.IsNullOrWhiteSpace(_appliedApplicationSearchQuery))
        {
            return true;
        }

        return ContainsSearchText(rule.DisplayName)
               || ContainsSearchText(rule.ProcessName)
               || ContainsSearchText(rule.ExecutablePath);
    }

    private bool FilterScrollProfile(ScrollProfile profile)
    {
        return string.IsNullOrWhiteSpace(_appliedScrollProfileSearchQuery)
               || ContainsSearchText(profile.Name, _appliedScrollProfileSearchQuery);
    }

    private bool ContainsSearchText(string value)
    {
        return ContainsSearchText(value, _appliedApplicationSearchQuery);
    }

    private static bool ContainsSearchText(string value, string searchQuery)
    {
        return !string.IsNullOrWhiteSpace(value)
               && !string.IsNullOrWhiteSpace(searchQuery)
               && value.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
    }

    private void QueueApplicationRulesSearch()
    {
        _applicationSearchTimer.Stop();
        _applicationSearchTimer.Start();
    }

    private void QueueScrollProfilesSearch()
    {
        _scrollProfileSearchTimer.Stop();
        _scrollProfileSearchTimer.Start();
    }

    private void ApplyApplicationRulesPageIndex(bool resetPage)
    {
        var pageIndex = CoercePageIndex(resetPage ? 0 : _applicationRulesPageIndex, ApplicationRulesPageCount);
        if (_applicationRulesPageIndex != pageIndex)
        {
            _applicationRulesPageIndex = pageIndex;
            OnPropertyChanged(nameof(ApplicationRulesPageIndex));
        }
    }

    private void ApplyScrollProfilesPageIndex(bool resetPage)
    {
        var pageIndex = CoercePageIndex(resetPage ? 0 : _scrollProfilesPageIndex, ScrollProfilesPageCount);
        if (_scrollProfilesPageIndex != pageIndex)
        {
            _scrollProfilesPageIndex = pageIndex;
            OnPropertyChanged(nameof(ScrollProfilesPageIndex));
        }
    }

    private static int CoercePageIndex(int pageIndex, int pageCount)
    {
        return Math.Clamp(pageIndex, 0, Math.Max(0, pageCount - 1));
    }

    private static int GetPageCount(int itemCount)
    {
        return Math.Max(1, (int)Math.Ceiling(itemCount / (double)ListPageSize));
    }

    private static List<T> GetPageItems<T>(IReadOnlyList<T> items, int pageIndex)
    {
        return items
            .Skip(pageIndex * ListPageSize)
            .Take(ListPageSize)
            .ToList();
    }

    private static void SyncCollection<T>(ObservableCollection<T> collection, IReadOnlyList<T> items)
    {
        var itemSet = items.ToHashSet();
        for (var index = collection.Count - 1; index >= 0; index--)
        {
            if (!itemSet.Contains(collection[index]))
            {
                collection.RemoveAt(index);
            }
        }

        for (var targetIndex = 0; targetIndex < items.Count; targetIndex++)
        {
            var item = items[targetIndex];
            var currentIndex = collection.IndexOf(item);
            if (currentIndex < 0)
            {
                collection.Insert(targetIndex, item);
                continue;
            }

            if (currentIndex != targetIndex)
            {
                collection.Move(currentIndex, targetIndex);
            }
        }
    }

    private static string BuildListCountText(int filteredCount, int totalCount, int pageIndex, bool isSearching)
    {
        if (filteredCount == 0)
        {
            return $"0 из {totalCount}";
        }

        if (filteredCount <= ListPageSize)
        {
            return isSearching ? $"{filteredCount} из {totalCount}" : $"{totalCount} из {totalCount}";
        }

        var firstItem = pageIndex * ListPageSize + 1;
        var lastItem = Math.Min(firstItem + ListPageSize - 1, filteredCount);
        return isSearching
            ? $"{firstItem}-{lastItem} из {filteredCount} (всего {totalCount})"
            : $"{firstItem}-{lastItem} из {totalCount}";
    }

    private static string GetApplicationDisplayName(ApplicationInfo application)
    {
        return string.IsNullOrWhiteSpace(application.DisplayName)
            ? "Неизвестное приложение"
            : application.DisplayName;
    }

    private static string GetApplicationProcessName(ApplicationInfo application)
    {
        return string.IsNullOrWhiteSpace(application.ProcessName)
            ? "процесс не определен"
            : application.ProcessName;
    }

    private static string? TryGetFileDescription(string fileName)
    {
        try
        {
            var description = FileVersionInfo.GetVersionInfo(fileName).FileDescription;
            return string.IsNullOrWhiteSpace(description) ? null : description;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void SaveAndNotify(params string[] propertyNames)
    {
        QueueSave();
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }

    private void SaveAndNotifyCurrentApplicationRuleState()
    {
        SaveAndNotify(
            nameof(CurrentApplicationRule),
            nameof(HasCurrentApplicationRule),
            nameof(IsCurrentApplicationRuleMissing),
            nameof(IsCurrentApplicationExcluded),
            nameof(IsCurrentApplicationNotExcluded),
            nameof(IsCurrentApplicationBypassed));
        (RemoveRuleCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    private void QueueSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
        StateChanged?.Invoke();
    }

    private void SyncStartup()
    {
        _startupService.SetEnabled(Settings.Tray.StartWithWindows);
    }

    private void ApplyTheme() => ThemeChanged?.Invoke(Settings.Theme);
}
