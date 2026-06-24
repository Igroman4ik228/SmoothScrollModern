using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using SmoothScrollModern.Core.Presentation;
using SmoothScrollModern.Scroll;
using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Features.Profiles.ViewModels;

public sealed class ProfilesViewModel : ObservableObject
{
    private const int ListPageSize = 8;
    private static readonly TimeSpan SearchDebounceInterval = TimeSpan.FromMilliseconds(300);
    private readonly DispatcherQueueTimer _searchTimer;
    private readonly Action _requestSave;
    private readonly List<ScrollProfile> _filteredMatches = [];
    private AppSettings _settings;
    private string _searchQuery = string.Empty;
    private string _appliedSearchQuery = string.Empty;
    private string _newProfileName = string.Empty;
    private int _pageIndex;

    public ProfilesViewModel(AppSettings settings, DispatcherQueue dispatcherQueue, Action requestSave)
    {
        _settings = settings;
        _requestSave = requestSave;
        GlobalScrollProfile = CreateGlobalProfile(settings);
        GlobalScrollProfile.PropertyChanged += OnGlobalScrollProfilePropertyChanged;
        UserScrollProfiles = new ObservableCollection<ScrollProfile>(settings.ScrollProfiles);
        FilteredUserScrollProfiles = [];
        ScrollProfileChoices = [];

        foreach (var profile in UserScrollProfiles)
        {
            profile.PropertyChanged += OnScrollProfilePropertyChanged;
        }

        AddScrollProfileCommand = new RelayCommand(AddScrollProfile, CanAddScrollProfile);
        RemoveScrollProfileCommand = new RelayCommand<ScrollProfile?>(RemoveScrollProfile, profile => profile is { IsGlobal: false });

        _searchTimer = dispatcherQueue.CreateTimer();
        _searchTimer.Interval = SearchDebounceInterval;
        _searchTimer.Tick += OnSearchTimerTick;

        RebuildScrollProfileChoices();
        RefreshFilter();
    }

    public IReadOnlyList<SelectionOption<EasingType>> EasingOptions { get; } =
    [
        new(EasingType.Linear, "Ровно"),
        new(EasingType.EaseOutCubic, "Мягко"),
        new(EasingType.EaseOutQuart, "Очень мягко"),
        new(EasingType.EaseOutQuint, "Максимально мягко")
    ];

    public ScrollProfile GlobalScrollProfile { get; }

    public ObservableCollection<ScrollProfile> UserScrollProfiles { get; }

    public ObservableCollection<ScrollProfile> FilteredUserScrollProfiles { get; }

    public ObservableCollection<ScrollProfile> ScrollProfileChoices { get; }

    public IRelayCommand AddScrollProfileCommand { get; }

    public IRelayCommand<ScrollProfile?> RemoveScrollProfileCommand { get; }

    public double ScrollMultiplier
    {
        get => _settings.Scroll.ScrollMultiplier;
        set => UpdateGlobalScroll(value, nameof(ScrollMultiplier));
    }

    public int DurationMs
    {
        get => _settings.Scroll.DurationMs;
        set => UpdateGlobalScroll(value, nameof(DurationMs));
    }

    public double Smoothness
    {
        get => _settings.Scroll.Smoothness;
        set => UpdateGlobalScroll(value, nameof(Smoothness));
    }

    public double Acceleration
    {
        get => _settings.Scroll.Acceleration;
        set => UpdateGlobalScroll(value, nameof(Acceleration));
    }

    public EasingType EasingType
    {
        get => _settings.Scroll.EasingType;
        set
        {
            if (_settings.Scroll.EasingType == value)
            {
                return;
            }

            _settings.Scroll.EasingType = value;
            SaveAndNotify(nameof(EasingType));
        }
    }

    public bool EnableHorizontalScroll
    {
        get => _settings.Scroll.EnableHorizontalScroll;
        set
        {
            if (_settings.Scroll.EnableHorizontalScroll == value)
            {
                return;
            }

            _settings.Scroll.EnableHorizontalScroll = value;
            SaveAndNotify(nameof(EnableHorizontalScroll));
        }
    }

    public string ScrollProfileSearchQuery
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

