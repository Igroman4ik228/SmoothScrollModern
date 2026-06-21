using System.Drawing;
using SmoothScrollModern.Core;
using Forms = System.Windows.Forms;

namespace SmoothScrollModern.Tray;

public sealed class TrayService : ITrayService
{
    private Forms.NotifyIcon? _notifyIcon;
    private Forms.ToolStripMenuItem? _toggleItem;
    private bool _disposed;

    public event Action? ShowRequested;

    public event Action? ToggleEnabledRequested;

    public event Action? DisableForCurrentApplicationRequested;

    public event Action? PauseRequested;

    public event Action? ExitRequested;

    public void Initialize()
    {
        if (_notifyIcon is not null)
        {
            return;
        }

        _toggleItem = new Forms.ToolStripMenuItem("Выключить плавную прокрутку", null, (_, _) => ToggleEnabledRequested?.Invoke());
        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.Add(new Forms.ToolStripMenuItem("Открыть настройки", null, (_, _) => ShowRequested?.Invoke()));
        contextMenu.Items.Add(_toggleItem);
        contextMenu.Items.Add(new Forms.ToolStripMenuItem("Отключить для текущего приложения", null, (_, _) => DisableForCurrentApplicationRequested?.Invoke()));
        contextMenu.Items.Add(new Forms.ToolStripMenuItem($"Пауза на {Constants.TrayPauseMinutes} минут", null, (_, _) => PauseRequested?.Invoke()));
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(new Forms.ToolStripMenuItem("Выход", null, (_, _) => ExitRequested?.Invoke()));

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            ContextMenuStrip = contextMenu,
            Text = Constants.ApplicationName,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => ShowRequested?.Invoke();
    }

    public void UpdateState(bool isEnabled, bool isPaused)
    {
        if (_notifyIcon is null)
        {
            return;
        }

        var state = isPaused ? "пауза" : isEnabled ? "включён" : "выключен";
        _notifyIcon.Text = $"{Constants.ApplicationName}: {state}";

        if (_toggleItem is not null)
        {
            _toggleItem.Text = isEnabled ? "Выключить плавную прокрутку" : "Включить плавную прокрутку";
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        _disposed = true;
    }
}
