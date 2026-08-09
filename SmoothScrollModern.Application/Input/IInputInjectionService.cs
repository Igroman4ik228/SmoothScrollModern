namespace SmoothScrollModern.Input;

public interface IInputInjectionService
{
    /// <summary>
    /// Sends a wheel delta only when it can still be delivered to the source window.
    /// </summary>
    /// <returns><see langword="true"/> when the delta was delivered; otherwise, <see langword="false"/>.</returns>
    bool SendWheel(int delta, bool horizontal, IntPtr targetWindowHandle);
}
