using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using SmoothScrollModern.Widgets.Shell.ViewModels;
using System.ComponentModel;
using Windows.UI;
using WinUIEx;
using Colors = Microsoft.UI.Colors;

namespace SmoothScrollModern;

public sealed partial class MainWindow : WindowEx
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;

        InitializeComponent();
        ContentRoot.DataContext = viewModel;

        ConfigureTitleBar();
        AppWindow.Closing += OnAppWindowClosing;

        viewModel.ThemeChanged += ApplyTheme;
        ApplyTheme(viewModel.Theme);
    }

    public event Action<CancelEventArgs>? ClosingRequested;

    public void HideWindow()
    {
        WindowExtensions.Hide(this);
    }

    public void ShowWindow()
    {
        WindowExtensions.Show(this);
    }

    private void ConfigureTitleBar()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        var titleBar = AppWindow.TitleBar;
        titleBar.ExtendsContentIntoTitleBar = true;
        titleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(24, 0, 0, 0);
        titleBar.ButtonPressedBackgroundColor = Color.FromArgb(36, 0, 0, 0);
    }

    private void OnTitleBarPaneToggleRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
    {
        ShellNavigation.TogglePane();
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        var cancelEventArgs = new CancelEventArgs();
        ClosingRequested?.Invoke(cancelEventArgs);
        args.Cancel = cancelEventArgs.Cancel;
    }

    private void ApplyTheme(string theme)
    {
        ContentRoot.RequestedTheme = theme switch
        {
            "Light" => ElementTheme.Light,
            "Dark" => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
    }
}
