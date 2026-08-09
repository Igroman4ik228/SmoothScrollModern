using SmoothScrollModern.Input;
using SmoothScrollModern.Native;

namespace SmoothScrollModern.Application.Tests.Input;

public sealed class WheelMessageParametersTests
{
    [Fact]
    public void CreateKeyState_ReturnsPressedModifierAndMouseButtonFlags()
    {
        var keyState = WheelMessageParameters.CreateKeyState(
            isControlDown: true,
            isShiftDown: true,
            isLeftButtonDown: true,
            isRightButtonDown: true,
            isMiddleButtonDown: true,
            isXButton1Down: true,
            isXButton2Down: true);

        var expected = (ushort)(NativeConstants.MK_CONTROL
            | NativeConstants.MK_SHIFT
            | NativeConstants.MK_LBUTTON
            | NativeConstants.MK_RBUTTON
            | NativeConstants.MK_MBUTTON
            | NativeConstants.MK_XBUTTON1
            | NativeConstants.MK_XBUTTON2);
        Assert.Equal(expected, keyState);
    }

    [Fact]
    public void CreateWParam_PreservesKeyStateAndSignedWheelDelta()
    {
        const ushort keyState = NativeConstants.MK_CONTROL | NativeConstants.MK_SHIFT;

        var wParam = WheelMessageParameters.CreateWParam(-120, keyState);

        Assert.Equal(keyState, (ushort)wParam);
        Assert.Equal(unchecked((ushort)-120), (ushort)(wParam >> 16));
    }
}
