using System.Runtime.InteropServices;
using SmoothScrollModern.Native;

namespace SmoothScrollModern.Input;

public sealed class InputInjectionService : IInputInjectionService
{
    public void SendWheel(int delta, bool horizontal)
    {
        if (delta == 0)
        {
            return;
        }

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
}
