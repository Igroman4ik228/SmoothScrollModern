namespace SmoothScrollModern.Tray;

public interface ITrayService : IDisposable
{
    event Action? ShowRequested;

    event Action? ToggleEnabledRequested;

    event Action? DisableForCurrentApplicationRequested;

    event Action? PauseRequested;

    event Action? ExitRequested;

    void Initialize();

    void UpdateState(bool isEnabled, bool isPaused);
}
