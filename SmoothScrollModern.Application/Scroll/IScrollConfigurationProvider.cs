using SmoothScrollModern.Settings;

namespace SmoothScrollModern.Scroll;

public interface IScrollConfigurationProvider
{
    ScrollConfigurationSnapshot Current { get; }
}

public interface IScrollConfigurationPublisher
{
    void Publish(AppSettings settings, DateTimeOffset? pausedUntilUtc);
}
