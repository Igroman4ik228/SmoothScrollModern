using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmoothScrollModern.Features.Profiles.ViewModels;
using SmoothScrollModern.Settings;
using SmoothScrollModern.Widgets.Common;

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
            && VisualTreeDataContext.FindAncestor<ProfilesViewModel>(this) is { } viewModel
            && viewModel.RemoveScrollProfileCommand.CanExecute(profile))
        {
            viewModel.RemoveScrollProfileCommand.Execute(profile);
        }
    }

    private void OnEasingComboBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox && VisualTreeDataContext.FindAncestor<ProfilesViewModel>(this) is { } viewModel)
        {
            comboBox.ItemsSource = viewModel.EasingOptions;
        }
    }

}
