namespace SmoothScrollModern.Native;

internal static class NativeConstants
{
    public const int WM_MOUSEWHEEL = 0x020A;
    public const int WM_MOUSEHWHEEL = 0x020E;

    public const int VK_LBUTTON = 0x01;
    public const int VK_RBUTTON = 0x02;
    public const int VK_MBUTTON = 0x04;
    public const int VK_XBUTTON1 = 0x05;
    public const int VK_XBUTTON2 = 0x06;
    public const int VK_SHIFT = 0x10;
    public const int VK_CONTROL = 0x11;

    public const ushort MK_LBUTTON = 0x0001;
    public const ushort MK_RBUTTON = 0x0002;
    public const ushort MK_SHIFT = 0x0004;
    public const ushort MK_CONTROL = 0x0008;
    public const ushort MK_MBUTTON = 0x0010;
    public const ushort MK_XBUTTON1 = 0x0020;
    public const ushort MK_XBUTTON2 = 0x0040;

    public const uint MONITOR_DEFAULTTONEAREST = 2;
    public const uint GA_ROOT = 2;
}
