using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SmoothScrollModern.ViewModels;

namespace SmoothScrollModern.Controls;

public sealed partial class ApplicationRuleSettings : UserControl
{
    public ApplicationRuleSettings()
    {
        InitializeComponent();
    }

    private void OnDeliveryModeComboBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox && FindMainViewModel() is { } viewModel)
        {
            comboBox.ItemsSource = viewModel.DeliveryModeOptions;
        }
    }

    private void OnRuleProfileComboBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is ComboBox comboBox && FindMainViewModel() is { } viewModel)
        {
            comboBox.ItemsSource = viewModel.ScrollProfileChoices;
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
