using System.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SmoothScrollModern.Scroll;
using SmoothScrollModern.Settings;
using SmoothScrollModern.ViewModels;
using Windows.Graphics;
using WinRT.Interop;

namespace SmoothScrollModern;

public sealed partial class MainWindow : Window
{
    private const int ShowWindowHide = 0;
    private const int ShowWindowNormal = 1;
    private const int InitialWidth = 1040;
    private const int InitialHeight = 760;
    private const int MinWidth = 820;
    private const int MinHeight = 620;

    private readonly Dictionary<ScrollViewer, double> _scrollTargets = [];
    private readonly Action<CancelEventArgs> _closingHandler;
    private readonly MainViewModel _viewModel;
    private readonly nint _hwnd;
    private readonly AppWindow _appWindow;

    public MainWindow(MainViewModel viewModel, Action<CancelEventArgs> closingHandler)
    {
        _viewModel = viewModel;
        _closingHandler = closingHandler;

        InitializeComponent();
        ContentRoot.DataContext = viewModel;
        ContentRoot.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler(OnPointerWheelChanged), true);

        _hwnd = WindowNative.GetWindowHandle(this);
        _appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(_hwnd));
        _appWindow.Title = Core.Constants.ApplicationName;
        ConfigureWindowSize();
        _appWindow.Closing += OnAppWindowClosing;

        viewModel.ThemeChanged += ApplyTheme;
        ApplyTheme(viewModel.Theme);
    }

    public void HideWindow()
    {
        Native.NativeMethods.ShowWindow(_hwnd, ShowWindowHide);
    }

    public void ShowWindow()
    {
        Native.NativeMethods.ShowWindow(_hwnd, ShowWindowNormal);
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        var cancelEventArgs = new CancelEventArgs();
        _closingHandler(cancelEventArgs);
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

    private void ConfigureWindowSize()
    {
        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = MinWidth;
            presenter.PreferredMinimumHeight = MinHeight;
        }

        _appWindow.Resize(new SizeInt32(InitialWidth, InitialHeight));
        CenterWindow();
    }

    private void CenterWindow()
    {
        var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;

        var x = workArea.X + Math.Max(0, (workArea.Width - InitialWidth) / 2);
        var y = workArea.Y + Math.Max(0, (workArea.Height - InitialHeight) / 2);

        _appWindow.Move(new PointInt32(x, y));
    }

    private void OnRemoveScrollProfileClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ScrollProfile profile }
            && _viewModel.RemoveScrollProfileCommand.CanExecute(profile))
        {
            _viewModel.RemoveScrollProfileCommand.Execute(profile);
        }
    }

    private void OnRemoveRuleClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ApplicationRule rule }
            && _viewModel.RemoveRuleCommand.CanExecute(rule))
        {
            _viewModel.RemoveRuleCommand.Execute(rule);
        }
    }

    private void OnEasingComboBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            comboBox.ItemsSource = _viewModel.EasingOptions;
        }
    }

    private void OnDeliveryModeComboBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            comboBox.ItemsSource = _viewModel.DeliveryModeOptions;
        }
    }

    private void OnRuleProfileComboBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox)
        {
            comboBox.ItemsSource = _viewModel.ScrollProfileChoices;
        }
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
