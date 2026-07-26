using SharpHook;
using SharpHook.Data;
using SharpHook.Providers;
using SmoothScrollModern.Native;
using SmoothScrollModern.Settings;
using Windows.System;

namespace SmoothScrollModern.Input;

public sealed class SharpHookInputService : IGlobalInputHookService
{
    private readonly object _gate = new();
    private readonly HashSet<VirtualKey> _pressedKeys = [];
    private IGlobalHook? _hook;
    private Task? _hookTask;
    private bool _disposed;

    public event Func<MouseWheelEvent, bool>? MouseWheel;

    public event Action<VirtualKey>? KeyDown;

    public bool IsRunning => _hook is not null;

    public bool IsAnyShortcutKeyDown(IEnumerable<VirtualKey> virtualKeys)
    {
        lock (_gate)
        {
            return virtualKeys.Any(configuredKey =>
                _pressedKeys.Any(pressedKey => ShortcutKeys.Matches(configuredKey, pressedKey)));
        }
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_hook is not null)
        {
            return;
        }

        UioHookProvider.Instance.KeyTypedEnabled = false;

        var hook = new SimpleGlobalHook(GlobalHookType.All);

        hook.KeyPressed += OnKeyPressed;
        hook.KeyReleased += OnKeyReleased;
        hook.MouseWheel += OnMouseWheel;

        lock (_gate)
        {
            _pressedKeys.Clear();
        }