    public string NewScrollProfileName
    {
        get => _newProfileName;
        set
        {
            if (SetProperty(ref _newProfileName, value))
            {
                AddScrollProfileCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string ScrollProfilesCountText => BuildListCountText(
        _filteredMatches.Count,
        UserScrollProfiles.Count,
        ScrollProfilesPageIndex,
        !string.IsNullOrWhiteSpace(_appliedSearchQuery));

    public bool HasVisibleUserScrollProfiles => FilteredUserScrollProfiles.Count > 0;

    public bool IsUserScrollProfilesEmpty => UserScrollProfiles.Count == 0;

    public bool IsScrollProfileSearchEmpty => UserScrollProfiles.Count > 0
        && !string.IsNullOrWhiteSpace(_appliedSearchQuery)
        && _filteredMatches.Count == 0;

    public int ScrollProfilesPageIndex
    {
        get => _pageIndex;
        set
        {
            var pageIndex = CoercePageIndex(value, ScrollProfilesPageCount);
            if (SetProperty(ref _pageIndex, pageIndex))
            {
                RefreshPage();
            }
        }
    }

    public int ScrollProfilesPageCount => GetPageCount(_filteredMatches.Count);

    public bool HasScrollProfilesPagination => _filteredMatches.Count > ListPageSize;

    public string ScrollProfilesPageText => $"Страница {ScrollProfilesPageIndex + 1} из {ScrollProfilesPageCount}";

    public void LoadSettings(AppSettings settings)
    {
        UnsubscribeProfiles();
        _settings = settings;
        UserScrollProfiles.Clear();
        GlobalScrollProfile.Scroll = settings.Scroll;

        foreach (var profile in settings.ScrollProfiles)
        {
            AddProfileToCollection(profile);
        }

        RebuildScrollProfileChoices();
        RefreshFilter(resetPage: true);
        OnPropertyChanged(string.Empty);
    }

    public ScrollSettings GetScrollSettings(string scrollProfileId)
    {
        if (string.IsNullOrWhiteSpace(scrollProfileId))
        {
            return _settings.Scroll;
        }

        return UserScrollProfiles.FirstOrDefault(profile =>
                string.Equals(profile.Id, scrollProfileId, StringComparison.OrdinalIgnoreCase))
            ?.Scroll ?? _settings.Scroll;
    }

    public void NormalizeApplicationRuleProfileReferences(IEnumerable<ApplicationRule> rules)
    {
        foreach (var rule in rules)
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

    public void SyncToSettings()
    {
        _settings.ScrollProfiles = UserScrollProfiles.ToList();
    }

    public void Dispose()
    {
        _searchTimer.Stop();
        _searchTimer.Tick -= OnSearchTimerTick;
        GlobalScrollProfile.PropertyChanged -= OnGlobalScrollProfilePropertyChanged;
        UnsubscribeProfiles();
    }

    private static ScrollProfile CreateGlobalProfile(AppSettings settings)
    {
        return new ScrollProfile
        {
            Id = string.Empty,
            Name = "Основной профиль",
            Scroll = settings.Scroll,
            IsGlobal = true
        };
    }

    private void UpdateGlobalScroll(double value, string propertyName)
    {
        var scroll = _settings.Scroll;
        var previous = propertyName switch
        {
            nameof(ScrollMultiplier) => scroll.ScrollMultiplier,
            nameof(Smoothness) => scroll.Smoothness,
            nameof(Acceleration) => scroll.Acceleration,
            _ => value
        };

        if (Math.Abs(previous - value) < 0.0005)
        {
            return;
        }

        switch (propertyName)
        {
            case nameof(ScrollMultiplier):
                scroll.ScrollMultiplier = value;
                break;
            case nameof(Smoothness):
                scroll.Smoothness = value;
                break;
            case nameof(Acceleration):
                scroll.Acceleration = value;
                break;
        }

        scroll.Validate();
        SaveAndNotify(propertyName);
    }

    private void UpdateGlobalScroll(int value, string propertyName)
    {
        if (_settings.Scroll.DurationMs == value)
        {
            return;
        }

        _settings.Scroll.DurationMs = value;
        _settings.Scroll.Validate();
        SaveAndNotify(propertyName);
    }

    private bool CanAddScrollProfile()
    {
        return !string.IsNullOrWhiteSpace(NewScrollProfileName);
    }

    private void AddScrollProfile()
    {
        var profile = new ScrollProfile
        {
            Name = NewScrollProfileName,
            Scroll = new ScrollSettings
            {
                ScrollMultiplier = _settings.Scroll.ScrollMultiplier,
                DurationMs = _settings.Scroll.DurationMs,
                Smoothness = _settings.Scroll.Smoothness,
                Acceleration = _settings.Scroll.Acceleration,
                EasingType = _settings.Scroll.EasingType,
                EnableHorizontalScroll = _settings.Scroll.EnableHorizontalScroll
            }
        };

        profile.Validate();
        AddProfileToCollection(profile);
        NewScrollProfileName = string.Empty;
        RebuildScrollProfileChoices();
        RefreshFilter(resetPage: true);
        SaveAndNotify(nameof(ScrollProfilesCountText));
    }

    private void RemoveScrollProfile(ScrollProfile? profile)
    {
        if (profile is null)
        {
            return;
        }

        profile.PropertyChanged -= OnScrollProfilePropertyChanged;
        UserScrollProfiles.Remove(profile);
        foreach (var rule in _settings.ApplicationRules.Where(rule =>
                     string.Equals(rule.ScrollProfileId, profile.Id, StringComparison.OrdinalIgnoreCase)))
        {
            rule.ScrollProfileId = string.Empty;
        }

        RebuildScrollProfileChoices();
        RefreshFilter(resetPage: true);
        SaveAndNotify(nameof(ScrollProfilesCountText));
    }

    private void AddProfileToCollection(ScrollProfile profile)
    {
        profile.PropertyChanged += OnScrollProfilePropertyChanged;
        UserScrollProfiles.Add(profile);
    }

    private void UnsubscribeProfiles()
    {
        foreach (var profile in UserScrollProfiles)
        {
            profile.PropertyChanged -= OnScrollProfilePropertyChanged;
        }
    }

    private void OnGlobalScrollProfilePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _settings.Scroll = GlobalScrollProfile.Scroll;
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
            RefreshFilter();
        }

        SaveAndNotify(nameof(ScrollProfilesCountText));
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

    private void QueueSearch()
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private void OnSearchTimerTick(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        _appliedSearchQuery = ScrollProfileSearchQuery;
        RefreshFilter(resetPage: true);
    }

    private void RefreshFilter(bool resetPage = false)
    {
        _filteredMatches.Clear();
        _filteredMatches.AddRange(UserScrollProfiles.Where(FilterProfile));
        ApplyPageIndex(resetPage);
        RefreshPage();
    }

    private void RefreshPage()
    {
        var pageItems = _filteredMatches
            .Skip(ScrollProfilesPageIndex * ListPageSize)
            .Take(ListPageSize)
            .ToList();

        CollectionSync.MatchOrder(FilteredUserScrollProfiles, pageItems);
        OnPropertyChanged(nameof(ScrollProfilesCountText));
        OnPropertyChanged(nameof(HasVisibleUserScrollProfiles));
        OnPropertyChanged(nameof(IsUserScrollProfilesEmpty));
        OnPropertyChanged(nameof(IsScrollProfileSearchEmpty));
        OnPropertyChanged(nameof(ScrollProfilesPageCount));
        OnPropertyChanged(nameof(HasScrollProfilesPagination));
        OnPropertyChanged(nameof(ScrollProfilesPageText));
    }

    private bool FilterProfile(ScrollProfile profile)
    {
        return string.IsNullOrWhiteSpace(_appliedSearchQuery)
               || profile.Name.Contains(_appliedSearchQuery, StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyPageIndex(bool resetPage)
    {
        var pageIndex = CoercePageIndex(resetPage ? 0 : _pageIndex, ScrollProfilesPageCount);
        if (_pageIndex != pageIndex)
        {
            _pageIndex = pageIndex;
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

    private void SaveAndNotify(params string[] propertyNames)
    {
        _requestSave();
        foreach (var propertyName in propertyNames)
        {
            OnPropertyChanged(propertyName);
        }
    }
}
