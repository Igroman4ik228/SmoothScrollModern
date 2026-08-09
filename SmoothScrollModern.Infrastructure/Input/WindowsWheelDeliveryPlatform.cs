using System.Diagnostics;
using SmoothScrollModern.Native;

namespace SmoothScrollModern.Input;

internal sealed class WindowsWheelDeliveryPlatform : IWheelDeliveryPlatform
{
    public bool TryGetCursorPosition(out POINT cursorPoint)
    {
        return NativeMethods.GetCursorPos(out cursorPoint);
    }

    public IntPtr GetWindowAt(POINT screenPoint)
    {
        return NativeMethods.WindowFromPoint(screenPoint);
    }

    public IntPtr GetRootWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var rootWindowHandle = NativeMethods.GetAncestor(windowHandle, NativeConstants.GA_ROOT);
        return rootWindowHandle == IntPtr.Zero ? windowHandle : rootWindowHandle;
    }

    public bool TryPostWheelMessage(IntPtr targetWindowHandle, int delta, bool horizontal, POINT screenPoint)
    {
        var message = horizontal ? NativeConstants.WM_MOUSEHWHEEL : NativeConstants.WM_MOUSEWHEEL;
        var keyState = GetCurrentKeyState();
        TraceWheelMessage(targetWindowHandle, delta, horizontal, keyState);
        return NativeMethods.PostMessageW(
            targetWindowHandle,
            (uint)message,
            WheelMessageParameters.CreateWParam(delta, keyState),
            MakePointLParam(screenPoint.X, screenPoint.Y));
    }

    private static ushort GetCurrentKeyState()
    {
        return WheelMessageParameters.CreateKeyState(
            IsKeyDown(NativeConstants.VK_CONTROL),
            IsKeyDown(NativeConstants.VK_SHIFT),
            IsKeyDown(NativeConstants.VK_LBUTTON),
            IsKeyDown(NativeConstants.VK_RBUTTON),
            IsKeyDown(NativeConstants.VK_MBUTTON),
            IsKeyDown(NativeConstants.VK_XBUTTON1),
            IsKeyDown(NativeConstants.VK_XBUTTON2));
    }

    private static bool IsKeyDown(int virtualKey)
    {
        return NativeMethods.GetAsyncKeyState(virtualKey) < 0;
    }

    [Conditional("DEBUG")]
    private static void TraceWheelMessage(IntPtr targetWindowHandle, int delta, bool horizontal, ushort keyState)
    {
        Debug.WriteLine(
            $"[FIX:WheelDelivery] posting delta={delta}, horizontal={horizontal}, " +
            $"keyState=0x{keyState:X4}, target=0x{targetWindowHandle.ToInt64():X}");
    }

    private static nint MakePointLParam(int x, int y)
    {
        return (nint)unchecked((int)(((uint)(ushort)y << 16) | (ushort)x));
    }
}
