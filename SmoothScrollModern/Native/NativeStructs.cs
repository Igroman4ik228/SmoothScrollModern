using System.Runtime.InteropServices;

namespace SmoothScrollModern.Native;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct POINT
{
    public readonly int X;
    public readonly int Y;
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct MSLLHOOKSTRUCT
{
    public readonly POINT Pt;
    public readonly uint MouseData;
    public readonly uint Flags;
    public readonly uint Time;
    public readonly UIntPtr DwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct INPUT
{
    public uint Type;
    public MOUSEINPUT MouseInput;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MOUSEINPUT
{
    public int Dx;
    public int Dy;
    public uint MouseData;
    public uint DwFlags;
    public uint Time;
    public UIntPtr DwExtraInfo;
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
