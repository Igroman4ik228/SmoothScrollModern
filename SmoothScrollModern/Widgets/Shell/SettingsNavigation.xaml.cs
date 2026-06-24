using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmoothScrollModern.Pages;
using SmoothScrollModern.Widgets.Shell.ViewModels;

namespace SmoothScrollModern.Widgets.Shell;

public sealed partial class SettingsNavigation : UserControl
{
    private ProfilesPage? _profilesPage;
    private ExceptionsPage? _exceptionsPage;
    private ApplicationSettingsPage? _settingsPage;
    private MainViewModel? _viewModel;
    private object? _fallbackDataContext;

    public SettingsNavigation()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) =>
        {
            RootNavigation.SelectedItem ??= ProfilesItem;
            NavigateTo("Profiles");
        };
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem { Tag: string tag })
        {
            return;
        }

        NavigateTo(tag);
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is MainViewModel viewModel)
        {
            _viewModel = viewModel;
            _fallbackDataContext = null;
            ApplyPageDataContexts();
            return;
        }

        _viewModel = null;
        _fallbackDataContext = args.NewValue;
        ApplyPageDataContexts();
    }

    private void NavigateTo(string tag)
    {
        PageHost.Content = tag switch
        {
            "Exceptions" => GetExceptionsPage(),
            "Settings" => GetSettingsPage(),
            _ => GetProfilesPage()
        };
    }

    private ProfilesPage GetProfilesPage()
    {
        _profilesPage ??= new ProfilesPage();
        _profilesPage.DataContext = _viewModel?.Profiles ?? _fallbackDataContext;
        return _profilesPage;
    }

    private ExceptionsPage GetExceptionsPage()
    {
        _exceptionsPage ??= new ExceptionsPage();
        _exceptionsPage.DataContext = _viewModel?.Applications ?? _fallbackDataContext;
        return _exceptionsPage;
    }

    private ApplicationSettingsPage GetSettingsPage()
    {
        _settingsPage ??= new ApplicationSettingsPage();
        _settingsPage.DataContext = _viewModel?.ApplicationSettings ?? _fallbackDataContext;
        return _settingsPage;
    }

    private void ApplyPageDataContexts()
    {
        if (_profilesPage is not null)
        {
            _profilesPage.DataContext = _viewModel?.Profiles ?? _fallbackDataContext;
        }

        if (_exceptionsPage is not null)
        {
            _exceptionsPage.DataContext = _viewModel?.Applications ?? _fallbackDataContext;
        }

        if (_settingsPage is not null)
        {
            _settingsPage.DataContext = _viewModel?.ApplicationSettings ?? _fallbackDataContext;
        }
    }

    public void TogglePane()
    {
        RootNavigation.IsPaneOpen = !RootNavigation.IsPaneOpen;
    }
}
