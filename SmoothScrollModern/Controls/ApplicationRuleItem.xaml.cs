using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SmoothScrollModern.Settings;
using SmoothScrollModern.ViewModels;

namespace SmoothScrollModern.Controls;

public sealed partial class ApplicationRuleItem : UserControl
{
    public ApplicationRuleItem()
    {
        InitializeComponent();
    }

    private void OnRemoveRuleClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ApplicationRule rule)
        {
            return;
        }

        var viewModel = FindMainViewModel();
        if (viewModel?.RemoveRuleCommand.CanExecute(rule) == true)
        {
            viewModel.RemoveRuleCommand.Execute(rule);
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
