using System.Collections.Concurrent;
using System.Diagnostics;
using SmoothScrollModern.Applications;
using SmoothScrollModern.Native;

namespace SmoothScrollModern.Applications;

/// <summary>
/// Потокобезопасно получает только те сведения об окне, которые нужны input-пайплайну.
/// Данные процесса и fullscreen-состояние кэшируются отдельно, чтобы не выполнять
/// тяжёлые операции при каждом движении колеса.
/// </summary>
public sealed class WindowIdentityResolver : IWindowIdentityResolver
{
    private const int MaximumCacheEntries = 256;
    private static readonly TimeSpan ProcessCacheDuration = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan WindowStateCacheDuration = TimeSpan.FromMilliseconds(300);
    private readonly ConcurrentDictionary<int, CachedProcessIdentity> _processCache = [];
    private readonly ConcurrentDictionary<nint, CachedWindowState> _windowStateCache = [];

    public WindowIdentity Resolve(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return WindowIdentity.Empty;
        }

        var rootWindowHandle = GetRootWindow(windowHandle);
        NativeMethods.GetWindowThreadProcessId(rootWindowHandle, out var processIdRaw);
        if (processIdRaw == 0)
        {
            return WindowIdentity.Empty;
        }

        var now = DateTimeOffset.UtcNow;
        var processId = unchecked((int)processIdRaw);
        var process = GetProcessIdentity(processId, now);
        var isFullscreen = GetWindowState(rootWindowHandle, now);

        return new WindowIdentity(
            rootWindowHandle,
            process.ProcessName,
            process.ExecutablePath,
            isFullscreen);
    }

    private CachedProcessIdentity GetProcessIdentity(int processId, DateTimeOffset now)
    {
        if (_processCache.TryGetValue(processId, out var cached)
            && now - cached.CachedAt <= ProcessCacheDuration)
        {
            return cached;
        }

        var identity = ReadProcessIdentity(processId, now);
        _processCache[processId] = identity;
        TrimCache(_processCache, static entry => entry.CachedAt);
        return identity;
    }

    private bool GetWindowState(IntPtr windowHandle, DateTimeOffset now)
    {
        var key = (nint)windowHandle;
        if (_windowStateCache.TryGetValue(key, out var cached)
            && now - cached.CachedAt <= WindowStateCacheDuration)
        {
            return cached.IsFullscreen;
        }

        var state = new CachedWindowState(IsFullscreen(windowHandle), now);
        _windowStateCache[key] = state;
        TrimCache(_windowStateCache, static entry => entry.CachedAt);
        return state.IsFullscreen;
    }

    private static CachedProcessIdentity ReadProcessIdentity(int processId, DateTimeOffset cachedAt)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            var processName = $"{process.ProcessName}.exe".ToLowerInvariant();
            return new CachedProcessIdentity(processName, TryGetExecutablePath(process) ?? string.Empty, cachedAt);
        }
        catch (Exception)
        {
            return new CachedProcessIdentity($"pid:{processId}", string.Empty, cachedAt);
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

    private static IntPtr GetRootWindow(IntPtr windowHandle)
    {
        var root = NativeMethods.GetAncestor(windowHandle, NativeConstants.GA_ROOT);
        return root == IntPtr.Zero ? windowHandle : root;
    }

    private static bool IsFullscreen(IntPtr windowHandle)
    {
        if (!NativeMethods.GetWindowRect(windowHandle, out var windowRect))
        {
            return false;
        }

        var monitor = NativeMethods.MonitorFromWindow(windowHandle, NativeConstants.MONITOR_DEFAULTTONEAREST);
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

    private static void TrimCache<TKey, TValue>(
        ConcurrentDictionary<TKey, TValue> cache,
        Func<TValue, DateTimeOffset> getCachedAt)
        where TKey : notnull
    {
        if (cache.Count <= MaximumCacheEntries)
        {
            return;
        }

        var entriesToRemove = cache
            .OrderBy(item => getCachedAt(item.Value))
            .Take(Math.Max(1, cache.Count - MaximumCacheEntries))
            .ToList();

        foreach (var item in entriesToRemove)
        {
            cache.TryRemove(item.Key, out _);
        }
    }

    private sealed record CachedProcessIdentity(string ProcessName, string ExecutablePath, DateTimeOffset CachedAt);

    private sealed record CachedWindowState(bool IsFullscreen, DateTimeOffset CachedAt);
}
