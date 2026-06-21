using System.Diagnostics;
using System.Text;
using SmoothScrollModern.Native;

namespace SmoothScrollModern.Applications;

public sealed class ActiveWindowService : IActiveWindowService
{
    private const int WindowTextCapacity = 512;
    public ApplicationInfo GetActiveApplication()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return ApplicationInfo.Empty;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out var processIdRaw);
        if (processIdRaw == 0)
        {
            return ApplicationInfo.Empty;
        }

        var processId = unchecked((int)processIdRaw);
        var processName = string.Empty;
        var executablePath = string.Empty;
        var displayName = string.Empty;

        try
        {
            using var process = Process.GetProcessById(processId);
            processName = $"{process.ProcessName}.exe".ToLowerInvariant();
            executablePath = TryGetExecutablePath(process) ?? string.Empty;
            displayName = TryGetDisplayName(process) ?? processName;
        }
        catch (Exception)
        {
            processName = $"pid:{processId}";
            displayName = processName;
        }

        var title = GetWindowTitle(hwnd);
        return new ApplicationInfo(
            hwnd,
            processId,
            processName,
            executablePath,
            string.IsNullOrWhiteSpace(displayName) ? processName : displayName,
            string.IsNullOrWhiteSpace(title) ? "Без заголовка окна" : title,
            IsFullscreen(hwnd));
    }

    private static string? TryGetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? TryGetDisplayName(Process process)
    {
        try
        {
            var fileName = process.MainModule?.FileVersionInfo.FileDescription;
            return string.IsNullOrWhiteSpace(fileName) ? null : fileName;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        var builder = new StringBuilder(WindowTextCapacity);
        return NativeMethods.GetWindowText(hwnd, builder, builder.Capacity) > 0
            ? builder.ToString()
            : string.Empty;
    }

    private static bool IsFullscreen(IntPtr hwnd)
    {
        if (!NativeMethods.GetWindowRect(hwnd, out var windowRect))
        {
            return false;
        }

        var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeConstants.MONITOR_DEFAULTTONEAREST);
        if (monitor == IntPtr.Zero)
        {
            return false;
        }

        var monitorInfo = new MONITORINFO { CbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
        if (!NativeMethods.GetMonitorInfoW(monitor, ref monitorInfo))
        {
            return false;
        }

        const int tolerance = 2;
        return Math.Abs(windowRect.Left - monitorInfo.RcMonitor.Left) <= tolerance
               && Math.Abs(windowRect.Top - monitorInfo.RcMonitor.Top) <= tolerance
               && Math.Abs(windowRect.Width - monitorInfo.RcMonitor.Width) <= tolerance
               && Math.Abs(windowRect.Height - monitorInfo.RcMonitor.Height) <= tolerance;
    }
}
