namespace SmoothScrollModern.Startup;

public interface IStartupService
{
    bool IsEnabled();

    void SetEnabled(bool enabled);
}
