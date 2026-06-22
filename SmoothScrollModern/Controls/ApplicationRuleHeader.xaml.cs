using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace SmoothScrollModern.Controls;

[ContentProperty(Name = nameof(ActionContent))]
public sealed partial class ApplicationRuleHeader : UserControl
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(ApplicationRuleHeader),
        new PropertyMetadata(string.Empty, OnTextPropertyChanged));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(ApplicationRuleHeader),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle),
        typeof(string),
        typeof(ApplicationRuleHeader),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DetailProperty = DependencyProperty.Register(
        nameof(Detail),
        typeof(string),
        typeof(ApplicationRuleHeader),
        new PropertyMetadata(string.Empty, OnTextPropertyChanged));

    public static readonly DependencyProperty ActionContentProperty = DependencyProperty.Register(
        nameof(ActionContent),
        typeof(object),
        typeof(ApplicationRuleHeader),
        new PropertyMetadata(null));

    public ApplicationRuleHeader()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string Detail
    {
        get => (string)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public object? ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }

    public Visibility HasLabel => string.IsNullOrWhiteSpace(Label) ? Visibility.Collapsed : Visibility.Visible;

    public Visibility HasDetail => string.IsNullOrWhiteSpace(Detail) ? Visibility.Collapsed : Visibility.Visible;

    private static void OnTextPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        if (dependencyObject is ApplicationRuleHeader header)
        {
            header.Bindings.Update();
        }
    }
}
