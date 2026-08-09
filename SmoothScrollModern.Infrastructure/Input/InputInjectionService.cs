using System.Diagnostics;
using SmoothScrollModern.Native;

namespace SmoothScrollModern.Input;

public sealed class InputInjectionService : IInputInjectionService
{
    private readonly IWheelDeliveryPlatform _platform;

    public InputInjectionService()
        : this(new WindowsWheelDeliveryPlatform())
    {
    }

    internal InputInjectionService(IWheelDeliveryPlatform platform)
    {
        ArgumentNullException.ThrowIfNull(platform);
        _platform = platform;
    }

    public bool SendWheel(int delta, bool horizontal, IntPtr targetWindowHandle)
    {
        if (delta == 0)
        {
            return true;
        }

        if (targetWindowHandle == IntPtr.Zero)
        {
            TraceDelivery(delta, horizontal, targetWindowHandle, IntPtr.Zero, delivered: false, "source window is unavailable");
            return false;
        }

        if (!_platform.TryGetCursorPosition(out var cursorPoint))
        {
            TraceDelivery(delta, horizontal, targetWindowHandle, IntPtr.Zero, delivered: false, "cursor position is unavailable");
            return false;
        }

        var currentWindowHandle = _platform.GetWindowAt(cursorPoint);
        var sourceRootWindowHandle = _platform.GetRootWindow(targetWindowHandle);
        var currentRootWindowHandle = _platform.GetRootWindow(currentWindowHandle);
        if (currentWindowHandle == IntPtr.Zero
            || sourceRootWindowHandle == IntPtr.Zero
            || sourceRootWindowHandle != currentRootWindowHandle)
        {
            TraceDelivery(delta, horizontal, targetWindowHandle, currentWindowHandle, delivered: false, "pointer left the source root window");
            return false;
        }

        var delivered = _platform.TryPostWheelMessage(currentWindowHandle, delta, horizontal, cursorPoint);
        TraceDelivery(
            delta,
            horizontal,
            targetWindowHandle,
            currentWindowHandle,
            delivered,
            delivered ? "posted" : "PostMessageW failed");
        return delivered;
    }

    [Conditional("DEBUG")]
    private static void TraceDelivery(
        int delta,
        bool horizontal,
        IntPtr sourceWindowHandle,
        IntPtr currentWindowHandle,
        bool delivered,
        string reason)
    {
        Debug.WriteLine(
            $"Wheel delivery delta={delta}, horizontal={horizontal}, source=0x{sourceWindowHandle.ToInt64():X}, " +
            $"current=0x{currentWindowHandle.ToInt64():X}, delivered={delivered}, reason={reason}");
    }
}
