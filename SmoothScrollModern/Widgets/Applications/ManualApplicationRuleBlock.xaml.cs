using Microsoft.UI.Xaml.Controls;
using SmoothScrollModern.Features.Applications.ViewModels;
using Windows.System;

namespace SmoothScrollModern.Widgets.Applications;

public sealed partial class ManualApplicationRuleBlock : UserControl
{
    public ManualApplicationRuleBlock()
    {
        InitializeComponent();
    }

    private void OnManualProcessNameKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter
            || DataContext is not ApplicationRulesViewModel viewModel
            || !viewModel.AddManualRuleCommand.CanExecute(null))
        {
            return;
        }

        viewModel.AddManualRuleCommand.Execute(null);
        e.Handled = true;
    }
}
