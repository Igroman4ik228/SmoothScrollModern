namespace SmoothScrollModern.Applications;

public sealed record ApplicationInfo(
    IntPtr WindowHandle,
    int ProcessId,
    string ProcessName,
    string ExecutablePath,
    string DisplayName,
    string WindowTitle,
    bool IsFullscreen)
{
    public static ApplicationInfo Empty { get; } = new(
        IntPtr.Zero,
        0,
        "unknown.exe",
        string.Empty,
        "Неизвестное приложение",
        "Без заголовка окна",
        false);
}
