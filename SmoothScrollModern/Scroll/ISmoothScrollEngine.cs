using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Scroll;

public interface ISmoothScrollEngine : IDisposable
{
    void EnqueueWheel(int delta, bool horizontal, ScrollSettings settings, ScrollDeliveryMode deliveryMode);

    void Stop();
}
