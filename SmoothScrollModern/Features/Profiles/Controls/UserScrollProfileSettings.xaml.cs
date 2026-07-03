using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmoothScrollModern.Features.Profiles.Controls;

public sealed partial class UserScrollProfileSettings : UserControl
{
    public static readonly DependencyProperty EasingOptionsProperty = DependencyProperty.Register(
        nameof(EasingOptions),
        typeof(object),
        typeof(UserScrollProfileSettings),
        new PropertyMetadata(null));

    public UserScrollProfileSettings()
    {
        InitializeComponent();
    }

    public object? EasingOptions
    {
        get => GetValue(EasingOptionsProperty);
        set => SetValue(EasingOptionsProperty, value);
    }
}
