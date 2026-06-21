using System.Diagnostics;
using Microsoft.Win32;
using SmoothScrollModern.Core;

namespace SmoothScrollModern.Startup;

public sealed class WindowsStartupService : IStartupService
{
    private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: false);
        return key?.GetValue(Constants.ApplicationName) is string;
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunRegistryPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunRegistryPath, writable: true);

        if (enabled)
        {
            var executablePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(executablePath))
            {
                key.SetValue(Constants.ApplicationName, $"\"{executablePath}\"");
            }

            return;
        }

        key.DeleteValue(Constants.ApplicationName, throwOnMissingValue: false);
    }
}
