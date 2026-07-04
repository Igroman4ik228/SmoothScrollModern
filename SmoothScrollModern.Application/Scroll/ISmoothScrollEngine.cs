using SmoothScrollModern.Settings;
using Windows.System;

namespace SmoothScrollModern.Scroll;

public interface ISmoothScrollEngine : IDisposable
{
    void EnqueueWheel(
        int delta,
        bool horizontal,
        ScrollSettings settings,
        ScrollDeliveryMode deliveryMode,
        IntPtr targetWindowHandle,
        int screenX,
        int screenY);

    void Stop();

    bool StopIfBypassKeyDown(VirtualKey virtualKey);
}
