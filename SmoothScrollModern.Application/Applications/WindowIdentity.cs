namespace SmoothScrollModern.Applications;

/// <summary>
/// Минимальный набор данных об окне, необходимый для принятия решения о прокрутке.
/// Не содержит данных, предназначенных только для отображения в интерфейсе.
/// </summary>
public sealed record WindowIdentity(
    IntPtr WindowHandle,
    string ProcessName,
    string ExecutablePath,
    bool IsFullscreen)
{
    public static WindowIdentity Empty { get; } = new(
        IntPtr.Zero,
        string.Empty,
        string.Empty,
        false);
}
