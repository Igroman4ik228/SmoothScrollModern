using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using SmoothScrollModern.Applications;
using SmoothScrollModern.Shared.Presentation;
using SmoothScrollModern.Features.Profiles.ViewModels;
using SmoothScrollModern.Scroll;
using SmoothScrollModern.Settings;
using Forms = System.Windows.Forms;

namespace SmoothScrollModern.Features.Applications.ViewModels;

public sealed class ApplicationRulesViewModel : ObservableObject
{
    private const int ListPageSize = 8;
    private static readonly TimeSpan SearchDebounceInterval = TimeSpan.FromMilliseconds(300);
    private static readonly string CurrentProcessName = $"{Process.GetCurrentProcess().ProcessName}.exe".ToLowerInvariant();
    private readonly IActiveWindowService _activeWindowService;
    private readonly IApplicationRulesService _applicationRulesService;
    private readonly ProfilesViewModel _profilesViewModel;
    private readonly DispatcherQueueTimer _searchTimer;
    private readonly Action _requestSave;
    private readonly Action _stateChanged;
    private readonly List<ApplicationRule> _filteredMatches = [];
    private AppSettings _settings;
    private ApplicationInfo _currentApplication = ApplicationInfo.Empty;
    private string _manualProcessName = string.Empty;
    private string _searchQuery = string.Empty;
    private string _appliedSearchQuery = string.Empty;
    private int _pageIndex;

    public ApplicationRulesViewModel(
        AppSettings settings,
        IActiveWindowService activeWindowService,
        IApplicationRulesService applicationRulesService,
        ProfilesViewModel profilesViewModel,
        DispatcherQueue dispatcherQueue,
        Action requestSave,
        Action stateChanged)
    {
        _settings = settings;
        _activeWindowService = activeWindowService;
        _applicationRulesService = applicationRulesService;
        _profilesViewModel = profilesViewModel;
        _requestSave = requestSave;
        _stateChanged = stateChanged;

        ApplicationRules = new ObservableCollection<ApplicationRule>(settings.ApplicationRules);
        FilteredApplicationRules = [];
        foreach (var rule in ApplicationRules)
        {
            rule.PropertyChanged += OnRulePropertyChanged;
        }

        DisableCurrentApplicationCommand = new RelayCommand(DisableCurrentApplication, CanDisableCurrentApplication);
        AddManualRuleCommand = new RelayCommand(AddManualRule, CanAddManualRule);
        BrowseApplicationCommand = new RelayCommand(BrowseApplication);
        RemoveRuleCommand = new RelayCommand<ApplicationRule?>(RemoveRule, rule => rule is not null);

        _searchTimer = dispatcherQueue.CreateTimer();
        _searchTimer.Interval = SearchDebounceInterval;
        _searchTimer.Tick += OnSearchTimerTick;

        _profilesViewModel.NormalizeApplicationRuleProfileReferences(ApplicationRules);
        RefreshFilter();
        RefreshCurrentApplication();
    }

    public IReadOnlyList<SelectionOption<ScrollDeliveryMode>> DeliveryModeOptions { get; } =
    [
        new(ScrollDeliveryMode.FineDelta, "Плавно"),
        new(ScrollDeliveryMode.WheelStep, "Как обычно")
    ];

    public ObservableCollection<ScrollProfile> ScrollProfileChoices => _profilesViewModel.ScrollProfileChoices;

    public ObservableCollection<ApplicationRule> ApplicationRules { get; }

    public ObservableCollection<ApplicationRule> FilteredApplicationRules { get; }

    public IRelayCommand DisableCurrentApplicationCommand { get; }

    public IRelayCommand AddManualRuleCommand { get; }

    public IRelayCommand BrowseApplicationCommand { get; }

    public IRelayCommand<ApplicationRule?> RemoveRuleCommand { get; }

