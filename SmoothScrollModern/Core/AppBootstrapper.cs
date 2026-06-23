using System.ComponentModel;
using Microsoft.UI.Xaml;
using SmoothScrollModern.Applications;
using SmoothScrollModern.Input;
using SmoothScrollModern.Scroll;
using SmoothScrollModern.Settings;
using SmoothScrollModern.Startup;
using SmoothScrollModern.Tray;
using SmoothScrollModern.ViewModels;

namespace SmoothScrollModern.Core;

public sealed class AppBootstrapper : IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly IInputInjectionService _inputInjectionService;
    private readonly ISmoothScrollEngine _smoothScrollEngine;
    private readonly IMouseHookService _mouseHookService;
    private readonly ITrayService _trayService;
    private bool _isExitRequested;
    private bool _disposed;

    public AppBootstrapper()
    {
        _settingsService = new JsonSettingsService();
        Settings = _settingsService.Load();

        var activeWindowService = new ActiveWindowService();
        var applicationRulesService = new ApplicationRulesService();
        var startupService = new WindowsStartupService();

        _inputInjectionService = new InputInjectionService();
        _smoothScrollEngine = new SmoothScrollEngine(_inputInjectionService);
        _mouseHookService = new MouseHookService();
        _trayService = new TrayService();

        MainViewModel = new MainViewModel(
            Settings,
            _settingsService,
            activeWindowService,
            applicationRulesService,
            startupService);

        MainWindow = new MainWindow(MainViewModel, HandleMainWindowClosing);
    }

    public AppSettings Settings { get; }

    public MainViewModel MainViewModel { get; }

    public MainWindow MainWindow { get; }

    public void Run()
    {
        _trayService.Initialize();
        _trayService.ShowRequested += ShowMainWindow;
        _trayService.ToggleEnabledRequested += () => MainViewModel.IsEnabled = !MainViewModel.IsEnabled;
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
        if (Settings.Tray.CloseToTray)
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
