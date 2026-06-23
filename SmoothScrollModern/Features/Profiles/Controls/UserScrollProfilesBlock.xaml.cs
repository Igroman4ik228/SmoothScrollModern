using Microsoft.UI.Xaml.Controls;
using SmoothScrollModern.ViewModels;
using Windows.System;

namespace SmoothScrollModern.Features.Profiles.Controls;

public sealed partial class UserScrollProfilesBlock : UserControl
{
    public UserScrollProfilesBlock()
    {
        InitializeComponent();
    }

    private void OnNewScrollProfileNameKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter
            || DataContext is not MainViewModel viewModel
            || !viewModel.AddScrollProfileCommand.CanExecute(null))
        {
            return;
        }

        viewModel.AddScrollProfileCommand.Execute(null);
        e.Handled = true;
    }

    private void OnScrollProfileSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (DataContext is MainViewModel viewModel
            && !string.Equals(viewModel.ScrollProfileSearchQuery, sender.Text, StringComparison.Ordinal))
        {
            viewModel.ScrollProfileSearchQuery = sender.Text;
        }
    }
}
