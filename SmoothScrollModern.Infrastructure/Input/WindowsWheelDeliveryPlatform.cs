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
        return NativeMethods.PostMessageW(
            targetWindowHandle,
            (uint)message,
            MakeWheelWParam(delta),
            MakePointLParam(screenPoint.X, screenPoint.Y));
    }

    private static nuint MakeWheelWParam(int delta)
    {
        return (nuint)(unchecked((uint)(ushort)delta) << 16);
    }

    private static nint MakePointLParam(int x, int y)
    {
        return (nint)unchecked((int)(((uint)(ushort)y << 16) | (ushort)x));
    }
}
