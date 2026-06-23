using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SmoothScrollModern.Settings;
using SmoothScrollModern.ViewModels;

namespace SmoothScrollModern.Features.Profiles.Controls;

public sealed partial class UserScrollProfileItem : UserControl
{
    public UserScrollProfileItem()
    {
        InitializeComponent();
    }

    private void OnRemoveScrollProfileClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is ScrollProfile profile
            && FindMainViewModel() is { } viewModel
            && viewModel.RemoveScrollProfileCommand.CanExecute(profile))
        {
            viewModel.RemoveScrollProfileCommand.Execute(profile);
        }
    }

    private void OnEasingComboBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox && FindMainViewModel() is { } viewModel)
        {
            comboBox.ItemsSource = viewModel.EasingOptions;
        }
    }

    private MainViewModel? FindMainViewModel()
    {
        for (DependencyObject? current = this; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is FrameworkElement { DataContext: MainViewModel viewModel })
            {
                return viewModel;
            }
        }

        return null;
    }
}
