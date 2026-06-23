using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SmoothScrollModern.Controls;

public sealed partial class ApplicationRuleHeader : UserControl
{
    public static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register(
        nameof(DisplayName),
        typeof(string),
        typeof(ApplicationRuleHeader),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ProcessNameProperty = DependencyProperty.Register(
        nameof(ProcessName),
        typeof(string),
        typeof(ApplicationRuleHeader),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ExecutablePathProperty = DependencyProperty.Register(
        nameof(ExecutablePath),
        typeof(string),
        typeof(ApplicationRuleHeader),
        new PropertyMetadata(string.Empty));

    public ApplicationRuleHeader()
    {
        InitializeComponent();
    }

    public string DisplayName
    {
        get => (string)GetValue(DisplayNameProperty);
        set => SetValue(DisplayNameProperty, value);
    }

    public string ProcessName
    {
        get => (string)GetValue(ProcessNameProperty);
        set => SetValue(ProcessNameProperty, value);
    }

    public string ExecutablePath
    {
        get => (string)GetValue(ExecutablePathProperty);
        set => SetValue(ExecutablePathProperty, value);
    }
}
