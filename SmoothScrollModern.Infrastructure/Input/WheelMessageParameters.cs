using SmoothScrollModern.Native;

namespace SmoothScrollModern.Input;

/// <summary>
/// Creates the <c>wParam</c> payload required by Windows wheel messages.
/// </summary>
internal static class WheelMessageParameters
{
    public static ushort CreateKeyState(
        bool isControlDown,
        bool isShiftDown,
        bool isLeftButtonDown,
        bool isRightButtonDown,
        bool isMiddleButtonDown,
        bool isXButton1Down,
        bool isXButton2Down)
    {
        return (ushort)((isControlDown ? NativeConstants.MK_CONTROL : 0)
            | (isShiftDown ? NativeConstants.MK_SHIFT : 0)
            | (isLeftButtonDown ? NativeConstants.MK_LBUTTON : 0)
            | (isRightButtonDown ? NativeConstants.MK_RBUTTON : 0)
            | (isMiddleButtonDown ? NativeConstants.MK_MBUTTON : 0)
            | (isXButton1Down ? NativeConstants.MK_XBUTTON1 : 0)
            | (isXButton2Down ? NativeConstants.MK_XBUTTON2 : 0));
    }

    public static nuint CreateWParam(int delta, ushort keyState)
    {
        return ((nuint)(uint)(ushort)delta << 16) | keyState;
    }
}
