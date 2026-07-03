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
    private string? _currentPageTag;
    private bool _pagesPreloaded;

    public SettingsNavigation()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) =>
        {
            if (RootNavigation.SelectedItem is null)
            {
                RootNavigation.SelectedItem = ProfilesItem;
            }

            NavigateTo("Profiles");
            PreloadPages();
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
        if (string.Equals(_currentPageTag, tag, StringComparison.Ordinal))
        {
            return;
        }

        UserControl page = tag switch
        {
            "Exceptions" => GetExceptionsPage(),
            "Settings" => GetSettingsPage(),
            _ => GetProfilesPage()
        };

        PageHost.Content = page;
        _currentPageTag = tag;
    }

    private ProfilesPage GetProfilesPage()
    {
        _profilesPage ??= new ProfilesPage();
        SetPageDataContext(_profilesPage, _viewModel?.Profiles ?? _fallbackDataContext);
        return _profilesPage;
    }

    private ExceptionsPage GetExceptionsPage()
    {
        _exceptionsPage ??= new ExceptionsPage();
        SetPageDataContext(_exceptionsPage, _viewModel?.Applications ?? _fallbackDataContext);
        return _exceptionsPage;
    }

    private ApplicationSettingsPage GetSettingsPage()
    {
        _settingsPage ??= new ApplicationSettingsPage();
        SetPageDataContext(_settingsPage, _viewModel?.ApplicationSettings ?? _fallbackDataContext);
        return _settingsPage;
    }

    private void ApplyPageDataContexts()
    {
        if (_profilesPage is not null)
        {
            SetPageDataContext(_profilesPage, _viewModel?.Profiles ?? _fallbackDataContext);
        }

        if (_exceptionsPage is not null)
        {
            SetPageDataContext(_exceptionsPage, _viewModel?.Applications ?? _fallbackDataContext);
        }

        if (_settingsPage is not null)
        {
            SetPageDataContext(_settingsPage, _viewModel?.ApplicationSettings ?? _fallbackDataContext);
        }
    }

    private static void SetPageDataContext(FrameworkElement page, object? dataContext)
    {
        if (!ReferenceEquals(page.DataContext, dataContext))
        {
            page.DataContext = dataContext;
        }
    }

    private void PreloadPages()
    {
        if (_pagesPreloaded)
        {
            return;
        }

        _pagesPreloaded = true;
        DispatcherQueue.TryEnqueue(() =>
        {
            _ = GetExceptionsPage();
            _ = GetSettingsPage();
        });
    }

    public void TogglePane()
    {
        RootNavigation.IsPaneOpen = !RootNavigation.IsPaneOpen;
    }
}
