using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmoothScrollModern.Features.Applications.Controls;

public sealed partial class ApplicationRuleSettings : UserControl
{
    public static readonly DependencyProperty DeliveryModeOptionsProperty = DependencyProperty.Register(
        nameof(DeliveryModeOptions),
        typeof(object),
        typeof(ApplicationRuleSettings),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ScrollProfileChoicesProperty = DependencyProperty.Register(
        nameof(ScrollProfileChoices),
        typeof(object),
        typeof(ApplicationRuleSettings),
        new PropertyMetadata(null));

    public ApplicationRuleSettings()
    {
        InitializeComponent();
    }

    public object? DeliveryModeOptions
    {
        get => GetValue(DeliveryModeOptionsProperty);
        set => SetValue(DeliveryModeOptionsProperty, value);
    }

    public object? ScrollProfileChoices
    {
        get => GetValue(ScrollProfileChoicesProperty);
        set => SetValue(ScrollProfileChoicesProperty, value);
    }
}
