namespace SmoothScrollModern.Native;

internal static class NativeConstants
{
    public const int WH_MOUSE_LL = 14;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_MBUTTONDOWN = 0x0207;
    public const int WM_MOUSEWHEEL = 0x020A;
    public const int WM_XBUTTONDOWN = 0x020B;
    public const int WM_MOUSEHWHEEL = 0x020E;
    public const int HC_ACTION = 0;
    public const int LLMHF_INJECTED = 0x00000001;
    public const int LLMHF_LOWER_IL_INJECTED = 0x00000002;

    public const uint INPUT_MOUSE = 0;
    public const uint MOUSEEVENTF_WHEEL = 0x0800;
    public const uint MOUSEEVENTF_HWHEEL = 0x01000;

    public const uint MONITOR_DEFAULTTONEAREST = 2;
    public const uint GA_ROOT = 2;

    public const int VK_LBUTTON = 0x01;
    public const int VK_RBUTTON = 0x02;
    public const int VK_SHIFT = 0x10;
    public const int VK_CONTROL = 0x11;
    public const int VK_MBUTTON = 0x04;
    public const int VK_XBUTTON1 = 0x05;
    public const int VK_XBUTTON2 = 0x06;

    public const uint MK_LBUTTON = 0x0001;
    public const uint MK_RBUTTON = 0x0002;
    public const uint MK_SHIFT = 0x0004;
    public const uint MK_CONTROL = 0x0008;
    public const uint MK_MBUTTON = 0x0010;
    public const uint MK_XBUTTON1 = 0x0020;
    public const uint MK_XBUTTON2 = 0x0040;
}
