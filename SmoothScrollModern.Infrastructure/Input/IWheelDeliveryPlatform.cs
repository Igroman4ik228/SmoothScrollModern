using SmoothScrollModern.Native;

namespace SmoothScrollModern.Input;

/// <summary>
/// Isolates Win32 calls used to validate and deliver an inertial wheel delta.
/// </summary>
internal interface IWheelDeliveryPlatform
{
    bool TryGetCursorPosition(out POINT cursorPoint);

    IntPtr GetWindowAt(POINT screenPoint);

    IntPtr GetRootWindow(IntPtr windowHandle);

    bool TryPostWheelMessage(IntPtr targetWindowHandle, int delta, bool horizontal, POINT screenPoint);
}
