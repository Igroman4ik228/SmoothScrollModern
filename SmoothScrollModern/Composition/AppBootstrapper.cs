using Microsoft.UI.Xaml;
using SmoothScrollModern.Core;
using SmoothScrollModern.Input;
using SmoothScrollModern.Scroll;
using SmoothScrollModern.Settings;
using SmoothScrollModern.Tray;
using SmoothScrollModern.Widgets.Shell.ViewModels;
using System.ComponentModel;
using Windows.System;

namespace SmoothScrollModern.Composition;

public sealed class AppBootstrapper : IDisposable
{
    private readonly ISmoothScrollEngine _smoothScrollEngine;
    private readonly IScrollDecisionService _scrollDecisionService;
    private readonly IGlobalInputHookService _inputHookService;
    private readonly ITrayService _trayService;
    private bool _inputHookWarningShown;
    private bool _isExitRequested;
    private bool _disposed;

    public AppBootstrapper(
        AppSettings settings,
        ISmoothScrollEngine smoothScrollEngine,
        IScrollDecisionService scrollDecisionService,
        IGlobalInputHookService inputHookService,
        ITrayService trayService,
        MainViewModel mainViewModel,
        MainWindow mainWindow)
    {
        Settings = settings;
        _smoothScrollEngine = smoothScrollEngine;
        _scrollDecisionService = scrollDecisionService;
        _inputHookService = inputHookService;
        _trayService = trayService;
        MainViewModel = mainViewModel;
        MainWindow = mainWindow;
        MainWindow.ClosingRequested += HandleMainWindowClosing;
    }

    public AppSettings Settings { get; }

    public MainViewModel MainViewModel { get; }

    public MainWindow MainWindow { get; }

    public IGlobalInputHookService InputHookService => _inputHookService;

    public void Run()
    {
        _trayService.Initialize();
        _trayService.ShowRequested += ShowMainWindow;
        _trayService.ToggleEnabledRequested += () => MainViewModel.ApplicationSettings.IsEnabled = !MainViewModel.IsEnabled;
        _trayService.DisableForCurrentApplicationRequested += MainViewModel.DisableCurrentApplication;
        _trayService.PauseRequested += () => MainViewModel.PauseFor(TimeSpan.FromMinutes(Constants.TrayPauseMinutes));
        _trayService.ExitRequested += ExitApplication;
        MainViewModel.StateChanged += UpdateTrayState;
        _inputHookService.MouseWheel += OnMouseWheel;
        _inputHookService.KeyDown += OnKeyDown;

        _trayService.UpdateState(MainViewModel.IsEnabled, MainViewModel.IsPaused);

        EnsureInputHookStarted();

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

        _inputHookService.MouseWheel -= OnMouseWheel;
        _inputHookService.KeyDown -= OnKeyDown;
        MainWindow.ClosingRequested -= HandleMainWindowClosing;
        _inputHookService.Dispose();
        _smoothScrollEngine.Dispose();
        MainViewModel.Dispose();
        _trayService.Dispose();
        _disposed = true;
    }

    private bool OnMouseWheel(MouseWheelEvent mouseWheelEvent)
    {
        var decision = _scrollDecisionService.Decide(mouseWheelEvent.TargetWindowHandle);
        if (!decision.ShouldSmooth)
        {
            _smoothScrollEngine.Stop();
            return false;
        }

        var settings = decision.Settings!;
        if (ShouldBypassSmoothScrollForShortcutWheel(settings))
        {
            _smoothScrollEngine.Stop();
            return false;
        }

        _smoothScrollEngine.EnqueueWheel(
            mouseWheelEvent.Delta,
            mouseWheelEvent.IsHorizontal,
            settings,
            decision.DeliveryMode,
            mouseWheelEvent.TargetWindowHandle);

        return true;
    }

    private bool ShouldBypassSmoothScrollForShortcutWheel(ScrollSettingsSnapshot settings)
    {
        return _inputHookService.IsAnyShortcutKeyDown(settings.BypassSmoothingVirtualKeys);
    }

    private void EnsureInputHookStarted()
    {
        if (!_inputHookService.IsRunning)
        {
            try
            {
                _inputHookService.Start();
            }
            catch (Exception ex)
            {
                ShowInputHookWarning(ex);
                return;
            }
        }
    }

    private void ShowInputHookWarning(Exception ex)
    {
        if (_inputHookWarningShown)
        {
            return;
        }

        _inputHookWarningShown = true;
        System.Windows.Forms.MessageBox.Show(
            $"Не удалось включить перехват колеса и клавиш. Обычная прокрутка продолжит работать.\n\n{ex.Message}",
            Constants.ApplicationName,
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Warning);
    }

    private void OnKeyDown(VirtualKey virtualKey)
    {
        _smoothScrollEngine.StopIfBypassKeyDown(virtualKey);
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
