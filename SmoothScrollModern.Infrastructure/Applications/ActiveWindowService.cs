using System.Diagnostics;
using System.Text;
using SmoothScrollModern.Native;

namespace SmoothScrollModern.Applications;

public sealed class ActiveWindowService : IActiveWindowService
{
    private const int WindowTextCapacity = 512;
    private static readonly TimeSpan ProcessInfoCacheDuration = TimeSpan.FromSeconds(10);
    private readonly Dictionary<int, CachedProcessInfo> _processInfoCache = [];
    private readonly object _processInfoCacheGate = new();

    public ApplicationInfo GetActiveApplication()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        return GetApplicationFromWindow(hwnd);
    }

    public ApplicationInfo GetApplicationFromWindow(IntPtr hwnd)
    {
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
        var processInfo = GetProcessInfo(processId);

        var title = GetWindowTitle(hwnd);
        return new ApplicationInfo(
            hwnd,
            processId,
            processInfo.ProcessName,
            processInfo.ExecutablePath,
            string.IsNullOrWhiteSpace(processInfo.DisplayName) ? processInfo.ProcessName : processInfo.DisplayName,
            string.IsNullOrWhiteSpace(title) ? "Без заголовка окна" : title,
            IsFullscreen(hwnd));
    }

    private CachedProcessInfo GetProcessInfo(int processId)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_processInfoCacheGate)
        {
            if (_processInfoCache.TryGetValue(processId, out var cached)
                && now - cached.CachedAt <= ProcessInfoCacheDuration)
            {
                return cached;
            }
        }

        var processInfo = ReadProcessInfo(processId, now);
        lock (_processInfoCacheGate)
        {
            _processInfoCache[processId] = processInfo;
            PruneProcessInfoCache(now);
        }

        return processInfo;
    }

    private static CachedProcessInfo ReadProcessInfo(int processId, DateTimeOffset cachedAt)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            var processName = $"{process.ProcessName}.exe".ToLowerInvariant();
            var executablePath = TryGetExecutablePath(process) ?? string.Empty;
            var displayName = TryGetDisplayName(executablePath) ?? processName;
            return new CachedProcessInfo(processName, executablePath, displayName, cachedAt);
        }
        catch (Exception)
        {
            var processName = $"pid:{processId}";
            return new CachedProcessInfo(processName, string.Empty, processName, cachedAt);
        }
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

    private static string? TryGetDisplayName(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        try
        {
            var description = FileVersionInfo.GetVersionInfo(executablePath).FileDescription;
            return string.IsNullOrWhiteSpace(description) ? null : description;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void PruneProcessInfoCache(DateTimeOffset now)
    {
        if (_processInfoCache.Count <= 128)
        {
            return;
        }

        foreach (var processId in _processInfoCache
                     .Where(item => now - item.Value.CachedAt > ProcessInfoCacheDuration)
                     .Select(item => item.Key)
                     .ToList())
        {
            _processInfoCache.Remove(processId);
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

    private sealed record CachedProcessInfo(
        string ProcessName,
        string ExecutablePath,
        string DisplayName,
        DateTimeOffset CachedAt);
}
