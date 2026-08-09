using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using SmoothScrollModern.Applications;
using SmoothScrollModern.Features.Applications.ViewModels;
using SmoothScrollModern.Features.Profiles.ViewModels;
using SmoothScrollModern.Features.Settings.ViewModels;
using SmoothScrollModern.Scroll;
using SmoothScrollModern.Settings;
using SmoothScrollModern.Startup;

namespace SmoothScrollModern.Widgets.Shell.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly IScrollConfigurationPublisher _configurationPublisher;
    private readonly DispatcherQueueTimer _activeApplicationTimer;
    private readonly DispatcherQueueTimer _saveTimer;
    private bool _disposed;

    public MainViewModel(
        AppSettings settings,
        ISettingsService settingsService,
        IActiveWindowService activeWindowService,
        IApplicationRulesService applicationRulesService,
        IStartupService startupService,
        IScrollConfigurationPublisher configurationPublisher)
    {
        Settings = settings;
        _settingsService = settingsService;
        _configurationPublisher = configurationPublisher;

        var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        Profiles = new ProfilesViewModel(Settings, dispatcherQueue, RequestConfigurationSave);
        ApplicationSettings = new ApplicationSettingsViewModel(
            settingsService,
            startupService,
            () => Settings,
            ReplaceSettings,
            RequestConfigurationSave,
            NotifyStateChanged,
            ApplyTheme,
            PublishRuntimeConfiguration);
        Applications = new ApplicationRulesViewModel(
            Settings,
            activeWindowService,
            applicationRulesService,
            Profiles,
            dispatcherQueue,
            RequestConfigurationSave,
            NotifyStateChanged);

        _activeApplicationTimer = dispatcherQueue.CreateTimer();
        _activeApplicationTimer.Interval = TimeSpan.FromMilliseconds(Core.Constants.ActiveApplicationRefreshMs);
        _activeApplicationTimer.Tick += OnActiveApplicationTimerTick;
        _activeApplicationTimer.Start();

        _saveTimer = dispatcherQueue.CreateTimer();
        _saveTimer.Interval = TimeSpan.FromMilliseconds(350);
        _saveTimer.Tick += OnSaveTimerTick;

        ApplicationSettings.LoadSettings();
        PublishRuntimeConfiguration();
    }

    public event Action? StateChanged;

    public event Action<string>? ThemeChanged;

    public AppSettings Settings { get; private set; }

    public ApplicationSettingsViewModel ApplicationSettings { get; }

    public ProfilesViewModel Profiles { get; }

    public ApplicationRulesViewModel Applications { get; }

    public bool IsEnabled => ApplicationSettings.IsEnabled;

    public bool IsPaused => ApplicationSettings.IsPaused;

    public string Theme => ApplicationSettings.Theme;

    public void PauseFor(TimeSpan duration)
    {
        ApplicationSettings.PauseFor(duration);
    }

    public void DisableCurrentApplication()
    {
        Applications.DisableCurrentApplication();
    }

    public void Save()
    {
        _saveTimer.Stop();
        Applications.SyncToSettings();
        Profiles.SyncToSettings();
        _settingsService.Save(Settings);
        NotifyStateChanged();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _activeApplicationTimer.Stop();
        _activeApplicationTimer.Tick -= OnActiveApplicationTimerTick;
        _saveTimer.Stop();
        _saveTimer.Tick -= OnSaveTimerTick;
        Applications.Dispose();
        Profiles.Dispose();
        _disposed = true;
    }

    private void ReplaceSettings(AppSettings settings)
    {
        Settings = settings;
        Profiles.LoadSettings(settings);
        Applications.LoadSettings(settings);
        ApplicationSettings.LoadSettings();
        PublishRuntimeConfiguration();
        OnPropertyChanged(nameof(Settings));
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(Theme));
        NotifyStateChanged();
    }

    private void OnActiveApplicationTimerTick(DispatcherQueueTimer sender, object args)
    {
        ApplicationSettings.RefreshPauseState();
        _ = Applications.RefreshCurrentApplicationAsync();
        OnPropertyChanged(nameof(IsPaused));
    }

    private void OnSaveTimerTick(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        Save();
    }

    private void QueueSave()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
        NotifyStateChanged();
    }

    private void RequestConfigurationSave()
    {
        PublishRuntimeConfiguration();
        QueueSave();
    }

    private void PublishRuntimeConfiguration()
    {
        Applications.SyncToSettings();
        Profiles.SyncToSettings();
        _configurationPublisher.Publish(Settings, ApplicationSettings.PausedUntilUtc);
    }

    private void ApplyTheme(string theme)
    {
        ThemeChanged?.Invoke(theme);
        OnPropertyChanged(nameof(Theme));
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(IsPaused));
        StateChanged?.Invoke();
    }
}
