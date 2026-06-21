namespace SmoothScrollModern.Input;

public interface IMouseHookService : IDisposable
{
    event Func<MouseWheelEvent, bool>? MouseWheel;

    bool IsRunning { get; }

    void Start();

    void Stop();
}
