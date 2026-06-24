using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmoothScrollModern.Features.Applications.ViewModels;
using SmoothScrollModern.Shared.Controls;

namespace SmoothScrollModern.Features.Applications.Controls;

public sealed partial class ApplicationRuleSettings : UserControl
{
    public ApplicationRuleSettings()
    {
        InitializeComponent();
    }

    private void OnDeliveryModeComboBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox && VisualTreeDataContext.FindAncestor<ApplicationRulesViewModel>(this) is { } viewModel)
        {
            comboBox.ItemsSource = viewModel.DeliveryModeOptions;
        }
    }

    private void OnRuleProfileComboBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox && VisualTreeDataContext.FindAncestor<ApplicationRulesViewModel>(this) is { } viewModel)
        {
            comboBox.ItemsSource = viewModel.ScrollProfileChoices;
        }
    }
}
