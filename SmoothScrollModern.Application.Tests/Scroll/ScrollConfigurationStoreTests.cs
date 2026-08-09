using SmoothScrollModern.Scroll;
using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Application.Tests.Scroll;

public sealed class ScrollConfigurationStoreTests
{
    [Fact]
    public void Publish_AtomicallyReplacesTheCurrentSnapshot()
    {
        var factory = new ScrollConfigurationSnapshotFactory();
        var store = new ScrollConfigurationStore(factory, new AppSettings { IsEnabled = false });
        var initial = store.Current;
        var settings = new AppSettings { IsEnabled = true };
        var pausedUntil = DateTimeOffset.UtcNow.AddMinutes(5);

        store.Publish(settings, pausedUntil);

        var current = store.Current;
        Assert.NotSame(initial, current);
        Assert.True(current.Version > initial.Version);
        Assert.True(current.IsEnabled);
        Assert.Equal(pausedUntil.ToUniversalTime(), current.PausedUntilUtc);
    }
}
