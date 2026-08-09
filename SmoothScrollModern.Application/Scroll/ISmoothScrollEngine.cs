using Windows.System;

namespace SmoothScrollModern.Scroll;

public interface ISmoothScrollEngine : IDisposable
{
    void EnqueueWheel(
        int delta,
        bool horizontal,
        ScrollSettingsSnapshot settings,
        ScrollDeliveryMode deliveryMode,
        IntPtr targetWindowHandle,
        int screenX,
        int screenY);

    void Stop();

    bool StopIfBypassKeyDown(VirtualKey virtualKey);
}
