using Windows.System;

namespace SmoothScrollModern.Input;

public interface IGlobalInputHookService : IDisposable
{
    event Func<MouseWheelEvent, bool>? MouseWheel;

    event Action<VirtualKey>? KeyDown;

    bool IsRunning { get; }

    bool IsAnyShortcutKeyDown(IEnumerable<VirtualKey> virtualKeys);

    void Start();

    void Stop();
}
