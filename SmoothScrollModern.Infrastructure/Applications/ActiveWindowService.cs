using System.Diagnostics;
using System.Text;
using SmoothScrollModern.Native;

namespace SmoothScrollModern.Applications;

public sealed class ActiveWindowService : IActiveWindowService
{
    private const int WindowTextCapacity = 512;
    private readonly IWindowIdentityResolver _windowIdentityResolver;

    public ActiveWindowService(IWindowIdentityResolver windowIdentityResolver)
    {
        _windowIdentityResolver = windowIdentityResolver;
    }

    public ApplicationInfo GetActiveApplication()
    {
        return GetApplicationFromWindow(NativeMethods.GetForegroundWindow());
    }

    public ApplicationInfo GetApplicationFromWindow(IntPtr windowHandle)
    {
        var identity = _windowIdentityResolver.Resolve(windowHandle);
        if (identity == WindowIdentity.Empty)
        {
            return ApplicationInfo.Empty;
        }

        var title = GetWindowTitle(identity.WindowHandle);
        return new ApplicationInfo(
            identity.WindowHandle,
            GetProcessId(identity.WindowHandle),
            identity.ProcessName,
            identity.ExecutablePath,
            TryGetDisplayName(identity.ExecutablePath) ?? identity.ProcessName,
            string.IsNullOrWhiteSpace(title) ? "Без заголовка окна" : title,
            identity.IsFullscreen);
    }

    private static int GetProcessId(IntPtr windowHandle)
    {
        NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId);
        return unchecked((int)processId);
    }

    private static string GetWindowTitle(IntPtr windowHandle)
    {
        var builder = new StringBuilder(WindowTextCapacity);
        return NativeMethods.GetWindowText(windowHandle, builder, builder.Capacity) > 0
            ? builder.ToString()
            : string.Empty;
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
}