    public string ManualProcessName
    {
        get => _manualProcessName;
        set
        {
            if (SetProperty(ref _manualProcessName, value))
            {
                AddManualRuleCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string ApplicationSearchQuery
    {
        get => _searchQuery;
        set
        {
            if (SetProperty(ref _searchQuery, value))
            {
                QueueSearch();
            }
        }
    }

    public string ProfileCountText => BuildListCountText(
        _filteredMatches.Count,
        ApplicationRules.Count,
        ApplicationRulesPageIndex,
        !string.IsNullOrWhiteSpace(_appliedSearchQuery));

    public bool HasVisibleApplicationRules => FilteredApplicationRules.Count > 0;

    public bool IsApplicationRulesEmpty => ApplicationRules.Count == 0;

    public bool IsApplicationRuleSearchEmpty => ApplicationRules.Count > 0
        && !string.IsNullOrWhiteSpace(_appliedSearchQuery)
        && _filteredMatches.Count == 0;

    public int ApplicationRulesPageIndex
    {
        get => _pageIndex;
        set
        {
            var pageIndex = CoercePageIndex(value, ApplicationRulesPageCount);
            if (SetProperty(ref _pageIndex, pageIndex))
            {
                RefreshPage();
            }
        }
    }

    public int ApplicationRulesPageCount => GetPageCount(_filteredMatches.Count);

    public bool HasApplicationRulesPagination => _filteredMatches.Count > ListPageSize;

    public string ApplicationRulesPageText => $"Страница {ApplicationRulesPageIndex + 1} из {ApplicationRulesPageCount}";

    public string CurrentApplicationDisplayNameText => GetApplicationDisplayName(_currentApplication);

    public string CurrentApplicationProcessNameText => GetApplicationProcessName(_currentApplication);

    public string CurrentApplicationExecutablePathText => string.IsNullOrWhiteSpace(_currentApplication.ExecutablePath)
        ? string.Empty
        : _currentApplication.ExecutablePath;

    public ApplicationRule? CurrentApplicationRule => FindCurrentApplicationRule();

    public bool HasCurrentApplicationRule => CurrentApplicationRule is not null;

    public bool IsCurrentApplicationRuleMissing => !HasCurrentApplicationRule;

    public void LoadSettings(AppSettings settings)
    {
        UnsubscribeRules();
        _settings = settings;
        ApplicationRules.Clear();

        foreach (var rule in settings.ApplicationRules)
        {
            AddRuleToCollection(rule);
        }

        _profilesViewModel.NormalizeApplicationRuleProfileReferences(ApplicationRules);
        RefreshFilter(resetPage: true);
        RefreshCurrentApplication();
        OnPropertyChanged(string.Empty);
    }

    public void RefreshCurrentApplication()
    {
        var activeApplication = _activeWindowService.GetActiveApplication();
        var previousApplication = _currentApplication;
        if (!IsOwnApplication(activeApplication))
        {
            _currentApplication = activeApplication;
        }

        if (_currentApplication == previousApplication)
        {
            return;
        }

        NotifyCurrentApplicationState();
        DisableCurrentApplicationCommand.NotifyCanExecuteChanged();
        RemoveRuleCommand.NotifyCanExecuteChanged();
        _stateChanged();
    }

    public void DisableCurrentApplication()
    {
        RefreshCurrentApplication();
        if (!CanDisableCurrentApplication())
        {
            return;
        }

        var rule = _applicationRulesService.AddOrUpdateRule(_settings, _currentApplication);
        AddRuleToCollection(rule);
        SaveAndNotifyCurrentApplicationRuleState();
    }

    public bool TryGetScrollProfile(bool isEnabled, bool isPaused, out ScrollSettings scrollSettings, out ScrollDeliveryMode deliveryMode)
    {
        scrollSettings = _settings.Scroll;
        deliveryMode = ScrollDeliveryMode.FineDelta;

        if (!isEnabled || isPaused)
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
            || _applicationRulesService.ShouldBypass(activeApplication, _settings))
        {
            return false;
        }

        if (rule is { IsRuleEnabled: true })
        {
            scrollSettings = _profilesViewModel.GetScrollSettings(rule.ScrollProfileId);
            deliveryMode = rule.DeliveryMode;
        }

        return true;
    }

    public void SyncToSettings()
    {
        _settings.ApplicationRules = ApplicationRules.ToList();
    }

    public void Dispose()
    {
        _searchTimer.Stop();
        _searchTimer.Tick -= OnSearchTimerTick;
        UnsubscribeRules();
    }

    private bool CanAddManualRule()
    {
        return !string.IsNullOrWhiteSpace(ManualProcessName);
    }

    private void AddManualRule()
    {
        var rule = _applicationRulesService.AddManualRule(_settings, ManualProcessName, ManualProcessName);
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
        var rule = _applicationRulesService.AddApplicationPath(_settings, dialog.FileName, displayName);
        AddRuleToCollection(rule);
        ManualProcessName = processName;
        SaveAndNotifyCurrentApplicationRuleState();
    }

    private void RemoveRule(ApplicationRule? rule)
    {
        if (rule is null)
        {
            return;
        }

        rule.PropertyChanged -= OnRulePropertyChanged;
        _settings.ApplicationRules.Remove(rule);
        ApplicationRules.Remove(rule);
        RefreshFilter(resetPage: true);
        SaveAndNotifyCurrentApplicationRuleState();
    }

    private bool CanDisableCurrentApplication()
    {
        return _currentApplication != ApplicationInfo.Empty && !IsOwnApplication(_currentApplication);
    }

    private void AddRuleToCollection(ApplicationRule rule)
    {
        if (ApplicationRules.Contains(rule))
        {
            return;
        }

        ApplicationRules.Add(rule);
        rule.PropertyChanged += OnRulePropertyChanged;
        RefreshFilter();
        OnPropertyChanged(nameof(ProfileCountText));
    }

    private void UnsubscribeRules()
    {
        foreach (var rule in ApplicationRules)
        {
            rule.PropertyChanged -= OnRulePropertyChanged;
        }
    }

    private void OnRulePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (RulePropertyAffectsSearch(e.PropertyName))
        {
            RefreshFilter();
            OnPropertyChanged(nameof(ProfileCountText));
        }

        SaveAndNotifyCurrentApplicationRuleState();
    }

    private void QueueSearch()
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private void OnSearchTimerTick(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        _appliedSearchQuery = ApplicationSearchQuery;
        RefreshFilter(resetPage: true);
    }

    private void RefreshFilter(bool resetPage = false)
    {
        _filteredMatches.Clear();
        _filteredMatches.AddRange(ApplicationRules.Where(FilterRule));
        ApplyPageIndex(resetPage);
        RefreshPage();
    }

    private void RefreshPage()
    {
        var pageItems = _filteredMatches
            .Skip(ApplicationRulesPageIndex * ListPageSize)
            .Take(ListPageSize)
            .ToList();

        CollectionSync.MatchOrder(FilteredApplicationRules, pageItems);
        OnPropertyChanged(nameof(ProfileCountText));
        OnPropertyChanged(nameof(HasVisibleApplicationRules));
        OnPropertyChanged(nameof(IsApplicationRulesEmpty));
        OnPropertyChanged(nameof(IsApplicationRuleSearchEmpty));
        OnPropertyChanged(nameof(ApplicationRulesPageCount));
        OnPropertyChanged(nameof(HasApplicationRulesPagination));
        OnPropertyChanged(nameof(ApplicationRulesPageText));
    }

    private bool FilterRule(ApplicationRule rule)
    {
        if (string.IsNullOrWhiteSpace(_appliedSearchQuery))
        {
            return true;
        }

        return ContainsSearchText(rule.DisplayName)
               || ContainsSearchText(rule.ProcessName)
               || ContainsSearchText(rule.ExecutablePath);
    }

    private bool ContainsSearchText(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
               && value.Contains(_appliedSearchQuery, StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyPageIndex(bool resetPage)
    {
        var pageIndex = CoercePageIndex(resetPage ? 0 : _pageIndex, ApplicationRulesPageCount);
        if (_pageIndex != pageIndex)
        {
            _pageIndex = pageIndex;
            OnPropertyChanged(nameof(ApplicationRulesPageIndex));
        }
    }

    private ApplicationRule? FindCurrentApplicationRule()
    {
        return FindApplicationRule(_currentApplication);
    }

    private ApplicationRule? FindApplicationRule(ApplicationInfo application)
    {
        return _settings.ApplicationRules.FirstOrDefault(rule => ApplicationRulesService.Matches(rule, application));
    }

    private void NotifyCurrentApplicationState()
    {
        OnPropertyChanged(nameof(CurrentApplicationDisplayNameText));
        OnPropertyChanged(nameof(CurrentApplicationProcessNameText));
        OnPropertyChanged(nameof(CurrentApplicationExecutablePathText));
        OnPropertyChanged(nameof(CurrentApplicationRule));
        OnPropertyChanged(nameof(HasCurrentApplicationRule));
        OnPropertyChanged(nameof(IsCurrentApplicationRuleMissing));
    }

    private void SaveAndNotify(params string[] propertyNames)
    {
        _requestSave();
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
            nameof(IsCurrentApplicationRuleMissing));
        RemoveRuleCommand.NotifyCanExecuteChanged();
    }

    private static bool RulePropertyAffectsSearch(string? propertyName)
    {
        return propertyName is nameof(ApplicationRule.DisplayName)
            or nameof(ApplicationRule.ProcessName)
            or nameof(ApplicationRule.ExecutablePath);
    }

    private static bool IsOwnApplication(ApplicationInfo application)
    {
        return string.Equals(application.ProcessName, CurrentProcessName, StringComparison.OrdinalIgnoreCase);
    }

    private static int CoercePageIndex(int pageIndex, int pageCount)
    {
        return Math.Clamp(pageIndex, 0, Math.Max(0, pageCount - 1));
    }

    private static int GetPageCount(int itemCount)
    {
        return Math.Max(1, (int)Math.Ceiling(itemCount / (double)ListPageSize));
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
}
