using System.Runtime.InteropServices;
using SmoothScrollModern.Native;

namespace SmoothScrollModern.Input;

public sealed class MouseHookService : IMouseHookService
{
    private readonly NativeMethods.LowLevelMouseProc _hookCallback;
    private IntPtr _hookHandle;
    private bool _disposed;

    public MouseHookService()
    {
        _hookCallback = HookCallback;
    }

    public event Func<MouseWheelEvent, bool>? MouseWheel;

    public bool IsRunning => _hookHandle != IntPtr.Zero;

    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _hookHandle = NativeMethods.SetWindowsHookExW(
            NativeConstants.WH_MOUSE_LL,
            _hookCallback,
            IntPtr.Zero,
            0);

        if (_hookHandle == IntPtr.Zero)
        {
            throw new WinApiException(nameof(NativeMethods.SetWindowsHookExW));
        }
    }

    public void Stop()
    {
        if (!IsRunning)
        {
            return;
        }

        if (!NativeMethods.UnhookWindowsHookEx(_hookHandle))
        {
            throw new WinApiException(nameof(NativeMethods.UnhookWindowsHookEx));
        }

        _hookHandle = IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (IsRunning)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }

        _disposed = true;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode != NativeConstants.HC_ACTION)
        {
            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var message = wParam.ToInt32();
        if (message is not NativeConstants.WM_MOUSEWHEEL and not NativeConstants.WM_MOUSEHWHEEL)
        {
            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
        if ((hookStruct.Flags & (NativeConstants.LLMHF_INJECTED | NativeConstants.LLMHF_LOWER_IL_INJECTED)) != 0)
        {
            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var delta = unchecked((short)((hookStruct.MouseData >> 16) & 0xffff));
        if (delta == 0)
        {
            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var handled = MouseWheel?.Invoke(new MouseWheelEvent(
            delta,
            message == NativeConstants.WM_MOUSEHWHEEL,
            hookStruct.Time)) ?? false;

        return handled ? new IntPtr(1) : NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }
}
