using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Scroll;

public sealed class ScrollConfigurationStore : IScrollConfigurationProvider, IScrollConfigurationPublisher
{
    private readonly ScrollConfigurationSnapshotFactory _factory;
    private ScrollConfigurationSnapshot _current;

    public ScrollConfigurationStore(ScrollConfigurationSnapshotFactory factory, AppSettings initialSettings)
    {
        _factory = factory;
        _current = _factory.Create(initialSettings, pausedUntilUtc: null);
    }

    public ScrollConfigurationSnapshot Current => Volatile.Read(ref _current);

    public void Publish(AppSettings settings, DateTimeOffset? pausedUntilUtc)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Interlocked.Exchange(ref _current, _factory.Create(settings, pausedUntilUtc));
    }
}