        _hook = hook;
        _hookTask = hook.RunAsync();
    }

    public void Stop()
    {
        if (_hook is null)
        {
            return;
        }

        var hook = _hook;
        _hook = null;

        hook.KeyPressed -= OnKeyPressed;
        hook.KeyReleased -= OnKeyReleased;
        hook.MouseWheel -= OnMouseWheel;
        hook.Stop();
        hook.Dispose();

        lock (_gate)
        {
            _pressedKeys.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _hookTask = null;
        _disposed = true;
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs args)
    {
        if (args.IsEventSimulated || !TryMapKeyCode(args.Data.KeyCode, out var virtualKey))
        {
            return;
        }

        lock (_gate)
        {
            _pressedKeys.Add(virtualKey);
        }

        KeyDown?.Invoke(virtualKey);
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs args)
    {
        if (args.IsEventSimulated || !TryMapKeyCode(args.Data.KeyCode, out var virtualKey))
        {
            return;
        }

        lock (_gate)
        {
            _pressedKeys.Remove(virtualKey);
        }
    }

    private void OnMouseWheel(object? sender, MouseWheelHookEventArgs args)
    {
        if (args.IsEventSimulated)
        {
            return;
        }

        UpdatePressedModifierKeys(args.RawEvent.Mask);

        var wheelData = args.Data;
        var delta = NormalizeWheelDelta(wheelData.Rotation);
        if (delta == 0)
        {
            return;
        }

        var handled = MouseWheel?.Invoke(new MouseWheelEvent(
            delta,
            wheelData.Direction == MouseWheelScrollDirection.Horizontal,
            unchecked((uint)args.RawEvent.Time),
            GetRootWindow(NativeMethods.WindowFromPoint(new POINT(wheelData.X, wheelData.Y))),
            wheelData.X,
            wheelData.Y)) ?? false;

        args.SuppressEvent = handled;
    }

    private void UpdatePressedModifierKeys(EventMask eventMask)
    {
        lock (_gate)
        {
            SetPressedKey(ShortcutKeys.LeftShift, eventMask.HasFlag(EventMask.LeftShift));
            SetPressedKey(ShortcutKeys.RightShift, eventMask.HasFlag(EventMask.RightShift));
            SetPressedKey(ShortcutKeys.LeftControl, eventMask.HasFlag(EventMask.LeftCtrl));
            SetPressedKey(ShortcutKeys.RightControl, eventMask.HasFlag(EventMask.RightCtrl));
            SetPressedKey(ShortcutKeys.LeftAlt, eventMask.HasFlag(EventMask.LeftAlt));
            SetPressedKey(ShortcutKeys.RightAlt, eventMask.HasFlag(EventMask.RightAlt));
        }
    }

    private void SetPressedKey(VirtualKey virtualKey, bool isPressed)
    {
        if (isPressed)
        {
            _pressedKeys.Add(virtualKey);
            return;
        }

        _pressedKeys.Remove(virtualKey);
    }

    private static int NormalizeWheelDelta(short rotation)
    {
        return rotation;
    }

    private static IntPtr GetRootWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var root = NativeMethods.GetAncestor(windowHandle, NativeConstants.GA_ROOT);
        return root == IntPtr.Zero ? windowHandle : root;
    }

    private static bool TryMapKeyCode(KeyCode keyCode, out VirtualKey virtualKey)
    {
        virtualKey = keyCode switch
        {
            KeyCode.VcEnter or KeyCode.VcNumPadEnter => VirtualKey.Enter,
            KeyCode.VcNumPadClear => VirtualKey.Clear,
            KeyCode.VcDelete => VirtualKey.Delete,
            KeyCode.VcInsert => VirtualKey.Insert,
            KeyCode.VcPrintScreen => VirtualKey.Snapshot,
            KeyCode.VcContextMenu => VirtualKey.Application,
            KeyCode.VcCancel => VirtualKey.Cancel,
            KeyCode.VcHelp => VirtualKey.Help,
            KeyCode.VcComma => ShortcutKeys.OemComma,
            KeyCode.VcPeriod => ShortcutKeys.OemPeriod,
            KeyCode.VcSlash => ShortcutKeys.OemSlash,
            KeyCode.VcSemicolon => ShortcutKeys.OemSemicolon,
            KeyCode.VcEquals => ShortcutKeys.OemPlus,
            KeyCode.VcMinus => ShortcutKeys.OemMinus,
            KeyCode.VcBackQuote => ShortcutKeys.OemBackQuote,
            KeyCode.VcOpenBracket => ShortcutKeys.OemOpenBracket,
            KeyCode.VcBackslash => ShortcutKeys.OemBackslash,
            KeyCode.VcCloseBracket => ShortcutKeys.OemCloseBracket,
            KeyCode.VcQuote => ShortcutKeys.OemQuote,
            KeyCode.Vc102 => ShortcutKeys.Oem102,
            KeyCode.VcNumPadEquals => ShortcutKeys.NumpadEquals,
            KeyCode.VcLeftShift => ShortcutKeys.LeftShift,
            KeyCode.VcRightShift => ShortcutKeys.RightShift,
            KeyCode.VcLeftControl => ShortcutKeys.LeftControl,
            KeyCode.VcRightControl => ShortcutKeys.RightControl,
            KeyCode.VcLeftAlt => ShortcutKeys.LeftAlt,
            KeyCode.VcRightAlt => ShortcutKeys.RightAlt,
            KeyCode.VcLeftMeta => VirtualKey.LeftWindows,
            KeyCode.VcRightMeta => VirtualKey.RightWindows,
            KeyCode.VcF13 => VirtualKey.F13,
            KeyCode.VcF14 => VirtualKey.F14,
            KeyCode.VcF15 => VirtualKey.F15,
            KeyCode.VcF16 => VirtualKey.F16,
            KeyCode.VcF17 => VirtualKey.F17,
            KeyCode.VcF18 => VirtualKey.F18,
            KeyCode.VcF19 => VirtualKey.F19,
            KeyCode.VcF20 => VirtualKey.F20,
            KeyCode.VcF21 => VirtualKey.F21,
            KeyCode.VcF22 => VirtualKey.F22,
            KeyCode.VcF23 => VirtualKey.F23,
            KeyCode.VcF24 => VirtualKey.F24,
            KeyCode.VcHanja => VirtualKey.Hanja,
            KeyCode.VcFinal => VirtualKey.Final,
            KeyCode.VcJunja => VirtualKey.Junja,
            KeyCode.VcProcess => ShortcutKeys.ProcessKey,
            KeyCode.VcModeChange => VirtualKey.ModeChange,
            KeyCode.VcImeOff => ShortcutKeys.ImeOff,
            KeyCode.VcImeOn => ShortcutKeys.ImeOn,
            KeyCode.VcAlphanumeric => ShortcutKeys.ImeAlphanumeric,
            KeyCode.VcKatakana => ShortcutKeys.ImeKatakana,
            KeyCode.VcHiragana => ShortcutKeys.ImeHiragana,
            KeyCode.VcMediaPrevious => ShortcutKeys.MediaPreviousTrack,
            KeyCode.VcMediaNext => ShortcutKeys.MediaNextTrack,
            KeyCode.VcMediaPlay => ShortcutKeys.MediaPlayPause,
            KeyCode.VcMediaStop => ShortcutKeys.MediaStop,
            KeyCode.VcMediaSelect => ShortcutKeys.LaunchMediaSelect,
            KeyCode.VcVolumeMute => ShortcutKeys.VolumeMute,
            KeyCode.VcVolumeDown => ShortcutKeys.VolumeDown,
            KeyCode.VcVolumeUp => ShortcutKeys.VolumeUp,
            KeyCode.VcBrowserBack => ShortcutKeys.BrowserBack,
            KeyCode.VcBrowserForward => ShortcutKeys.BrowserForward,
            KeyCode.VcBrowserRefresh => ShortcutKeys.BrowserRefresh,
            KeyCode.VcBrowserStop => ShortcutKeys.BrowserStop,
            KeyCode.VcBrowserSearch => ShortcutKeys.BrowserSearch,
            KeyCode.VcBrowserFavorites => ShortcutKeys.BrowserFavorites,
            KeyCode.VcBrowserHome => ShortcutKeys.BrowserHome,
            KeyCode.VcAppMail => ShortcutKeys.LaunchMail,
            KeyCode.VcApp1 => ShortcutKeys.LaunchApplication1,
            KeyCode.VcApp2 => ShortcutKeys.LaunchApplication2,
            KeyCode.VcPower => ShortcutKeys.Power,
            KeyCode.VcSleep => VirtualKey.Sleep,
            _ => (VirtualKey)keyCode
        };

        return ShortcutKeys.IsValid(virtualKey);
    }
}
