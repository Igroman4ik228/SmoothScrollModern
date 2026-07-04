using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmoothScrollModern.Features.Profiles.ViewModels;
using SmoothScrollModern.Settings;
using SmoothScrollModern.Shared.Controls;

namespace SmoothScrollModern.Features.Profiles.Controls;

public sealed partial class UserScrollProfileItem : UserControl
{
    public static readonly DependencyProperty AreSettingsVisibleProperty = DependencyProperty.Register(
        nameof(AreSettingsVisible),
        typeof(bool),
        typeof(UserScrollProfileItem),
        new PropertyMetadata(false, OnAreSettingsVisibleChanged));

    public UserScrollProfileItem()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public bool AreSettingsVisible
    {
        get => (bool)GetValue(AreSettingsVisibleProperty);
        set => SetValue(AreSettingsVisibleProperty, value);
    }

    private static void OnAreSettingsVisibleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is UserScrollProfileItem { AreSettingsVisible: true } item)
        {
            item.EnsureProfileSettings();
        }
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (ProfileSettingsHost.Content is FrameworkElement settings)
        {
            settings.DataContext = args.NewValue;
        }
    }

    private void OnProfileExpanderExpanded(object? sender, EventArgs e)
    {
        EnsureProfileSettings();
    }

    private void EnsureProfileSettings()
    {
        if (ProfileSettingsHost.Content is UserScrollProfileSettings)
        {
            return;
        }

        var profileSettings = new UserScrollProfileSettings
        {
            DataContext = DataContext,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        if (VisualTreeDataContext.FindAncestor<ProfilesViewModel>(this) is { } viewModel)
        {
            profileSettings.EasingOptions = viewModel.EasingOptions;
        }

        ProfileSettingsHost.Content = profileSettings;
    }

    private void OnRemoveScrollProfileClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ScrollProfile profile
            && VisualTreeDataContext.FindAncestor<ProfilesViewModel>(this) is { } viewModel
            && viewModel.RemoveScrollProfileCommand.CanExecute(profile))
        {
            viewModel.RemoveScrollProfileCommand.Execute(profile);
        }
    }

    private void OnDuplicateScrollProfileClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ScrollProfile profile
            && VisualTreeDataContext.FindAncestor<ProfilesViewModel>(this) is { } viewModel
            && viewModel.DuplicateScrollProfileCommand.CanExecute(profile))
        {
            viewModel.DuplicateScrollProfileCommand.Execute(profile);
        }
    }
}
