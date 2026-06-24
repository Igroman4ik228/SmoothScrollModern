using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmoothScrollModern.Pages;
using SmoothScrollModern.Widgets.Shell.ViewModels;

namespace SmoothScrollModern.Widgets.Shell;

public sealed partial class SettingsNavigation : UserControl
{
    private readonly ProfilesPage _profilesPage = new();
    private readonly ExceptionsPage _exceptionsPage = new();
    private readonly ApplicationSettingsPage _settingsPage = new();

    public SettingsNavigation()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) =>
        {
            RootNavigation.SelectedItem ??= ProfilesItem;
            PageHost.Content ??= _profilesPage;
        };
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem { Tag: string tag })
        {
            return;
        }

        PageHost.Content = tag switch
        {
            "Exceptions" => _exceptionsPage,
            "Settings" => _settingsPage,
            _ => _profilesPage
        };
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is MainViewModel viewModel)
        {
            _profilesPage.DataContext = viewModel.Profiles;
            _exceptionsPage.DataContext = viewModel.Applications;
            _settingsPage.DataContext = viewModel.ApplicationSettings;
            return;
        }

        _profilesPage.DataContext = args.NewValue;
        _exceptionsPage.DataContext = args.NewValue;
        _settingsPage.DataContext = args.NewValue;
    }

    public void TogglePane()
    {
        RootNavigation.IsPaneOpen = !RootNavigation.IsPaneOpen;
    }
}
