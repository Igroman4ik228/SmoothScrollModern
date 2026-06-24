using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmoothScrollModern.Features.Applications.ViewModels;
using SmoothScrollModern.Settings;
using SmoothScrollModern.Widgets.Common;

namespace SmoothScrollModern.Features.Applications.Controls;

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

        var viewModel = VisualTreeDataContext.FindAncestor<ApplicationRulesViewModel>(this);
        if (viewModel?.RemoveRuleCommand.CanExecute(rule) == true)
        {
            viewModel.RemoveRuleCommand.Execute(rule);
        }
    }
}
