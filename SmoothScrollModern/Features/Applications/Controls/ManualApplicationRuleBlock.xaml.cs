using Microsoft.UI.Xaml.Controls;
using SmoothScrollModern.ViewModels;
using Windows.System;

namespace SmoothScrollModern.Features.Applications.Controls;

public sealed partial class ManualApplicationRuleBlock : UserControl
{
    public ManualApplicationRuleBlock()
    {
        InitializeComponent();
    }

    private void OnManualProcessNameKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Enter
            || DataContext is not MainViewModel viewModel
            || !viewModel.AddManualRuleCommand.CanExecute(null))
        {
            return;
        }

        viewModel.AddManualRuleCommand.Execute(null);
        e.Handled = true;
    }
}
