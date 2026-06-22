using System.ComponentModel;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SmoothScrollModern.Scroll;
using SmoothScrollModern.Settings;
using SmoothScrollModern.ViewModels;
using WinUIEx;

namespace SmoothScrollModern;

public sealed partial class MainWindow : Window
{
    private const double InitialWindowWidth = 1120;
    private const double InitialWindowHeight = 760;
    private const double MinimumWindowWidth = 820;
    private const double MinimumWindowHeight = 560;

    private readonly Dictionary<ScrollViewer, double> _scrollTargets = [];
    private readonly Action<CancelEventArgs> _closingHandler;
    private readonly MainViewModel _viewModel;
    private readonly WindowManager _windowManager;
    private bool _isPlacementApplied;

    public MainWindow(MainViewModel viewModel, Action<CancelEventArgs> closingHandler)
    {
        _viewModel = viewModel;
        _closingHandler = closingHandler;

        InitializeComponent();
        ContentRoot.DataContext = viewModel;
        ContentRoot.AddHandler(UIElement.PointerWheelChangedEvent, new PointerEventHandler(OnPointerWheelChanged), true);

        _windowManager = WindowManager.Get(this);
        _windowManager.MinWidth = MinimumWindowWidth;
        _windowManager.MinHeight = MinimumWindowHeight;
        _windowManager.AppWindow.Closing += OnAppWindowClosing;

        viewModel.ThemeChanged += ApplyTheme;
        ApplyTheme(viewModel.Theme);
    }

    public void HideWindow()
    {
        WindowExtensions.Hide(this);
    }

    public void ShowWindow()
    {
        ApplyInitialPlacement();
        WindowExtensions.Show(this);
    }

    private void ApplyInitialPlacement()
    {
        if (_isPlacementApplied)
        {
            return;
        }

        this.CenterOnScreen(InitialWindowWidth, InitialWindowHeight);
        _isPlacementApplied = true;
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
