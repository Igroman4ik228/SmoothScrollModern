namespace SmoothScrollModern.Native;

internal static class NativeConstants
{
    public const int WH_MOUSE_LL = 14;
    public const int WM_MOUSEWHEEL = 0x020A;
    public const int WM_MOUSEHWHEEL = 0x020E;
    public const int HC_ACTION = 0;
    public const int LLMHF_INJECTED = 0x00000001;
    public const int LLMHF_LOWER_IL_INJECTED = 0x00000002;

    public const uint INPUT_MOUSE = 0;
    public const uint MOUSEEVENTF_WHEEL = 0x0800;
    public const uint MOUSEEVENTF_HWHEEL = 0x01000;

    public const uint MONITOR_DEFAULTTONEAREST = 2;
}
