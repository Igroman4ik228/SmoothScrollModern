using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmoothScrollModern.Input;
using SmoothScrollModern.Settings;
using Windows.System;

namespace SmoothScrollModern.Features.Profiles.Controls;

public sealed partial class ProfileBypassKeysEditor : UserControl
{
    private IGlobalInputHookService? _inputHookService;
    private bool _isCapturingKey;

    public ProfileBypassKeysEditor()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
    }

    private void OnCaptureKeyButtonClick(object sender, RoutedEventArgs e)
    {
        if (!TryBeginCapturingKey())
        {
            return;
        }

        _isCapturingKey = true;
        CaptureKeyButton.Visibility = Visibility.Collapsed;
        CaptureKeyWaitingButton.Visibility = Visibility.Visible;
        CaptureKeyWaitingButton.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
    }

    private bool TryBeginCapturingKey()
    {
        _inputHookService ??= (App.Current as App)?.Bootstrapper?.InputHookService;
        if (_inputHookService is null)
        {
            return false;
        }

        if (!_inputHookService.IsRunning)
        {
            _inputHookService.Start();
        }

        _inputHookService.KeyDown -= OnGlobalKeyDown;
        _inputHookService.KeyDown += OnGlobalKeyDown;
        return true;
    }

    private void OnGlobalKeyDown(VirtualKey virtualKey)
    {
        DispatcherQueue.TryEnqueue(() => CaptureKey(virtualKey));
    }

    private void CaptureKey(VirtualKey virtualKey)
    {
        if (!_isCapturingKey)
        {
            return;
        }

        if (virtualKey == VirtualKey.Escape)
        {
            StopCapturingKey();
            return;
        }

        if (DataContext is ScrollProfile profile)
        {
            profile.AddProfileBypassSmoothingKey(virtualKey);
        }

        StopCapturingKey();
    }

    private void OnRemoveKeyClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ScrollProfile profile
            || sender is not FrameworkElement { DataContext: ShortcutKeyDisplay key })
        {
            return;
        }

        profile.RemoveProfileBypassSmoothingKey(key.VirtualKey);
    }

    private void StopCapturingKey()
    {
        _isCapturingKey = false;
        if (_inputHookService is not null)
        {
            _inputHookService.KeyDown -= OnGlobalKeyDown;
        }

        CaptureKeyWaitingButton.Visibility = Visibility.Collapsed;
        CaptureKeyButton.Visibility = Visibility.Visible;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopCapturingKey();
        Unloaded -= OnUnloaded;
    }
}
