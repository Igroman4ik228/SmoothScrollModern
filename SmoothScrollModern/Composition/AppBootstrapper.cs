using System.ComponentModel;
using Microsoft.UI.Xaml;
using SmoothScrollModern.Applications;
using SmoothScrollModern.Core;
using SmoothScrollModern.Input;
using SmoothScrollModern.Scroll;
using SmoothScrollModern.Settings;
using SmoothScrollModern.Startup;
using SmoothScrollModern.Tray;
using SmoothScrollModern.Widgets.Shell.ViewModels;

namespace SmoothScrollModern.Composition;

public sealed class AppBootstrapper : IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly ISmoothScrollEngine _smoothScrollEngine;
    private readonly IMouseHookService _mouseHookService;
    private readonly ITrayService _trayService;
    private bool _isExitRequested;
    private bool _disposed;

    public AppBootstrapper(
        AppSettings settings,
        ISettingsService settingsService,
        ISmoothScrollEngine smoothScrollEngine,
        IMouseHookService mouseHookService,
        ITrayService trayService,
        MainViewModel mainViewModel,
        MainWindow mainWindow)
    {
        Settings = settings;
        _settingsService = settingsService;
        _smoothScrollEngine = smoothScrollEngine;
        _mouseHookService = mouseHookService;
        _trayService = trayService;
        MainViewModel = mainViewModel;
        MainWindow = mainWindow;
        MainWindow.ClosingRequested += HandleMainWindowClosing;
    }

    public AppSettings Settings { get; }

    public MainViewModel MainViewModel { get; }

    public MainWindow MainWindow { get; }

    public void Run()
    {
        _trayService.Initialize();
        _trayService.ShowRequested += ShowMainWindow;
        _trayService.ToggleEnabledRequested += () => MainViewModel.ApplicationSettings.IsEnabled = !MainViewModel.IsEnabled;
        _trayService.DisableForCurrentApplicationRequested += MainViewModel.DisableCurrentApplication;
        _trayService.PauseRequested += () => MainViewModel.PauseFor(TimeSpan.FromMinutes(Constants.TrayPauseMinutes));
        _trayService.ExitRequested += ExitApplication;
        MainViewModel.StateChanged += UpdateTrayState;
        _mouseHookService.MouseWheel += OnMouseWheel;

        _trayService.UpdateState(MainViewModel.IsEnabled, MainViewModel.IsPaused);

        try
        {
            _mouseHookService.Start();
        }
        catch (Exception ex)
        {
            System.Windows.Forms.MessageBox.Show(
                $"Не удалось включить плавную прокрутку. Обычная прокрутка продолжит работать.\n\n{ex.Message}",
                Constants.ApplicationName,
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Warning);
        }

        if (!Settings.Tray.StartMinimizedToTray)
        {
            ShowMainWindow();
        }
    }

    public void HandleMainWindowClosing(CancelEventArgs args)
    {
        if (_isExitRequested)
        {
            return;
        }

        args.Cancel = true;
        if (MainViewModel.Settings.Tray.CloseToTray)
        {
            MainWindow.HideWindow();
            return;
        }

        ExitApplication();
    }

    public void ExitApplication()
    {
        _isExitRequested = true;
        MainViewModel.Save();
        Dispose();
        Application.Current.Exit();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _mouseHookService.MouseWheel -= OnMouseWheel;
        MainWindow.ClosingRequested -= HandleMainWindowClosing;
        _mouseHookService.Dispose();
        _smoothScrollEngine.Dispose();
        MainViewModel.Dispose();
        _trayService.Dispose();
        _disposed = true;
    }

    private bool OnMouseWheel(MouseWheelEvent mouseWheelEvent)
    {
        if (!MainViewModel.TryGetScrollProfile(out var scrollSettings, out var deliveryMode))
        {
            _smoothScrollEngine.Stop();
            return false;
        }

        _smoothScrollEngine.EnqueueWheel(
            mouseWheelEvent.Delta,
            mouseWheelEvent.IsHorizontal,
            scrollSettings,
            deliveryMode);

        return true;
    }

    private void ShowMainWindow()
    {
        MainWindow.ShowWindow();
        MainWindow.Activate();
    }

    private void UpdateTrayState()
    {
        _trayService.UpdateState(MainViewModel.IsEnabled, MainViewModel.IsPaused);
    }
}
