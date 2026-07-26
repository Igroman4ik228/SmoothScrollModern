using SharpHook;
using SharpHook.Data;
using SmoothScrollModern.Native;

namespace SmoothScrollModern.Input;

public sealed class InputInjectionService : IInputInjectionService
{
    private readonly IEventSimulator _eventSimulator = new EventSimulator();

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

        if (targetWindowHandle != IntPtr.Zero
            && TryPostWheelToCurrentPointerTarget(targetWindowHandle, delta, horizontal, screenX, screenY))
        {
            return;
        }

        SendWheelInput(delta, horizontal);
    }

    private void SendWheelInput(int delta, bool horizontal)
    {
        _eventSimulator.SimulateMouseWheel(
            (short)Math.Clamp(delta, short.MinValue, short.MaxValue),
            horizontal ? MouseWheelScrollDirection.Horizontal : MouseWheelScrollDirection.Vertical,
            MouseWheelScrollType.UnitScroll);
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
        return (nuint)(unchecked((uint)(ushort)delta) << 16);
    }

    private static nint MakePointLParam(int x, int y)
    {
        return (nint)unchecked((int)(((uint)(ushort)y << 16) | (ushort)x));
    }

}
