using Windows.System;

namespace SmoothScrollModern.Scroll;

public interface ISmoothScrollEngine : IDisposable
{
    void EnqueueWheel(
        int delta,
        bool horizontal,
        ScrollSettingsSnapshot settings,
        ScrollDeliveryMode deliveryMode,
        IntPtr targetWindowHandle);

    void Stop();

    bool StopIfBypassKeyDown(VirtualKey virtualKey);
}
