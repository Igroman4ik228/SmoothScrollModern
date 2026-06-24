using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SmoothScrollModern.Widgets.Shell.ViewModels;
using System.ComponentModel;
using Windows.UI;
using WinUIEx;
using Colors = Microsoft.UI.Colors;

namespace SmoothScrollModern;

public sealed partial class MainWindow : WindowEx
{
    private readonly Dictionary<ScrollViewer, double> _scrollTargets = [];
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        _viewModel = viewModel;

        InitializeComponent();
        ContentRoot.DataContext = viewModel;
        ContentRoot.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler(OnPointerWheelChanged), true);

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
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(24, 0, 0, 0);
        titleBar.ButtonPressedBackgroundColor = Color.FromArgb(36, 0, 0, 0);

        UpdateCaptionButtonInset(titleBar.RightInset);
        AppWindow.Changed += (_, args) =>
        {
            if (args.DidPresenterChange || args.DidSizeChange)
            {
                UpdateCaptionButtonInset(titleBar.RightInset);
            }
        };
    }

    private void UpdateCaptionButtonInset(double rightInset)
    {
        if (double.IsNaN(rightInset) || double.IsInfinity(rightInset) || rightInset < 0)
        {
            rightInset = 0;
        }

        CaptionButtonInsetColumn.Width = new GridLength(rightInset);
    }

    private void OnNavigationPaneToggleClick(object sender, RoutedEventArgs e)
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

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var wheelDelta = e.GetCurrentPoint(ContentRoot).Properties.MouseWheelDelta;
        if (wheelDelta == 0)
        {
            return;
        }

        var viewer = FindScrollableViewer(e.OriginalSource as DependencyObject, wheelDelta);
        if (viewer is null)
        {
            return;
        }

        e.Handled = true;
        AnimateScroll(viewer, wheelDelta);
    }

    private void AnimateScroll(ScrollViewer viewer, int wheelDelta)
    {
        var settings = _viewModel.Settings.Scroll;
        var multiplier = settings.ScrollMultiplier;
        var acceleration = settings.Acceleration;
        var currentTarget = _scrollTargets.TryGetValue(viewer, out var pendingTarget)
            ? pendingTarget
            : viewer.VerticalOffset;

        var target = Math.Clamp(
            currentTarget - (wheelDelta * multiplier * acceleration),
            0,
            viewer.ScrollableHeight);

        _scrollTargets[viewer] = target;
        viewer.ChangeView(null, target, null, disableAnimation: false);

        _ = ClearScrollTargetAsync(viewer, target, TimeSpan.FromMilliseconds(Math.Clamp(settings.DurationMs, 30, 900)));
    }

    private async Task ClearScrollTargetAsync(ScrollViewer viewer, double target, TimeSpan delay)
    {
        await Task.Delay(delay);
        if (_scrollTargets.TryGetValue(viewer, out var existingTarget)
            && Math.Abs(existingTarget - target) < 0.1)
        {
            _scrollTargets.Remove(viewer);
        }
    }

    private static ScrollViewer? FindScrollableViewer(DependencyObject? source, int wheelDelta)
    {
        for (var current = source; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is ScrollViewer viewer && CanScroll(viewer, wheelDelta))
            {
                return viewer;
            }
        }

        return null;
    }

    private static bool CanScroll(ScrollViewer viewer, int wheelDelta)
    {
        return viewer.ScrollableHeight > 0
               && ((wheelDelta < 0 && viewer.VerticalOffset < viewer.ScrollableHeight)
                   || (wheelDelta > 0 && viewer.VerticalOffset > 0));
    }
}
