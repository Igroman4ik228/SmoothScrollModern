namespace SmoothScrollModern.Input;

public interface IInputInjectionService
{
    void SendWheel(int delta, bool horizontal, IntPtr targetWindowHandle, int screenX, int screenY);
}
