using System.Runtime.InteropServices;
using SmoothScrollModern.Native;

namespace SmoothScrollModern.Input;

public sealed class InputInjectionService : IInputInjectionService
{
    public void SendWheel(int delta, bool horizontal, IntPtr targetWindowHandle, int screenX, int screenY)
    {
        if (delta == 0)
        {
            return;
        }

        if (!horizontal)
        {
            SendWheelInput(delta, horizontal);
            return;
        }

        if (targetWindowHandle != IntPtr.Zero)
        {
            TryPostWheelToCurrentPointerTarget(targetWindowHandle, delta, horizontal, screenX, screenY);
            return;
        }

        SendWheelInput(delta, horizontal);
    }

    private static void SendWheelInput(int delta, bool horizontal)
    {
        var input = new INPUT
        {
            Type = NativeConstants.INPUT_MOUSE,
            MouseInput = new MOUSEINPUT
            {
                MouseData = unchecked((uint)delta),
                DwFlags = horizontal ? NativeConstants.MOUSEEVENTF_HWHEEL : NativeConstants.MOUSEEVENTF_WHEEL
            }
        };

        var sent = NativeMethods.SendInput(1, [input], Marshal.SizeOf<INPUT>());
        if (sent != 1)
        {
            throw new WinApiException(nameof(NativeMethods.SendInput));
        }
    }

    private static bool TryPostWheelToCurrentPointerTarget(
        IntPtr targetWindowHandle,
        int delta,
        bool horizontal,
        int fallbackScreenX,
        int fallbackScreenY)
    {
        var point = NativeMethods.GetCursorPos(out var cursorPoint)
            ? cursorPoint
            : new POINT(fallbackScreenX, fallbackScreenY);

        var postWindowHandle = GetCurrentTargetWindow(targetWindowHandle, point);
        if (postWindowHandle == IntPtr.Zero)
        {
            return false;
        }

        var message = horizontal ? NativeConstants.WM_MOUSEHWHEEL : NativeConstants.WM_MOUSEWHEEL;
        return NativeMethods.PostMessageW(
            postWindowHandle,
            (uint)message,
            MakeWheelWParam(delta),
            MakePointLParam(point.X, point.Y));
    }

    private static IntPtr GetCurrentTargetWindow(IntPtr originalTargetWindowHandle, POINT point)
    {
        var currentWindowHandle = NativeMethods.WindowFromPoint(point);
        if (currentWindowHandle == IntPtr.Zero)
        {
            return originalTargetWindowHandle;
        }

        var originalRoot = GetRootWindow(originalTargetWindowHandle);
        var currentRoot = GetRootWindow(currentWindowHandle);

        return originalRoot == currentRoot
            ? currentWindowHandle
            : IntPtr.Zero;
    }

    private static IntPtr GetRootWindow(IntPtr windowHandle)
    {
        var root = NativeMethods.GetAncestor(windowHandle, NativeConstants.GA_ROOT);
        return root == IntPtr.Zero ? windowHandle : root;
    }

    private static nuint MakeWheelWParam(int delta)
    {
        return (nuint)((unchecked((uint)(ushort)delta) << 16) | GetMouseKeyState());
    }

    private static nint MakePointLParam(int x, int y)
    {
        return (nint)unchecked((int)(((uint)(ushort)y << 16) | (ushort)x));
    }

    private static uint GetMouseKeyState()
    {
        uint state = 0;
        state |= IsKeyDown(NativeConstants.VK_LBUTTON) ? NativeConstants.MK_LBUTTON : 0;
        state |= IsKeyDown(NativeConstants.VK_RBUTTON) ? NativeConstants.MK_RBUTTON : 0;
        state |= IsKeyDown(NativeConstants.VK_SHIFT) ? NativeConstants.MK_SHIFT : 0;
        state |= IsKeyDown(NativeConstants.VK_CONTROL) ? NativeConstants.MK_CONTROL : 0;
        state |= IsKeyDown(NativeConstants.VK_MBUTTON) ? NativeConstants.MK_MBUTTON : 0;
        state |= IsKeyDown(NativeConstants.VK_XBUTTON1) ? NativeConstants.MK_XBUTTON1 : 0;
        state |= IsKeyDown(NativeConstants.VK_XBUTTON2) ? NativeConstants.MK_XBUTTON2 : 0;
        return state;
    }

    private static bool IsKeyDown(int virtualKey)
    {
        return (NativeMethods.GetKeyState(virtualKey) & 0x8000) != 0;
    }
}
