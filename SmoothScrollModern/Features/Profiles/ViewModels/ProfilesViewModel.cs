using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using SmoothScrollModern.Shared.Presentation;
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
    private string _newScrollProfileNameErrorText = string.Empty;
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
        DuplicateScrollProfileCommand = new RelayCommand<ScrollProfile?>(DuplicateScrollProfile, profile => profile is { IsGlobal: false });
        RemoveScrollProfileCommand = new RelayCommand<ScrollProfile?>(RemoveScrollProfile, profile => profile is { IsGlobal: false });

        _searchTimer = dispatcherQueue.CreateTimer();
        _searchTimer.Interval = SearchDebounceInterval;
        _searchTimer.Tick += OnSearchTimerTick;

        RebuildScrollProfileChoices();
        RefreshFilter();
    }

    public ScrollProfile GlobalScrollProfile { get; }

    public ObservableCollection<ScrollProfile> UserScrollProfiles { get; }

    public ObservableCollection<ScrollProfile> FilteredUserScrollProfiles { get; }

    public ObservableCollection<ScrollProfile> ScrollProfileChoices { get; }

    public IRelayCommand AddScrollProfileCommand { get; }

    public IRelayCommand<ScrollProfile?> DuplicateScrollProfileCommand { get; }

    public IRelayCommand<ScrollProfile?> RemoveScrollProfileCommand { get; }

    public double ScrollStrength
    {
        get => _settings.Scroll.DistanceMultiplier;
        set => UpdateGlobalScroll(value, nameof(ScrollStrength));
    }

    public double Friction
    {
        get => _settings.Scroll.Friction;
        set => UpdateGlobalScroll(value, nameof(Friction));
    }

    public double FastScrollBoostPercent
    {
        get => _settings.Scroll.BurstAcceleration * 100.0;
        set => UpdateGlobalScroll(value, nameof(FastScrollBoostPercent));
    }

    public double MaxVelocity
    {
        get => _settings.Scroll.MaxVelocity;
        set => UpdateGlobalScroll(value, nameof(MaxVelocity));
    }

    public double StopVelocityThreshold
    {
        get => _settings.Scroll.StopVelocityThreshold;
        set => UpdateGlobalScroll(value, nameof(StopVelocityThreshold));
    }

    public double PrecisionMultiplier
    {
        get => _settings.Scroll.PrecisionMultiplier;
        set => UpdateGlobalScroll(value, nameof(PrecisionMultiplier));
    }

    public double DirectionControlPercent
    {
        get => (1.0 - _settings.Scroll.DirectionChangeDamping) * 100.0;
        set => UpdateGlobalScroll(value, nameof(DirectionControlPercent));
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
                UpdateNewScrollProfileNameError();
                AddScrollProfileCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string NewScrollProfileNameErrorText
    {
        get => _newScrollProfileNameErrorText;
        private set
        {
            if (SetProperty(ref _newScrollProfileNameErrorText, value))
            {
                OnPropertyChanged(nameof(HasNewScrollProfileNameError));
            }
        }
    }

    public bool HasNewScrollProfileNameError => !string.IsNullOrWhiteSpace(NewScrollProfileNameErrorText);

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
        UpdateNewScrollProfileNameError();
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
            nameof(ScrollStrength) => scroll.DistanceMultiplier,
            nameof(Friction) => scroll.Friction,
            nameof(FastScrollBoostPercent) => scroll.BurstAcceleration * 100.0,
            nameof(MaxVelocity) => scroll.MaxVelocity,
            nameof(StopVelocityThreshold) => scroll.StopVelocityThreshold,
            nameof(PrecisionMultiplier) => scroll.PrecisionMultiplier,
            nameof(DirectionControlPercent) => (1.0 - scroll.DirectionChangeDamping) * 100.0,
            _ => value
        };

        if (Math.Abs(previous - value) < 0.0005)
        {
            return;
        }

        switch (propertyName)
        {
            case nameof(ScrollStrength):
                scroll.DistanceMultiplier = value;
                break;
            case nameof(Friction):
                scroll.Friction = value;
                break;
            case nameof(FastScrollBoostPercent):
                scroll.BurstAcceleration = value / 100.0;
                break;
            case nameof(MaxVelocity):
                scroll.MaxVelocity = value;
                break;
            case nameof(StopVelocityThreshold):
                scroll.StopVelocityThreshold = value;
                break;
            case nameof(PrecisionMultiplier):
                scroll.PrecisionMultiplier = value;
                break;
            case nameof(DirectionControlPercent):
                scroll.DirectionChangeDamping = 1.0 - (value / 100.0);
                break;
        }

        scroll.Validate();
        SaveAndNotify(propertyName);
    }

    private bool CanAddScrollProfile()
    {
        return !string.IsNullOrWhiteSpace(NewScrollProfileName)
               && !HasDuplicateProfileName(NewScrollProfileName);
    }

    private void AddScrollProfile()
    {
        UpdateNewScrollProfileNameError();
        if (HasNewScrollProfileNameError)
        {
            return;
        }

        var profile = new ScrollProfile
        {
            Name = NewScrollProfileName,
            Scroll = new ScrollSettings
            {
                DistanceMultiplier = _settings.Scroll.DistanceMultiplier,
                Friction = _settings.Scroll.Friction,
                BurstAcceleration = _settings.Scroll.BurstAcceleration,
                DirectionChangeDamping = _settings.Scroll.DirectionChangeDamping,
                MaxVelocity = _settings.Scroll.MaxVelocity,
                StopVelocityThreshold = _settings.Scroll.StopVelocityThreshold,
                PrecisionMultiplier = _settings.Scroll.PrecisionMultiplier,
                EnableHorizontalScroll = _settings.Scroll.EnableHorizontalScroll,
                BypassSmoothingVirtualKeys = _settings.Scroll.BypassSmoothingVirtualKeys.ToList()
            }
        };

        profile.Validate();
        AddProfileToCollection(profile);
        NewScrollProfileName = string.Empty;
        RebuildScrollProfileChoices();
        RefreshFilter(resetPage: true);
        SaveAndNotify(nameof(ScrollProfilesCountText));
    }

    private void DuplicateScrollProfile(ScrollProfile? sourceProfile)
    {
        if (sourceProfile is null)
        {
            return;
        }

        var profile = new ScrollProfile
        {
            Name = CreateUniqueCopyName(sourceProfile.Name),
            Scroll = CloneScrollSettings(sourceProfile.Scroll)
        };

        profile.Validate();
        AddProfileToCollection(profile);
        RebuildScrollProfileChoices();
        RefreshFilter(resetPage: true);
        RefreshNewScrollProfileNameState();
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

    private static ScrollSettings CloneScrollSettings(ScrollSettings source)
    {
        return new ScrollSettings
        {
            DistanceMultiplier = source.DistanceMultiplier,
            Friction = source.Friction,
            BurstAcceleration = source.BurstAcceleration,
            DirectionChangeDamping = source.DirectionChangeDamping,
            MaxVelocity = source.MaxVelocity,
            StopVelocityThreshold = source.StopVelocityThreshold,
            PrecisionMultiplier = source.PrecisionMultiplier,
            EnableHorizontalScroll = source.EnableHorizontalScroll,
            BypassSmoothingVirtualKeys = source.BypassSmoothingVirtualKeys.ToList()
        };
    }

    private void AddProfileToCollection(ScrollProfile profile)
    {
        profile.PropertyChanged += OnScrollProfilePropertyChanged;
        UserScrollProfiles.Add(profile);
        RefreshNewScrollProfileNameState();
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
            nameof(ScrollStrength),
            nameof(Friction),
            nameof(FastScrollBoostPercent),
            nameof(MaxVelocity),
            nameof(StopVelocityThreshold),
            nameof(PrecisionMultiplier),
            nameof(DirectionControlPercent),
            nameof(EnableHorizontalScroll));
    }

    private void OnScrollProfilePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ScrollProfile.Name) or nameof(ScrollProfile.Id))
        {
            RebuildScrollProfileChoices();
            RefreshFilter();
            RefreshNewScrollProfileNameState();
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

    private void UpdateNewScrollProfileNameError()
    {
        NewScrollProfileNameErrorText = HasDuplicateProfileName(NewScrollProfileName)
            ? "Профиль с таким названием уже есть. Выберите другое название."
            : string.Empty;
    }

    private void RefreshNewScrollProfileNameState()
    {
        UpdateNewScrollProfileNameError();
        AddScrollProfileCommand.NotifyCanExecuteChanged();
    }

    private bool HasDuplicateProfileName(string profileName)
    {
        var normalizedName = NormalizeProfileName(profileName);
        return !string.IsNullOrWhiteSpace(normalizedName)
               && UserScrollProfiles.Any(profile =>
                   string.Equals(NormalizeProfileName(profile.Name), normalizedName, StringComparison.OrdinalIgnoreCase));
    }

    private string CreateUniqueCopyName(string sourceName)
    {
        var baseName = $"{NormalizeProfileName(sourceName)} копия".Trim();
        var candidate = baseName;
        var index = 2;

        while (HasDuplicateProfileName(candidate))
        {
            candidate = $"{baseName} {index}";
            index++;
        }

        return candidate;
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

    private static string NormalizeProfileName(string profileName)
    {
        return profileName.Trim();
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
