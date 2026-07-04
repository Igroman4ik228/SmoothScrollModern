using System.Runtime.InteropServices;

namespace SmoothScrollModern.Native;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct POINT
{
    public POINT(int x, int y)
    {
        X = x;
        Y = y;
    }

    public readonly int X;

    public readonly int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;

    public int Top;

    public int Right;

    public int Bottom;

    public readonly int Width => Right - Left;

    public readonly int Height => Bottom - Top;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
internal struct MONITORINFO
{
    public uint CbSize;

    public RECT RcMonitor;

    public RECT RcWork;

    public uint DwFlags;
}
